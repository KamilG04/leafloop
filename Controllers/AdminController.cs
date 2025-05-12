using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LeafLoop.Services.Interfaces;
using LeafLoop.Models; 
using LeafLoop.Services.DTOs;


namespace LeafLoop.Controllers
{
    [Authorize(Roles = "Admin")] // Ensures only users with the "Admin" role can access actions in this controller.
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly IUserService _userService; // TODO: Verify if IUserService is directly used or if its functionality is covered by IAdminService. Remove if unused.
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

        // Helper method for logging administrative actions.
        private async Task LogAdminAction(string action, string entityType = null, int? entityId = null, string details = null)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // TODO: Consider adding logging or specific handling in LogAdminAction if the admin user ID claim (NameIdentifier) is missing or invalid.
            if (userIdClaim != null && int.TryParse(userIdClaim, out int adminUserId))
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
                _logger.LogWarning("Could not log admin action '{Action}' because admin user ID claim was missing or invalid.", action);
            }
        }

        // GET: /Admin/Index - Displays the main admin dashboard.
        public async Task<IActionResult> Index()
        {
            await LogAdminAction("View Dashboard");
            // TODO: Consider adding caching for dashboard data if it's expensive to compute and doesn't need to be real-time.
            var dashboardData = await _adminService.GetDashboardDataAsync();
            return View(dashboardData);
        }

        // GET: /Admin/Users - Displays a list of all users.
        public async Task<IActionResult> Users()
        {
            await LogAdminAction("View Users List");
            // TODO: Implement pagination and filtering for the user list to handle potentially large datasets.
            var users = await _adminService.GetAllUsersAsync();
            return View(users);
        }

        // GET: /Admin/UserDetails/{id} - Displays details for a specific user.
        [HttpGet("Admin/UserDetails/{id:int}")]
        public async Task<IActionResult> UserDetails(int id)
        {
            // TODO: Consider using a shared validation filter/attribute for common checks like positive IDs.
            if (id <= 0)
            {
                TempData["Error"] = "Invalid user ID.";
                return RedirectToAction(nameof(Users));
            }

            await LogAdminAction("View User Details", "User", id);
            var user = await _adminService.GetUserDetailsAsync(id);
            if (user == null)
            {
                // TODO: Localize TempData messages.
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }
            // TODO: Populate necessary data for role editing in the view model/ViewBag (e.g., list of available roles).
            return View(user);
        }

        // POST: /Admin/ToggleUserStatus - Activates or deactivates a user account.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            if (id <= 0)
            {
                TempData["Error"] = "Invalid user ID.";
                return RedirectToAction(nameof(Users)); // Redirecting to Users might be confusing, maybe back to UserDetails if accessed from there?
            }

            // TODO: Evaluate if fetching full UserDetails is necessary here. A dedicated service method like GetUserStatusAsync(id)
            // might be more efficient if only the status is needed before toggling.
            var user = await _adminService.GetUserDetailsAsync(id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

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
                TempData["Success"] = $"User status updated successfully."; // TODO: Localize messages.
            }
            else
            {
                TempData["Error"] = "Failed to update user status."; // TODO: Localize messages.
            }

            return RedirectToAction(nameof(UserDetails), new { id });
        }

        // POST: /Admin/UpdateUserRoles - Updates the roles assigned to a user.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserRoles(int id, List<string> roles)
        {
            // TODO: Validate incoming 'roles' list - ensure roles exist and are permissible.
            if (id <= 0)
            {
                TempData["Error"] = "Invalid user ID.";
                return RedirectToAction(nameof(Users));
            }
            // TODO: Add server-side validation to prevent an admin from removing their own 'Admin' role unintentionally, or locking out all admins.

            var result = await _adminService.UpdateUserRolesAsync(id, roles);

            if (result)
            {
                await LogAdminAction("Update User Roles", "User", id, $"Roles: {string.Join(", ", roles)}");
                TempData["Success"] = "User roles updated successfully."; // TODO: Localize messages.
            }
            else
            {
                TempData["Error"] = "Failed to update user roles."; // TODO: Localize messages.
            }

            return RedirectToAction(nameof(UserDetails), new { id });
        }

        // GET: /Admin/Reports - Displays content reports.
        public async Task<IActionResult> Reports()
        {
            await LogAdminAction("View Reports");
            // TODO: Implement pagination and filtering (e.g., by status, date, reporter) for the reports list.
            var reports = await _reportService.GetAllReportsAsync();
            return View(reports);
        }

        // POST: /Admin/ProcessReport - Updates the status of a report.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessReport(int id, ReportStatus status)
        {
            // TODO: Add validation for the incoming 'status' parameter to ensure it's a valid ReportStatus enum value and perhaps a logical transition.
            // TODO: Add validation for report ID 'id'.
            await _reportService.UpdateReportStatusAsync(id, status);

            await LogAdminAction("Process Report", "Report", id, $"Status changed to: {status}");
            TempData["Success"] = "Report processed successfully."; // TODO: Localize messages.

            return RedirectToAction(nameof(Reports));
        }

        // GET: /Admin/AdminLogs - Displays the audit trail of admin actions.
        public async Task<IActionResult> AdminLogs()
        {
            await LogAdminAction("View Admin Logs");
            // TODO: Implement pagination, filtering (e.g., by date range, admin user, action type) for admin logs.
            var logs = await _adminService.GetAdminLogsAsync();
            return View(logs);
        }

        // GET: /Admin/Items - Displays a list of items (presumably for moderation).
        public async Task<IActionResult> Items()
        {
            await LogAdminAction("View Items List");
            // TODO: Enhance the Items view and this action to accept search/filter criteria via ItemSearchDto instead of using a default instance. Implement pagination.
            var items = await _itemService.SearchItemsAsync(new Services.DTOs.ItemSearchDto { /* Add default filters/paging? */ });
            return View(items);
        }

        // POST: /Admin/DeleteItem - Deletes an item.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteItem(int id)
        {
             // TODO: Add validation for item ID 'id'.
            var item = await _itemService.GetItemByIdAsync(id);
            if (item == null)
            {
                TempData["Error"] = "Item not found."; // TODO: Localize messages.
                return RedirectToAction(nameof(Items));
            }

            // FIXME: Replace int.Parse with int.TryParse for retrieving the admin user ID from claims to prevent exceptions
            // if the claim is missing or not a valid integer. Handle the failure case appropriately (e.g., log error, return error view).
            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            await _itemService.DeleteItemAsync(id, adminId); // Assuming DeleteItemAsync handles soft/hard delete logic and potentially related entities.

            await LogAdminAction("Delete Item", "Item", id, $"Item: {item.Name}");
            TempData["Success"] = "Item deleted successfully."; // TODO: Localize messages.

            return RedirectToAction(nameof(Items));
        }

        // GET: /Admin/Transactions - Displays transactions needing attention.
        public async Task<IActionResult> Transactions()
        {
            await LogAdminAction("View Transactions");
            // TODO: Allow filtering transactions by different statuses (not just Pending) based on user selection in the view. Implement pagination.
            var transactions = await _transactionService.GetTransactionsByStatusAsync(TransactionStatus.Pending); // Currently hardcoded to Pending
            return View(transactions);
        }

        // POST: /Admin/UpdateTransactionStatus - Updates the status of a transaction.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTransactionStatus(int id, TransactionStatus status)
        {
            // TODO: Add validation for transaction ID 'id'.
            // TODO: Add validation for the incoming 'status' parameter to ensure it's a valid TransactionStatus enum value and a logical transition.
            // FIXME: Replace int.Parse with int.TryParse for retrieving the admin user ID. Handle failure.
            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            await _transactionService.UpdateTransactionStatusAsync(id, status, adminId);

            await LogAdminAction("Update Transaction", "Transaction", id, $"Status changed to: {status}");
            TempData["Success"] = "Transaction status updated successfully."; // TODO: Localize messages.

            return RedirectToAction(nameof(Transactions));
        }

        // GET: /Admin/SystemSettings - Displays system configuration settings.
        public async Task<IActionResult> SystemSettings()
        {
            await LogAdminAction("View System Settings");
            var settings = await _adminService.GetSystemSettingsAsync();
            return View(settings);
        }

        // POST: /Admin/SystemSettings - Updates system configuration settings.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSystemSettings(SystemSettingsDto settings)
        {
             // TODO: Add model validation checks beyond [ValidateAntiForgeryToken].
             if (!ModelState.IsValid)
             {
                 // TODO: Consider returning the view with validation errors instead of redirecting immediately.
                 TempData["Error"] = "Invalid settings provided.";
                 return View(settings); // Return view with current (invalid) settings
             }

            // FIXME: Replace int.Parse with int.TryParse for retrieving the admin user ID. Handle failure.
            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _adminService.UpdateSystemSettingsAsync(settings, adminId);

            if (result)
            {
                await LogAdminAction("Update System Settings", "System", null, "Settings updated");
                TempData["Success"] = "System settings updated successfully."; // TODO: Localize messages.
            }
            else
            {
                TempData["Error"] = "Failed to update system settings."; // TODO: Localize messages.
                 // TODO: Provide more specific error feedback if possible.
                 // Consider returning the view again if the update failed server-side after parsing adminId etc.
            }

            // Redirecting prevents resubmission, but loses ModelState errors if validation failed before the service call attempt.
            return RedirectToAction(nameof(SystemSettings));
        }
    }
}