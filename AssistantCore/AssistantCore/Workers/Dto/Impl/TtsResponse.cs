namespace AssistantCore.Workers.Dto.Impl;

public record TtsResponse(string RequestId, WorkerUsage Usage, TtsOutput Output, string? Error) 
    : WorkerResponse<TtsOutput>(RequestId, Usage, Output, Error);
public record TtsOutput(byte[] AudioData, string Encoding, int SampleRate, int Channels);