using MassMessagingAPI.Data; // AppDbContext için gerekli
using MassMessagingAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MassMessagingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _context; // Context tanımı eklendi

        public UserController(UserManager<AppUser> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context; // Enjeksiyon tamamlandı
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetUsersForChat()
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var users = await _userManager.Users
                .Where(u => u.Id != currentUserId)
                .Select(u => new
                {
                    u.Id,
                    FullName = u.FirstName + " " + u.LastName
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            return Ok(new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email
            });
        }
    }
}