using System;
using System.ComponentModel.DataAnnotations;

namespace LeafLoop.Models
{
    public class Photo
    {
        public int Id { get; set; }
        
        [Required]
        public string Path { get; set; }
        
        public string FileName { get; set; }
        
        public long FileSize { get; set; }
        
        public DateTime AddedDate { get; set; }
        
        public int ItemId { get; set; }
        
        // Relacje
        public virtual Item Item { get; set; }
    }
}
