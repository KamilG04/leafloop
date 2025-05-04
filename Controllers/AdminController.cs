using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LeafLoop.Services.Interfaces;
using LeafLoop.Models;
using LeafLoop.Services.DTOs;
using System.Collections.Generic;
using LeafLoop.Services.DTOs;

namespace LeafLoop.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly IUserService _userService;
        private readonly IItemService _itemService;
        private readonly IReportService _reportService;
        private readonly ITransactionService _transactionService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IAdminService adminService,
            IUserService userService,
            IItemService itemService,
            IReportService reportService,
            ITransactionService transactionService,
            ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _userService = userService;
            _itemService = itemService;
            _reportService = reportService;
            _transactionService = transactionService;
            _logger = logger;
        }

        private async Task LogAdminAction(string action, string entityType = null, int? entityId = null, string details = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null && int.TryParse(userId, out int adminUserId))
            {
                var actionDto = new AdminActionDto
                {
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId,
                    Details = details
                };

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                await _adminService.LogAdminActionAsync(adminUserId, actionDto, ipAddress);
            }
        }

        // Dashboard
        public async Task<IActionResult> Index()
        {
            await LogAdminAction("View Dashboard");
            var dashboardData = await _adminService.GetDashboardDataAsync();
            return View(dashboardData);
        }

        // User Management
        public async Task<IActionResult> Users()
        {
            await LogAdminAction("View Users List");
            var users = await _adminService.GetAllUsersAsync();
            return View(users);
        }

        public async Task<IActionResult> UserDetails(int id)
        {
            await LogAdminAction("View User Details", "User", id);
            var user = await _adminService.GetUserDetailsAsync(id);
            if (user == null)
                return NotFound();

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            var user = await _adminService.GetUserDetailsAsync(id);
            if (user == null)
                return NotFound();

            var newStatus = !user.IsActive;
            var result = await _adminService.UpdateUserStatusAsync(id, newStatus);
            
            if (result)
            {
                await LogAdminAction(
                    newStatus ? "Activate User" : "Deactivate User", 
                    "User", 
                    id, 
                    $"Changed status to: {(newStatus ? "Active" : "Inactive")}"
                );
                TempData["Success"] = $"User status updated successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to update user status.";
            }

            return RedirectToAction(nameof(UserDetails), new { id });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUserRoles(int id, List<string> roles)
        {
            var result = await _adminService.UpdateUserRolesAsync(id, roles);
            
            if (result)
            {
                await LogAdminAction("Update User Roles", "User", id, $"Roles: {string.Join(", ", roles)}");
                TempData["Success"] = "User roles updated successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to update user roles.";
            }

            return RedirectToAction(nameof(UserDetails), new { id });
        }

        // Content Moderation
        public async Task<IActionResult> Reports()
        {
            await LogAdminAction("View Reports");
            var reports = await _reportService.GetAllReportsAsync();
            return View(reports);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessReport(int id, ReportStatus status)
        {
            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            await _reportService.UpdateReportStatusAsync(id, status);
            
            await LogAdminAction("Process Report", "Report", id, $"Status changed to: {status}");
            TempData["Success"] = "Report processed successfully.";
            
            return RedirectToAction(nameof(Reports));
        }

        // Admin Logs
        public async Task<IActionResult> AdminLogs()
        {
            await LogAdminAction("View Admin Logs");
            var logs = await _adminService.GetAdminLogsAsync();
            return View(logs);
        }

        // Item Management
        public async Task<IActionResult> Items()
        {
            await LogAdminAction("View Items List");
            var items = await _itemService.SearchItemsAsync(new Services.DTOs.ItemSearchDto());
            return View(items);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteItem(int id)
        {
            var item = await _itemService.GetItemByIdAsync(id);
            if (item == null)
                return NotFound();

            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            await _itemService.DeleteItemAsync(id, adminId);
            
            await LogAdminAction("Delete Item", "Item", id, $"Item: {item.Name}");
            TempData["Success"] = "Item deleted successfully.";
            
            return RedirectToAction(nameof(Items));
        }

        // Transaction Management
        public async Task<IActionResult> Transactions()
        {
            await LogAdminAction("View Transactions");
            var transactions = await _transactionService.GetTransactionsByStatusAsync(TransactionStatus.Pending);
            return View(transactions);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTransactionStatus(int id, TransactionStatus status)
        {
            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            await _transactionService.UpdateTransactionStatusAsync(id, status, adminId);
            
            await LogAdminAction("Update Transaction", "Transaction", id, $"Status changed to: {status}");
            TempData["Success"] = "Transaction status updated successfully.";
            
            return RedirectToAction(nameof(Transactions));
        }

        // System Management
        public async Task<IActionResult> SystemSettings()
        {
            await LogAdminAction("View System Settings");
            var settings = await _adminService.GetSystemSettingsAsync();
            return View(settings);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSystemSettings(SystemSettingsDto settings)
        {
            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _adminService.UpdateSystemSettingsAsync(settings, adminId);
            
            if (result)
            {
                await LogAdminAction("Update System Settings", "System", null, "Settings updated");
                TempData["Success"] = "System settings updated successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to update system settings.";
            }
            
            return RedirectToAction(nameof(SystemSettings));
        }
    }
}