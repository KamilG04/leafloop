// Models/AdminLog.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeafLoop.Models
{
    public class AdminLog
    {
        public int Id { get; set; }
        
        [Required]
        public int AdminUserId { get; set; }
        
        [ForeignKey("AdminUserId")]
        public virtual User AdminUser { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Action { get; set; }
        
        [StringLength(50)]
        public string EntityType { get; set; }
        
        public int? EntityId { get; set; }
        
        [StringLength(500)]
        public string Details { get; set; }
        
        [StringLength(45)]
        public string IPAddress { get; set; }
        
        public DateTime ActionDate { get; set; }
    }
}