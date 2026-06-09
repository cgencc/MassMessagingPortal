namespace MassMessagingAPI.DTOs
{
    public class CreateGroupDto
    {
        public string Name { get; set; } = string.Empty;
    }

    public class AddUserToGroupDto
    {
        public string UserId { get; set; } = string.Empty;
        public int GroupId { get; set; }
    }

    // ✅ Used by both add-users-bulk and remove-users-bulk
    public class BulkUserGroupDto
    {
        public int GroupId { get; set; }
        public List<string> UserIds { get; set; } = new List<string>();
    }
}