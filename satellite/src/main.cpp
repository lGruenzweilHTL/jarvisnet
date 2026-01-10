#include <Arduino.h>
#include <ArduinoJson.h>
#include <WiFi.h>
#include <WebSocketsClient.h>
#include <Adafruit_NeoPixel.h>
#include "driver/i2s.h"
#include "config.h"

enum State
{
  DISCONNECTED,
  CONNECTED,
  READY,
  STREAMING_AUDIO,
  WAITING_FOR_RESPONSE,
  PLAYING_TTS
};

String currentSessionId = "";
State currentState = DISCONNECTED;
int16_t audioBuffer[BUFFER_SAMPLES];
bool buttonWasPressed = false;

// Playback ring buffer for incoming TTS audio
constexpr size_t PLAYBACK_BUFFER_SIZE = 65536; // 64KB buffer
uint8_t playbackBuffer[PLAYBACK_BUFFER_SIZE];
size_t playbackWritePos = 0;
size_t playbackReadPos = 0;
size_t playbackBytesAvailable = 0;

void flushPlaybackBuffer()
{
  playbackWritePos = 0;
  playbackReadPos = 0;
  playbackBytesAvailable = 0;
}

// Audio recording state
int16_t recordBuffer[BUFFER_SAMPLES * 2]; // 2x buffer for accumulating audio before sending
size_t recordedBytes = 0;

// Send chunk data in smaller pieces to avoid memory bloat
#define SEND_CHUNK_SIZE (BUFFER_SAMPLES * sizeof(int16_t)) // Send when we have this much data

// =========================================

WebSocketsClient ws;
Adafruit_NeoPixel led(1, LEDPin, NEO_GRB + NEO_KHZ800);

void showColor(uint32_t color)
{
  led.setPixelColor(0, color);
  led.show();
}
void showColor(uint8_t r, uint8_t g, uint8_t b)
{
  led.setPixelColor(0, r, g, b);
  led.show();
}

void setupI2SMic()
{
  i2s_config_t i2s_config = {
      .mode = (i2s_mode_t)(I2S_MODE_MASTER | I2S_MODE_RX | I2S_MODE_PDM),
      .sample_rate = SAMPLE_RATE,
      .bits_per_sample = I2S_BITS_PER_SAMPLE_16BIT,
      .channel_format = I2S_CHANNEL_FMT_ALL_RIGHT,
      .communication_format = I2S_COMM_FORMAT_STAND_I2S,
      .intr_alloc_flags = ESP_INTR_FLAG_LEVEL1,
      .dma_buf_count = 6,
      .dma_buf_len = 60,
      .use_apll = false,
      .tx_desc_auto_clear = false,
      .fixed_mclk = 0};

  i2s_pin_config_t pin_config = {
      .bck_io_num = I2S_BCK,
      .ws_io_num = I2S_WS,
      .data_out_num = I2S_PIN_NO_CHANGE,
      .data_in_num = I2S_MIC_SD};

  i2s_driver_install(I2S_NUM_0, &i2s_config, 0, NULL);
  i2s_set_pin(I2S_NUM_0, &pin_config);
  i2s_set_clk(I2S_NUM_0, SAMPLE_RATE, I2S_BITS_PER_SAMPLE_16BIT, I2S_CHANNEL_MONO);
}

void setupI2SSpeaker()
{
  // Stop mic temporarily while speaker is active
  i2s_stop(I2S_NUM_0);
  
  i2s_config_t i2s_config = {
      .mode = (i2s_mode_t)(I2S_MODE_MASTER | I2S_MODE_TX),
      .sample_rate = SAMPLE_RATE,
      .bits_per_sample = I2S_BITS_PER_SAMPLE_16BIT,
      .channel_format = I2S_CHANNEL_FMT_ALL_RIGHT,
      .communication_format = I2S_COMM_FORMAT_STAND_I2S,
      .intr_alloc_flags = ESP_INTR_FLAG_LEVEL1,
      .dma_buf_count = 2,
      .dma_buf_len = 128,
      .use_apll = false,
      .tx_desc_auto_clear = true,
      .fixed_mclk = 0};

  i2s_pin_config_t pin_config = {
      .bck_io_num = I2S_BCK,
      .ws_io_num = I2S_WS,
      .data_out_num = I2S_SPK_SD,
      .data_in_num = I2S_PIN_NO_CHANGE};
      
  i2s_driver_install(I2S_NUM_1, &i2s_config, 0, NULL);
  i2s_set_pin(I2S_NUM_1, &pin_config);
  i2s_set_clk(I2S_NUM_1, SAMPLE_RATE, I2S_BITS_PER_SAMPLE_16BIT, I2S_CHANNEL_MONO);
  i2s_start(I2S_NUM_1);
  Serial.println("I2S speaker initialized");
}

