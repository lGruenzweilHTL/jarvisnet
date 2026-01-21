import os
import time
import requests

CORE_URL = os.getenv("CORE_URL", "http://core")
PORT = os.getenv("PORT", "8000")
worker_id = None

def register_with_core(worker_type, speciality=None):
    payload = {
        "type": worker_type,
        "speciality": speciality,
        "endpoint": "http://localhost:" + PORT
    }

    print(f"Registering worker with core at {CORE_URL}...")
    while True:
        try:
            res = requests.post(f"{CORE_URL}/worker/register", json=payload)
            res.raise_for_status()

            data = res.json()
            if not data.get("accepted", False):
                raise Exception("Worker registration was not accepted")

            global worker_id
            worker_id = data["worker_id"]
            print(f"Registered worker with ID: {worker_id}")
            break
        except Exception:
            time.sleep(2)

def start_heartbeat():
    while True:
        try:
            requests.post(f"{CORE_URL}/worker/heartbeat", json={
                "worker_id": worker_id
            })
        except Exception:
            pass
        time.sleep(10)
