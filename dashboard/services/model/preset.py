from typing import Optional
from dashboard.services.model.request.tool import Tool
import ollama
from ollama import Message, ChatResponse


class Preset:
    name: str
    model: str
    tools: list[Tool] = []
    system: Optional[str] = None
    stream: bool = False
    think: bool = False

    history: list[Message] = []

    def __init__(self, name: str, model: str, tools: Optional[list[Tool]] = None, system: Optional[str] = None, stream: bool = False, think: bool = False):
        self.name = name
        self.model = model
        self.tools = tools or []
        self.system = system
        self.stream = stream
        self.think = think

        if system:
            self.history.append(Message(role="system", content=system))

    def prompt(self, prompt, images = None) -> str:
        self.history.append(Message(role="user", content=prompt, images=images))
        res = self.__run_agent()
        return res.message.content

    def __run_agent(self) -> ChatResponse:
        res = ollama.chat(
            model=self.model,
            messages=self.history,
            tools=[tool.to_schema() for tool in self.tools if tool.available],
            stream=False,  # TODO: streaming support
            think=self.think
        )
        self.history.append(res.message)

        if res.message.tool_calls:
            for call in res.message.tool_calls:
                self.__handle_tool(call.function.name, call.function.arguments)
            return self.__run_agent()
        return res

    def __handle_tool(self, name, args):
        tool = next((t for t in self.tools if t.name == name and t.available), None)
        if tool and tool.func:
            result = tool.func(**args)
            self.history.append(Message(role="tool", tool_name=tool.name, content=result))
        else:
            self.history.append(Message(role="tool", tool_name=name, content=f"Tool {name} not found or unavailable."))

    def __str__(self):
        return f"Preset(name={self.name}, model={self.model}, tools={[tool.name for tool in self.tools]}, system={self.system}, stream={self.stream}, think={self.think})"
