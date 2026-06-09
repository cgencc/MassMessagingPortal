namespace MassMessagingAPI.Models
{
    public class Group
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public ICollection<UserGroup>? UserGroups { get; set; }
        public ICollection<Message>? Messages { get; set; }
    }
}