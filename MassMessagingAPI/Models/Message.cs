namespace MassMessagingAPI.Models
{
    public class Message
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime SentDate { get; set; } = DateTime.Now;

        public string SenderId { get; set; } = string.Empty;
        public AppUser? Sender { get; set; }

        public string? ReceiverId { get; set; }
        public AppUser? Receiver { get; set; }

        public int? GroupId { get; set; }
        public Group? Group { get; set; }

        public bool IsRead { get; set; } = false;
        public bool IsDeleted { get; set; } = false; 
        public bool IsEdited { get; set; } = false; 
    }
}