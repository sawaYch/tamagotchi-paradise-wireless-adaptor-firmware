#include <BluetoothSerial.h>
#include <NimBLEDevice.h>
#include "freertos/FreeRTOS.h"
#include "freertos/stream_buffer.h"

constexpr int PIN_MODE_SWITCH = 4;
constexpr int PIN_LED = 2;
constexpr int PIN_UART_RX = 16;
constexpr int PIN_UART_TX = 17;
constexpr uint32_t UART_BAUD = 460800;

static const char *NAME_SPP = "Tamagotchi Paradise Bridge SPP";
static const char *NAME_BLE = "Tamagotchi Paradise Bridge BLE";
static const char *NAME_BLE_ADV = "Tamagotchi Paradise Bridge";

static const char *NUS_SERVICE = "6E400001-B5A3-F393-E0A9-E50E24DCCA9E";
static const char *NUS_RX_CHAR = "6E400002-B5A3-F393-E0A9-E50E24DCCA9E";
static const char *NUS_TX_CHAR = "6E400003-B5A3-F393-E0A9-E50E24DCCA9E";

constexpr size_t STREAM_SIZE = 16384;
constexpr size_t MAX_CHUNK = 512;
constexpr size_t NOTIFY_MAX = 244;
constexpr uint32_t SWITCH_STABLE_MS = 150;

enum Mode { MODE_SPP, MODE_BLE };

Mode mode;
BluetoothSerial SerialBT;
NimBLECharacteristic *txChar = nullptr;
StreamBufferHandle_t fromHost;
StreamBufferHandle_t toHost;

volatile bool bleConnected = false;
volatile uint16_t bleMtu = 23;
volatile uint32_t dropped = 0;

uint8_t buf[MAX_CHUNK];
uint8_t pending[MAX_CHUNK];
size_t pendingLen = 0;

Mode readSwitch() {
  return digitalRead(PIN_MODE_SWITCH) == LOW ? MODE_BLE : MODE_SPP;
}

class ServerCb : public NimBLEServerCallbacks {
  void onConnect(NimBLEServer *server, NimBLEConnInfo &info) override {
    bleConnected = true;
    server->updateConnParams(info.getConnHandle(), 12, 24, 0, 400);
  }
  void onDisconnect(NimBLEServer *, NimBLEConnInfo &, int) override {
    bleConnected = false;
    bleMtu = 23;
  }
  void onMTUChange(uint16_t mtu, NimBLEConnInfo &) override { bleMtu = mtu; }
};

class RxCb : public NimBLECharacteristicCallbacks {
  void onWrite(NimBLECharacteristic *c, NimBLEConnInfo &) override {
    const NimBLEAttValue &v = c->getValue();
    size_t sent = xStreamBufferSend(fromHost, v.data(), v.length(), 0);
    if (sent < v.length()) dropped += v.length() - sent;
  }
};

ServerCb serverCb;
RxCb rxCb;

void startSpp() {
  SerialBT.onData([](const uint8_t *data, size_t len) {
    size_t sent = xStreamBufferSend(fromHost, data, len, 0);
    if (sent < len) dropped += len - sent;
  });
  SerialBT.begin(NAME_SPP, false, true);
}

void startBle() {
  NimBLEDevice::init(NAME_BLE);
  NimBLEDevice::setMTU(247);

  NimBLEServer *server = NimBLEDevice::createServer();
  server->setCallbacks(&serverCb);
  server->advertiseOnDisconnect(true);

  NimBLEService *svc = server->createService(NUS_SERVICE);
  txChar = svc->createCharacteristic(NUS_TX_CHAR, NIMBLE_PROPERTY::NOTIFY);
  NimBLECharacteristic *rxChar = svc->createCharacteristic(
    NUS_RX_CHAR, NIMBLE_PROPERTY::WRITE | NIMBLE_PROPERTY::WRITE_NR);
  rxChar->setCallbacks(&rxCb);
  svc->start();

  NimBLEAdvertising *adv = NimBLEDevice::getAdvertising();
  adv->addServiceUUID(NUS_SERVICE);
  adv->enableScanResponse(true);
  adv->setName(NAME_BLE_ADV);
  adv->start();
}

bool hostConnected() {
  return mode == MODE_SPP ? SerialBT.hasClient() : bleConnected;
}

void updateLed() {
  uint32_t t = millis();
  bool on;
  if (hostConnected()) {
    on = true;
  } else if (mode == MODE_SPP) {
    on = (t % 1000) < 500;
  } else {
    uint32_t p = t % 1200;
    on = p < 100 || (p >= 250 && p < 350);
  }
  digitalWrite(PIN_LED, on);
}

void watchSwitch() {
  static uint32_t since = 0;
  if (readSwitch() == mode) {
    since = 0;
    return;
  }
  if (since == 0) since = millis();
  if (millis() - since >= SWITCH_STABLE_MS) {
    Serial.println("Mode switch changed, restarting");
    Serial2.flush();
    delay(50);
    ESP.restart();
  }
}

size_t pumpHostToUart() {
  size_t n = xStreamBufferReceive(fromHost, buf, MAX_CHUNK, 0);
  if (n > 0) Serial2.write(buf, n);
  return n;
}

size_t pumpUartToHost() {
  int avail = Serial2.available();
  if (avail <= 0) return 0;
  int got = Serial2.readBytes(buf, min((size_t)avail, MAX_CHUNK));
  if (mode == MODE_SPP) {
    if (SerialBT.hasClient()) SerialBT.write(buf, got);
  } else if (bleConnected) {
    size_t sent = xStreamBufferSend(toHost, buf, got, 0);
    if (sent < (size_t)got) dropped += got - sent;
  }
  return got;
}

size_t pumpBleNotify() {
  if (pendingLen == 0) {
    size_t chunk = bleMtu > 23 ? bleMtu - 3 : 20;
    if (chunk > NOTIFY_MAX) chunk = NOTIFY_MAX;
    pendingLen = xStreamBufferReceive(toHost, pending, chunk, 0);
  }
  if (pendingLen == 0) return 0;
  if (!bleConnected) {
    pendingLen = 0;
    return 0;
  }
  if (txChar->notify(pending, pendingLen)) {
    size_t sent = pendingLen;
    pendingLen = 0;
    return sent;
  }
  delay(1);
  return 0;
}

void setup() {
  pinMode(PIN_LED, OUTPUT);
  pinMode(PIN_MODE_SWITCH, INPUT_PULLUP);
  Serial.begin(115200);
  delay(30);

  mode = readSwitch();

  Serial2.setRxBufferSize(8192);
  Serial2.setTxBufferSize(8192);
  Serial2.begin(UART_BAUD, SERIAL_8N1, PIN_UART_RX, PIN_UART_TX);

  fromHost = xStreamBufferCreate(STREAM_SIZE, 1);
  toHost = xStreamBufferCreate(STREAM_SIZE, 1);

  if (mode == MODE_SPP) {
    startSpp();
    Serial.printf("Mode SPP: %s\n", NAME_SPP);
  } else {
    startBle();
    Serial.printf("Mode BLE: %s\n", NAME_BLE);
  }
}

void loop() {
  size_t moved = pumpHostToUart() + pumpUartToHost();
  if (mode == MODE_BLE) moved += pumpBleNotify();

  watchSwitch();
  updateLed();

  if (moved == 0) delay(1);
}
