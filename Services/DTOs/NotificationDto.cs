using System;
using System.ComponentModel.DataAnnotations;

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
    public class SystemNotificationDto
    {
        [Required]
        public string Type { get; set; } = null!; // Użyj null-forgiving operator lub inicjalizacji

        [Required]
        public string Content { get; set; } = null!;

        // Może być null, jeśli powiadomienie jest globalne
        public IEnumerable<int>? UserIds { get; set; }
    }
    public class NotificationCreateDto
    {
        public string Type { get; set; }
        public string Content { get; set; }
        public int UserId { get; set; }
    }
}
