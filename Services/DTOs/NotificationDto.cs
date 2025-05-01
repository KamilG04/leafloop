using System;

namespace LeafLoop.Services.DTOs
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Content { get; set; }
        public bool IsRead { get; set; }
        public DateTime SentDate { get; set; }
        public int UserId { get; set; }
    }

    public class NotificationCreateDto
    {
        public string Type { get; set; }
        public string Content { get; set; }
        public int UserId { get; set; }
    }
}
