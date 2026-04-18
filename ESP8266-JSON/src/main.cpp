#include <Arduino.h>
#include <ESP8266WiFi.h>
#include <WebSocketsClient.h>

const char* SSID     = "WIFI_SSID";
const char* PASSWORD = "WIFI_PASSWORD";
const char* SERVER   = "192.168.1.72";  // computer IP running the server
const int   PORT     = 5295;

WebSocketsClient ws;

void onWebSocketEvent(WStype_t type, uint8_t* payload, size_t length) {
    switch (type) {
        case WStype_CONNECTED:
            Serial.println("[WS] Connected");
            ws.sendTXT("{\"json\": {\"test\": \"Successful!\"}}");
            break;
        case WStype_DISCONNECTED:
            Serial.println("[WS] Disconnected");
            break;
        case WStype_TEXT:
            Serial.printf("[WS] Received: %s\n", payload);
            break;
        default:
            break;
    }
}

void setup() {
    Serial.begin(115200);

    WiFi.begin(SSID, PASSWORD);
    Serial.print("Connecting to WiFi");
    while (WiFi.status() != WL_CONNECTED) {
        delay(500);
        Serial.print(".");
    }
    Serial.println();
    Serial.print("[WiFi] Connected — IP: ");
    Serial.println(WiFi.localIP());

    ws.begin(SERVER, PORT, "/");
    ws.onEvent(onWebSocketEvent);
    ws.setReconnectInterval(3000);
}

void loop() {
    ws.loop();
}