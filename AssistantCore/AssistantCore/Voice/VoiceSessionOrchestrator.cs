using AssistantCore.Voice.Dto;

namespace AssistantCore.Voice;

/// <summary>
/// Handles the processing of a voice session from audio received to response sent.
/// It exists after the audio end message is received until the response is sent back to the satellite.
/// </summary>
public class VoiceSessionOrchestrator
{
    private readonly SatelliteSession _session;
    private readonly SatelliteConnection _connection;
    // TODO: interface with worker services for STT, Router, LLM, TTS

    public VoiceSessionOrchestrator(SatelliteSession session, SatelliteConnection connection)
    {
        _session = session;
        _connection = connection;
    }

    public async Task RunAsync(CancellationToken token)
    {
        var text = await RunSttAsync(_session, token);
        var speciality = await RouteAsync(text, token);
        var response = await RunLlmAsync(speciality, text, token);
        var audioBytes = await RunTtsAsync(response, token);
        await _connection.SendTtsAsync(audioBytes, token);
    }

    private static async Task<string> RunSttAsync(SatelliteSession session, CancellationToken token)
    {
        return "";
    }

    // TODO: use an enum for specialities
    private static async Task<string> RouteAsync(string transcript, CancellationToken token)
    {
        return "";
    }

    private static async Task<string> RunLlmAsync(string speciality, string prompt, CancellationToken token)
    {
        return "";
    }

    private static async Task<byte[]> RunTtsAsync(string response, CancellationToken token)
    {
        return [];
    }
}