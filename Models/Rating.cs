using System;
using System.ComponentModel.DataAnnotations;

namespace LeafLoop.Models
{
    public enum RatedEntityType
    {
        User,
        Company
    }
    
    public class Rating
    {
        public int Id { get; set; }
        
        [Range(1, 5)]
        public int Value { get; set; }
        
        public string Comment { get; set; }
        
        public DateTime RatingDate { get; set; }
        
        public int RaterId { get; set; }
        
        public int RatedEntityId { get; set; }
        
        public RatedEntityType RatedEntityType { get; set; }
        
        public int? TransactionId { get; set; }
        
        // Relacje
        public virtual User Rater { get; set; }
        public virtual Transaction Transaction { get; set; }
    }
}
