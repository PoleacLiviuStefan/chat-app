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

        public Message? LastMessage
        {
            get
            {
                // Sortăm mesajele în funcție de data și ora trimiterei în ordine descrescătoare
                var sortedMessages = Messages?.OrderByDescending(m => m.Date);
                // Returnăm primul mesaj din colecția sortată sau null dacă colecția este null sau goală
                return sortedMessages?.FirstOrDefault();
            }
        }

        // Starea ultimului mesaj trimis (de exemplu, "nevizualizat", "văzut")
        public AplicatieMDS.Models.Message.MessageStatus? LastMessageStatus
        {
            get
            {
                // Obținem ultimul mesaj
                var lastMessage = LastMessage;
                // Dacă există un ultim mesaj, returnăm starea acestuia
                if (lastMessage != null)
                    return lastMessage.Status;
                else
                    return null;
            }
        }



        // un chat poate avea o colectie de mesaje
        public virtual ICollection<Message>? Messages { get; set; }

        public ICollection<ChatUser> ChatUsers { get; set; } = new List<ChatUser>();


    }
}
