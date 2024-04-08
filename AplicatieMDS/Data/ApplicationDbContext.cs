using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AplicatieMDS.Models;
using System.Threading.Channels;

namespace AplicatieMDS.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<ChatUser> ChatUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ChatUser>()
            .HasKey(cu => new { cu.ChatId, cu.UserId });

            builder.Entity<ChatUser>()
                .HasOne(cu => cu.Chat)
                .WithMany(c => c.ChatUsers)
                .HasForeignKey(cu => cu.ChatId);

            builder.Entity<ChatUser>()
                .HasOne(cu => cu.User)
                .WithMany(u => u.ChannelUsers)
                .HasForeignKey(cu => cu.UserId);
        }

    }
}