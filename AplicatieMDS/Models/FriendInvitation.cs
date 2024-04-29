using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AplicatieMDS.Models
{
    public class FriendInvitation
    {
        [Key]
        public int InvitationId { get; set; } // Primary key for the invitation

        public string SenderId { get; set; } // ID of the user who sent the invitation
        [ForeignKey("SenderId")]
        public virtual ApplicationUser Sender { get; set; }

        public string ReceiverId { get; set; } // ID of the user who received the invitation
        [ForeignKey("ReceiverId")]
        public virtual ApplicationUser Receiver { get; set; }
    }
}
