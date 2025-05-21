using AutoMapper;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace LeafLoop.Services
{
    public class AdminService : IAdminService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;
        private readonly ILogger<AdminService> _logger;

        public AdminService(
            IUnitOfWork unitOfWork,
            UserManager<User> userManager,
            IMapper mapper,
            ILogger<AdminService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AdminDashboardDto> GetDashboardDataAsync()
        {
            try
            {
                var dashboard = new AdminDashboardDto
                {
                    Statistics = await GetStatisticsAsync(),
                    RecentUsers = _mapper.Map<List<UserDto>>(
                        await _unitOfWork.Users.FindAsync(u => u.CreatedDate >= DateTime.UtcNow.AddDays(-7))
                    ),
                    RecentItems = _mapper.Map<List<ItemDto>>(
                        await _unitOfWork.Items.GetAvailableItemsAsync(10)
                    ),
                    RecentTransactions = _mapper.Map<List<TransactionDto>>(
                        await _unitOfWork.Transactions.GetTransactionsByStatusAsync(TransactionStatus.Pending)
                    ),
                    RecentAdminActions = _mapper.Map<List<AdminLogDto>>(
                        await _unitOfWork.AdminLogs.GetRecentLogsAsync(20)
                    ),
                    ActionSummary = await _unitOfWork.AdminLogs.GetActionSummaryAsync(
                        DateTime.UtcNow.AddDays(-30), 
                        DateTime.UtcNow
                    )
                };

                return dashboard;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin dashboard data");
                throw;
            }
        }

        public async Task<AdminStatisticsDto> GetStatisticsAsync()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                
                var stats = new AdminStatisticsDto
                {
                    // Totals
                    TotalUsers = await _unitOfWork.Users.CountAsync(),
                    ActiveUsers = await _unitOfWork.Users.CountAsync(u => u.IsActive),
                    TotalItems = await _unitOfWork.Items.CountAsync(),
                    ActiveItems = await _unitOfWork.Items.CountAsync(i => i.IsAvailable),
                    TotalTransactions = await _unitOfWork.Transactions.CountAsync(),
                    CompletedTransactions = await _unitOfWork.Transactions.CountAsync(
                        t => t.Status == TransactionStatus.Completed
                    ),
                    PendingReports = await _unitOfWork.Reports.CountAsync(
                        r => r.Status == ReportStatus.Pending
                    ),
                    
                    // Today's stats
                    UsersToday = await _unitOfWork.Users.CountAsync(
                        u => u.CreatedDate >= today
                    ),
                    ItemsToday = await _unitOfWork.Items.CountAsync(
                        i => i.DateAdded >= today
                    ),
                    TransactionsToday = await _unitOfWork.Transactions.CountAsync(
                        t => t.StartDate >= today
                    )
                };

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin statistics");
                throw;
            }
        }

        public async Task LogAdminActionAsync(int adminUserId, AdminActionDto actionDto, string ipAddress)
        {
            try
            {
                var adminLog = new AdminLog
                {
                    AdminUserId = adminUserId,
                    Action = actionDto.Action,
                    EntityType = actionDto.EntityType,
                    EntityId = actionDto.EntityId,
                    Details = actionDto.Details,
                    IPAddress = ipAddress,
                    ActionDate = DateTime.UtcNow
                };

                await _unitOfWork.AdminLogs.AddAsync(adminLog);
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging admin action");
                // Don't throw - logging shouldn't break the main operation
            }
        }

        public async Task<bool> UpdateUserStatusAsync(int userId, bool isActive)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                    return false;

                user.IsActive = isActive;
                await _unitOfWork.CompleteAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user status for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<UserManagementDto>> GetAllUsersAsync()
        {
            try
            {
                var users = await _unitOfWork.Users.GetAllAsync();
                var userDtos = new List<UserManagementDto>();

                foreach (var user in users)
                {
                    var dto = _mapper.Map<UserManagementDto>(user);
                    dto.Roles = (await _userManager.GetRolesAsync(user)).ToList();
                    userDtos.Add(dto);
                }

                return userDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                throw;
            }
        }

        public async Task<UserManagementDto> GetUserDetailsAsync(int userId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                    return null;

                var dto = _mapper.Map<UserManagementDto>(user);
                dto.Roles = (await _userManager.GetRolesAsync(user)).ToList();
                
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user details for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> UpdateUserRolesAsync(int userId, List<string> roles)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                    return false;

                var currentRoles = await _userManager.GetRolesAsync(user);
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                    return false;

                if (roles != null && roles.Any())
                {
                    var addResult = await _userManager.AddToRolesAsync(user, roles);
                    if (!addResult.Succeeded)
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user roles for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<AdminLogDto>> GetAdminLogsAsync()
        {
            try
            {
                var logs = await _unitOfWork.AdminLogs.GetRecentLogsAsync(100);
                return _mapper.Map<IEnumerable<AdminLogDto>>(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin logs");
                throw;
            }
        }

        public async Task<SystemSettingsDto> GetSystemSettingsAsync()
        {
            try
            {
                var settings = new SystemSettingsDto
                {
                    Settings = new Dictionary<string, string>
                    {
                        { "SiteName", "LeafLoop" },
                        { "MaintenanceMode", "false" },
                        { "MaxUploadSize", "10485760" } // 10MB
                    },
                    LastUpdated = DateTime.UtcNow,
                    LastUpdatedBy = "System"
                };

                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system settings");
                throw;
            }
        }

        public async Task<bool> UpdateSystemSettingsAsync(SystemSettingsDto settings, int adminUserId)
        {
            try
            {
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating system settings");
                throw;
            }
        }
    }
}