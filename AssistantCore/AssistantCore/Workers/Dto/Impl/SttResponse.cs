namespace AssistantCore.Workers.Dto.Impl;

public record SttResponse(string RequestId, SttOutput Output, WorkerUsage Usage, string? Error) 
    : WorkerResponse<SttOutput>(RequestId, Usage, Output, Error);
public record SttOutput(string Text, float Confidence, string Language);