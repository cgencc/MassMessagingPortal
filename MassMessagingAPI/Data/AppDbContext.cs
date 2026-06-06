using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MassMessagingAPI.Models; // Kendi proje adınıza göre düzenleyin

namespace MassMessagingAPI.Data
{
    // Standart DbContext yerine IdentityDbContext kullanıyoruz ki
    // üye giriş/çıkış tabloları (AspNetUsers vb.) otomatik gelsin.
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Kendi oluşturduğumuz tabloları da buraya ekliyoruz
        public DbSet<Group> Groups { get; set; }
        public DbSet<UserGroup> UserGroups { get; set; }
        public DbSet<Message> Messages { get; set; }

        // Tablolar arası ilişkilerin (Foreign Key vs.) kurallarını belirliyoruz
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // Bunu silmeyin, Identity tabloları patlar!

            // 1. UserGroup Ara Tablosu İçin Çiftli Birincil Anahtar (Composite Key)
            builder.Entity<UserGroup>()
                .HasKey(ug => new { ug.UserId, ug.GroupId });

            // 2. Mesaj ve Gönderen İlişkisi
            // Bir mesaj silindiğinde veya kullanıcı silindiğinde birbirlerini etkilemesinler (Restrict)
            builder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            // 3. Mesaj ve Alıcı İlişkisi
            builder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}