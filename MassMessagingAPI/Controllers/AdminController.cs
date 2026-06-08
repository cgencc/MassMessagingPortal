using MassMessagingAPI.Data;
using MassMessagingAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MassMessagingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager; // Tanımlama eklendi
        private readonly AppDbContext _context;

        public AdminController(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager, // Constructor'a eklendi
            AppDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager; // Atama yapıldı
            _context = context;
        }

        // GET: api/admin/users
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userManager.Users
                .Select(u => new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.Email
                })
                .ToListAsync();

            return Ok(users);
        }

        // POST: api/admin/assign-role/{userId}/{role}
        [HttpPost("assign-role/{userId}/{role}")]
        public async Task<IActionResult> AssignRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            // Rolün var olup olmadığını kontrol et
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }

            // Rolü ata
            var result = await _userManager.AddToRoleAsync(user, role);

            if (result.Succeeded)
                return Ok(new { message = "Rol atandı." });
            else
                return BadRequest(result.Errors);
        }
        // --- SİLME VE YETKİ ALMA İÇİN GÜNCEL METOTLAR ---

        [HttpPost("remove-role/{userId}/{role}")]
        public async Task<IActionResult> RemoveRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            await _userManager.RemoveFromRoleAsync(user, role);
            return Ok(new { message = "Yetki başarıyla alındı." });
        }

        [HttpDelete("delete-user/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // 1. Kullanıcının mesajlarını sil (FK hatasını engellemek için)
            var userMessages = _context.Messages.Where(m => m.SenderId == id || m.ReceiverId == id);
            _context.Messages.RemoveRange(userMessages);

            // 2. Kullanıcıyı sil
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded) return BadRequest(result.Errors);

            await _context.SaveChangesAsync();
            return Ok(new { message = "Kullanıcı ve verileri silindi." });
        }
    }
}