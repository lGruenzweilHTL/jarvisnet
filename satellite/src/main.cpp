#include <Arduino.h>
#include <WiFi.h>
#include <WebSocketsClient.h>
#include <Adafruit_NeoPixel.h>
#include "driver/i2s.h"
#include "config.h"

int16_t audioBuffer[BUFFER_SAMPLES];

// =========================================

WebSocketsClient ws;
Adafruit_NeoPixel led(1, LEDPin, NEO_GRB + NEO_KHZ800);

void showColor(uint8_t r, uint8_t g, uint8_t b)
{
  led.setPixelColor(0, led.Color(r, g, b));
  led.show();
}

void setupI2SMic()
{
  i2s_config_t i2s_config = {
      .mode = (i2s_mode_t)(I2S_MODE_MASTER | I2S_MODE_RX | I2S_MODE_PDM),
      .sample_rate = SAMPLE_RATE,
      .bits_per_sample = I2S_BITS_PER_SAMPLE_16BIT,
      .channel_format = I2S_CHANNEL_FMT_ONLY_LEFT,
      .communication_format = I2S_COMM_FORMAT_I2S_MSB,
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

void onWebSocketEvent(WStype_t type, uint8_t *payload, size_t length)
{
  if (type == WStype_CONNECTED)
  {
    Serial.println("WebSocket connected");
  }
}

void setup()
{
  Serial.begin(115200);
  led.begin();
  led.clear();
  led.show();

  showColor(0, 0, 255);

  WiFi.begin(WIFI_SSID, WIFI_PASS);
  Serial.print("Connecting WiFi");
  while (WiFi.status() != WL_CONNECTED)
  {
    delay(500);
    Serial.print(".");
  }
  Serial.println("\nWiFi connected");

  Serial.println("Connecting to WebSocket at " + String(WS_HOST) + ":" + String(WS_PORT) + WS_PATH);
  ws.begin(WS_HOST, WS_PORT, WS_PATH);
  ws.onEvent(onWebSocketEvent);
  ws.setReconnectInterval(5000);

  Serial.println("Setting up I2S microphone...");
  setupI2SMic();

  showColor(0, 255, 0);
  Serial.println("Setup complete");
}

void loop()
{
  ws.loop();

  // TODO: get web socket to connect properly (change host to valid server)
  Serial.println("WebSocket connected: " + String(ws.isConnected()));
  if (!ws.isConnected())
    return;

  if (digitalRead(ButtonPin) == HIGH)
    return; // Don't send audio if button not pressed (active low)

  Serial.println("Reading audio samples...");

  size_t bytesRead = 0;
  i2s_read(I2S_NUM_0, audioBuffer, sizeof(audioBuffer), &bytesRead, portMAX_DELAY);

  if (bytesRead > 0)
  {
    Serial.println("Sending " + String(bytesRead) + " bytes of audio data");
    ws.sendBIN((uint8_t *)audioBuffer, bytesRead);
  }
}
