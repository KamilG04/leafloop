using System;
using System.ComponentModel.DataAnnotations;

namespace LeafLoop.Models
{
    public class Notification
    {
        public int Id { get; set; }
        
        public string Type { get; set; }
        
        [Required]
        public string Content { get; set; }
        
        public bool IsRead { get; set; }
        
        public DateTime SentDate { get; set; }
        
        public int UserId { get; set; }
        
        // Relacje
        public virtual User User { get; set; }
    }
}
