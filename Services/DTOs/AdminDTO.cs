// Services/DTOs/AdminDtos.cs
using System;
using System.Collections.Generic;

namespace LeafLoop.Services.DTOs
{
    public class AdminLogDto
    {
        public int Id { get; set; }
        public int AdminUserId { get; set; }
        public string AdminUserName { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public int? EntityId { get; set; }
        public string Details { get; set; }
        public string IPAddress { get; set; }
        public DateTime ActionDate { get; set; }
    }

    public class AdminDashboardDto
    {
        public AdminStatisticsDto Statistics { get; set; }
        public List<UserDto> RecentUsers { get; set; }
        public List<ItemDto> RecentItems { get; set; }
        public List<TransactionDto> RecentTransactions { get; set; }
        public List<AdminLogDto> RecentAdminActions { get; set; }
        public Dictionary<string, int> ActionSummary { get; set; }
    }

    public class AdminStatisticsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalItems { get; set; }
        public int ActiveItems { get; set; }
        public int TotalTransactions { get; set; }
        public int CompletedTransactions { get; set; }
        public int PendingReports { get; set; }
        public int UnreadMessages { get; set; }

        // Time-based stats
        public int UsersToday { get; set; }
        public int ItemsToday { get; set; }
        public int TransactionsToday { get; set; }
    }

    public class UserManagementDto
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool IsActive { get; set; }
        public bool LockoutEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public List<string> Roles { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastActivity { get; set; }
        public int EcoScore { get; set; }
    }

    public class AdminActionDto
    {
        public string Action { get; set; }
        public string EntityType { get; set; }
        public int? EntityId { get; set; }
        public string Details { get; set; }
    }

    public class BulkActionResultDto
    {
        public int TotalProcessed { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> Errors { get; set; }
    }

    public class SystemSettingsDto
    {
        public Dictionary<string, string> Settings { get; set; }
        public DateTime LastUpdated { get; set; }
        public string LastUpdatedBy { get; set; }
    }
}