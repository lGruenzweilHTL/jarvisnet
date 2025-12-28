import numpy as np
import sounddevice as sd
from piper import PiperVoice


def speak(text: str, voice: PiperVoice):
    """
    Synthesize speech from text using the specified voice model.

    Args:
        text (str): The text to be synthesized.
        voice (PiperVoice): The PiperVoice model to use for synthesis.
    """
    stream = sd.OutputStream(samplerate=voice.config.sample_rate, channels=1, dtype='int16')
    stream.start()
    for chunk in voice.synthesize(text):
        audio_bytes = chunk.audio_int16_bytes
        int_data = np.frombuffer(audio_bytes, dtype=np.int16)
        stream.write(int_data)

    stream.stop()
    stream.close()
