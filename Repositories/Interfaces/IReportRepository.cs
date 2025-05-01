using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;

namespace LeafLoop.Repositories.Interfaces
{
    public interface IReportRepository : IRepository<Report>
    {
        Task<IEnumerable<Report>> GetReportsByStatusAsync(ReportStatus status);
        Task<IEnumerable<Report>> GetReportsByContentTypeAsync(ContentType contentType);
    }
}
