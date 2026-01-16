import base64
import time
from os.path import basename
from fastapi import FastAPI
from piper import PiperVoice

MODEL_PATH = "models/en_US-lessac-low.onnx"
voice = PiperVoice.load(MODEL_PATH)

app = FastAPI()

@app.post("/infer")
async def infer(data: dict):
    start = time.perf_counter()

    req_id = data["request_id"]
    text = data["input"]["text"]
    audio, info = synthesize(text)

    end = time.perf_counter()
    latency_ms = int((end - start) * 1000)
    result = {
        "request_id": req_id,
        "output": {
            "data_base64": audio.decode("ascii"),
            "sample_rate": info.sample_rate,
            "channels": info.num_speakers,
            "encoding": "pcm_s16le"
        },
        "usage": {
            "latency_ms": latency_ms,
            "model": basename(MODEL_PATH),
            "version": info.piper_version,
        },
        "error": None
    }
    return result


def synthesize(text: str):
    global voice
    audio = voice.synthesize(text)
    raw_bytes = b"".join([chunk.audio_int16_bytes for chunk in audio])
    return base64.b64encode(raw_bytes), voice.config
