using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace AplicatieMDS.Models
{
    public class Chat
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("CurrentUser")]
        public string CurrentUserId { get; set; }
        public virtual ApplicationUser CurrentUser { get; set; }

        [ForeignKey("FriendUser")]
        public string FriendUserId { get; set; }
        public virtual ApplicationUser FriendUser { get; set; }

        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
        public virtual ICollection<ChatUser> ChatUsers { get; set; } = new List<ChatUser>();

        [NotMapped]
        public Message? LastMessage => Messages?.OrderByDescending(m => m.Date).FirstOrDefault();

        [NotMapped]
        public Message.MessageStatus? LastMessageStatus => LastMessage?.Status;
    }
}
