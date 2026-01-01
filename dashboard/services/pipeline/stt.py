def transcribe(wav_path, model, beam_size=5):
    """
    Transcribe a wav file using faster-whisper.
    Returns concatenated text.
    """
    segments, _ = model.transcribe(wav_path, beam_size=beam_size)
    return " ".join(seg.text for seg in segments).strip()
