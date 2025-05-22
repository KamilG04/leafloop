// Services/ReportService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace LeafLoop.Services
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ReportService> _logger;

        public ReportService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<ReportService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ReportDto> GetReportByIdAsync(int id)
        {
            try
            {
                var report = await _unitOfWork.Reports.GetByIdAsync(id);
                return _mapper.Map<ReportDto>(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting report with ID: {ReportId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<ReportDto>> GetAllReportsAsync()
        {
            try
            {
                var reports = await _unitOfWork.Reports.GetAllAsync();
                return _mapper.Map<IEnumerable<ReportDto>>(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all reports");
                throw;
            }
        }

        public async Task<IEnumerable<ReportDto>> GetReportsByStatusAsync(ReportStatus status)
        {
            try
            {
                var reports = await _unitOfWork.Reports.GetReportsByStatusAsync(status);
                return _mapper.Map<IEnumerable<ReportDto>>(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting reports by status: {Status}", status);
                throw;
            }
        }

        public async Task<IEnumerable<ReportDto>> GetReportsByContentTypeAsync(ContentType contentType)
        {
            try
            {
                var reports = await _unitOfWork.Reports.GetReportsByContentTypeAsync(contentType);
                return _mapper.Map<IEnumerable<ReportDto>>(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting reports by content type: {ContentType}", contentType);
                throw;
            }
        }

        public async Task<int> CreateReportAsync(ReportCreateDto reportDto)
        {
            try
            {
                var report = _mapper.Map<Report>(reportDto);
                report.ReportDate = DateTime.UtcNow;
                report.Status = ReportStatus.Pending;
                
                await _unitOfWork.Reports.AddAsync(report);
                await _unitOfWork.CompleteAsync();
                
                return report.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating report");
                throw;
            }
        }

        // Przykład w ReportService.cs
        public async Task<bool> UpdateReportStatusAsync(int id, ReportStatus status)
        {
            try
            {
                var report = await _unitOfWork.Reports.GetByIdAsync(id); // Zakładając, że masz takie repozytorium i metodę
                if (report == null)
                {
                    _logger.LogWarning("Report with ID {ReportId} not found for status update.", id);
                    return false; // Raport nie znaleziony
                }
                report.Status = status;
                // _unitOfWork.Reports.Update(report); // Jeśli repozytorium tego wymaga
                var changes = await _unitOfWork.CompleteAsync();
                return changes > 0; // Zwróć true, jeśli zmiany zostały zapisane
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating report status for ReportID {ReportId}", id);
                return false; // Błąd podczas operacji
            }
        }

        public async Task DeleteReportAsync(int id)
        {
            try
            {
                var report = await _unitOfWork.Reports.GetByIdAsync(id);
                
                if (report == null)
                {
                    throw new KeyNotFoundException($"Report with ID {id} not found");
                }
                
                _unitOfWork.Reports.Remove(report);
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting report: {ReportId}", id);
                throw;
            }
        }
    }
}