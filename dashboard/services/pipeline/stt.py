from faster_whisper import WhisperModel

# Create model once
MODEL_NAME = "small"  # change to desired model
WHISPER_DEVICE = "cpu"  # or "cuda"
_whisper = WhisperModel(MODEL_NAME, device=WHISPER_DEVICE)


def transcribe(wav_path, beam_size=5):
    """
    Transcribe a wav file using faster-whisper.
    Returns concatenated text.
    """
    segments, _ = _whisper.transcribe(wav_path, beam_size=beam_size)
    return " ".join(seg.text for seg in segments).strip()
