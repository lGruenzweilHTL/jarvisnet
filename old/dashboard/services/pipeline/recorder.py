import time
import tempfile
import wave
import numpy as np
import sounddevice as sd
import webrtcvad

SAMPLE_RATE = 16000
CHANNELS = 1
FRAME_DURATION_MS = 30
FRAME_SIZE = int(SAMPLE_RATE * FRAME_DURATION_MS / 1000)


def _write_wav(path, audio_np):
    with wave.open(path, "wb") as wf:
        wf.setnchannels(CHANNELS)
        wf.setsampwidth(2)
        wf.setframerate(SAMPLE_RATE)
        wf.writeframes(audio_np.tobytes())


def record_utterance(initial_timeout=10, silence_timeout=0.5, total_timeout=60, aggressiveness=2):
    """
    Record audio after wake until silence (webrtcvad) or timeout.
    Returns path to WAV file.
    Parameters:
    - initial_timeout: Maximum time to wait for initial speech (in seconds). Will return None if no speech is detected within this time.
    - silence_timeout: Time of silence to consider the end of speech (in seconds).
    - total_timeout: Maximum total recording time (in seconds).
    - aggressiveness: VAD aggressiveness (0-3).
    """
    vad = webrtcvad.Vad(aggressiveness)
    frames = []
    silence_start = None
    start_time = time.time()
    started = False

    with sd.InputStream(channels=CHANNELS, samplerate=SAMPLE_RATE, dtype="int16", blocksize=FRAME_SIZE) as stream:
        while True:
            data, _ = stream.read(FRAME_SIZE)
            frames.append(data.copy())
            pcm = data.tobytes()
            is_speech = vad.is_speech(pcm, SAMPLE_RATE)

            now = time.time()
            if is_speech:
                started = True
                silence_start = None
            else:
                if silence_start is None:
                    silence_start = now
                elif started and now - silence_start > silence_timeout:
                    break
                elif not started and now - start_time > initial_timeout:
                    return None # No speech detected, don't waste time transcribing silence

            if now - start_time > total_timeout:
                break

    audio = np.concatenate(frames, axis=0).ravel().astype(np.int16)
    tf = tempfile.NamedTemporaryFile(suffix=".wav", delete=False)
    _write_wav(tf.name, audio)
    return tf.name
