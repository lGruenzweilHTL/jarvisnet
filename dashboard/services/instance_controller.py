from dashboard.repository.sqlite.db import load_all_shared_data, test_connection
from dashboard.services.model.preset import Preset
from dashboard.services.model.request.tool import Tool
from dashboard.services.model.request.tool_param import ToolParameter

instance_tools = [
    Tool('search_web', 'Search the web for information', [
        ToolParameter('query', 'string', 'The search query', required=True),
    ], func=lambda query: f"Results for {query}", available=True),
    Tool('show_storage', 'Use this tool to query the cross-instance shared storage. This storage may contain important information from other instances.',
         [], func=load_all_shared_data, available=test_connection()),
    Tool('start_instance_with_prompt', 'Start a new instance with a given preset and an optional initial prompt', [
         ToolParameter('preset_name', 'string', 'The preset name', required=True, enum=[i.name for i in []]) # TODO: turn into class and actually load presets
    ], func=lambda preset_name: create_instance(preset_name), available=True),
]
master_preset = Preset('master', 'llama3.2', [],
                       'You are a conversational AI chat bot meant to fulfill user request and hold conversations',
                       False, False)
instance_presets = [
    master_preset
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