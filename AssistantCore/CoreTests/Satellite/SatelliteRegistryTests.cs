using System.Net.WebSockets;
using System.Text;
using AssistantCore.Voice;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CoreTests.Satellite;

public class SatelliteRegistryTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task ConnectWebsocket_ConnectionRegistered()
    {
        var client = factory.Server.CreateWebSocketClient();

        using var socket = await client.ConnectAsync(
            new Uri("ws://localhost/ws/satellite"),
            CancellationToken.None);

        Assert.Equal(WebSocketState.Open, socket.State);
        Assert.NotNull(SatelliteRegistry.Instance);
        var conn = Assert.Single(SatelliteRegistry.Instance.Connections);
        Assert.Equal(SatelliteConnectionState.Connected, conn.State);
    }
}