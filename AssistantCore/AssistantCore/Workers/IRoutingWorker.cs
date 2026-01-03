namespace AssistantCore.Workers;

public interface IRoutingWorker
{
    public Task<LlmSpeciality> RouteAsync(string inputText, CancellationToken token);
}