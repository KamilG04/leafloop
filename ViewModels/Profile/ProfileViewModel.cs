// Path: LeafLoop/ViewModels/Profile/ProfileViewModel.cs

using LeafLoop.Services.DTOs; // For ItemDto, AddressDto, BadgeDto
using System;
using System.Collections.Generic;

namespace LeafLoop.ViewModels.Profile
{
    /// <summary>
    /// ViewModel representing the data needed to display a user's profile page.
    /// </summary>
    public class ProfileViewModel
    {
        public int UserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; } // Might be needed for display
        public string? AvatarPath { get; set; }
        public int EcoScore { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastActivity { get; set; } // If you want to display it

        // Data typically from UserWithDetailsDto (optional, if needed directly)
        public AddressDto? Address { get; set; } // User's primary address
        public decimal SearchRadius { get; set; } // User's preferred search radius - ADDED FOR COMPLETENESS if needed by view/JS directly
        public double AverageRating { get; set; }
        public List<BadgeDto> Badges { get; set; } = new List<BadgeDto>();

        // Data specific to the profile view, often aggregated
        public List<ItemDto> RecentItems { get; set; } = new List<ItemDto>();
        public int TotalItemsCount { get; set; }
        public int TotalTransactionsCount { get; set; }

        /// <summary>
        /// Indicates if the profile being viewed belongs to the currently authenticated user.
        /// This allows the view to show/hide certain elements (e.g., "Edit Profile" button).
        /// </summary>
        public bool IsCurrentUserProfile { get; set; } // <<< --- DODANA WŁAŚCIWOŚĆ --- >>>
        
        // You can add more statistics/data if needed for the view
    }
}