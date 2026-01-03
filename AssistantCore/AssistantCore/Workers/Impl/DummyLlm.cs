namespace AssistantCore.Workers.Impl;

public class DummyLlm : ILlmWorker
{
    public LlmSpeciality Speciality => LlmSpeciality.General;

    public Task<string> GetResponseAsync(string inputText, CancellationToken token)
    {
        return Task.FromResult("Dummy response");
    }
}