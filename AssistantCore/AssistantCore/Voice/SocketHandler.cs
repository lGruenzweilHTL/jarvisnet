using System.Net.WebSockets;

namespace AssistantCore.Voice;

public static class SocketHandler
{
    public static async Task HandleSatelliteConnection(WebSocket sock, HttpContext context)
    {
        var connId = Guid.NewGuid().ToString();
        var satelliteConnection = new SatelliteConnection(connId, sock);
        SatelliteRegistry.Instance.Register(satelliteConnection);
        await satelliteConnection.RunAsync(CancellationToken.None); // TODO: cancellation token
    }
}