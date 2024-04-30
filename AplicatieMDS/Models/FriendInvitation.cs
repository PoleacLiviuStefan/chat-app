using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AplicatieMDS.Models
{
    public class FriendInvitation
    {
        [Key]
        public int InvitationId { get; set; }

        public string SenderId { get; set; }
        [ForeignKey("SenderId")]
        public virtual ApplicationUser Sender { get; set; }

        public string ReceiverId { get; set; }
        [ForeignKey("ReceiverId")]
        public virtual ApplicationUser Receiver { get; set; }
    }

}
