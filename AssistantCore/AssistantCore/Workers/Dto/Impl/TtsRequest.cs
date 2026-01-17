namespace AssistantCore.Workers.Dto.Impl;

public record TtsRequest(string RequestId, TtsInput Input, TtsConfig Config, TtsContext Context) 
    : WorkerRequest<TtsInput, TtsConfig, TtsContext>(RequestId, Input, Config, Context);
public record TtsInput(string Text);
public record TtsConfig(string Voice, float Speed);
public record TtsContext();