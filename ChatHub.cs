using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

// Usamos el namespace raíz para que todos lo vean fácil
namespace CUChatNet.Api;

public class ChatHub : Hub
{
    private static readonly ConcurrentDictionary<string, string> _onlineUsers = new();

    public override async Task OnConnectedAsync()
    {
        var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString();
        if (!string.IsNullOrEmpty(userId))
        {
            _onlineUsers[userId] = Context.ConnectionId;
            await Clients.All.SendAsync("UserStatusChanged", userId, true);
        }
        await base.OnConnectedAsync();
    }
    public async Task JoinChat(string chatId)
    {
        // Quitamos el "chat_" para que sea solo el ID, igual que en el Controller
        await Groups.AddToGroupAsync(Context.ConnectionId, chatId);
    }

    // Opcional: Mejora esta función para que no envíe a "All" (todos), 
    // sino solo a los del chat específico

    public async Task ConfirmDelivery(int messageId)
    {
        await Clients.All.SendAsync("UpdateMessageStatus", messageId, 2);
    }

    public async Task MarkMessageAsRead(int messageId)
    {
        await Clients.All.SendAsync("UpdateMessageStatus", messageId, 3);
    }

    public async Task SendMessage(string chatId, object message)
    {
        await Clients.Group(chatId).SendAsync("ReceiveMessage", message);
    }

    public override async Task OnDisconnectedAsync(Exception? ex)
    {
        var userId = _onlineUsers.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
        if (userId != null)
        {
            _onlineUsers.TryRemove(userId, out _);
            await Clients.All.SendAsync("UserStatusChanged", userId, false);
        }
        await base.OnDisconnectedAsync(ex);
    }
}