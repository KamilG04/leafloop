// Path: LeafLoop/Services/DTOs/ItemSummaryDto.cs
using System;

namespace LeafLoop.Services.DTOs
{
    /// <summary>
    /// Represents a summary of an item, typically used in lists or search results.
    /// </summary>
    public class ItemSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string MainPhotoPath { get; set; } // URL or path to the primary photo
        public string Condition { get; set; }
        public bool IsForExchange { get; set; }
        public decimal ExpectedValue { get; set; }
        public DateTime DateAdded { get; set; }
        public bool IsAvailable { get; set; }

        public int UserId { get; set; }
        public string UserName { get; set; } // Name of the user who listed the item

        // Location-related info (denormalized from User's Address for display)
        public string City { get; set; }
        public string Country { get; set; } // Optional, but can be useful
        public decimal? Latitude { get; set; } // Item's location (from user's address)
        public decimal? Longitude { get; set; } // Item's location (from user's address)
        public string CategoryName { get; set; } // <<< --- TA WŁAŚCIWOŚĆ JEST KLUCZOWA --- >>>

        // public decimal? DistanceKm { get; set; } // if we want to calculate distance
}
}