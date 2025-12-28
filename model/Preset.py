from typing import Optional
from model.request.Tool import Tool
import ollama
from ollama import Message

class Preset:
    name: str
    model: str
    tools: list[Tool] = []
    stream: bool = False
    think: bool = False

    history: list[Message] = []

    def __init__(self, name: str, model: str, tools: Optional[list[Tool]] = None, system: Optional[str] = None, stream: bool = False, think: bool = False):
        self.name = name
        self.model = model
        self.tools = tools or []
        self.stream = stream
        self.think = think

        if system:
            self.history.append(Message(role="system", content=system))

    def prompt(self, prompt, images = None) -> str:
        self.history.append(Message(role="user", content=prompt, images=images))
        res = ollama.chat(
            model=self.model,
            messages=self.history,
            tools=[tool.to_schema() for tool in self.tools],
            stream=False, # TODO: streaming support
            think=self.think
        )
        self.history.append(res.message)

        if res.message.role == "tool":
            # TODO: handle tool calls
            pass

        return res.message
