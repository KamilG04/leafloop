using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LeafLoop.Models
{
    public class Address
    {
        public int Id { get; set; }
        
        public string Street { get; set; }
        
        public string BuildingNumber { get; set; }
        
        public string ApartmentNumber { get; set; }
        
        public string PostalCode { get; set; }
        
        public string City { get; set; }
        
        public string Province { get; set; }
        
        public string Country { get; set; }
        
        public double Latitude { get; set; }
        
        public double Longitude { get; set; }
        
        // Relacje
        public virtual ICollection<User> Users { get; set; }
        public virtual ICollection<Company> Companies { get; set; }
        public virtual ICollection<Event> Events { get; set; }
    }
}
