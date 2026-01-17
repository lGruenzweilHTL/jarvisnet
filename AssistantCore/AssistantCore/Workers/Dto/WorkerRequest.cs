namespace AssistantCore.Workers;

public record WorkerRequest<TInput, TConfig, TContext>(
    string RequestId,
    TInput Input,
    TConfig Config,
    TContext Context
);