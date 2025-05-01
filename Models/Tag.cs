using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LeafLoop.Models
{
    public class Tag
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; }
        
        public bool IsSystem { get; set; }
        
        // Relacje
        public virtual ICollection<ItemTag> Items { get; set; }
    }
}
