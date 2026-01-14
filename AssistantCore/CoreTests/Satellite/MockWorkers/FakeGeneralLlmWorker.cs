using AssistantCore.Workers;
using AssistantCore.Chat;

namespace CoreTests.Satellite.MockWorkers;

public class FakeGeneralLlmWorker : ILlmWorker
{
    public LlmSpeciality Speciality => LlmSpeciality.General;
    public Task<string> GetResponseAsync(LlmInput input, CancellationToken token)
    {
        return Task.FromResult("Okay, turning on the kitchen lights.");
    }
}