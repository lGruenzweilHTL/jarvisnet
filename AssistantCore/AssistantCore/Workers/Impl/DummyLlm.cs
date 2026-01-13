using AssistantCore.Tools;

namespace AssistantCore.Workers.Impl;

public class DummyLlm(ToolCollector toolCollector) : ILlmWorker
{
    public LlmSpeciality Speciality => LlmSpeciality.General;

    public Task<string> GetResponseAsync(string inputText, CancellationToken token)
    {
        var tools = toolCollector.GetToolsBySpeciality(Speciality);
        return Task.FromResult("Dummy response with " + tools.Length + " available tools.");
    }
}