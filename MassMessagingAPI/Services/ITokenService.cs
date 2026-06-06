using MassMessagingAPI.Models;

namespace MassMessagingAPI.Services
{
    public interface ITokenService
    {
        // Geri dönüş tipini Task<string> yaparak asenkron hale getirdik
        Task<string> GenerateTokenAsync(AppUser user);
    }
}