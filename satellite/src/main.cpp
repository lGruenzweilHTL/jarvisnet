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
      .channel_format = I2S_CHANNEL_FMT_ONLY_LEFT,
      .communication_format = I2S_COMM_FORMAT_STAND_MSB,
      .intr_alloc_flags = ESP_INTR_FLAG_LEVEL1,
      .dma_buf_count = 4,
      .dma_buf_len = BUFFER_SAMPLES,
      .use_apll = false,
      .tx_desc_auto_clear = false,
      .fixed_mclk = 0};

  i2s_pin_config_t pin_config = {
      .bck_io_num = I2S_BCK,
      .ws_io_num = I2S_WS,
      .data_out_num = I2S_PIN_NO_CHANGE,
      .data_in_num = I2S_SD};

  i2s_driver_install(I2S_NUM_0, &i2s_config, 0, NULL);
  i2s_set_pin(I2S_NUM_0, &pin_config);
}

void setupI2SSpeaker()
{
  i2s_config_t i2s_config = {
      .mode = (i2s_mode_t)(I2S_MODE_MASTER | I2S_MODE_TX),
      .sample_rate = SAMPLE_RATE,
      .bits_per_sample = I2S_BITS_PER_SAMPLE_16BIT,
      .channel_format = I2S_CHANNEL_FMT_ONLY_LEFT,
      .communication_format = I2S_COMM_FORMAT_STAND_MSB,
      .intr_alloc_flags = ESP_INTR_FLAG_LEVEL1,
      .dma_buf_count = 4,
      .dma_buf_len = BUFFER_SAMPLES,
      .use_apll = false,
      .tx_desc_auto_clear = true,
      .fixed_mclk = 0};

  i2s_pin_config_t pin_config = {
      .bck_io_num = I2S_BCK,
      .ws_io_num = I2S_WS,
      .data_out_num = I2S_SD,
      .data_in_num = I2S_PIN_NO_CHANGE};
  i2s_driver_install(I2S_NUM_1, &i2s_config, 0, NULL);
  i2s_set_pin(I2S_NUM_1, &pin_config);
}

void deinitI2SSpeaker()
{
  i2s_driver_uninstall(I2S_NUM_1);
  // Return pins to GPIO mode and set to safe state (LOW with pulldown)
  pinMode(I2S_BCK, INPUT_PULLDOWN);
  pinMode(I2S_WS, INPUT_PULLDOWN);
  pinMode(I2S_SD, INPUT_PULLDOWN);
  Serial.println("I2S speaker deinitialized");
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

void playAudioChunk(const uint8_t *data, size_t length)
{
  size_t bytesWritten = 0;
  i2s_write(I2S_NUM_1, data, length, &bytesWritten, portMAX_DELAY);
  Serial.println("Played audio chunk: " + String(bytesWritten) + " bytes");
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
  setupI2SSpeaker();
  currentState = PLAYING_TTS;
  showColor(PLAYBACK_COLOR);
}
void handleTTSEnd(JsonDocument &doc)
{
  Serial.println("TTS ended");
  deinitI2SSpeaker();
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
  // TODO: Buffer audio data if needed, Play audio data immediately for now
  // playAudioChunk(data, length);
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
  pinMode(I2S_BCK, INPUT_PULLDOWN);
  pinMode(I2S_WS, INPUT_PULLDOWN);
  pinMode(I2S_SD, INPUT_PULLDOWN);

  digitalWrite(I2S_BCK, LOW);
  digitalWrite(I2S_WS, LOW);
  digitalWrite(I2S_SD, LOW);

  Serial.println("Initializing System...");
  led.begin();
  led.clear();
  led.show();

  showColor(255, 255, 255);

  Serial.println("Setting up I2S microphone...");
  setupI2SMic();
  Serial.println("I2S microphone setup complete.");
  
  // Set I2S speaker pins to safe state (will be initialized on-demand during TTS)
  pinMode(I2S_BCK, INPUT_PULLDOWN);
  pinMode(I2S_WS, INPUT_PULLDOWN);
  pinMode(I2S_SD, INPUT_PULLDOWN);
  Serial.println("I2S speaker pins initialized to safe state.");

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
    // TODO: Play TTS audio
    break;
  }
}
