﻿using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AplicatieMDS.Models;

namespace AplicatieMDS.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ApplicationUser> Users { get; set; } // Renamed from ApplicationUsers to Users
        public DbSet<Chat> Chats { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<ChatUser> ChatUsers { get; set; }
        public DbSet<UserFriend> UserFriends { get; set; }
        public DbSet<FriendInvitation> FriendInvitations { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configurations for ChatUser
            builder.Entity<ChatUser>()
                .HasKey(cu => new { cu.ChatId, cu.UserId });

            builder.Entity<ChatUser>()
                .HasOne(cu => cu.Chat)
                .WithMany(c => c.ChatUsers)
                .HasForeignKey(cu => cu.ChatId);

            builder.Entity<ChatUser>()
                .HasOne(cu => cu.User)
                .WithMany(u => u.ChatUsers)
                .HasForeignKey(cu => cu.UserId);

            // Configurations for UserFriend
            builder.Entity<UserFriend>()
                .HasKey(uf => uf.Id);

            builder.Entity<UserFriend>()
                .HasOne(uf => uf.User)
                .WithMany(u => u.UserFriends)
                .HasForeignKey(uf => uf.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserFriend>()
                .HasOne(uf => uf.Friend)
                .WithMany(u => u.FriendOf)
                .HasForeignKey(uf => uf.FriendId)
                .OnDelete(DeleteBehavior.Restrict);

            // Add configuration for FriendInvitation
            builder.Entity<FriendInvitation>()
                .HasKey(fi => fi.InvitationId);

            builder.Entity<FriendInvitation>()
                .HasOne(fi => fi.Sender)
                .WithMany(u => u.SentInvitations)
                .HasForeignKey(fi => fi.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FriendInvitation>()
                .HasOne(fi => fi.Receiver)
                .WithMany(u => u.ReceivedInvitations)
                .HasForeignKey(fi => fi.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configurations for Chat
            builder.Entity<Chat>()
                .HasOne(c => c.CurrentUser)
                .WithMany(u => u.ChatsCreated)
                .HasForeignKey(c => c.CurrentUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Chat>()
                .HasOne(c => c.FriendUser)
                .WithMany(u => u.ChatsParticipated)
                .HasForeignKey(c => c.FriendUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
