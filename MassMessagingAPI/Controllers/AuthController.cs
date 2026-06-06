using MassMessagingAPI.DTOs;
using MassMessagingAPI.Models;
using MassMessagingAPI.Services; // Servisi kullanmak için
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MassMessagingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ITokenService _tokenService; // Token servisimizi buraya çağırdık

        // IConfiguration yerine ITokenService kullanıyoruz
        public AuthController(UserManager<AppUser> userManager, ITokenService tokenService)
        {
            _userManager = userManager;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            var user = new AppUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
                return Ok(new { Message = "Kayıt başarılı!" });

            return BadRequest(result.Errors);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                // Uzun uzun kod yazmak yerine temiz bir şekilde servisi çağırıyoruz
                var token = _tokenService.GenerateToken(user);
                return Ok(new { Token = token, Message = "Giriş başarılı!" });
            }

            return Unauthorized(new { Message = "E-posta veya şifre hatalı!" });
        }
    }
}