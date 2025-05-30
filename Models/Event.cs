using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LeafLoop.Models
{
    public enum OrganizerType
    {
        User,
        Company
    }
    
    public class Event
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; }
        
        public string Description { get; set; }
        
        public DateTime StartDate { get; set; }
        
        public DateTime EndDate { get; set; }
        
        public int ParticipantsLimit { get; set; }
        
        public int? AddressId { get; set; }
        
        public int OrganizerId { get; set; }
        
        public OrganizerType OrganizerType { get; set; }
        /*
        public void SetStartDate(DateTime date)
        {
            StartDate = date.Kind == DateTimeKind.Utc ? date : DateTime.SpecifyKind(date, DateTimeKind.Utc);
        }
        */ //to plus ten null od daty to perm fix ale japierdole 
        public virtual Address Address { get; set; }
        public virtual ICollection<EventParticipant> Participants { get; set; }
    }
}
