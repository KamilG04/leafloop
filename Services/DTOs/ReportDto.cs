using System;
using LeafLoop.Models;

namespace LeafLoop.Services.DTOs
{
    public class ReportDto
    {
        public int Id { get; set; }
        public string Reason { get; set; }
        public string Description { get; set; }
        public ReportStatus Status { get; set; }
        public DateTime ReportDate { get; set; }
        public ContentType ContentType { get; set; }
        public int ContentId { get; set; }
        public int ReporterId { get; set; }
        public string ReporterName { get; set; }
    }

    public class ReportCreateDto
    {
        public string Reason { get; set; }
        public string Description { get; set; }
        public ContentType ContentType { get; set; }
        public int ContentId { get; set; }
        public int ReporterId { get; set; }
    }
}
