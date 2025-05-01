using System;
using LeafLoop.Models;

namespace LeafLoop.Services.DTOs
{
    public class SubscriptionDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public SubscriptionContentType ContentType { get; set; }
        public int ContentId { get; set; }
        public string ContentName { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class SubscriptionCreateDto
    {
        public int UserId { get; set; }
        public SubscriptionContentType ContentType { get; set; }
        public int ContentId { get; set; }
    }
}
