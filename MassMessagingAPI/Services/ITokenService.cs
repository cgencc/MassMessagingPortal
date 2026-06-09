using MassMessagingAPI.Models;

namespace MassMessagingAPI.Services
{
    public interface ITokenService
    {
        Task<string> GenerateTokenAsync(AppUser user);
    }
}