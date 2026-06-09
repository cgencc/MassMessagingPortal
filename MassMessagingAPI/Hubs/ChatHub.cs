using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace MassMessagingAPI.Hubs 
{
    [Authorize]
    public class ChatHub : Hub
    {
        public async Task SendMessageToUser(string receiverId, string message)
        {
            var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var senderName = Context.User?.FindFirst("FirstName")?.Value;

            await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, senderName, message);
        }

        public async Task SendMessageToGroup(string groupName, string message)
        {
            var senderName = Context.User?.FindFirst("FirstName")?.Value;

            await Clients.Group(groupName).SendAsync("ReceiveGroupMessage", groupName, senderName, message);
        }

        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }
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
        public async Task SendTypingStatus(string receiverId, bool isGroup)
        {
            var senderName = Context.User?.FindFirst("FirstName")?.Value ?? "Biri";

            if (isGroup)
            {
                await Clients.Group(receiverId).SendAsync("ReceiveTyping", receiverId, senderName, true);
            }
            else if (!string.IsNullOrEmpty(receiverId))
            {
                var myId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                await Clients.User(receiverId).SendAsync("ReceiveTyping", myId, senderName, false);
            }
        }

    }
}