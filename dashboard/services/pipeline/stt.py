def transcribe(wav_path, model, beam_size=5):
    """
    Transcribe a wav file using faster-whisper.
    Returns concatenated text.
    """
    segments, info = model.transcribe(wav_path, beam_size=beam_size)
    text = " ".join(seg.text for seg in segments).strip()
    language_data = (info.language, info.language_probability)
    return str(text), language_data
