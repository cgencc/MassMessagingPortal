namespace MassMessagingAPI.Models
{
    public class Message
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime SentDate { get; set; } = DateTime.Now;

        // Mesajı Gönderen (Her mesajın bir göndereni olmak zorundadır)
        public string SenderId { get; set; } = string.Empty;
        public AppUser? Sender { get; set; }

        // Birebir Mesaj ise Alıcı (Grup mesajıysa burası NULL kalır)
        public string? ReceiverId { get; set; }
        public AppUser? Receiver { get; set; }

        // Grup Mesajı ise Hangi Grup? (Birebir mesajsa burası NULL kalır)
        public int? GroupId { get; set; }
        public Group? Group { get; set; }

        public bool IsRead { get; set; } = false;
    }
}