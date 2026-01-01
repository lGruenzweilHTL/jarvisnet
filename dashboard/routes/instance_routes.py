from flask import Blueprint, abort
from dashboard.services import instance_controller

instance_bp = Blueprint('instance', __name__)

@instance_bp.route('/status/<int:instance_id>')
def get_instance(instance_id):
    data = instance_controller.instances.get(instance_id)
    if data is None:
        abort(404)
    return str(data)

@instance_bp.route('/create/<preset_name>')
def create_instance_route(preset_name):
    pid = instance_controller.create_instance(preset_name)
    if pid == -1:
        abort(404, description="Preset not found")
    return f"Instance created with preset {preset_name} and ID {pid}"

@instance_bp.route('/list')
def list_instances():
    return list(f"{k}: {v.model} ({v.priority})" for k, v in instance_controller.instances.items())

@instance_bp.route('/delete/<int:instance_id>')
def delete_instance(instance_id):
    return instance_controller.remove_instance(instance_id)

@instance_bp.route('/presets')
def list_presets():
    return list(instance_controller.instance_presets.keys())

@instance_bp.route('/prompt/<int:instance_id>/<prompt>')
def prompt_instance(instance_id, prompt):
    instance = instance_controller.instances.get(instance_id)
    if instance is None:
        abort(404)
    return instance.generate(prompt)
