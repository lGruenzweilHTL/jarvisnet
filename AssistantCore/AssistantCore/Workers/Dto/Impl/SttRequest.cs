namespace AssistantCore.Workers.Dto.Impl;

public record SttRequest(string RequestId, SttInput Input, SttConfig Config, SttContext Context) 
    : WorkerRequest<SttInput, SttConfig, SttContext>(RequestId, Input, Config, Context);
public record SttInput(byte[] AudioData, string Encoding, int SampleRate, int Channels);
public record SttConfig();
public record SttContext(string Location);