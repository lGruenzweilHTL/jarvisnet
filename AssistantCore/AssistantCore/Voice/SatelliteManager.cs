using System.Collections.Concurrent;
using AssistantCore.Workers;
using AssistantCore.Chat;
using Microsoft.Extensions.Logging;

namespace AssistantCore.Voice;

public class SatelliteManager
{
    private ISttWorker _stt;
    private IRoutingWorker _router;
    private ILlmWorkerFactory _llmFactory;
    private ITtsWorker _tts;
    private ChatManager _chat;
    private readonly ILogger<SatelliteManager> _logger;

    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activePipelines = new();

    public SatelliteManager(
        ISttWorker stt,
        IRoutingWorker router,
        ILlmWorkerFactory llmFactory,
        ITtsWorker tts,
        ChatManager chat,
        ILogger<SatelliteManager> logger)
    {
        _stt = stt;
        _router = router;
        _llmFactory = llmFactory;
        _tts = tts;
        _chat = chat;
        _logger = logger;
    }

    public void RegisterConnection(SatelliteConnection connection)
    {
        _logger.LogInformation("Registering connection {ConnectionId}", connection.ConnectionId);
        connection.OnSessionCompleted += session => HandleSessionCompletedAsync(connection, session);
    }

    public void UnregisterConnection(SatelliteConnection connection)
    {
        _logger.LogInformation("Unregistering connection {ConnectionId}", connection.ConnectionId);
        CancelPipeline(connection.ConnectionId);
    }

    private async Task HandleSessionCompletedAsync(SatelliteConnection connection, SatelliteSession session)
    {
        _logger.LogInformation("Handling completed session {SessionId} for connection {ConnectionId}", session.SessionId, connection.ConnectionId);
        CancelPipeline(connection.ConnectionId); // cancel any existing pipeline for this connection

        var cts = new CancellationTokenSource();
        _activePipelines[connection.ConnectionId] = cts;
        
        var orchestrator = new VoiceSessionOrchestrator(session, connection, _stt, _router, _llmFactory, _tts, _chat, _logger);

        try
        {
            await orchestrator.RunAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Pipeline cancelled for connection {ConnectionId}", connection.ConnectionId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while processing session {SessionId} for connection {ConnectionId}", session.SessionId, connection.ConnectionId);
        }
        finally
        {
            _activePipelines.TryRemove(connection.ConnectionId, out _);
            _logger.LogInformation("Pipeline finished for connection {ConnectionId}", connection.ConnectionId);
        }
    }

    public void CancelPipeline(string connectionId)
    {
        if (_activePipelines.TryRemove(connectionId, out var cts))
        {
            _logger.LogInformation("Cancelling pipeline for connection {ConnectionId}", connectionId);
            cts.Cancel();
        }
    }
}