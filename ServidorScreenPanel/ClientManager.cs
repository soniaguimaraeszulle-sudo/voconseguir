using System;
using System.Collections.Concurrent;

public class ClientManager
{
    public static ClientManager Instance { get; } = new ClientManager();
    private ClientManager() { }

    public ConcurrentDictionary<string, ClientSession> Clients { get; }
        = new ConcurrentDictionary<string, ClientSession>();

    public event Action<ClientSession>? ClientConnected;
    public event Action<ClientSession>? ClientDisconnected;

    public ClientSession GetOrCreateClient(string pcName, string ip, string country, string mac, string antivirus)
    {
        var id = $"{pcName}@{ip}";

        var session = Clients.GetOrAdd(id, _ =>
        {
            var s = new ClientSession
            {
                Id = id,
                PcName = pcName,
                Ip = ip,
                Country = string.IsNullOrWhiteSpace(country) ? "" : country,
                MacAddress = string.IsNullOrWhiteSpace(mac) ? "" : mac,
                Antivirus = string.IsNullOrWhiteSpace(antivirus) ? "" : antivirus,
                ConnectedAt = DateTime.Now
            };

            ClientConnected?.Invoke(s);
            return s;
        });

        return session;
    }

    public void RemoveClient(string id)
    {
        if (Clients.TryRemove(id, out var s))
        {
            ClientDisconnected?.Invoke(s);
        }
    }
}
