using System.Collections.Generic;
using LeafLoop.Services.DTOs;

namespace LeafLoop.ViewModels.Home
{
    public class HomeViewModel
    {
        // Featured items (carousel)
        public List<ItemDto> FeaturedItems { get; set; } = new List<ItemDto>();
        
        // Recent items (grid)
        public List<ItemDto> RecentItems { get; set; } = new List<ItemDto>();
        
        // Top users by EcoScore
        public List<UserDto> TopUsers { get; set; } = new List<UserDto>();
        
        // Upcoming events
        public List<EventDto> UpcomingEvents { get; set; } = new List<EventDto>();
        
        // Stats summary
        public StatsSummaryDto Stats { get; set; } = new StatsSummaryDto();
    }
    
    public class StatsSummaryDto
    {
        public int TotalUsers { get; set; }
        public int TotalItems { get; set; }
        public int CompletedTransactions { get; set; }
        public int TotalEvents { get; set; }
    }
}