void deinitI2SSpeaker()
{
  // Stop I2S output and flush DMA buffers
  i2s_zero_dma_buffer(I2S_NUM_1);
  i2s_stop(I2S_NUM_1);
  i2s_driver_uninstall(I2S_NUM_1);
  
  // Set speaker data pin to LOW (don't touch shared BCK/WS)
  pinMode(I2S_SPK_SD, OUTPUT);
  digitalWrite(I2S_SPK_SD, LOW);
  
  // Restart microphone
  i2s_start(I2S_NUM_0);
  
  Serial.println("I2S speaker deinitialized, mic restarted");
}

void createHelloMessage(char *buffer, size_t bufferSize)
{
  JsonDocument doc;
  doc["type"] = "hello";
  doc["protocol_version"] = PROTOCOL_VERSION;
  doc["satellite_id"] = DEVICE_ID;
  doc["area"] = DEVICE_AREA;
  doc["language"] = DEVICE_LANGUAGE;
  doc["capabilities"]["speaker"] = true;
  doc["capabilities"]["display"] = false;
  doc["capabilities"]["supports_barge_in"] = true;
  doc["capabilities"]["supports_streaming_tts"] = true;
  doc["audio_format"]["encoding"] = "pcm_s16le";
  doc["audio_format"]["sample_rate"] = SAMPLE_RATE;
  doc["audio_format"]["channels"] = 1;
  doc["audio_format"]["frame_ms"] = BUFFER_SAMPLES * 1000 / SAMPLE_RATE;
  serializeJson(doc, buffer, bufferSize);
}
void createSessionStartMessage(char *buffer, size_t bufferSize)
{
  JsonDocument doc;
  doc["type"] = "session.start";
  doc["timestamp"] = millis();
  doc["session_id"] = String(millis()); // TODO: Use better session ID
  serializeJson(doc, buffer, bufferSize);
}
void createAudioEndMessage(char *buffer, size_t bufferSize)
{
  JsonDocument doc;
  doc["type"] = "audio.end";
  doc["session_id"] = currentSessionId;
  doc["reason"] = "button_release";
  serializeJson(doc, buffer, bufferSize);
}

void recordAudioChunk()
{
  size_t bytesRead = 0;
  i2s_read(I2S_NUM_0, audioBuffer, sizeof(audioBuffer), &bytesRead, portMAX_DELAY);

  if (bytesRead > 0 && recordedBytes + bytesRead <= sizeof(recordBuffer))
  {
    memcpy((uint8_t *)recordBuffer + recordedBytes, audioBuffer, bytesRead);
    recordedBytes += bytesRead;

    // Send chunk when buffer reaches threshold
    if (recordedBytes >= SEND_CHUNK_SIZE)
    {
      Serial.println("Sending chunk: " + String(SEND_CHUNK_SIZE) + " bytes");
      ws.sendBIN((uint8_t *)recordBuffer, SEND_CHUNK_SIZE);

      // Shift remaining data to start of buffer
      recordedBytes -= SEND_CHUNK_SIZE;
      memmove(recordBuffer, (uint8_t *)recordBuffer + SEND_CHUNK_SIZE, recordedBytes);
    }
  }
}

void sendRemainingAudio()
{
  if (recordedBytes > 0)
  {
    Serial.println("Sending final chunk: " + String(recordedBytes) + " bytes");
    ws.sendBIN((uint8_t *)recordBuffer, recordedBytes);
    recordedBytes = 0;
  }

  showColor(IDLE_COLOR);
}

// Enqueue incoming audio data into playback ring buffer
void enqueuePlayback(const uint8_t *data, size_t length)
{
  if (length > PLAYBACK_BUFFER_SIZE)
  {
    // Too large; drop
    Serial.println("Playback buffer overflow: chunk too large");
    return;
  }

  // Check available space
  size_t freeSpace = PLAYBACK_BUFFER_SIZE - playbackBytesAvailable;
  if (length > freeSpace)
  {
    Serial.println("Playback buffer overflow: not enough space");
    return;
  }

  size_t first = min(length, PLAYBACK_BUFFER_SIZE - playbackWritePos);
  memcpy(playbackBuffer + playbackWritePos, data, first);
  size_t remaining = length - first;
  if (remaining > 0)
  {
    memcpy(playbackBuffer, data + first, remaining);
  }

  playbackWritePos = (playbackWritePos + length) % PLAYBACK_BUFFER_SIZE;
  playbackBytesAvailable += length;
}

