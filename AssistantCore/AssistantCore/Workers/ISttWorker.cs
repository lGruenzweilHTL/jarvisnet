namespace AssistantCore.Workers;

public interface ISttWorker
{
    Task<string> TranscribeAsync(byte[] audioData, CancellationToken token);
}