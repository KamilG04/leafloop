// Repositories/AdminRepository.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LeafLoop.Data;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LeafLoop.Repositories
{
    public class AdminRepository : Repository<AdminLog>, IAdminRepository
    {
        public AdminRepository(LeafLoopDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<AdminLog>> GetAdminLogsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.AdminLogs
                .Where(log => log.ActionDate >= startDate && log.ActionDate <= endDate)
                .Include(log => log.AdminUser)
                .OrderByDescending(log => log.ActionDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<AdminLog>> GetAdminLogsByUserAsync(int adminUserId)
        {
            return await _context.AdminLogs
                .Where(log => log.AdminUserId == adminUserId)
                .Include(log => log.AdminUser)
                .OrderByDescending(log => log.ActionDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<AdminLog>> GetAdminLogsByActionAsync(string action)
        {
            return await _context.AdminLogs
                .Where(log => log.Action == action)
                .Include(log => log.AdminUser)
                .OrderByDescending(log => log.ActionDate)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetActionSummaryAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.AdminLogs.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(log => log.ActionDate >= startDate.Value);
            
            if (endDate.HasValue)
                query = query.Where(log => log.ActionDate <= endDate.Value);

            return await query
                .GroupBy(log => log.Action)
                .Select(group => new { Action = group.Key, Count = group.Count() })
                .ToDictionaryAsync(x => x.Action, x => x.Count);
        }

        public async Task<IEnumerable<AdminLog>> GetRecentLogsAsync(int count = 100)
        {
            return await _context.AdminLogs
                .Include(log => log.AdminUser)
                .OrderByDescending(log => log.ActionDate)
                .Take(count)
                .ToListAsync();
        }
    }
}