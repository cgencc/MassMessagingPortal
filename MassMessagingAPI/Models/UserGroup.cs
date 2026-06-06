namespace MassMessagingAPI.Models
{
    public class UserGroup
    {
        public string UserId { get; set; } = string.Empty;
        public AppUser? User { get; set; }

        public int GroupId { get; set; }
        public Group? Group { get; set; }
    }
}