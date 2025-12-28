import subprocess
import simpleaudio as sa
import tempfile
import os

def speak(text, voice="alloy", out_path=None):
    """
    Call piper CLI to produce TTS and play it.
    Adjust CLI flags for your piper install.
    """
    if out_path is None:
        tf = tempfile.NamedTemporaryFile(suffix=".wav", delete=False)
        out_path = tf.name
        tf.close()

    cmd = ["piper", "tts", "--voice", voice, "--text", text, "--out", out_path]
    subprocess.run(cmd, check=True)
    wave_obj = sa.WaveObject.from_wave_file(out_path)
    play_obj = wave_obj.play()
    play_obj.wait_done()
    try:
        os.remove(out_path)
    except Exception:
        pass
