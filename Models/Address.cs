using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Needed for [Column] attribute

namespace LeafLoop.Models
{
    public class Address
    {
        [Key] // Explicitly defines this as the Primary Key
        public int Id { get; set; }

        [Required(ErrorMessage = "Street is required.")]
        [StringLength(100, ErrorMessage = "Street name cannot exceed 100 characters.")]
        public string Street { get; set; }

        [Required(ErrorMessage = "Building number is required.")]
        [StringLength(20, ErrorMessage = "Building number cannot exceed 20 characters.")]
        public string BuildingNumber { get; set; }

        // Apartment number is often optional; string? for explicit nullability (if Nullable Reference Types are enabled).
        [StringLength(20, ErrorMessage = "Apartment number cannot exceed 20 characters.")]
        public string? ApartmentNumber { get; set; } 

        [Required(ErrorMessage = "Postal code is required.")]
        [StringLength(20, ErrorMessage = "Postal code cannot exceed 20 characters.")] // e.g., "90210" or "SW1A 1AA"
        public string PostalCode { get; set; }

        [Required(ErrorMessage = "City is required.")]
        [StringLength(100, ErrorMessage = "City name cannot exceed 100 characters.")]
        public string City { get; set; }

        // Province/State/Region may be optional or not applicable in all countries.
        [StringLength(100, ErrorMessage = "Province/State/Region name cannot exceed 100 characters.")]
        public string? Province { get; set; } 

        [Required(ErrorMessage = "Country is required.")]
        [StringLength(100, ErrorMessage = "Country name cannot exceed 100 characters.")]
        public string Country { get; set; }

        // Geographic coordinates. Using decimal for precision suitable for mapping.
        // Nullable because geocoding might fail or coordinates might not be initially available.
        [Column(TypeName = "decimal(8, 6)")] // Precision: 8 total digits, 6 after decimal. Suitable for latitude (-90 to 90).
        public decimal? Latitude { get; set; }

        [Column(TypeName = "decimal(9, 6)")] // Precision: 9 total digits, 6 after decimal. Suitable for longitude (-180 to 180).
        public decimal? Longitude { get; set; }

        // --- Relationships ---
        // These collections allow an Address to be associated with multiple other entities.
        // By default, EF Core will create join tables for many-to-many relationships
        // or expect foreign keys in User/Company/Event entities for one-to-many relationships if configured on the other side.

        /// <summary>
        /// Users associated with this address (e.g., residents, registered at this address).
        /// </summary>
        public virtual ICollection<User> Users { get; set; } = new HashSet<User>(); // Initialize to prevent NullReferenceException on access

        /// <summary>
        /// Companies located at or associated with this address.
        /// </summary>
        public virtual ICollection<Company> Companies { get; set; } = new HashSet<Company>(); // Initialize

        /// <summary>
        /// Events taking place at or associated with this address.
        /// </summary>
        public virtual ICollection<Event> Events { get; set; } = new HashSet<Event>(); // Initialize
    }
}