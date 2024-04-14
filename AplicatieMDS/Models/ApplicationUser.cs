using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations.Schema;

namespace AplicatieMDS.Models
{
    public class ApplicationUser : IdentityUser
    {
        //un user posteaza mai multe mesaje
        public virtual ICollection<Message>? Messages { get; set; }

        // un user face parte din mai multe chat-uri
        public virtual ICollection<Chat>? Chats { get; set; }


        // atribute suplimentare adaugate pentru user
        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        // variabila in care vom retine rolurile existente in baza de date
        // pentru popularea unui dropdown list
        [NotMapped]
        public IEnumerable<SelectListItem>? AllRoles { get; set; }

<<<<<<< Updated upstream
        public ICollection<ChatUser> ChannelUsers { get; set; } = new List<ChatUser>();
=======
        public ICollection<ChatUser> ChatUsers { get; set; } = new List<ChatUser>();
>>>>>>> Stashed changes

    }
}
