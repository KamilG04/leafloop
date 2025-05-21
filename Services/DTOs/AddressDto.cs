using System.ComponentModel.DataAnnotations; 

namespace LeafLoop.Services.DTOs
{
    public class AddressDto
    {
        public int Id { get; set; }

        [StringLength(100)] 
        public string Street { get; set; }

        [StringLength(20)]  
        public string BuildingNumber { get; set; }

        [StringLength(20)]  
        public string ApartmentNumber { get; set; }

        [StringLength(20)]  
        public string PostalCode { get; set; }

        [StringLength(100)] 
        public string City { get; set; }

        [StringLength(100)]
        public string Province { get; set; }

        [StringLength(100)] 
        public string Country { get; set; }

      
        public decimal? Latitude { get; set; }  

        public decimal? Longitude { get; set; } 


        public decimal SearchRadius { get; set; } = 10; 

        public bool HasCoordinates => Latitude.HasValue && Longitude.HasValue; 
    }
}