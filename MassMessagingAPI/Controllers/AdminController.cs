using MassMessagingAPI.Data;
using MassMessagingAPI.Models;
using MassMessagingAPI.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MassMessagingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public AdminController(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            AppDbContext context,
            IHubContext<ChatHub> hubContext)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _hubContext = hubContext;
        }

        private string? GetCurrentUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var userList = new List<object>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new { user.Id, user.FirstName, user.LastName, user.Email, Roles = roles });
            }
            return Ok(userList);
        }

        [HttpPost("assign-role/{userId}/{role}")]
        public async Task<IActionResult> AssignRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");
            if (!await _roleManager.RoleExistsAsync(role))
                return BadRequest("Böyle bir rol bulunmuyor.");
            if (await _userManager.IsInRoleAsync(user, role))
                return BadRequest("Kullanıcı zaten bu role sahip.");
            var result = await _userManager.AddToRoleAsync(user, role);
            if (!result.Succeeded) return BadRequest(result.Errors);
            return Ok(new { Message = "Rol başarıyla atandı." });
        }

        [HttpPost("remove-role/{userId}/{role}")]
        public async Task<IActionResult> RemoveRole(string userId, string role)
        {
            if (userId == GetCurrentUserId())
                return BadRequest(new { Message = "Kendi yetkilerini kaldıramazsın." });
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");
            if (!await _userManager.IsInRoleAsync(user, role))
                return BadRequest(new { Message = "Kullanıcı bu role sahip değil." });
            var result = await _userManager.RemoveFromRoleAsync(user, role);
            if (!result.Succeeded) return BadRequest(result.Errors);
            return Ok(new { Message = "Rol başarıyla kaldırıldı." });
        }

        [HttpDelete("delete-group/{groupId}")]
        public async Task<IActionResult> DeleteGroup(int groupId)
        {
            var group = await _context.Groups.FindAsync(groupId);
            if (group == null) return NotFound(new { Message = "Grup bulunamadı." });

            // ✅ FIX: Delete related records first to avoid FK constraint 500 error.
            // 1. Remove all messages that belong to this group
            var groupMessages = _context.Messages.Where(m => m.GroupId == groupId);
            _context.Messages.RemoveRange(groupMessages);

            // 2. Remove all UserGroup memberships for this group
            var userGroups = _context.UserGroups.Where(ug => ug.GroupId == groupId);
            _context.UserGroups.RemoveRange(userGroups);

            // 3. Now safe to delete the group itself
            _context.Groups.Remove(group);

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Grup başarıyla silindi." });
        }

        [HttpDelete("remove-member/{groupId}/{userId}")]
        public async Task<IActionResult> RemoveMember(int groupId, string userId)
        {
            var userGroup = await _context.UserGroups.FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId);
            if (userGroup == null) return NotFound("Üye bulunamadı.");

            _context.UserGroups.Remove(userGroup);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.User(userId).SendAsync("RemovedFromGroup", groupId);

            return Ok(new { Message = "Üye gruptan çıkarıldı." });
        }

        [HttpDelete("delete-user/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (id == GetCurrentUserId())
                return BadRequest(new { Message = "Kendini silemezsin." });
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");
            var messages = _context.Messages.Where(m => m.SenderId == id || m.ReceiverId == id);
            _context.Messages.RemoveRange(messages);
            await _context.SaveChangesAsync();
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded) return BadRequest(result.Errors);
            return Ok(new { Message = "Kullanıcı silindi." });
        }

        [HttpPost("broadcast")]
        public async Task<IActionResult> Broadcast([FromBody] BroadcastDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Message))
                return BadRequest(new { Message = "Mesaj boş olamaz." });

            await _hubContext.Clients.All.SendAsync("ReceiveAdminMessage", dto.Message);
            return Ok(new { Message = "Duyuru tüm kullanıcılara gönderildi." });
        }
    }

    public class BroadcastDto
    {
        public string Message { get; set; } = string.Empty;
    }
}