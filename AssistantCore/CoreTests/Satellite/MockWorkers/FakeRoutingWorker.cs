using AssistantCore.Workers;

namespace CoreTests.Satellite.MockWorkers;

public class FakeRoutingWorker : IRoutingWorker
{
    public Task<LlmSpeciality> RouteAsync(string inputText, CancellationToken token)
    {
        return Task.FromResult(LlmSpeciality.General);
    }
}