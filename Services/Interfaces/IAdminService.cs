// Services/Interfaces/IAdminService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Services.DTOs;
using LeafLoop.Services.DTOs;

namespace LeafLoop.Services.Interfaces
{
    public interface IAdminService
    {
        // Dashboard
        Task<AdminDashboardDto> GetDashboardDataAsync();
        Task<AdminStatisticsDto> GetStatisticsAsync();
        
        // User Management - Fixed these method signatures
        Task<IEnumerable<UserManagementDto>> GetAllUsersAsync();
        Task<UserManagementDto> GetUserDetailsAsync(int userId);
        Task<bool> UpdateUserStatusAsync(int userId, bool isActive);
        Task<bool> UpdateUserRolesAsync(int userId, List<string> roles);
        
        // Admin Logging
        Task LogAdminActionAsync(int adminUserId, AdminActionDto actionDto, string ipAddress);
        Task<IEnumerable<AdminLogDto>> GetAdminLogsAsync();
        
        // System Management - Added these missing methods
        Task<SystemSettingsDto> GetSystemSettingsAsync();
        Task<bool> UpdateSystemSettingsAsync(SystemSettingsDto settings, int adminUserId);
    }
}