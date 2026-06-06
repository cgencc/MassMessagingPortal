namespace MassMessagingAPI.DTOs
{
    public class SendMessageDto
    {
        // Eğer birebir mesaj atılıyorsa bu alan dolu gelecek
        public string? ReceiverId { get; set; }

        // Eğer gruba toplu mesaj atılıyorsa bu alan dolu gelecek
        public int? GroupId { get; set; }

        public string Content { get; set; } = string.Empty;
    }
}