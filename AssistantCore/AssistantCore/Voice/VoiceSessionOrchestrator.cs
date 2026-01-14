using AssistantCore.Chat;
using AssistantCore.Workers;

namespace AssistantCore.Voice;

/// <summary>
/// Handles the processing of a voice session from audio received to response sent.
/// It exists after the audio end message is received until the response is sent back to the satellite.
/// </summary>
public class VoiceSessionOrchestrator(
    SatelliteSession session,
    SatelliteConnection connection,
    ISttWorker stt,
    IRoutingWorker router,
    ILlmWorkerFactory llmFactory,
    ITtsWorker tts,
    ChatManager chat)
{
    public async Task RunAsync(CancellationToken token)
    {
        var text = await stt.TranscribeAsync(session.AudioBytes, token);
        var speciality = await router.RouteAsync(text, token);
        var llm = llmFactory.GetWorkerBySpeciality(speciality);
        var context = chat.GetContext();
        var input = new LlmInput(SystemPromptRegistry.GetPromptBySpeciality(speciality),
            context.Events, [], text);
        var response = await llm.GetResponseAsync(input, token);
        var audioBytes = await tts.SynthesizeAsync(response, token);
        await connection.SendTtsAsync(audioBytes, token);
    }
}