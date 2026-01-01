from dashboard.services.model.preset import Preset
from dashboard.services.model.request.tool import Tool
from dashboard.services.model.request.tool_param import ToolParameter

instance_tools = [
    Tool('search_web', 'Search the web for information', [
        ToolParameter('query', 'string', 'The search query', required=True),
    ], func=lambda query: f"Results for {query}", available=True),
]
instance_presets = [
    Preset('master', 'llama3.2', instance_tools, '', False, False)
]
instances: dict[int, Preset] = { }
curr_id = 0

def create_instance(preset_name):
    preset = next((p for p in instance_presets if p.name == preset_name), None)
    if preset is None:
        return -1

    global curr_id
    curr_id += 1
    # Create a copy of the preset for the new instance
    instances[curr_id] = Preset(
        name=preset.name,
        model=preset.model,
        tools=preset.tools,
        system=preset.system,
        stream=preset.stream,
        think=preset.think
    )
    return curr_id

def remove_instance(instance_id):
    if instance_id in instances:
        del instances[instance_id]

def get_instance(instance_id):
    return instances.get(instance_id, None)