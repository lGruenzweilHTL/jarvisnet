namespace AssistantCore.Workers;

public sealed class WorkerCapabilities
{
    // LLM-specific
    public IReadOnlyList<LlmSpeciality>? Specialities { get; init; }

    // Optional feature flags
    public bool SupportsStreaming { get; init; }
    public bool SupportsTools { get; init; }

    // Optional metadata
    public IReadOnlyList<string>? Models { get; init; }
}