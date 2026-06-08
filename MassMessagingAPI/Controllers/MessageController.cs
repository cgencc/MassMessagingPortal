using MassMessagingAPI.DTOs;
using MassMessagingAPI.Hubs;
using MassMessagingAPI.Models;
using MassMessagingAPI.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace MassMessagingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MessageController : ControllerBase
    {
        private readonly IGenericRepository<Message> _messageRepository;
        private readonly IGenericRepository<Group> _groupRepository;
        private readonly IHubContext<ChatHub> _hubContext;

        public MessageController(
            IGenericRepository<Message> messageRepository,
            IGenericRepository<Group> groupRepository,
            IHubContext<ChatHub> hubContext)
        {
            _messageRepository = messageRepository;
            _groupRepository = groupRepository;
            _hubContext = hubContext;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto model)
        {
            var senderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var senderName = User.FindFirst("FirstName")?.Value;

            if (senderId == null) return Unauthorized("Kullanıcı bulunamadı.");

            var message = new Message
            {
                Content = model.Content,
                SenderId = senderId,
                ReceiverId = model.ReceiverId,
                GroupId = model.GroupId,
                SentDate = DateTime.Now
            };

            await _messageRepository.AddAsync(message);

            if (model.GroupId.HasValue)
            {
                var group = await _groupRepository.GetByIdAsync(model.GroupId.Value);
                if (group != null)
                {
                    await _hubContext.Clients.Group(group.Name)
                        .SendAsync("ReceiveGroupMessage", group.Name, senderName, model.Content);
                }
            }
            else if (!string.IsNullOrEmpty(model.ReceiverId))
            {
                await _hubContext.Clients.User(model.ReceiverId)
                    .SendAsync("ReceiveMessage", senderId, senderName, model.Content);
            }

            return Ok(new { Message = "Mesaj gönderildi." });
        }

        // Direct message history between two users
        [HttpGet("history/{userId}")]
        public async Task<IActionResult> GetMessageHistory(string userId)
        {
            var myId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var messages = await _messageRepository.FindAsync(
                m => (m.SenderId == myId && m.ReceiverId == userId) ||
                     (m.SenderId == userId && m.ReceiverId == myId),
                m => m.Sender!);

            var result = messages
                .OrderBy(m => m.SentDate)
                .Select(m => new
                {
                    m.Id,
                    m.Content,
                    m.SentDate,
                    SenderName = m.Sender!.FirstName + " " + m.Sender.LastName,
                    IsMine = m.SenderId == myId
                });

            return Ok(result);
        }

        // FIX (Chat UI): Group message history — was missing, caused chat box to be empty for groups
        [HttpGet("history/group/{groupId}")]
        public async Task<IActionResult> GetGroupMessageHistory(int groupId)
        {
            var myId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var messages = await _messageRepository.FindAsync(
                m => m.GroupId == groupId,
                m => m.Sender!);

            var result = messages
                .OrderBy(m => m.SentDate)
                .Select(m => new
                {
                    m.Id,
                    m.Content,
                    m.SentDate,
                    SenderName = m.Sender!.FirstName + " " + m.Sender.LastName,
                    IsMine = m.SenderId == myId
                });

            return Ok(result);
        }

        [HttpPut("mark-as-read/{messageId}")]
        public async Task<IActionResult> MarkAsRead(int messageId)
        {
            var message = await _messageRepository.GetByIdAsync(messageId);
            if (message == null) return NotFound();

            message.IsRead = true;
            await _messageRepository.UpdateAsync(message);

            return Ok();
        }

        [HttpPost("upload-file")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Dosya yok.");

            var allowedExtensions = new[] { ".jpg", ".png", ".mp4", ".pdf" };
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(ext)) return BadRequest("Desteklenmeyen dosya tipi.");

            var fileName = Guid.NewGuid().ToString() + ext;
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok(new { url = "/uploads/" + fileName });
        }

        [HttpPost("send-admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SendAdminMessage([FromBody] string content)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveAdminMessage", content);
            return Ok();
        }
    }
}