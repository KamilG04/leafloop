using System;

namespace LeafLoop.Services.DTOs
{
    public class PhotoDto
    {
        public int Id { get; set; }
        public string Path { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public DateTime AddedDate { get; set; }
        public int ItemId { get; set; }
    }

    public class PhotoCreateDto
    {
        public string Path { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public int ItemId { get; set; }
    }
}
