from flask import Blueprint
from dashboard.services.conversation import start_conversation
from dashboard.services.instance_controller import master_preset

conversation_bp = Blueprint('conversation', __name__)

@conversation_bp.route('/')
def start():
    start_conversation(master_preset)