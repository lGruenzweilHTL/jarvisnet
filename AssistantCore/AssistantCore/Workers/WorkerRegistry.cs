using System.Collections.Concurrent;

namespace AssistantCore.Workers;

public sealed class WorkerRegistry
{
    private readonly ConcurrentDictionary<string, WorkerDescriptor> _workers = new();

    public void Register(WorkerDescriptor worker)
    {
        worker.LastSeenUtc = DateTime.UtcNow;
        _workers[worker.WorkerId] = worker;
    }

    public void Heartbeat(string workerId)
    {
        if (_workers.TryGetValue(workerId, out var worker))
        {
            worker.LastSeenUtc = DateTime.UtcNow;
        }
    }

    public IReadOnlyList<WorkerDescriptor> GetWorkers(
        WorkerType type,
        LlmSpeciality? speciality = null)
    {
        var now = DateTime.UtcNow;

        return _workers.Values
            .Where(w => w.Type == type)
            .Where(w => (now - w.LastSeenUtc) < TimeSpan.FromSeconds(30))
            .Where(w =>
                speciality == null ||
                w.Capabilities.Specialities?.Contains(speciality.Value) == true)
            .ToList();
    }
}