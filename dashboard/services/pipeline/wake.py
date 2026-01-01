import sounddevice as sd
import openwakeword

SAMPLE_RATE = 16000
FRAME_DURATION_MS = 80
FRAME_SIZE = int(SAMPLE_RATE * FRAME_DURATION_MS / 1000)


class WakeListener:
    """
    Wraps an openwakeword engine. Replace model_path / API calls to match your local engine.
    """
    def __init__(self, model_paths: list[str], sensitivity: float = 0.5):
        self.model = openwakeword.Model(wakeword_model_paths=model_paths)
        self.sensitivity = sensitivity

    def listen(self):
        """Blocking: yields when wake word is detected."""
        with sd.InputStream(channels=1, samplerate=SAMPLE_RATE, dtype="int16", blocksize=FRAME_SIZE) as stream:
            while True:
                data, _ = stream.read(FRAME_SIZE)
                pcm = data.ravel()
                try:
                    scores = self.model.predict(pcm).values()
                    score = list(scores)[0] if scores and len(scores) > 0 else 0.0
                    detected = score >= self.sensitivity
                except Exception:
                    detected = False
                if detected:
                    return

