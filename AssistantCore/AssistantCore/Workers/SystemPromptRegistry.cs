using System.Collections.Concurrent;

namespace AssistantCore.Workers;

public static class SystemPromptRegistry
{
    private static readonly ConcurrentDictionary<LlmSpeciality, string> Prompts = new();

    public static void Register(LlmSpeciality speciality, string prompt, bool overwrite = false)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt must be a non-empty string", nameof(prompt));

        if (overwrite)
        {
            Prompts.AddOrUpdate(speciality, prompt, (_, __) => prompt);
            return;
        }

        if (!Prompts.TryAdd(speciality, prompt))
            throw new InvalidOperationException($"A prompt is already registered for speciality {speciality}");
    }

    public static void RegisterAll(IDictionary<LlmSpeciality, string> entries, bool overwrite = false)
    {
        if (entries == null) return;
        foreach (var kv in entries)
        {
            Register(kv.Key, kv.Value, overwrite);
        }
    }

    public static bool TryGetPrompt(LlmSpeciality speciality, out string prompt)
        => Prompts.TryGetValue(speciality, out prompt);

    public static string GetPromptBySpeciality(LlmSpeciality speciality)
    {
        if (TryGetPrompt(speciality, out var prompt))
        {
            return prompt;
        }

        throw new ArgumentException($"No system prompt found for speciality {speciality}");
    }

    public static void Clear() => Prompts.Clear();
}