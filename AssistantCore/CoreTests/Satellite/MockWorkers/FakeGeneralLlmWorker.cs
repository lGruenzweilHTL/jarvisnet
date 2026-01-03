using AssistantCore.Workers;

namespace CoreTests.Satellite.MockWorkers;

public class FakeGeneralLlmWorker : ILlmWorker
{
    public LlmSpeciality Speciality => LlmSpeciality.General;
    public Task<string> GetResponseAsync(string inputText, CancellationToken token)
    {
        return Task.FromResult("Okay, turning on the kitchen lights.");
    }
}