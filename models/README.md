# Models

This is where you place your models for Jarvisnet. Make sure to download the necessary models for wake word detection and text-to-speech synthesis and place them in this directory.

Get the required models from the following sources:
- **OpenWakeWord Models**: [OpenWakeWord GitHub releases](https://github.com/dscripka/openWakeWord/releases)
- **Piper TTS Models**: [Hugging Face Repository](https://huggingface.co/rhasspy/piper-voices/tree/main)

Ensure that the models are correctly named and formatted as expected by the application to ensure smooth operation.

> [!IMPORTANT]
> Piper models require the .onnx and the .onnx.json files in the same directory to work properly. Be sure to download both files for each model you intend to use.