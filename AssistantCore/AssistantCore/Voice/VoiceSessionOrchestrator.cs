using AssistantCore.Workers;

namespace AssistantCore.Voice;

/// <summary>
/// Handles the processing of a voice session from audio received to response sent.
/// It exists after the audio end message is received until the response is sent back to the satellite.
/// </summary>
public class VoiceSessionOrchestrator
{
    private readonly SatelliteSession _session;
    private readonly SatelliteConnection _connection;
    private readonly ISttWorker _stt;
    private readonly IRoutingWorker _router;
    private readonly ILlmWorkerFactory _llmFactory;
    private readonly ITtsWorker _tts;

    public VoiceSessionOrchestrator(
        SatelliteSession session, 
        SatelliteConnection connection,
        ISttWorker stt,
        IRoutingWorker router,
        ILlmWorkerFactory llmFactory,
        ITtsWorker tts)
    {
        _session = session;
        _connection = connection;
        _stt = stt;
        _router = router;
        _llmFactory = llmFactory;
        _tts = tts;
    }

    public async Task RunAsync(CancellationToken token)
    {
        var text = await _stt.TranscribeAsync(_session.AudioBytes, token);
        var speciality = await _router.RouteAsync(text, token);
        var llm = _llmFactory.GetWorkerBySpeciality(speciality);
        var response = await llm.GetResponseAsync(text, token);
        var audioBytes = await _tts.SynthesizeAsync(response, token);
        await _connection.SendTtsAsync(audioBytes, token);
    }
}