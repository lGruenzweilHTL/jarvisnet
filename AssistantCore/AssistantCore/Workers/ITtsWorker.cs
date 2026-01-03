namespace AssistantCore.Workers;

public interface ITtsWorker
{
    public Task<byte[]> SynthesizeAsync(string inputText, CancellationToken token);
}