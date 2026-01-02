using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using AssistantCore.Voice.Dto;

namespace AssistantCore.Voice;

/// <summary>
/// Represents a WebSocket connection to a satellite. TODO: Separate session management from connection handling
/// </summary>
public class SatelliteConnection
{
    private readonly string _connectionId;
    private readonly WebSocket _socket;
    
    private SatelliteState _state;
    private SatelliteHello? _info;
    private SatelliteSessionStart? _session;
    
    public SatelliteConnection(string connectionId, WebSocket socket)
    {
        _connectionId = connectionId;
        _socket = socket;
        _state = SatelliteState.Disconnected;
    }

    public async Task RunAsync(CancellationToken token)
    {
        while (_socket.State == WebSocketState.Open)
        {
            var buffer = new byte[4096];
            var result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", token);
            }

            switch (result.MessageType)
            {
                case WebSocketMessageType.Close:
                    await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", token);
                    break;
                case WebSocketMessageType.Text:
                    await HandleTextMessageAsync(buffer, token);
                    break;
                case WebSocketMessageType.Binary:
                    await HandleByteMessageAsync(buffer, result.Count, token);
                    break;
                default:
                    throw new ArgumentException("Unsupported WebSocket message type");
            }
        }
    }

    private async Task HandleByteMessageAsync(byte[] buf, int count, CancellationToken token)
    {
        if (_state != SatelliteState.SessionActive) return; // Can't accept audio if no session is active
        // TODO: Handle incoming audio frames
    }

    private async Task HandleTextMessageAsync(byte[] buf, CancellationToken token)
    {
        var stream = new MemoryStream(buf);
        var json = await JsonDocument.ParseAsync(stream, cancellationToken: token);

        if (!json.RootElement.TryGetProperty("type", out var messageType))
        {
            // Invalid message (TODO: log)
            return;
        }

        var raw = json.RootElement.GetRawText();
        var deserializeOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        switch (messageType.GetString())
        {
            case "hello":
                _info = JsonSerializer.Deserialize<SatelliteHello>(raw, deserializeOpts);
                if (_info == null) return;
                _state = SatelliteState.Connected;
                var helloAck = new SatelliteHelloAck
                {
                    Type = "hello.ack",
                    ProtocolVersion = _info.ProtocolVersion,
                    Accepted = true
                };
                await SendMessageAsync(helloAck, token);
                break;
            case "session.start":
                _session = JsonSerializer.Deserialize<SatelliteSessionStart>(raw, deserializeOpts);
                if (_session == null) return;
                _state = SatelliteState.SessionActive;
                var sessionAck = new SatelliteSessionAck
                {
                    Type = "session.ack",
                    SessionId = _session.SessionId
                };
                await SendMessageAsync(sessionAck, token);
                break;
            case "audio.end":
                var audioEnd = JsonSerializer.Deserialize<SatelliteAudioEnd>(raw, deserializeOpts);
                if (audioEnd == null) return;
                _state = SatelliteState.Processing;
                break;
            case "session.abort":
                var sessionAbort = JsonSerializer.Deserialize<SatelliteSessionAbort>(raw, deserializeOpts);
                if (sessionAbort == null) return;
                _state = SatelliteState.Connected;
                break;
        }
    }

    public async Task SendTtsAsync(byte[] ttsData, CancellationToken token)
    {
        if (_state != SatelliteState.Processing) return;

        var ttsStart = new SatelliteTtsStart
        {
            Type = "tts.start",
            SessionId = _session!.SessionId,
            AudioFormat = _info!.AudioFormat,
            Streaming = true
        };
        await SendMessageAsync(ttsStart, token);
        _state = SatelliteState.Playback;
        
        // Stream audio in chunks
        throw new NotImplementedException();

        var ttsEnd = new SatelliteTtsEnd
        {
            Type = "tts.end",
            SessionId = _session!.SessionId
        };
        await SendMessageAsync(ttsEnd, token);
        _state = SatelliteState.Connected;
    }

    public async Task SendErrorAsync(string code, string message, CancellationToken token)
    {
        var error = new SatelliteError
        {
            Type = "error",
            SessionId = _session?.SessionId ?? string.Empty,
            ErrorCode = code,
            Message = message
        };
        await SendMessageAsync(error, token);

        _state = SatelliteState.Connected;
    }

    public async Task SendBargeInAsync(CancellationToken token)
    {
        if (_state != SatelliteState.Playback) return; // Can only barge in during tts playback
        
        var bargeIn = new SatelliteBargeIn
        {
            Type = "barge_in",
            SessionId = _session!.SessionId
        };
        await SendMessageAsync(bargeIn, token);
    }

    private async Task SendMessageAsync(SatelliteDto message, CancellationToken token)
    {
        var json = JsonSerializer.Serialize(message);
        var buffer = Encoding.UTF8.GetBytes(json);
        await _socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, token);
    }
}