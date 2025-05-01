using System;

namespace LeafLoop.Models
{
    public enum SubscriptionContentType
    {
        Category,
        Tag
    }
    
    public class Subscription
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public SubscriptionContentType ContentType { get; set; }
        public int ContentId { get; set; }
        public DateTime CreatedDate { get; set; }
        
        // Relacje
        public virtual User User { get; set; }
        
        public Subscription()
        {
            CreatedDate = DateTime.UtcNow;
        }
    }
}
