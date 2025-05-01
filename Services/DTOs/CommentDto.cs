using System;
using LeafLoop.Models;

namespace LeafLoop.Services.DTOs
{
    public class CommentDto
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime AddedDate { get; set; }
        public CommentContentType ContentType { get; set; }
        public int ContentId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
    }

    public class CommentCreateDto
    {
        public string Content { get; set; }
        public CommentContentType ContentType { get; set; }
        public int ContentId { get; set; }
        public int UserId { get; set; }
    }

    public class CommentUpdateDto
    {
        public int Id { get; set; }
        public string Content { get; set; }
    }
}
