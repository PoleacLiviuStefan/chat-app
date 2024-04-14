using System.ComponentModel.DataAnnotations;

namespace AplicatieMDS.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Continutul mesajului este obligatoriu")]
        public string Content { get; set; }

        public DateTime Date { get; set; }

        // un mesaj apartine unui chat
        public int? ChatId { get; set; }

        // un mesaj este postat de catre un user
        public string? UserId { get; set; }

        public virtual ApplicationUser? User { get; set; }

        public virtual Chat? Chat { get; set; }


    }
}
