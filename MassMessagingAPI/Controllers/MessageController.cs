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

            // 1. Repository ile Veritabanına Kaydet
            await _messageRepository.AddAsync(message);

            // 2. SignalR ile Anlık İlet
            if (model.GroupId.HasValue)
            {
                var group = await _groupRepository.GetByIdAsync(model.GroupId.Value);
                if (group != null)
                {
                    await _hubContext.Clients.Group(group.Name).SendAsync("ReceiveGroupMessage", group.Name, senderName, model.Content);
                }
            }
            else if (!string.IsNullOrEmpty(model.ReceiverId))
            {
                await _hubContext.Clients.User(model.ReceiverId).SendAsync("ReceiveMessage", senderId, senderName, model.Content);
            }

            return Ok(new { Message = "Mesaj gönderildi." });
        }

        [HttpGet("history/{userId}")]
        public async Task<IActionResult> GetMessageHistory(string userId)
        {
            var myId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Repository üzerinden Include kullanarak Sender (Gönderen Kullanıcı) bilgilerini çekiyoruz
            var messages = await _messageRepository.FindAsync(
                m => (m.SenderId == myId && m.ReceiverId == userId) || (m.SenderId == userId && m.ReceiverId == myId),
                m => m.Sender!
            );

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
            if (file == null || file.Length == 0) return BadRequest("Dosya seçilmedi.");

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok(new { url = "/uploads/" + fileName });
        }
    }
}