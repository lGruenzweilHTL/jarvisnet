import random
import time
from model.preset import Preset
from model.request.tool import Tool
from model.request.tool_param import ToolParameter
from pipeline.wake import WakeListener
from pipeline.recorder import record_utterance
from pipeline.stt import transcribe
from pipeline.tts import speak

WAKE_MODEL = "models/wake.onnx"  # point to your openwakeword model file
OLLAMA_MODEL = "your-local-model"
VOICE = "alloy"

def main_loop():
    wake = WakeListener(["hey_jarvis_v0.1.onnx"])
    try:
        while True:
            print("Listening for wake word...")
            wake.listen()
            print("Wake word detected — recording...")
            wav = record_utterance()
            print("Recorded:", wav)

            text = transcribe(wav)
            print("User:", text)
            if not text:
                print("No speech detected.")
                continue

            resp = sample_preset.prompt(text)
            print("Assistant:", resp)

            speak(resp, voice=VOICE)
            time.sleep(0.2)
    except KeyboardInterrupt:
        print("Exiting.")
    finally:
        wake.close()

weather_tool = Tool(
    name="get_current_weather",
    description="Get the current weather for a city",
    available=True,
    func=lambda city: f"Sunny, {random.choice(range(15, 30))}°C", # Dummy implementation
    params=[
        ToolParameter(
            name="city",
            type="string",
            description="The city to get the weather for",
            required=True
        )
    ]
)

sample_preset = Preset("dummy", "llama3.2", [weather_tool], system="You are a helpful assistant.")

if __name__ == "__main__":
    main_loop()
