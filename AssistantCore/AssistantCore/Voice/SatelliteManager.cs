using System.Collections.Concurrent;

namespace AssistantCore.Voice;

public class SatelliteManager
{
    public static SatelliteManager Instance { get; } = new();
    
    // TODO: fields for python workers

    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activePipelines = new();
    
    public SatelliteManager() {} // TODO: parameters for python workers

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

        var orchestrator = new VoiceSessionOrchestrator(session, connection); // TODO: pass in python workers

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