using System;

namespace LeafLoop.Models
{
    public class UserBadge
    {
        public int UserId { get; set; }
        public int BadgeId { get; set; }
        public DateTime AcquiredDate { get; set; }
        
        // seems to be working 
        public virtual User User { get; set; }
        public virtual Badge Badge { get; set; }
        
        public UserBadge()
        {
            AcquiredDate = DateTime.UtcNow;
        }
    }
}
