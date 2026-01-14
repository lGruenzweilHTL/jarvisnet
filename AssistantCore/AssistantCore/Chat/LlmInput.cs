using AssistantCore.Tools;
using AssistantCore.Tools.Dto;

namespace AssistantCore.Chat;

public record LlmInput(
    string SystemPrompt,
    IReadOnlyList<ChatEvent> Context,
    IReadOnlyList<ToolDefinition> Tools,
    string UserMessage
);
