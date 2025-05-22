// Path: LeafLoop/Controllers/AdminController.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LeafLoop.Services.Interfaces;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using LeafLoop.Services.DTOs; // Assuming AdminActionDto and SystemSettingsDto are here

namespace LeafLoop.Controllers
{
    [Authorize(Roles = "Admin")] // Ensures only users with the "Admin" role can access actions in this controller
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly IUnitOfWork _unitOfWork; 
        private readonly IUserService _userService; // Retained for now, review if its methods are fully covered by IAdminService for admin scenarios
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
            _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _itemService = itemService ?? throw new ArgumentNullException(nameof(itemService));
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
            _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Helper method for logging administrative actions.
        private async Task LogAdminAction(string action, string entityType = null, int? entityId = null, string details = null)
        {
            if (User.FindFirstValue(ClaimTypes.NameIdentifier) is string userIdString && int.TryParse(userIdString, out int adminUserId))
            {
                var actionDto = new AdminActionDto
                {
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId,
                    Details = details
                };
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                // TODO: Ensure IP address logging considers proxy scenarios (X-Forwarded-For header) if applicable.
                await _adminService.LogAdminActionAsync(adminUserId, actionDto, ipAddress);
            }
            else
            {
                _logger.LogWarning("Could not log admin action '{Action}' for entity type '{EntityType}' and entity ID '{EntityId}' because admin user ID claim was missing or invalid.", 
                    action, entityType ?? "N/A", entityId?.ToString() ?? "N/A");
            }
        }

