using Microsoft.AspNetCore.Identity;

namespace MassMessagingAPI.Models // Kendi proje adınıza göre MassMessagingAPI kısmını düzenleyin
{
    public class AppUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        // Birebir mesajlaşma için ilişkiler
        public ICollection<Message>? SentMessages { get; set; }
        public ICollection<Message>? ReceivedMessages { get; set; }

        // Çoklu mesajlaşma (Gruplar) için ilişki
        public ICollection<UserGroup>? UserGroups { get; set; }
    }
}