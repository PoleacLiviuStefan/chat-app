using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace AplicatieMDS.Models
{
    public class ApplicationUser : IdentityUser
    {
        // User posts many messages
        public virtual ICollection<Message>? Messages { get; set; }

        // User is part of many chats
        public virtual ICollection<ChatUser> ChatUsers { get; set; } = new List<ChatUser>();

        // Additional attributes added for the user
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public virtual ICollection<Chat> ChatsCreated { get; set; } = new List<Chat>();
        public virtual ICollection<Chat> ChatsParticipated { get; set; } = new List<Chat>();

        // Variable to hold existing roles in the database for populating a dropdown list
        [NotMapped]
        public IEnumerable<SelectListItem>? AllRoles { get; set; }

        // New property to store additional information in JSON format
        // Can include various key-value pairs
        public string? AdditionalInfoJson { get; set; }

        // Helper method to deserialize JSON into a manageable object
        [NotMapped]
        public Dictionary<string, object>? AdditionalInfo
        {
            get => string.IsNullOrEmpty(AdditionalInfoJson) ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(AdditionalInfoJson);
            set => AdditionalInfoJson = JsonSerializer.Serialize(value);
        }

        public virtual ICollection<UserFriend> UserFriends { get; set; }
        public virtual ICollection<UserFriend> FriendOf { get; set; }

        public virtual ICollection<FriendInvitation> SentInvitations { get; set; } = new List<FriendInvitation>();
        public virtual ICollection<FriendInvitation> ReceivedInvitations { get; set; } = new List<FriendInvitation>();
    }
}
