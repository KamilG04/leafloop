using System;
using System.ComponentModel.DataAnnotations;

namespace LeafLoop.Models
{
    public class Message
    {
        public int Id { get; set; }
        
        [Required]
        public string Content { get; set; }
        
        public DateTime SentDate { get; set; }
        
        public bool IsRead { get; set; }
        
        public int SenderId { get; set; }
        
        public int ReceiverId { get; set; }
        
        public int? TransactionId { get; set; }
        
        // Relacje
        public virtual User Sender { get; set; }
        public virtual User Receiver { get; set; }
        public virtual Transaction Transaction { get; set; }
    }
}
