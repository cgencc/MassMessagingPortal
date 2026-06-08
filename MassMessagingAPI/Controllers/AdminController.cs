using MassMessagingAPI.Data;
using MassMessagingAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims; // ClaimTypes için gerekli

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

        public AdminController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, AppDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // Helper: İşlemi yapan adminin ID'sini almak
        private string GetCurrentUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var userList = new List<object>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new
                {
                    user.Id,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    Roles = roles
                });
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
            var currentUserId = GetCurrentUserId();

            // GÜVENLİK KONTROLÜ: Admin kendi yetkisini kaldıramaz
            if (userId == currentUserId)
                return BadRequest("Kendi yetkilerini kaldıramazsın.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            if (!await _userManager.IsInRoleAsync(user, role))
                return BadRequest("Kullanıcı bu role sahip değil.");

            var result = await _userManager.RemoveFromRoleAsync(user, role);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok(new { Message = "Rol başarıyla kaldırıldı." });
        }

        [HttpDelete("delete-user/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var currentUserId = GetCurrentUserId();

            // GÜVENLİK KONTROLÜ: Admin kendini silemez
            if (id == currentUserId)
                return BadRequest("Kendini silemezsin.");

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            // Kullanıcı ile ilgili verileri temizle
            var messages = _context.Messages.Where(m => m.SenderId == id || m.ReceiverId == id);
            _context.Messages.RemoveRange(messages);
            await _context.SaveChangesAsync();

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok(new { Message = "Kullanıcı silindi." });
        }

    }
}