namespace AssistantCore.Chat;

public sealed class ChatContext
{
    public Guid ChatId { get; init; }
    public List<ChatEvent> Events { get; init; } = [];
}

public abstract record ChatEvent(DateTime Timestamp)
{
    protected ChatEvent() : this(DateTime.UtcNow) { }
}

public record UserMessage(string Text) : ChatEvent;
public record AssistantMessage(string Text) : ChatEvent;
public record ToolCall(string ToolName, string JsonArgs) : ChatEvent;
public record ToolResult(string ToolName, string JsonResult) : ChatEvent;