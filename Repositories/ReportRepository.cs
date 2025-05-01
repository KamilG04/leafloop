using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LeafLoop.Data;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LeafLoop.Repositories
{
    public class ReportRepository : Repository<Report>, IReportRepository
    {
        public ReportRepository(LeafLoopDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Report>> GetReportsByStatusAsync(ReportStatus status)
        {
            return await _context.Reports
                .Where(r => r.Status == status)
                .Include(r => r.Reporter)
                .OrderByDescending(r => r.ReportDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Report>> GetReportsByContentTypeAsync(ContentType contentType)
        {
            return await _context.Reports
                .Where(r => r.ContentType == contentType)
                .Include(r => r.Reporter)
                .OrderByDescending(r => r.ReportDate)
                .ToListAsync();
        }
    }
}