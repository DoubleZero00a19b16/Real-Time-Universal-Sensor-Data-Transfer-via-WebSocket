import asyncio
import json
import os
import sys
from datetime import datetime
import websockets

HOST = "0.0.0.0"
PORT = 5295
JSON_FILE = sys.argv[1] if len(sys.argv) > 1 else "sensor_log.json"


def load_log():
    if os.path.exists(JSON_FILE):
        with open(JSON_FILE, "r") as f:
            return json.load(f)
    return []


log = load_log()


async def handle(websocket):
    print(f"[Device] Connected from {websocket.remote_address}")
    try:
        async for message in websocket:
            try:
                parsed = json.loads(message)

                if "json" not in parsed:
                    print(f"[Warning] No 'json' key found in message: {message}")
                    continue

                data = parsed["json"]
                if isinstance(data, str):
                    data = json.loads(data)

                entry = {
                    "timestamp": datetime.now().isoformat(),
                    "data": data
                }

                log.append(entry)
                with open(JSON_FILE, "w") as f:
                    json.dump(log, f, indent=2)

                print(f"[{entry['timestamp']}] {data}")

            except Exception as e:
                print(f"[Error] {e} — raw: {message}")

    except websockets.exceptions.ConnectionClosed:
        print("[Device] Disconnected")


async def main():
    print(f"WebSocket server listening on ws://{HOST}:{PORT}")
    print(f"Logging to: {JSON_FILE}")
    async with websockets.serve(handle, HOST, PORT):
        await asyncio.Future()


if __name__ == "__main__":
    asyncio.run(main())
