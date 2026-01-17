namespace AssistantCore.Workers;

public record WorkerResponse<TOutput>(
    string RequestId,
    WorkerUsage Usage,
    TOutput Output,
    string? Error
);
public record WorkerUsage(string Model, int LatencyMs);