// Drain a small chunk to the speaker to avoid blocking too long in loop
void drainPlaybackToSpeaker()
{
  if (playbackBytesAvailable == 0)
  {
    // Nothing to play
    return;
  }

  const size_t chunkSize = 1024; // bytes per drain
  size_t toWrite = min(chunkSize, playbackBytesAvailable);

  size_t first = min(toWrite, PLAYBACK_BUFFER_SIZE - playbackReadPos);
  size_t writtenTotal = 0;

  // First segment
  size_t bytesWritten = 0;
  esp_err_t res = i2s_write(I2S_NUM_1,
                            playbackBuffer + playbackReadPos,
                            first,
                            &bytesWritten,
                            portMAX_DELAY);
  writtenTotal += bytesWritten;

  // Second segment if wrapped
  if (res == ESP_OK && writtenTotal == first && toWrite > first)
  {
    size_t secondLen = toWrite - first;
    size_t bytesWritten2 = 0;
    res = i2s_write(I2S_NUM_1,
                    playbackBuffer,
                    secondLen,
                    &bytesWritten2,
                    portMAX_DELAY);
    writtenTotal += bytesWritten2;
  }

  playbackReadPos = (playbackReadPos + writtenTotal) % PLAYBACK_BUFFER_SIZE;
  playbackBytesAvailable -= writtenTotal;

  if (res != ESP_OK)
  {
    Serial.printf("I2S write error during drain: %d\n", res);
  }
}

void playAudioChunk(const uint8_t *data, size_t length)
{
  // Ensure we push the entire buffer to the speaker; loop in case of partial writes
  size_t totalWritten = 0;
  while (totalWritten < length)
  {
    size_t bytesWritten = 0;
    esp_err_t result = i2s_write(I2S_NUM_1,
                                 data + totalWritten,
                                 length - totalWritten,
                                 &bytesWritten,
                                 portMAX_DELAY);
    if (result != ESP_OK)
    {
      Serial.printf("I2S write error: %d after %u bytes\n", result, (unsigned)totalWritten);
      break;
    }
    totalWritten += bytesWritten;
    if (bytesWritten == 0)
    {
      // Avoid a tight loop if nothing was written
      delay(1);
    }
  }

  Serial.printf("Played audio chunk: %u/%u bytes\n", (unsigned)totalWritten, (unsigned)length);
}

void handleSessionAck(JsonDocument &doc)
{
  currentSessionId = doc["session_id"].as<String>();
  currentState = STREAMING_AUDIO;
  Serial.println("Session acknowledged: " + currentSessionId);
}
void handleTTSStart(JsonDocument &doc)
{
  Serial.println("TTS started");
  flushPlaybackBuffer();
  setupI2SSpeaker();
  currentState = PLAYING_TTS;
  showColor(PLAYBACK_COLOR);
}
void handleTTSEnd(JsonDocument &doc)
{
  Serial.println("TTS ended");
  // Ensure buffer is cleared before stopping speaker to avoid leftover hum
  flushPlaybackBuffer();
  deinitI2SSpeaker();
  delay(20);
  currentState = READY;
  showColor(IDLE_COLOR);
}
void handleBinaryMessage(const uint8_t *data, size_t length)
{
  if (currentState != PLAYING_TTS)
  {
    Serial.println("Received unexpected binary data");
    return;
  }
  // Buffer audio data; playback is drained in loop() to maintain order
  enqueuePlayback(data, length);
}

void onWebSocketEvent(WStype_t type, uint8_t *payload, size_t length)
{
  switch (type)
  {
  case WStype_DISCONNECTED:
    Serial.println("[WS] Disconnected");
    currentState = DISCONNECTED;
    showColor(ERROR_COLOR);
    break;

  case WStype_CONNECTED:
    Serial.printf("[WS] Connected to: %s\n", payload);
    currentState = CONNECTED;
    showColor(IDLE_COLOR);
    break;

  case WStype_TEXT:
  {
    Serial.printf("[WS] Received text (%d bytes): %s\n", length, payload);
    JsonDocument doc;
    DeserializationError error = deserializeJson(doc, payload);
    if (!error)
    {
      const char *type = doc["type"];
      if (strcmp(type, "hello.ack") == 0)
        Serial.println("[WS] Hello acknowledged by server");
      else if (strcmp(type, "session.ack") == 0)
        handleSessionAck(doc);
      else if (strcmp(type, "tts.start") == 0)
        handleTTSStart(doc);
      else if (strcmp(type, "tts.end") == 0)
        handleTTSEnd(doc);
      else
        Serial.printf("[WS] Unknown message type: %s\n", type);
    }
    break;
  }

  case WStype_BIN:
    Serial.printf("[WS] Received binary (%d bytes)\n", length);
    handleBinaryMessage(payload, length);
    break;

  case WStype_ERROR:
    Serial.printf("[WS] Error: %s\n", payload);
    break;

  case WStype_PING:
    Serial.println("[WS] Received ping");
    break;

  case WStype_PONG:
    Serial.println("[WS] Received pong");
    break;

  default:
    Serial.printf("[WS] Unknown event type: %d\n", type);
    break;
  }
}

