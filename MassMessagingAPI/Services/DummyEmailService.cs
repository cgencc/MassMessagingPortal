namespace MassMessagingAPI.Services
{
    public class DummyEmailService : IEmailService
    {
        public Task SendEmailAsync(string toEmail, string subject, string body)
        {

            Console.WriteLine("\n=========================================");
            Console.WriteLine($"DUMMY MAIL GÖNDERİLDİ!");
            Console.WriteLine($"Kime: {toEmail}");
            Console.WriteLine($"Konu: {subject}");
            Console.WriteLine($"İçerik/Link: \n{body}");
            Console.WriteLine("=========================================\n");

            return Task.CompletedTask;
        }
    }
}