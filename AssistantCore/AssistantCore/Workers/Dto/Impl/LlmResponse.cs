namespace AssistantCore.Workers.Dto.Impl;

public record LlmResponse(string RequestId, WorkerUsage Usage, LlmOutput Output, string? Error) 
    : WorkerResponse<LlmOutput>(RequestId, Usage, Output, Error);
public record LlmOutput(string Text);