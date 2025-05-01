using System;
using System.Collections.Generic;

namespace LeafLoop.Models
{
    public class Badge
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string IconPath { get; set; }
        public string RequirementCondition { get; set; }
        
        // Relacje
        public virtual ICollection<UserBadge> Users { get; set; }
        
        public Badge()
        {
            Users = new List<UserBadge>();
        }
    }
}
