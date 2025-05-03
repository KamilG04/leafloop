using LeafLoop.Services.DTOs; // Dla ItemDto i AddressDto
using System;
using System.Collections.Generic;

namespace LeafLoop.ViewModels.Profile
{
    public class ProfileViewModel
    {
        public int UserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; } // Może być potrzebne
        public string? AvatarPath { get; set; }
        public int EcoScore { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastActivity { get; set; } // Jeśli chcesz wyświetlić

        // Dane z UserWithDetailsDto (opcjonalnie, jeśli potrzebujesz)
        public AddressDto? Address { get; set; }
        public double AverageRating { get; set; }
        public List<BadgeDto> Badges { get; set; } = new List<BadgeDto>();

        // Dane specyficzne dla widoku profilu
        public List<ItemDto> RecentItems { get; set; } = new List<ItemDto>();
        public int TotalItemsCount { get; set; }
        public int TotalTransactionsCount { get; set; }
        // Możesz dodać więcej statystyk/danych, jeśli są potrzebne
    }
}