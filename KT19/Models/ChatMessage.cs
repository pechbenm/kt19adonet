namespace KT19.Models;

/// <summary>Сообщение чата. Имя отправителя всегда берётся из куки на сервере, а не от клиента.</summary>
public sealed record ChatMessage(string User, string Text, DateTimeOffset SentAt);
