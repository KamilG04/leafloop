
using System.ComponentModel.DataAnnotations;

namespace LeafLoop.Services.DTOs
{
    /// <summary>
    /// Data Transfer Object for updating a user's location.
    /// </summary>
    public class LocationUpdateDto
    {
        /// <summary>
        /// Geographic latitude.
        /// Must be between -90 and 90.
        /// </summary>
        [Required(ErrorMessage = "Latitude is required.")]
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
        public decimal? Latitude { get; set; }

        /// <summary>
        /// Geographic longitude.
        /// Must be between -180 and 180.
        /// </summary>
        [Required(ErrorMessage = "Longitude is required.")]
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
        public decimal? Longitude { get; set; }

        /// <summary>
        /// Search radius in kilometers.
        /// Must be between 1 and 200 km. Defaults to 10 km if not provided.
        /// </summary>
        [Range(1, 200, ErrorMessage = "Search radius must be between 1 and 200 km.")]
        public decimal SearchRadius { get; set; } = 10; // Default to 10km

        /// <summary>
        /// Optional: Name of the location (e.g., "City Park", "User's Home").
        /// This could be provided by the user or potentially auto-populated by reverse geocoding
        /// on the backend if such functionality is implemented.
        /// </summary>
        [StringLength(255, ErrorMessage = "Location name cannot exceed 255 characters.")]
        public string? LocationName { get; set; } // Explicitly nullable string (if NRTs enabled)
    }
}