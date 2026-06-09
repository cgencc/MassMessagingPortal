namespace MassMessagingAPI.DTOs
{
    public class SendMessageDto
    {
        public string? ReceiverId { get; set; }

        public int? GroupId { get; set; }

        public string Content { get; set; } = string.Empty;
    }
}