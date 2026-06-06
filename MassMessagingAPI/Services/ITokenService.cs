using MassMessagingAPI.Models;

namespace MassMessagingAPI.Services
{
    public interface ITokenService
    {
        string GenerateToken(AppUser user);
    }
}