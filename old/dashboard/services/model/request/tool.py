from typing import List, Optional, Dict, Any
from old.dashboard.services.model.request.tool_param import ToolParameter

class Tool:
    name: str
    description: Optional[str] = None
    params: List[ToolParameter] = []
    available: bool = True
    func = None

    def __init__(self, name: str, description: Optional[str] = None, params: Optional[List[ToolParameter]] = None, available: bool = True, func = None):
        self.name = name
        self.description = description
        self.params = params or []
        self.available = available
        self.func = func

    def to_schema(self) -> Dict[str, Any]:
        """Convert the Tool instance to the requested function schema.

        Returns shape:
        {
          'type': 'function',
          'function': {
            'name': 'get_current_weather',
            'description': 'Get the current weather for a city',
            'parameters': {
              'type': 'object',
              'properties': { ... },
              'required': [ ... ]
            }
          }
        }
        """
        properties: Dict[str, Any] = {}
        required: List[str] = []

        for p in self.params:
            properties[p.name] = p.to_schema()
            if getattr(p, "required", False):
                required.append(p.name)

        function_obj: Dict[str, Any] = {
            "name": self.name,
            "description": self.description or "",
            "parameters": {
                "type": "object",
                "properties": properties,
            },
        }

        if required:
            function_obj["parameters"]["required"] = required

        return {"type": "function", "function": function_obj}
