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

            // ✅ YENİ EKLENEN: Gruptan çıkarılan birinin mesaj atmasını engelleyen güvenlik kalkanı
            if (model.GroupId.HasValue)
            {
                var isMember = await _context.UserGroups
                    .AnyAsync(ug => ug.GroupId == model.GroupId.Value && ug.UserId == senderId);

                if (!isMember)
                {
                    return Unauthorized(new { Message = "Bu gruba mesaj gönderme yetkiniz yok. Gruptan çıkarılmış olabilirsiniz." });
                }
            }

            var message = new Message
            {
                Content = model.Content,
                SenderId = senderId,
                ReceiverId = model.ReceiverId,
                GroupId = model.GroupId,
                SentDate = DateTime.Now,
                IsDeleted = false
            };
            await _messageRepository.AddAsync(message);

            if (model.GroupId.HasValue)
            {
                var group = await _groupRepository.GetByIdAsync(model.GroupId.Value);
                if (group != null)
                    await _hubContext.Clients.Group(group.Name).SendAsync("ReceiveGroupMessage", group.Name, senderName, model.Content);
            }
            else if (!string.IsNullOrEmpty(model.ReceiverId))
            {
                await _hubContext.Clients.User(model.ReceiverId).SendAsync("ReceiveMessage", senderId, senderName, model.Content);
            }

            return Ok(new { Message = "Mesaj gönderildi." });
        }

        [HttpGet("history/{id}/{page}")]
        public async Task<IActionResult> GetHistory(string id, int page = 1)
        {
            int pageSize = 20;
            var myId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            bool isGroup = int.TryParse(id, out int groupId);

            var query = _context.Messages.Where(m => !m.IsDeleted).Include(m => m.Sender).AsQueryable();
            if (isGroup) query = query.Where(m => m.GroupId == groupId);
            else query = query.Where(m => (m.SenderId == myId && m.ReceiverId == id) || (m.SenderId == id && m.ReceiverId == myId));

            var messages = await query
                .OrderByDescending(m => m.SentDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .OrderBy(m => m.SentDate)
                .Select(m => new
                {
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

        // ✅ NEW: Search through messages the current user is part of
        // GET /api/Message/search?q=hello&page=1
        [HttpGet("search")]
        public async Task<IActionResult> SearchMessages([FromQuery] string q, [FromQuery] int page = 1)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { Message = "Arama terimi boş olamaz." });

            int pageSize = 30;
            var myId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Search messages the user sent or received (DMs + group messages in their groups)
            var myGroupIds = await _context.UserGroups
                .Where(ug => ug.UserId == myId)
                .Select(ug => ug.GroupId)
                .ToListAsync();

            var results = await _context.Messages
                .Where(m => !m.IsDeleted &&
                    m.Content.Contains(q) &&
                    (m.SenderId == myId ||
                     m.ReceiverId == myId ||
                     (m.GroupId != null && myGroupIds.Contains(m.GroupId.Value))))
                .Include(m => m.Sender)
                .Include(m => m.Group)
                .OrderByDescending(m => m.SentDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new
                {
                    m.Id,
                    m.Content,
                    m.SentDate,
                    SenderName = m.Sender.FirstName + " " + m.Sender.LastName,
                    IsMine = m.SenderId == myId,
                    Context = m.GroupId != null ? m.Group!.Name : "Özel mesaj",
                    TargetId = m.GroupId != null ? m.GroupId.ToString() : (m.SenderId == myId ? m.ReceiverId : m.SenderId),
                    IsGroup = m.GroupId != null
                })
                .ToListAsync();

            return Ok(results);
        }

        [HttpPut("edit")]
        public async Task<IActionResult> EditMessage([FromBody] EditMessageDto dto)
        {
            var msg = await _messageRepository.GetByIdAsync(dto.Id);
            if (msg == null || msg.SenderId != User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
                return Unauthorized();
            msg.Content = dto.Content;
            msg.IsEdited = true;
            await _messageRepository.UpdateAsync(msg);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var msg = await _messageRepository.GetByIdAsync(id);
            var myId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("Admin");

            // Mesajı silmeye çalışan kişi mesajın sahibi değilse VE admin değilse engelle
            if (msg == null || (msg.SenderId != myId && !isAdmin))
                return Unauthorized(new { Message = "Bu mesajı silme yetkiniz yok." });

            msg.IsDeleted = true;
            await _messageRepository.UpdateAsync(msg);
            await _context.SaveChangesAsync();

            // SignalR ile mesajın silindiğini anlık olarak herkese bildir
            if (msg.GroupId != null)
            {
                var group = await _groupRepository.GetByIdAsync(msg.GroupId.Value);
                if (group != null)
                    await _hubContext.Clients.Group(group.Name).SendAsync("MessageDeleted", id);
            }
            else
            {
                // Birebir sohbetlerde iki tarafa da silinme komutunu gönder
                await _hubContext.Clients.User(msg.SenderId).SendAsync("MessageDeleted", id);
                if (!string.IsNullOrEmpty(msg.ReceiverId))
                {
                    await _hubContext.Clients.User(msg.ReceiverId).SendAsync("MessageDeleted", id);
                }
            }

            return Ok();
        }

        [HttpPut("mark-as-read/{messageId}")]
        public async Task<IActionResult> MarkAsRead(int messageId)
        {
            var message = await _messageRepository.GetByIdAsync(messageId);
            if (message == null) return NotFound();
            message.IsRead = true;
            await _messageRepository.UpdateAsync(message);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // ✅ NEW: Get unread count for a specific conversation (DM or group)
        // GET /api/Message/unread-count/{id}   (id = userId or groupId)
        [HttpGet("unread-count/{id}")]
        public async Task<IActionResult> GetUnreadCount(string id)
        {
            var myId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            bool isGroup = int.TryParse(id, out int groupId);

            int count;
            if (isGroup)
            {
                count = await _context.Messages
                    .CountAsync(m => !m.IsDeleted && !m.IsRead && m.GroupId == groupId && m.SenderId != myId);
            }
            else
            {
                count = await _context.Messages
                    .CountAsync(m => !m.IsDeleted && !m.IsRead && m.SenderId == id && m.ReceiverId == myId);
            }

            return Ok(new { UnreadCount = count });
        }

        [HttpPost("upload-file")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Dosya yok.");

            // BURAYI GÜNCELLEDİK: .webm, .ogg, .mp3, .wav eklendi
            var allowedExtensions = new[] { ".jpg", ".png", ".mp4", ".pdf", ".webm", ".ogg", ".mp3", ".wav" };

            var ext = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(ext)) return BadRequest("Desteklenmeyen dosya tipi.");

            var fileName = Guid.NewGuid().ToString() + ext;
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            Directory.CreateDirectory(uploadsPath);

            var path = Path.Combine(uploadsPath, fileName);
            using (var stream = new FileStream(path, FileMode.Create))
                await file.CopyToAsync(stream);

            return Ok(new { url = "/uploads/" + fileName });
        }

        [HttpPut("mark-conversation-read/{id}")]
        public async Task<IActionResult> MarkConversationRead(string id, [FromQuery] bool isGroup)
        {
            var myId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            IQueryable<Message> unreadMessages;

            if (isGroup)
            {
                if (!int.TryParse(id, out int groupId)) return BadRequest();
                // Gruptaki benim dışımdaki kişilerin yazdığı okunmamış mesajlar
                unreadMessages = _context.Messages.Where(m => !m.IsDeleted && !m.IsRead && m.GroupId == groupId && m.SenderId != myId);
            }
            else
            {
                // Birebir sohbetteki okunmamış mesajlar
                unreadMessages = _context.Messages.Where(m => !m.IsDeleted && !m.IsRead && m.SenderId == id && m.ReceiverId == myId);
            }

            var messagesToUpdate = await unreadMessages.ToListAsync();

            if (messagesToUpdate.Any())
            {
                foreach (var msg in messagesToUpdate)
                {
                    msg.IsRead = true; // Veritabanında kalıcı olarak okundu yapıyoruz
                }
                await _context.SaveChangesAsync();
            }

            return Ok();
        }
    }
}