using KT19.Models;

namespace KT19.Services;

/// <summary>Последние сообщения чата в памяти — чтобы новый участник видел контекст.</summary>
public sealed class ChatHistory
{
    private const int Capacity = 50;
    private readonly Queue<ChatMessage> _messages = new();
    private readonly object _sync = new();

    public void Add(ChatMessage message)
    {
        lock (_sync)
        {
            _messages.Enqueue(message);
            while (_messages.Count > Capacity) _messages.Dequeue();
        }
    }

    public ChatMessage[] Snapshot()
    {
        lock (_sync) return _messages.ToArray();
    }
}
