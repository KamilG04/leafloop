// Repositories/Interfaces/IAdminRepository.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;

namespace LeafLoop.Repositories.Interfaces
{
    public interface IAdminRepository : IRepository<AdminLog>
    {
        Task<IEnumerable<AdminLog>> GetAdminLogsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<AdminLog>> GetAdminLogsByUserAsync(int adminUserId);
        Task<IEnumerable<AdminLog>> GetAdminLogsByActionAsync(string action);
        Task<Dictionary<string, int>> GetActionSummaryAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<IEnumerable<AdminLog>> GetRecentLogsAsync(int count = 100);
    }
}