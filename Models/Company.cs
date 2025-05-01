using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LeafLoop.Models
{
    public enum VerificationStatus
    {
        Pending,
        Verified,
        Rejected
    }
    
    public class Company
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; }
        
        public string NIP { get; set; }
        
        public string REGON { get; set; }
        
        public string Description { get; set; }
        
        public string LogoPath { get; set; }
        
        public DateTime JoinDate { get; set; }
        
        public VerificationStatus VerificationStatus { get; set; }
        
        public int AddressId { get; set; }
        
        // Relacje
        public virtual Address Address { get; set; }
        public virtual ICollection<Rating> Ratings { get; set; }
    }
}
