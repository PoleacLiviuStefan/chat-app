using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AplicatieMDS.Models
{
    public class Message
    {
        public enum MessageStatus
        {
            Delivered = 0,
            Unseen = 1,
            Seen = 2
        }

        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Continutul mesajului este obligatoriu")]
        public string Content { get; set; }

        public DateTime Date { get; set; }

        public MessageStatus Status { get; set; }

        // un mesaj apartine unui chat
        public int? ChatId { get; set; }

        // un mesaj este postat de catre un user
        public string? UserId { get; set; }

        public virtual ApplicationUser? User { get; set; }

        public virtual Chat? Chat { get; set; }
    }
}
