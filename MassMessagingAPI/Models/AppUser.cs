using Microsoft.AspNetCore.Identity;

namespace MassMessagingAPI.Models 
{
    public class AppUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public ICollection<Message>? SentMessages { get; set; }
        public ICollection<Message>? ReceivedMessages { get; set; }

        public ICollection<UserGroup>? UserGroups { get; set; }
    }
}