namespace AssistantCore.Voice;

public class SatelliteRegistry
{
    public static SatelliteRegistry Instance { get; } = new();
    private List<SatelliteConnection> _connections = [];

    public void Register(SatelliteConnection conn)
    {
        _connections.Add(conn);
    }

    public bool Unregister(SatelliteConnection conn)
    {
        return _connections.Remove(conn);
    }
}