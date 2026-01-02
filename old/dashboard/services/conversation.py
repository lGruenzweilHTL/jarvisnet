import time

from faster_whisper import WhisperModel
from piper import PiperVoice

from old.dashboard.services.pipeline.recorder import record_utterance
from old.dashboard.services.pipeline.stt import transcribe
from old.dashboard.services.pipeline.tts import speak
from old.dashboard.services.pipeline.wake import WakeListener

wake: WakeListener | None = None
voice: PiperVoice | None = None
whisper: WhisperModel | None = None

def init_models(wake_model, voice_model, whisper_model, whisper_device):
    global wake, voice, whisper
    wake = WakeListener([wake_model])
    voice = PiperVoice.load(voice_model)
    whisper = WhisperModel(whisper_model, device=whisper_device)

def start_conversation(preset, rounds=999):
    global wake, voice, whisper
    try:
        print("Listening for wake word...")
        wake.listen()
        print("Wake word detected")
        speak("Wie kann ich behilflich sein?", voice)
        curr_rounds = 0
        while curr_rounds < rounds:
            print("Recording...")
            wav = record_utterance(silence_timeout=1.5)
            if not wav:
                print("No speech detected.")
                continue
            print("Recorded:", wav)

            text, lang_info = transcribe(wav, whisper)
            if not text:
                print("No speech detected.")
                continue
            print(f"User: {text} ({lang_info[0]}, {lang_info[1]:.2f})")

            lang_text = f"Detected language with code {lang_info[0]}. Please respond in the same language."
            resp = preset.prompt(text + "\n" + lang_text)
            print("Assistant:", resp)

            speak(resp, voice)
            time.sleep(0.2)
            curr_rounds += 1
    except KeyboardInterrupt:
        print("Exiting.")