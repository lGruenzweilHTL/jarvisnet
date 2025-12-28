import random
import time
from piper import PiperVoice
from model.preset import Preset
from model.request.tool import Tool
from model.request.tool_param import ToolParameter
from pipeline.tts import speak
from pipeline.wake import WakeListener
from pipeline.recorder import record_utterance
from pipeline.stt import transcribe

WAKE_MODEL = "models/hey_jarvis_v0.1.onnx"
VOICE = "models/de_DE-karlsson-low.onnx"

def start_conversation():
    wake = WakeListener([WAKE_MODEL])
    voice = PiperVoice.load(VOICE)
    try:
        print("Listening for wake word...")
        wake.listen()
        print("Wake word detected")
        speak("Wie kann ich behilflich sein?", voice)
        while True:
            print("Recording...")
            wav = record_utterance(silence_timeout=2.0, timeout=15)
            print("Recorded:", wav)

            text = transcribe(wav)
            if not text:
                print("No speech detected.")
                continue
            print("User:", text)

            resp = sample_preset.prompt(text)
            print("Assistant:", resp)

            speak(resp, voice)
            time.sleep(0.2)
    except KeyboardInterrupt:
        print("Exiting.")

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

sample_preset = Preset("dummy", "llama3.2", [weather_tool], system="You are a helpful assistant that speaks german. Respond only in german.")

if __name__ == "__main__":
    start_conversation()
