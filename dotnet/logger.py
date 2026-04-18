import asyncio
import json
import os
import sys
from datetime import datetime
import websockets

WS_URL = "ws://localhost:5295/sensor"
JSON_FILE = sys.argv[1] if len(sys.argv) > 1 else "sensor_log.json"


def load_log():
    if os.path.exists(JSON_FILE):
        with open(JSON_FILE, "r") as f:
            return json.load(f)
    return []


log = load_log()


async def listen():
    print(f"Connecting to {WS_URL} ...")

    while True:
        try:
            async with websockets.connect(WS_URL) as ws:
                print(f"Connected. Logging to {JSON_FILE} ...")
                async for message in ws:
                    try:
                        parsed = json.loads(message)

                        if "json" in parsed:
                            data = parsed["json"]
                            if isinstance(data, str):
                                data = json.loads(data)
                        else:
                            data = parsed

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

        except (websockets.exceptions.ConnectionClosed, OSError) as e:
            print(f"Disconnected: {e} — retrying in 3 seconds...")
            await asyncio.sleep(3)


if __name__ == "__main__":
    asyncio.run(listen())
