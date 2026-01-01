import random
from pathlib import Path

from flask import Flask

from dashboard.services.conversation import init_models

from dashboard.routes.dashboard_routes import dashboard_bp
from dashboard.routes.instance_routes import instance_bp

WAKE_MODEL = "models/hey_jarvis_v0.1.onnx"
VOICE = "models/de_DE-karlsson-low.onnx"
WHISPER_MODEL = "small"
WHISPER_DEVICE = "cpu"

if __name__ == "__main__":
    init_models(WAKE_MODEL, VOICE, WHISPER_MODEL, WHISPER_DEVICE)

    base = Path(__file__).resolve().parent
    app = Flask(__name__, root_path=str(base / "dashboard"))
    app.register_blueprint(dashboard_bp)
    app.register_blueprint(instance_bp, url_prefix='/instance')
    app.run(host='0.0.0.0', debug=True)
