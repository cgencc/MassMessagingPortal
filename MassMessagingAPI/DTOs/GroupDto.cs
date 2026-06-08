namespace MassMessagingAPI.DTOs
{
    // FIX #4: Replaces [FromBody] string in CreateGroup — now a proper JSON object { "name": "..." }
    public class CreateGroupDto
    {
        public string Name { get; set; } = string.Empty;
    }

    // FIX #3: Used by POST /api/Group/add-user
    public class AddUserToGroupDto
    {
        public string UserId { get; set; } = string.Empty;
        public int GroupId { get; set; }
    }
}