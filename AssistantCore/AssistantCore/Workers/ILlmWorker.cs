namespace AssistantCore.Workers;

public interface ILlmWorker
{
    public LlmSpeciality Speciality { get; }
    public Task<string> GetResponseAsync(string inputText, CancellationToken token);
}