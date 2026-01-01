from flask import Blueprint, abort
from dashboard.services import instance_controller

instance_bp = Blueprint('instance', __name__)

@instance_bp.route('/status/<int:instance_id>')
def get_instance(instance_id):
    data = instance_controller.get_instance(instance_id)
    if data is None:
        abort(404, description=f"Instance with ID {instance_id} not found")
    return str(data)

@instance_bp.route('/create/<preset_name>')
def create_instance_route(preset_name):
    iid = instance_controller.create_instance(preset_name)
    if iid == -1:
        abort(404, description=f"Preset {preset_name} not found")
    return f"Instance created with preset {preset_name} and ID {iid}"

@instance_bp.route('/list')
def list_instances():
    instances = instance_controller.instances
    return {iid: str(inst) for iid, inst in instances.items()}

@instance_bp.route('/delete/<int:instance_id>')
def delete_instance(instance_id):
    return instance_controller.remove_instance(instance_id)

@instance_bp.route('/presets')
def list_presets():
    return [preset.name for preset in instance_controller.instance_presets]

@instance_bp.route('/prompt/<int:instance_id>/<prompt>')
def prompt_instance(instance_id, prompt):
    instance = instance_controller.get_instance(instance_id)
    if instance is None:
        abort(404, description=f"Instance with ID {instance_id} not found")
    return instance.prompt(prompt)
