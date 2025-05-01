using System;

namespace LeafLoop.Models
{
    public enum CommentContentType
    {
        Item,
        Event,
        User
    }
    
    public class Comment
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime AddedDate { get; set; }
        public CommentContentType ContentType { get; set; }
        public int ContentId { get; set; }
        public int UserId { get; set; }
        
        // Relacje
        public virtual User User { get; set; }
        
        public Comment()
        {
            AddedDate = DateTime.UtcNow;
        }
    }
}
