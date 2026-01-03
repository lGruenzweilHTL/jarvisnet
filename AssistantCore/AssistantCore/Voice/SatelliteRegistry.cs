namespace AssistantCore.Voice;

public class SatelliteRegistry
{
    public static SatelliteRegistry Instance { get; } = new();
    public List<SatelliteConnection> Connections { get; } = [];

    public void Register(SatelliteConnection conn)
    {
        Connections.Add(conn);
    }

    public bool Unregister(SatelliteConnection conn)
    {
        return Connections.Remove(conn);
    }
}