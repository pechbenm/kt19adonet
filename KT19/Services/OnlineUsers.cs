namespace KT19.Services;

/// <summary>Кто сейчас онлайн. Один пользователь может держать несколько соединений (вкладок).</summary>
public sealed class OnlineUsers
{
    private readonly Dictionary<string, HashSet<string>> _connections = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _sync = new();

    public void Add(string user, string connectionId)
    {
        lock (_sync)
        {
            if (!_connections.TryGetValue(user, out var set))
                _connections[user] = set = new HashSet<string>();
            set.Add(connectionId);
        }
    }

    public void Remove(string user, string connectionId)
    {
        lock (_sync)
        {
            if (!_connections.TryGetValue(user, out var set)) return;
            set.Remove(connectionId);
            if (set.Count == 0) _connections.Remove(user);
        }
    }

    public string[] Names()
    {
        lock (_sync)
            return _connections.Keys.OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase).ToArray();
    }
}