void connectToWifi()
{
  WiFi.begin(WIFI_SSID, WIFI_PASS);
  Serial.print("Connecting WiFi");
  while (WiFi.status() != WL_CONNECTED)
  {
    delay(500);
    Serial.print(".");
  }
  Serial.println("\nWiFi connected");
}

void connectToWebSocket()
{
  Serial.printf("Connecting to WebSocket at %s:%d%s", WS_HOST, WS_PORT, WS_PATH);
  ws.begin(WS_HOST, WS_PORT, WS_PATH);
  while (!ws.isConnected())
  {
    Serial.print(".");
    ws.loop();
    delay(500);
  }
  Serial.println("\nWebSocket connected");
}

void sendHelloMessage()
{
  Serial.println("\nSending hello message...");
  char helloMsg[512];
  createHelloMessage(helloMsg, sizeof(helloMsg));
  ws.sendTXT(helloMsg);
  Serial.println("Hello message sent");
}

void startSession()
{
  char sessionStartMsg[256];
  createSessionStartMessage(sessionStartMsg, sizeof(sessionStartMsg));
  ws.sendTXT(sessionStartMsg);
  Serial.println("Session started");
}
void endSession()
{
  sendRemainingAudio();
  char audioEndMsg[256];
  createAudioEndMessage(audioEndMsg, sizeof(audioEndMsg));
  ws.sendTXT(audioEndMsg);
  Serial.println("Session ended: " + currentSessionId);
  currentSessionId = "";
  showColor(IDLE_COLOR);
}

bool buttonPressedDebounced()
{
  static bool lastReading = false;
  static uint32_t lastChangeTime = 0;
  const uint32_t debounceMs = 30;

  bool reading = (digitalRead(ButtonPin) == LOW);
  if (reading != lastReading)
  {
    lastChangeTime = millis();
    lastReading = reading;
  }

  if ((millis() - lastChangeTime) < debounceMs)
  {
    return false;
  }

  bool firstPress = reading && !buttonWasPressed;
  buttonWasPressed = reading;
  return firstPress;
}

void setup()
{
  Serial.begin(115200);

  pinMode(ButtonPin, INPUT_PULLDOWN);
  pinMode(LEDPin, OUTPUT);  

  Serial.println("Initializing System...");
  led.begin();
  led.clear();
  led.show();

  showColor(255, 255, 255);

  Serial.println("Setting up I2S microphone...");
  setupI2SMic();
  Serial.println("I2S microphone setup complete.");
  
  // Set I2S speaker data pin to safe state (BCK/WS are shared with mic, don't touch)
  pinMode(I2S_SPK_SD, OUTPUT);
  digitalWrite(I2S_SPK_SD, LOW);
  Serial.println("I2S speaker data pin initialized to safe state.");

  delay(100);
  connectToWifi();
  ws.onEvent(onWebSocketEvent);
  ws.setReconnectInterval(5000);

  showColor(IDLE_COLOR);
  Serial.println("Setup complete");
}

void loop()
{
  bool buttonPressed = buttonPressedDebounced();

  ws.loop();

  switch (currentState)
  {
  case DISCONNECTED:
    connectToWebSocket();
    currentState = CONNECTED;
    break;
  case CONNECTED:
    sendHelloMessage();
    currentState = READY;
    break;
  case READY:
    if (buttonPressed)
      startSession();
    break;
  case STREAMING_AUDIO:
    recordedBytes = 0;
    showColor(RECORDING_COLOR);
    recordAudioChunk();
    if (buttonPressed)
      endSession();
    break;
  case WAITING_FOR_RESPONSE:
    // Waiting for server response
    break;
  case PLAYING_TTS:
    drainPlaybackToSpeaker();
    break;
  }
}
