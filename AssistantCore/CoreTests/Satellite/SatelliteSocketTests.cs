using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CoreTests.Satellite;

public class SatelliteSocketTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string HelloMessage =
        "{\n  \"type\": \"hello\",\n  \"protocol_version\": 1,\n  \"mic_id\": \"kitchen_satellite\",\n  \"area\": \"kitchen\",\n  \"language\": \"en-US\",\n  \"capabilities\": {\n    \"speaker\": true,\n    \"display\": false,\n    \"supports_barge_in\": true,\n    \"supports_streaming_tts\": true\n  },\n  \"audio_format\": {\n    \"encoding\": \"pcm_s16le\",\n    \"sample_rate\": 16000,\n    \"channels\": 1,\n    \"frame_ms\": 20\n  }\n}";

    private const string SessionStartMessage =
        "{\n  \"type\": \"session.start\",\n  \"session_id\": \"uuid-v4\",\n  \"timestamp\": 123456789\n}";

    private const string AudioEndMessage =
        "{\n  \"type\": \"audio.end\",\n  \"session_id\": \"uuid-v4\",\n  \"reason\": \"silence\"\n}";

    private const int AudioFrameSizeBytes = 640; // 20ms of PCM 16kHz mono audio with 16-bit samples (values from HelloMessage)
    
    [Fact]
    public async Task Satellite_SendHello_ReceiveAck()
    {
        var client = factory.Server.CreateWebSocketClient();

        using var socket = await client.ConnectAsync(
            new Uri("ws://localhost/ws/satellite"),
            CancellationToken.None);
        
        var message = Encoding.UTF8.GetBytes(HelloMessage);

        await socket.SendAsync(
            message,
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);

        Assert.Equal(WebSocketState.Open, socket.State);

        var buffer = new byte[1024];
        var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        var response = Encoding.UTF8.GetString(buffer, 0, result.Count);
        var json = JsonDocument.Parse(response);
        Assert.True(json.RootElement.TryGetProperty("type", out var messageType));
        Assert.Equal("hello.ack", messageType.GetString());
    }

    [Fact]
    public async Task Satellite_StartSession_ReceiveAck()
    {
        var client = factory.Server.CreateWebSocketClient();

        using var socket = await client.ConnectAsync(
            new Uri("ws://localhost/ws/satellite"),
            CancellationToken.None);

        var message = Encoding.UTF8.GetBytes(HelloMessage);

        await socket.SendAsync(
            message,
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);

        Assert.Equal(WebSocketState.Open, socket.State);

        var buffer = new byte[1024];
        var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        var response = Encoding.UTF8.GetString(buffer, 0, result.Count);
        var json = JsonDocument.Parse(response);
        Assert.True(json.RootElement.TryGetProperty("type", out var messageType));
        Assert.Equal("hello.ack", messageType.GetString());
        
        message = Encoding.UTF8.GetBytes(SessionStartMessage);
        await socket.SendAsync(
            message,
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);

        Assert.Equal(WebSocketState.Open, socket.State);
        
        buffer = new byte[1024];
        result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        response = Encoding.UTF8.GetString(buffer, 0, result.Count);
        json = JsonDocument.Parse(response);
        Assert.True(json.RootElement.TryGetProperty("type", out messageType));
        Assert.Equal("session.ack", messageType.GetString());
    }

    [Fact]
    public async Task Satellite_SendAudioFrames_ReceiveTtsStart()
    {
        var client = factory.Server.CreateWebSocketClient();

        using var socket = await client.ConnectAsync(
            new Uri("ws://localhost/ws/satellite"),
            CancellationToken.None);

        var message = Encoding.UTF8.GetBytes(HelloMessage);

        await socket.SendAsync(
            message,
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);

        Assert.Equal(WebSocketState.Open, socket.State);

        var buffer = new byte[1024];
        var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        var response = Encoding.UTF8.GetString(buffer, 0, result.Count);
        var json = JsonDocument.Parse(response);
        Assert.True(json.RootElement.TryGetProperty("type", out var messageType));
        Assert.Equal("hello.ack", messageType.GetString());
        
        message = Encoding.UTF8.GetBytes(SessionStartMessage);
        await socket.SendAsync(
            message,
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);

        Assert.Equal(WebSocketState.Open, socket.State);
        
        result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        response = Encoding.UTF8.GetString(buffer, 0, result.Count);
        json = JsonDocument.Parse(response);
        Assert.True(json.RootElement.TryGetProperty("type", out messageType));
        Assert.Equal("session.ack", messageType.GetString());

        // Send random dummy audio
        var audioBuffer = new byte[AudioFrameSizeBytes];
        for (int i = 0; i < 50; i++) // Send 1 second of audio
        {
            Random.Shared.NextBytes(audioBuffer);
            await socket.SendAsync(audioBuffer, WebSocketMessageType.Binary, true, CancellationToken.None);
        }

        message = Encoding.UTF8.GetBytes(AudioEndMessage);
        await socket.SendAsync(
            message,
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);
        Assert.Equal(WebSocketState.Open, socket.State);
        
        response = Encoding.UTF8.GetString(buffer, 0, result.Count);
        json = JsonDocument.Parse(response);
        Assert.True(json.RootElement.TryGetProperty("type", out messageType));
        Assert.Equal("tts.start", messageType.GetString());
    }
}
