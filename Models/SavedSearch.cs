using System;

namespace LeafLoop.Models
{
    public class SavedSearch
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string SearchParameters { get; set; } // JSON format think bout search engines
        public DateTime CreatedDate { get; set; }
        public int UserId { get; set; }
        public virtual User User { get; set; }
        
        public SavedSearch()
        {
            CreatedDate = DateTime.UtcNow;
        }
    }
}
