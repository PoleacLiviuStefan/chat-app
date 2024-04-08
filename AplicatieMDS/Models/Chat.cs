using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Threading.Channels;

namespace AplicatieMDS.Models
{
    public class Chat
    {

        [Key]
        public int Id { get; set; }

        // un canal este creat de catre un user - FK
        public string? UserId { get; set; }

        public virtual ApplicationUser? User { get; set; }

        // un chat poate avea o colectie de mesaje
        public virtual ICollection<Message>? Messages { get; set; }

        public ICollection<ChatUser> ChatUsers { get; set; } = new List<ChatUser>();


    }
}