        // GET: /Admin or /Admin/Index - Displays the main admin dashboard.
        
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            await LogAdminAction("View Dashboard");
            // TODO: Consider adding caching for dashboard data if it's expensive to compute and not strictly real-time.
            var dashboardData = await _adminService.GetDashboardDataAsync();
            return View(dashboardData);
        }

        // GET: /Admin/Users - Displays a list of all users.
        [HttpGet]
        public async Task<IActionResult> Users()
        {
            await LogAdminAction("View Users List");
            // TODO: Implement pagination and robust filtering for the user list.
            var users = await _adminService.GetAllUsersAsync(); // This might return a lot of users.
            return View(users);
        }

        // GET: /Admin/UserDetails/{id} - Displays details for a specific user.
        [HttpGet("UserDetails/{id:int}")]
        public async Task<IActionResult> UserDetails(int id)
        {
            if (id <= 0)
            {
                TempData["Error"] = "Invalid user ID provided.";
                return RedirectToAction(nameof(Users));
            }

            await LogAdminAction("View User Details", "User", id);
            var user = await _adminService.GetUserDetailsAsync(id); // This should return a ViewModel or a comprehensive DTO
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }
            // TODO: Populate ViewBag or ViewModel with available roles for the Edit Roles form.
            // ViewBag.AllRoles = await _adminService.GetAllRolesAsync();
            return View(user); // Expects Views/Admin/UserDetails.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            if (id <= 0)
            {
                TempData["Error"] = "Invalid user ID.";
                return RedirectToAction(nameof(UserDetails), new { id }); // Redirect back to details or users list
            }

            var user = await _adminService.GetUserDetailsAsync(id); // Consider a lighter GetUserByIdAsync if only status is needed.
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            // Prevent admin from deactivating themselves (important safeguard)
            if (User.FindFirstValue(ClaimTypes.NameIdentifier) is string adminIdString && 
                int.TryParse(adminIdString, out int adminId) && adminId == id && user.IsActive)
            {
                TempData["Error"] = "Administrators cannot deactivate their own account.";
                return RedirectToAction(nameof(UserDetails), new { id });
            }

            var newStatus = !user.IsActive;
            var result = await _adminService.UpdateUserStatusAsync(id, newStatus);

            if (result)
            {
                await LogAdminAction(newStatus ? "Activate User" : "Deactivate User", "User", id, $"Changed status to: {(newStatus ? "Active" : "Inactive")}");
                TempData["Success"] = "User status updated successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to update user status.";
            }
            return RedirectToAction(nameof(UserDetails), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserRoles(int id, List<string> roles)
        {
            if (id <= 0)
            {
                TempData["Error"] = "Invalid user ID.";
                return RedirectToAction(nameof(UserDetails), new { id });
            }
            if (roles == null) roles = new List<string>(); // Ensure roles list is not null

            // TODO: Add more robust validation for roles (e.g., check if roles exist).
            // TODO: Critical: Prevent admin from removing their own "Admin" role if they are the sole admin, or from removing all admins.
            // This requires more complex logic in IAdminService.UpdateUserRolesAsync.

            var result = await _adminService.UpdateUserRolesAsync(id, roles);

            if (result)
            {
                await LogAdminAction("Update User Roles", "User", id, $"New roles: {string.Join(", ", roles)}");
                TempData["Success"] = "User roles updated successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to update user roles.";
            }
            return RedirectToAction(nameof(UserDetails), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Reports()
        {
            await LogAdminAction("View Reports");
            // TODO: Implement pagination and filtering for reports.
            var reports = await _reportService.GetAllReportsAsync(); // Consider a DTO with more info if needed
            return View(reports); // Expects Views/Admin/Reports.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessReport(int id, ReportStatus status)
        {
            if (id <= 0)
            {
                TempData["Error"] = "Invalid report ID.";
                return RedirectToAction(nameof(Reports));
            }
            // TODO: Validate if 'status' is a valid enum member.

            var success = await _reportService.UpdateReportStatusAsync(id, status);
            if (success)
            {
                await LogAdminAction("Process Report", "Report", id, $"Status changed to: {status}");
                TempData["Success"] = "Report processed successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to process report or report not found.";
            }
            return RedirectToAction(nameof(Reports));
        }

        [HttpGet]
        // W AdminController.cs
        [HttpGet] // Możesz też dodać [HttpGet("AdminLogs")] dla jawnej trasy
        public async Task<IActionResult> AdminLogs(int page = 1, int pageSize = 50)
        {
            await LogAdminAction("View Admin Logs");
            if (page < 1) page = 1;
            // Zmniejszmy domyślny/maksymalny rozmiar strony dla logów admina, np. do 100
            if (pageSize < 1 || pageSize > 100) pageSize = 50; 
    
            try
            {
                // Wywołaj metodę serwisu, która zwraca PagedResult<AdminLogDto>
                var pagedLogs = await _adminService.GetAdminLogsAsync(page, pageSize);
                return View(pagedLogs); // Przekaż PagedResult do widoku
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin logs for page {Page}, size {PageSize}", page, pageSize);
                TempData["Error"] = "An error occurred while retrieving admin logs.";
                // W przypadku błędu, przekaż pusty PagedResult, aby widok się nie wysypał
                return View(new PagedResult<AdminLogDto>(new List<AdminLogDto>(), 0, page, pageSize));
            }
        }

     
        [HttpGet("Items")] // Jawna trasa dla /Admin/Items
        public async Task<IActionResult> Items(int page = 1, int pageSize = 20, string searchTerm = null, int? categoryId = null)
        {
            await LogAdminAction("View Items List", details: $"Search: '{searchTerm}', CatID: {categoryId?.ToString() ?? "Any"}");
    
            var searchDto = new ItemSearchDto 
            { 
                Page = page, 
                PageSize = pageSize,
                SearchTerm = searchTerm,
                CategoryId = categoryId,
                // Rozważ dodanie flagi do ItemSearchDto, np. IncludeUnavailable = true,
                // aby serwis mógł zwrócić wszystkie przedmioty dla admina, a nie tylko dostępne.
                // Obecnie SearchItemsAsync domyślnie filtruje po IsAvailable = true.
            };

            try
            {
                // ItemService.SearchItemsAsync zwraca IEnumerable<ItemDto> (tylko dla bieżącej strony)
                // ItemService.GetItemsCountAsync zwraca całkowitą liczbę pasujących przedmiotów
                var itemsList = await _itemService.SearchItemsAsync(searchDto);
                var totalItems = await _itemService.GetItemsCountAsync(searchDto);
        
                var pagedResult = new PagedResult<ItemDto>(itemsList ?? new List<ItemDto>(), totalItems, page, pageSize);

                // Widok Views/Admin/Items.cshtml powinien teraz oczekiwać @model PagedResult<ItemDto>
                return View(pagedResult); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving items for admin view with SearchDto: {@SearchDto}", searchDto);
                TempData["Error"] = "An error occurred while retrieving items.";
                // Możesz chcieć zwrócić pusty PagedResult lub przekierować do strony błędu
                return View(new PagedResult<ItemDto>(new List<ItemDto>(), 0, page, pageSize));
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteItem(int id) // Item ID to delete
        {
            if (id <= 0) {
                TempData["Error"] = "Invalid item ID.";
                return RedirectToAction(nameof(Items));
            }

            var item = await _itemService.GetItemByIdAsync(id); // Get item to log its name before deletion
            if (item == null)
            {
                TempData["Error"] = "Item not found.";
                return RedirectToAction(nameof(Items));
            }

            if (User.FindFirstValue(ClaimTypes.NameIdentifier) is string adminIdString && int.TryParse(adminIdString, out int adminId))
            {
                try
                {
                    // In ItemService.DeleteItemAsync, the check item.UserId != userId will fail for admin.
                    // The ItemService.DeleteItemAsync needs to be adjusted to allow admins to delete items
                    // not owned by them, or you need a specific AdminService method.
                    // For now, assuming DeleteItemAsync will throw if adminId is not item.UserId.
                    // A better approach:
                    // await _adminService.DeleteItemAsAdminAsync(id, adminId); 
                    // OR modify ItemService.DeleteItemAsync to accept an isAdmin flag or check role.
                    
                    // TEMPORARY: To make it work, we assume admin can delete any item, so ItemService should not check ownership FOR ADMINS.
                    // This requires a change in ItemService.DeleteItemAsync:
                    // if (item.UserId != userId && !await _userManager.IsInRoleAsync(await _userManager.FindByIdAsync(userId.ToString()), "Admin"))
                    // { /* throw UnauthorizedAccessException */ }
                    // OR more simply, if DeleteItemAsync is called from AdminController, we pass a flag or use a specific service method.

                    // For this example, let's assume the service handles it or we call it with the item's actual owner ID
                    // This is NOT a good long-term solution for admin delete.
                    // A proper solution is an admin-specific delete method in a service or modified ItemService.
                    // await _itemService.DeleteItemAsync(id, item.UserId); // This would work if admin "acts as owner"
                    // Ideal call:
                    await _adminService.ForceDeleteItemAsync(id, adminId); // This method needs to exist in IAdminService/AdminService
                                                                       // and bypass ownership checks in ItemService or call a specific repo method.

                    // Assuming a direct call to ItemService that is Admin-aware or bypasses owner check for this example
                    // This will likely fail with current ItemService.DeleteItemAsync due to ownership check.
                    // User needs to implement an Admin-specific delete or modify ItemService.
                    _logger.LogWarning("DeleteItem in AdminController is calling ItemService.DeleteItemAsync with adminId. Ensure ItemService.DeleteItemAsync can handle admin deletions or use a dedicated admin service method.");
                    await _itemService.DeleteItemAsync(id, adminId); // This will fail if adminId != item.UserId in ItemService

                    await LogAdminAction("Delete Item", "Item", id, $"Item: {item.Name} (ID: {item.Id})");
                    TempData["Success"] = "Item deleted successfully.";
                }
                catch (UnauthorizedAccessException)
                {
                    TempData["Error"] = "You are not authorized to delete this item (this should not happen for an Admin if service logic is correct).";
                }
                catch (KeyNotFoundException)
                {
                     TempData["Error"] = "Item not found for deletion.";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting item {ItemId} by admin {AdminId}", id, adminId);
                    TempData["Error"] = "An error occurred while deleting the item.";
                }
            }
            else
            {
                TempData["Error"] = "Could not identify admin user.";
            }
            return RedirectToAction(nameof(Items));
        }

        [HttpGet]
        public async Task<IActionResult> Transactions(int page = 1, int pageSize = 20, TransactionStatus? statusFilter = null)
        {
            await LogAdminAction("View Transactions List", details: $"Status: {statusFilter?.ToString() ?? "All"}");
    
            try
            {
                PagedResult<TransactionDto> pagedTransactions;
        
                if (statusFilter.HasValue)
                {
                    // Use the paged version with filter
                    pagedTransactions = await _transactionService.GetAllTransactionsAsync(page, pageSize, statusFilter);
                }
                else
                {
                    // Use the paged version without filter
                    pagedTransactions = await _transactionService.GetAllTransactionsAsync(page, pageSize);
                }
        
                return View(pagedTransactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transactions for admin view. Page: {Page}, PageSize: {PageSize}, Status: {Status}", 
                    page, pageSize, statusFilter);
                TempData["Error"] = "An error occurred while retrieving transactions.";
                return View(new PagedResult<TransactionDto>(new List<TransactionDto>(), 0, page, pageSize));
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTransactionStatus(int id, TransactionStatus status)
        {
            if (id <= 0) 
            {
                TempData["Error"] = "Invalid transaction ID.";
                return RedirectToAction(nameof(Transactions));
            }
            // TODO: Add more validation for 'status'.

            if (User.FindFirstValue(ClaimTypes.NameIdentifier) is string adminIdString && int.TryParse(adminIdString, out int adminId))
            {
                try
                {
                    // Admin should be able to override normal user restrictions on status updates
                    // So, UpdateTransactionStatusAsync might need an isAdmin flag or a separate admin method.
                    // For now, passing adminId as the 'userId' performing the action.
                    // The service method will check if this user is part of the transaction.
                    // An admin might need to bypass this check.
                    _logger.LogWarning("UpdateTransactionStatus in AdminController is calling TransactionService.UpdateTransactionStatusAsync with adminId. Ensure service method allows admin overrides if needed.");
                    await _transactionService.UpdateTransactionStatusAsync(id, status, adminId); 
                    await LogAdminAction("Update Transaction Status", "Transaction", id, $"New status: {status}");
                    TempData["Success"] = "Transaction status updated successfully.";
                }
                catch (KeyNotFoundException)
                {
                    TempData["Error"] = "Transaction not found.";
                }
                catch (UnauthorizedAccessException)
                {
                     TempData["Error"] = "Admin not authorized to update this transaction (this indicates an issue in service logic if admin should override).";
                }
                catch (InvalidOperationException ex)
                {
                    TempData["Error"] = $"Invalid status transition: {ex.Message}";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating transaction {TransactionId} status by admin {AdminId}", id, adminId);
                    TempData["Error"] = "An error occurred while updating transaction status.";
                }
            }
            else
            {
                 TempData["Error"] = "Could not identify admin user.";
            }
            return RedirectToAction(nameof(Transactions));
        }

        [HttpGet]
        public async Task<IActionResult> SystemSettings()
        {
            await LogAdminAction("View System Settings");
            var settings = await _adminService.GetSystemSettingsAsync(); // Assuming this returns a DTO/ViewModel
            return View(settings); // Expects Views/Admin/SystemSettings.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSystemSettings(SystemSettingsDto settings) // Assuming SystemSettingsDto
        {
             if (!ModelState.IsValid)
             {
                 TempData["Error"] = "Invalid settings provided. Please correct the errors.";
                 return View(settings); // Return view with current (invalid) settings and ModelState errors
             }

            if (User.FindFirstValue(ClaimTypes.NameIdentifier) is string adminIdString && int.TryParse(adminIdString, out int adminId))
            {
                var result = await _adminService.UpdateSystemSettingsAsync(settings, adminId);
                if (result)
                {
                    await LogAdminAction("Update System Settings", details: "Settings updated successfully");
                    TempData["Success"] = "System settings updated successfully.";
                }
                else
                {
                    TempData["Error"] = "Failed to update system settings. Please try again.";
                }
            }
            else
            {
                TempData["Error"] = "Could not identify admin user.";
            }
            return RedirectToAction(nameof(SystemSettings));
        }
    }
}