using AssistantCore.Chat;
using AssistantCore.Workers;
using Microsoft.Extensions.Logging;

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
    ChatManager chat,
    ILogger<SatelliteManager> parentLogger) // pass parent logger to correlate logs
{
    private readonly ILogger _logger = parentLogger;

    public async Task RunAsync(CancellationToken token)
    {
        try
        {
            _logger.LogInformation("Orchestrator starting for session {SessionId} on connection {ConnectionId}", session.SessionId, connection.ConnectionId);

            var text = await stt.TranscribeAsync(session.AudioBytes, token);
            _logger.LogInformation("Transcription for session {SessionId}: {Text}", session.SessionId, text);

            var speciality = await router.RouteAsync(text, token);
            _logger.LogInformation("Routed session {SessionId} to speciality {Speciality}", session.SessionId, speciality);

            var llm = llmFactory.GetWorkerBySpeciality(speciality);
            var context = chat.GetContext();
            var input = new LlmInput(SystemPromptRegistry.GetPromptBySpeciality(speciality),
                context.Events, [], text);

            _logger.LogInformation("Sending input to LLM for session {SessionId}", session.SessionId);
            var response = await llm.GetResponseAsync(input, token);
            _logger.LogInformation("LLM response for session {SessionId}: {ResponsePreview}", session.SessionId, response?.Substring(0, Math.Min(200, response.Length)));

            var audioBytes = await tts.SynthesizeAsync(response, token);
            _logger.LogInformation("Synthesized audio for session {SessionId}, {ByteCount} bytes", session.SessionId, audioBytes.Length);

            await connection.SendTtsAsync(audioBytes, token);
            _logger.LogInformation("Sent TTS for session {SessionId}", session.SessionId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Orchestrator cancelled for session {SessionId}", session.SessionId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in orchestrator for session {SessionId} on connection {ConnectionId}", session.SessionId, connection.ConnectionId);
            throw;
        }
    }
}