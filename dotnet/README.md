# dotnet — .NET Server + Python Logger

## Compatibility

**This approach works with any Wi-Fi capable microcontroller** — ESP8266, ESP32, Raspberry Pi Pico W, Arduino Nano 33 IoT, or any other device that can open a WebSocket connection. ESP8266 is used as the example throughout.

## How It Works

```
Any Wi-Fi Microcontroller (e.g. ESP8266)
  └── WebSocket ws://server:5295/
        └── ASP.NET Core Server (SensorHandler.cs)
              ├── Broadcasts to ws://server:5295/sensor
              │     └── logger.py → sensor_log.json
              │     └── Browser dashboard (wwwroot/index.html)
              └── HTTP POST → n8n webhook (optional)
```

1. The microcontroller connects to `ws://<computer-ip>:5295/` and sends sensor data.
2. `SensorHandler.cs` receives the message, validates it as JSON, and broadcasts the raw message to all clients connected to `/sensor`.
3. `logger.py` is connected to `/sensor` as a client. Every message it receives is appended to a JSON file with a timestamp.
4. The browser dashboard (served from `wwwroot/`) also connects to `/sensor` and displays data in real time.
5. Optionally, data is forwarded to an n8n webhook via HTTP POST for further automation (e.g. Google Sheets).

## Requirements

- .NET 8 SDK
- Python 3.x
- `pip install websockets`

## Run

**Terminal 1 — start the server:**
```powershell
cd dotnet
$env:N8N_WEBHOOK_URL = "http://localhost:5678/webhook/distance-data"  # optional
dotnet run
```

**Terminal 2 — start the logger:**
```powershell
cd dotnet
python logger.py                    # saves to sensor_log.json
python logger.py imu_session.json   # custom filename
```

Server runs on port `5295`. Open `http://localhost:5295` for the browser dashboard.

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

## Files

| File | Purpose |
|------|---------|
| `Program.cs` | WebSocket routing and server setup |
| `Services/SensorHandler.cs` | Receives from device, broadcasts to clients, forwards to n8n |
| `logger.py` | Python client — connects to /sensor and writes to JSON |
| `wwwroot/index.html` | Browser dashboard |
