from typing import List, Optional, Dict, Any

class ToolParameter:
    """Represents a single parameter for a tool/function.

    Fields:
    - name: parameter name
    - type: JSON Schema type (e.g., 'string', 'number')
    - description: optional description
    - enum: optional list of allowed values
    - required: whether this parameter is required
    """
    name: str
    type: str = "string"
    description: Optional[str] = None
    enum: Optional[List[str]] = None
    required: bool = False

    def __init__(self, name: str, type: str = "string", description: Optional[str] = None, enum: Optional[List[Any]] = None, required: bool = False):
        self.name = name
        self.type = type or "string"
        self.description = description
        self.enum = enum
        self.required = required

    def to_schema(self) -> Dict[str, Any]:
        """Convert the ToolParameter instance to a JSON Schema property dict.

        Example:
        {
            "type": "string",
            "description": "The name of the city",
        }
        """
        prop: Dict[str, Any] = {"type": self.type}
        if self.description:
            prop["description"] = self.description
        if self.enum is not None:
            prop["enum"] = self.enum
        return prop
