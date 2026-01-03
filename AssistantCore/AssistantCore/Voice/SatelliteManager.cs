using System.Collections.Concurrent;
using AssistantCore.Workers;

namespace AssistantCore.Voice;

public class SatelliteManager
{
    private ISttWorker _stt;
    private IRoutingWorker _router;
    private ILlmWorkerFactory _llmFactory;
    private ITtsWorker _tts;

    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activePipelines = new();

    public SatelliteManager(
        ISttWorker stt,
        IRoutingWorker router,
        ILlmWorkerFactory llmFactory,
        ITtsWorker tts)
    {
        _stt = stt;
        _router = router;
        _llmFactory = llmFactory;
        _tts = tts;
    }

    public void RegisterConnection(SatelliteConnection connection)
    {
        connection.OnSessionCompleted += session => HandleSessionCompletedAsync(connection, session);
    }

    public void UnregisterConnection(SatelliteConnection connection)
    {
        CancelPipeline(connection.ConnectionId);
    }

    private async Task HandleSessionCompletedAsync(SatelliteConnection connection, SatelliteSession session)
    {
        CancelPipeline(connection.ConnectionId); // cancel any existing pipeline for this connection

        var cts = new CancellationTokenSource();
        _activePipelines[connection.ConnectionId] = cts;
        
        var orchestrator = new VoiceSessionOrchestrator(session, connection, _stt, _router, _llmFactory, _tts);

        try
        {
            await orchestrator.RunAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            // TODO: log
        }
        finally
        {
            _activePipelines.TryRemove(connection.ConnectionId, out _);
        }
    }

    public void CancelPipeline(string connectionId)
    {
        if (_activePipelines.TryRemove(connectionId, out var cts)) cts.Cancel();
    }
}