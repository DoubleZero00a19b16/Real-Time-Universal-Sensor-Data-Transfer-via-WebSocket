# python-only — Python WebSocket Server

## Compatibility

**This approach works with any Wi-Fi capable microcontroller** — ESP8266, ESP32, Raspberry Pi Pico W, Arduino Nano 33 IoT, or any other device that can open a WebSocket connection. ESP8266 is used as the example throughout.

## How It Works

```
Any Wi-Fi Microcontroller (e.g. ESP8266)
  └── WebSocket ws://server:5295/
        └── server.py → sensor_log.json
```

1. The microcontroller connects directly to `ws://<computer-ip>:5295/`.
2. `server.py` acts as the WebSocket server — no .NET, no middleware.
3. Every message received is validated, timestamped, and appended to a JSON file.
4. If the connection drops, the microcontroller reconnects and logging resumes automatically.

## Requirements

- Python 3.x
- `pip install websockets`

## Run

```powershell
cd python-only
python server.py                    # saves to sensor_log.json
python server.py imu_session.json   # custom filename
```

Server listens on port `5295` on all interfaces.

## Data Format

ESP8266 sends:
```json
{"json": "{\"distance\": 12.5}"}
```

Each entry saved to JSON:
```json
{
  "timestamp": "2026-04-18T10:00:00.123",
  "data": { "distance": 12.5 }
}
```

The `json` value can be either a JSON string or a JSON object — both are handled.

## Files

| File | Purpose |
|------|---------|
| `server.py` | WebSocket server — receives data from device and writes to JSON |
| `sensor_log.json` | Created automatically on first message |
