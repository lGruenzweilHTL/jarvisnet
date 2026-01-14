using AssistantCore.Chat;

namespace AssistantCore.Workers;

public interface ILlmWorker
{
    public LlmSpeciality Speciality { get; }
    public Task<string> GetResponseAsync(LlmInput input, CancellationToken token);
}