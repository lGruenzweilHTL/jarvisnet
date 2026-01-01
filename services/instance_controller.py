from services.instance import LLMInstance

instance_tools = {
    'search': {
        'name': 'web_search',
        'description': 'A tool for searching the web.',
        'function': lambda query: f"Search results for {query}"
    },
    'calculator': {
        'name': 'calculator',
        'description': 'A tool for performing mathematical calculations.',
        'function': lambda expression: eval(expression)
    },
    'start_instance': {
        'name': 'start_instance',
        'description': 'A tool for starting a new AI instance with a given preset and optional priority override.',
        'function': lambda preset, prio=None: f"Started instance with preset {preset} and priority {prio}"
    }
}
instance_presets = {
    'master': {
        'priority': 1,
        'model': 'llama3.2',
        'system_prompt': """You are the manager of multiple AI instances, capable of deploying new ones on demand
        You will be presented with a task, and your job is to dispatch the correct instance for the job.
        Use your start_instance tool for that. You will be given presets in the form of an enum, from which you will select the best fitting one.
        """,
        'tools': [instance_tools['search'], instance_tools['calculator']]
    },
    'dummy': {
        'priority': 9999,
        'model': '',
        'system_prompt': "You are a dummy instance that does nothing.",
        'tools': []
    }
}
instances = { }
curr_id = 0

def create_instance(preset_name, priority_override=None):
    preset = instance_presets.get(preset_name)
    if preset is None:
        return -1

    if priority_override is not None:
        preset['priority'] = priority_override

    instance = LLMInstance(preset)
    global curr_id
    curr_id += 1
    instances[curr_id] = instance
    return curr_id

def remove_instance(instance_id):
    if instance_id in instances:
        del instances[instance_id]

def prompt_instance(instance_id, prompt):
    instance = instances.get(instance_id)
    if instance is None:
        return None
    if instance.model == '':
        return "Dummy instance does not process prompts."
    return instance.generate(prompt)