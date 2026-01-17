namespace AssistantCore.Workers.Dto.Impl;

public record RoutingResponse(string RequestId, WorkerUsage Usage, RoutingOutput Output, string? Error) 
    : WorkerResponse<RoutingOutput>(RequestId, Usage, Output, Error);
public record RoutingOutput(string Speciality, float Confidence, string Reason);