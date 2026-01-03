namespace AssistantCore.Workers.Impl;

public class DummyStt : ISttWorker
{
    public Task<string> TranscribeAsync(byte[] audioData, CancellationToken token)
    {
        return Task.FromResult("Hello world");
    }
}