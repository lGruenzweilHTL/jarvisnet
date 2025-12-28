from typing import Optional
from model.request.Tool import Tool
import ollama
from ollama import GenerateResponse

class Preset:
    name: str
    model: str
    system: Optional[str] = None
    tools: list[Tool] = []
    stream: bool = False
    think: bool = False

    def __init__(self, name: str, model: str, tools: Optional[list[Tool]] = None, system: Optional[str] = None, stream: bool = False, think: bool = False):
        self.name = name
        self.model = model
        self.system = system
        self.tools = tools or []
        self.stream = stream
        self.think = think

    def prompt(self, prompt, images) -> GenerateResponse:
        return ollama.generate(
            model=self.model,
            prompt=prompt,
            #tools=[tool.to_schema() for tool in self.tools],
            images=images,
            system=self.system,
            stream=self.stream,
            think=self.think
        )
