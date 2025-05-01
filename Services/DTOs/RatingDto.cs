using System;
using LeafLoop.Models;

namespace LeafLoop.Services.DTOs
{
    public class RatingDto
    {
        public int Id { get; set; }
        public int Value { get; set; }
        public string Comment { get; set; }
        public DateTime RatingDate { get; set; }
        public int RaterId { get; set; }
        public string RaterName { get; set; }
        public int RatedEntityId { get; set; }
        public string RatedEntityName { get; set; }
        public RatedEntityType RatedEntityType { get; set; }
        public int? TransactionId { get; set; }
    }

    public class RatingCreateDto
    {
        public int Value { get; set; }
        public string Comment { get; set; }
        public int RaterId { get; set; }
        public int RatedEntityId { get; set; }
        public RatedEntityType RatedEntityType { get; set; }
        public int? TransactionId { get; set; }
    }

    public class RatingUpdateDto
    {
        public int Id { get; set; }
        public int Value { get; set; }
        public string Comment { get; set; }
    }
}
