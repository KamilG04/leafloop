using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LeafLoop.Models
{
    public class Category
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; }
        
        public string Description { get; set; }
        
        public string IconPath { get; set; }
        
        // Relacje
        public virtual ICollection<Item> Items { get; set; }
    }
}
