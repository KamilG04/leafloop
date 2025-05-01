using System;

namespace LeafLoop.Models
{
    public enum ReportStatus
    {
        Pending,
        InProgress,
        Resolved,
        Rejected
    }
    
    public enum ContentType
    {
        User,
        Item,
        Comment,
        Event
    }
    
    public class Report
    {
        public int Id { get; set; }
        public string Reason { get; set; }
        public string Description { get; set; }
        public ReportStatus Status { get; set; }
        public DateTime ReportDate { get; set; }
        public ContentType ContentType { get; set; }
        public int ContentId { get; set; }
        public int ReporterId { get; set; }
        
        // Relacje
        public virtual User Reporter { get; set; }
        
        public Report()
        {
            ReportDate = DateTime.UtcNow;
            Status = ReportStatus.Pending;
        }
    }
}
