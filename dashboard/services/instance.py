import ollama

class LLMInstance(object):
    def __init__(self, config, prefs=None):
        if prefs is None:
            prefs = {}

        self.priority = config.get('priority', 0)
        self.model = config.get('model', 'llama3.2')
        self.tools = config.get('tools', [])
        self.system_prompt = config.get('system_prompt', '')
        self.preferStream = prefs.get('stream', False)
        self.think = prefs.get('think', False)
        pass

    def generate(self, prompt):
        ollama.generate(self.model, prompt, stream=self.preferStream, system=self.system_prompt, think=self.think)

    def chat(self, messages):
        ollama.chat(self.model, messages, stream=self.preferStream, system=self.system_prompt, think=self.think)

    def __str__(self):
        return f"LLMInstance(model={self.model}, priority={self.priority})"