using System;

namespace LeafLoop.Models
{
    public class SavedSearch
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string SearchParameters { get; set; } // JSON format
        public DateTime CreatedDate { get; set; }
        public int UserId { get; set; }
        
        // asddfsfadth
        public virtual User User { get; set; }
        
        public SavedSearch()
        {
            CreatedDate = DateTime.UtcNow;
        }
    }
}
