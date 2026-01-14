using AssistantCore.Chat;
using AssistantCore.Tools;
using Microsoft.Extensions.Logging;

namespace AssistantCore.Workers.Impl;

public class DummyLlm : ILlmWorker
{
    private readonly ToolCollector _toolCollector;
    private readonly ILogger<DummyLlm> _logger;

    public DummyLlm(ToolCollector toolCollector, ILogger<DummyLlm> logger)
    {
        _toolCollector = toolCollector;
        _logger = logger;
    }

    public LlmSpeciality Speciality => LlmSpeciality.General;

    public Task<string> GetResponseAsync(LlmInput input, CancellationToken token)
    {
        var tools = _toolCollector.GetToolsBySpeciality(Speciality);
        _logger.LogInformation("DummyLlm.GetResponseAsync invoked for speciality {Speciality}; tools available: {ToolCount}", Speciality, tools.Length);
        return Task.FromResult("Dummy response with " + tools.Length + " available tools.");
    }
}