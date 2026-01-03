namespace AssistantCore.Workers.Impl;

public class LlmWorkerFactory : ILlmWorkerFactory
{
    private readonly Dictionary<LlmSpeciality, ILlmWorker> _workers;

    public LlmWorkerFactory(IEnumerable<ILlmWorker> workers)
    {
        _workers = workers.ToDictionary(
            w => w.Speciality,
            w => w);
    }
    
    public ILlmWorker GetWorkerBySpeciality(LlmSpeciality speciality)
    {
        if (!_workers.TryGetValue(speciality, out var worker))
            throw new InvalidOperationException("No LLM worker registered for speciality " + speciality);

        return worker;
    }
}