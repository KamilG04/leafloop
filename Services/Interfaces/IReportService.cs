using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Services.DTOs;

namespace LeafLoop.Services.Interfaces
{
    public interface IReportService
    {
        Task<ReportDto> GetReportByIdAsync(int id);
        Task<IEnumerable<ReportDto>> GetAllReportsAsync();
        Task<IEnumerable<ReportDto>> GetReportsByStatusAsync(ReportStatus status);
        Task<IEnumerable<ReportDto>> GetReportsByContentTypeAsync(ContentType contentType);
        Task<int> CreateReportAsync(ReportCreateDto reportDto);
        Task UpdateReportStatusAsync(int id, ReportStatus status);
        Task DeleteReportAsync(int id);
    }
}
