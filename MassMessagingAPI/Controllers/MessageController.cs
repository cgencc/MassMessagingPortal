using MassMessagingAPI.Data;
using MassMessagingAPI.DTOs;
using MassMessagingAPI.Hubs;
using MassMessagingAPI.Models;
using MassMessagingAPI.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
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
        private readonly AppDbContext _context;

        public MessageController(
            IGenericRepository<Message> messageRepository,
            IGenericRepository<Group> groupRepository,
            IHubContext<ChatHub> hubContext,
            AppDbContext context)
        {
            _messageRepository = messageRepository;
            _groupRepository = groupRepository;
            _hubContext = hubContext;
            _context = context;
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

        // --- HATA DÜZELTİLDİ: int id yerine string id kabul ediyoruz ---
        [HttpGet("history/{id}/{page}")]
        public async Task<IActionResult> GetHistory(string id, int page = 1)
        {
            int pageSize = 20;
            var myId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            bool isGroup = int.TryParse(id, out int groupId);

            // Query'yi başlat
            var query = _context.Messages.Include(m => m.Sender).AsQueryable();

            if (isGroup)
            {
                query = query.Where(m => m.GroupId == groupId);
            }
            else
            {
                // Kullanıcı ise Sender ve Receiver eşleşmesine bak
                query = query.Where(m => (m.SenderId == myId && m.ReceiverId == id) ||
                                         (m.SenderId == id && m.ReceiverId == myId));
            }

            var messages = await query
                .OrderByDescending(m => m.SentDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .OrderBy(m => m.SentDate)
                .Select(m => new {
                    m.Id,
                    m.Content,
                    m.SentDate,
                    m.IsEdited,
                    SenderName = m.Sender.FirstName + " " + m.Sender.LastName,
                    IsMine = m.SenderId == myId
                })
                .ToListAsync();

            return Ok(messages);
        }

        [HttpGet("history/group/{groupId}")]
        public async Task<IActionResult> GetGroupMessageHistory(int groupId)
        {
            var myId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var messages = await _messageRepository.FindAsync(m => m.GroupId == groupId, m => m.Sender!);

            var result = messages.OrderBy(m => m.SentDate).Select(m => new
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
            using (var stream = new FileStream(path, FileMode.Create)) await file.CopyToAsync(stream);
            return Ok(new { url = "/uploads/" + fileName });
        }

        [HttpPost("send-admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SendAdminMessage([FromBody] string content)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveAdminMessage", content);
            return Ok();
        }

        [HttpPut("edit")]
        public async Task<IActionResult> EditMessage([FromBody] EditMessageDto dto)
        {
            var msg = await _messageRepository.GetByIdAsync(dto.Id);
            if (msg == null || msg.SenderId != User.FindFirst(ClaimTypes.NameIdentifier)?.Value) return Unauthorized();

            msg.Content = dto.Content;
            msg.IsEdited = true;
            await _messageRepository.UpdateAsync(msg);
            return Ok();
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var msg = await _messageRepository.GetByIdAsync(id);
            if (msg == null || msg.SenderId != User.FindFirst(ClaimTypes.NameIdentifier)?.Value) return Unauthorized();

            msg.IsDeleted = true;
            await _messageRepository.UpdateAsync(msg);
            return Ok();
        }
    }
}