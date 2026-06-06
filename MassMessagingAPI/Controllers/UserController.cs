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
    [Authorize] // Sadece giriş yapanlar görebilir
    public class UserController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;

        public UserController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetUsersForChat()
        {
            // İsteği atan kişinin ID'sini bul
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Kendimiz hariç diğer herkesin ID'sini ve Ad Soyadını getir
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
    }
}