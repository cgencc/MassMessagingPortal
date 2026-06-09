using MassMessagingAPI.DTOs;
using MassMessagingAPI.Models;
using MassMessagingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Web;

namespace MassMessagingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService; 

        public AuthController(UserManager<AppUser> userManager, ITokenService tokenService, IEmailService emailService)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _emailService = emailService;
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
                var token = await _tokenService.GenerateTokenAsync(user);
                return Ok(new { Token = token, Message = "Giriş başarılı!" });
            }

            return Unauthorized(new { Message = "E-posta veya şifre hatalı!" });
        }
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return Ok(new { Message = "Şifre sıfırlama bağlantısı gönderildi (Eğer e-posta doğruysa)." });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = HttpUtility.UrlEncode(token);

            var resetLink = $"https://localhost:7261/Auth/ResetPassword?email={model.Email}&token={encodedToken}";

            await _emailService.SendEmailAsync(model.Email, "MassPortal - Şifre Sıfırlama", resetLink);

            return Ok(new { Message = "Şifre sıfırlama bağlantısı gönderildi (Eğer e-posta doğruysa)." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return BadRequest(new { Message = "Geçersiz işlem." });

            var decodedToken = HttpUtility.UrlDecode(model.Token);
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.NewPassword);

            if (result.Succeeded)
                return Ok(new { Message = "Şifreniz başarıyla güncellendi!" });

            return BadRequest(result.Errors);
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Unauthorized();

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (result.Succeeded)
                return Ok(new { Message = "Şifreniz başarıyla değiştirildi." });

            return BadRequest(new { Message = "Eski şifreniz yanlış veya yeni şifre kurallara uymuyor." });
        }
    }
}