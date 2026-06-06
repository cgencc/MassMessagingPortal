using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace MassMessagingAPI.Hubs // Kendi namespace'iniz
{
    // [Authorize] etiketi sayesinde sadece Token'ı olanlar bu merkeze bağlanabilir.
    [Authorize]
    public class ChatHub : Hub
    {
        // 1. Birebir Mesaj Gönderme
        public async Task SendMessageToUser(string receiverId, string message)
        {
            var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var senderName = Context.User?.FindFirst("FirstName")?.Value;

            // SignalR ile mesajı anlık olarak alıcıya iletiyoruz
            await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, senderName, message);
        }

        // 2. Gruba Mesaj Gönderme (Toplu Mesajlaşma)
        public async Task SendMessageToGroup(string groupName, string message)
        {
            var senderName = Context.User?.FindFirst("FirstName")?.Value;

            // Mesajı o gruba bağlı olan herkese anlık olarak iletiyoruz
            await Clients.Group(groupName).SendAsync("ReceiveGroupMessage", groupName, senderName, message);
        }

        // 3. Kullanıcının bir gruba katılması
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        // 4. Kullanıcının bir gruptan çıkması
        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }
        // Bağlı olan kullanıcıları tutan statik sözlük
        private static readonly HashSet<string> ConnectedUsers = new HashSet<string>();

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (userId != null)
            {
                ConnectedUsers.Add(userId);
                await Clients.All.SendAsync("UserStatusChanged", userId, true);
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            if (userId != null)
            {
                ConnectedUsers.Remove(userId);
                await Clients.All.SendAsync("UserStatusChanged", userId, false);
            }
            await base.OnDisconnectedAsync(exception);
        }

    }
}