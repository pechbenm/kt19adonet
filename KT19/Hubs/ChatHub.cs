using KT19.Models;
using KT19.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace KT19.Hubs;

/// <summary>
/// Чат. [Authorize] — подключиться к хабу (даже на уровне negotiate) могут только
/// пользователи с валидной auth-кукой; остальные получают 401.
/// </summary>
[Authorize]
public sealed class ChatHub(ChatHistory history, OnlineUsers online) : Hub
{
    public const int MaxMessageLength = 500;

    // Имя берём из аутентифицированного пользователя — подделать его с клиента нельзя.
    private string CurrentUser =>
        Context.User?.Identity?.Name ?? throw new HubException("Пользователь не определён.");

    public override async Task OnConnectedAsync()
    {
        online.Add(CurrentUser, Context.ConnectionId);

        await Clients.Caller.SendAsync("LoadHistory", history.Snapshot());
        await Clients.All.SendAsync("UsersChanged", online.Names());

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        online.Remove(CurrentUser, Context.ConnectionId);
        await Clients.All.SendAsync("UsersChanged", online.Names());

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string text)
    {
        text = (text ?? string.Empty).Trim();
        if (text.Length == 0) return;
        if (text.Length > MaxMessageLength)
            throw new HubException($"Сообщение длиннее {MaxMessageLength} символов.");

        var message = new ChatMessage(CurrentUser, text, DateTimeOffset.UtcNow);
        history.Add(message);

        await Clients.All.SendAsync("ReceiveMessage", message);
    }
}
