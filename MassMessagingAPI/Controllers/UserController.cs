using MassMessagingAPI.Data;
using MassMessagingAPI.DTOs;
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
        private readonly AppDbContext _context; 

        public UserController(UserManager<AppUser> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context; 
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
        [HttpPut("update-profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.Email = dto.Email;

            await _userManager.UpdateAsync(user);
            return Ok(new { message = "Profil başarıyla güncellendi." });
        }
    }
}