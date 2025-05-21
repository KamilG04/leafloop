using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LeafLoop.Models
{
    public class Item
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; }
        
        public string Description { get; set; }
        
        public string Condition { get; set; }
        
        public DateTime DateAdded { get; set; }
        
        public bool IsAvailable { get; set; }
        
        public bool IsForExchange { get; set; }
        
        public decimal ExpectedValue { get; set; }
        
        public int UserId { get; set; }
        
        public int CategoryId { get; set; }
        
        
        
        // Relacje
        public virtual User User { get; set; }
        public virtual Category Category { get; set; }
        public virtual ICollection<Photo> Photos { get; set; }
        public virtual ICollection<ItemTag> Tags { get; set; }
        public virtual ICollection<Transaction> Transactions { get; set; }
        
        
    }
    
}
