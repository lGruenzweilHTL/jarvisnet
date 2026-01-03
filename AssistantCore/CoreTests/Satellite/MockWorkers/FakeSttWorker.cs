using AssistantCore.Workers;

namespace CoreTests.Satellite.MockWorkers;

public class FakeSttWorker : ISttWorker
{
    public Task<string> TranscribeAsync(byte[] audioData, CancellationToken token)
    {
        return Task.FromResult("Turn on the kitchen lights.");
    }
}