using AssistantCore.Workers;

namespace CoreTests.Satellite.MockWorkers;

public class FakeTtsWorker : ITtsWorker
{
    public Task<byte[]> SynthesizeAsync(string inputText, CancellationToken token)
    {
        return Task.FromResult(new byte[SatelliteProtocol.AudioFrameSize * SatelliteProtocol.TtsFrames]);
    }
}