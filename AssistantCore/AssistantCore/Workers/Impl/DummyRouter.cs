namespace AssistantCore.Workers.Impl;

public class DummyRouter : IRoutingWorker
{
    public Task<LlmSpeciality> RouteAsync(string inputText, CancellationToken token)
    {
        return Task.FromResult(LlmSpeciality.General);
    }
}