using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LeafLoop.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace LeafLoop.Controllers
{
    /// <summary>
    /// Controller for Events views - serves React-based event management interface
    /// </summary>
    public class EventsController : Controller
    {
        private readonly IEventService _eventService;
        private readonly ILogger<EventsController> _logger;

        public EventsController(
            IEventService eventService,
            ILogger<EventsController> logger)
        {
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Main events page - displays React-based events interface
        /// </summary>
        /// <returns>Events index view with React components</returns>
        public IActionResult Index()
        {
            _logger.LogInformation("Events Index page accessed by user: {UserName}", User.Identity?.Name ?? "Anonymous");
            
            ViewData["Title"] = "Wydarzenia - LeafLoop";
            ViewData["Description"] = "Odkryj lokalne wydarzenia ekologiczne w Twojej okolicy. Dołącz do społeczności dbającej o środowisko.";
            
            return View();
        }

        /// <summary>
        /// Events calendar view (future feature)
        /// </summary>
        /// <returns>Calendar view of events</returns>
        public IActionResult Calendar()
        {
            _logger.LogInformation("Events Calendar page accessed by user: {UserName}", User.Identity?.Name ?? "Anonymous");
            
            ViewData["Title"] = "Kalendarz wydarzeń - LeafLoop";
            
            // For now, redirect to main events page
            // In the future, this could render a calendar-specific React component
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// My events page - shows events created by the current user
        /// Requires authentication
        /// </summary>
        /// <returns>User's events view</returns>
        [Authorize]
        public IActionResult MyEvents()
        {
            _logger.LogInformation("My Events page accessed by user: {UserName}", User.Identity?.Name ?? "Anonymous");
            
            ViewData["Title"] = "Moje wydarzenia - LeafLoop";
            ViewData["ShowOnlyUserEvents"] = true; // This can be used by React components to filter
            
            return View("Index"); // Use the same view but with different parameters
        }

        /// <summary>
        /// Event details page - shows detailed view of a specific event
        /// </summary>
        /// <param name="id">Event ID</param>
        /// <returns>Event details view</returns>
        public async Task<IActionResult> Details(int id)
        {
            _logger.LogInformation("Event details page accessed for EventID: {EventId} by user: {UserName}", 
                id, User.Identity?.Name ?? "Anonymous");

            if (id <= 0)
            {
                _logger.LogWarning("Invalid EventID provided: {EventId}", id);
                return BadRequest("Invalid event ID");
            }

            try
            {
                // Check if event exists
                var eventDto = await _eventService.GetEventByIdAsync(id);
                if (eventDto == null)
                {
                    _logger.LogWarning("Event not found for ID: {EventId}", id);
                    return NotFound("Event not found");
                }

                ViewData["Title"] = $"{eventDto.Name} - Wydarzenia - LeafLoop";
                ViewData["EventId"] = id;
                ViewData["EventName"] = eventDto.Name;
                ViewData["Description"] = eventDto.Description;
                
                return View("Index"); // Use the same view, React will handle the details modal
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accessing event details for EventID: {EventId}", id);
                return StatusCode(500, "An error occurred while loading the event details");
            }
        }

        /// <summary>
        /// Create event page - shows form for creating new events
        /// Requires authentication
        /// </summary>
        /// <returns>Create event view</returns>
        [Authorize]
        public IActionResult Create()
        {
            _logger.LogInformation("Create Event page accessed by user: {UserName}", User.Identity?.Name ?? "Anonymous");
            
            ViewData["Title"] = "Dodaj wydarzenie - LeafLoop";
            ViewData["ShowCreateModal"] = true; // Signal to React to show create modal immediately
            
            return View("Index");
        }

        /// <summary>
        /// Edit event page - shows form for editing existing events
        /// Requires authentication and ownership/admin rights
        /// </summary>
        /// <param name="id">Event ID to edit</param>
        /// <returns>Edit event view</returns>
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            _logger.LogInformation("Edit Event page accessed for EventID: {EventId} by user: {UserName}", 
                id, User.Identity?.Name ?? "Anonymous");

            if (id <= 0)
            {
                _logger.LogWarning("Invalid EventID provided for edit: {EventId}", id);
                return BadRequest("Invalid event ID");
            }

            try
            {
                // Check if event exists
                var eventDto = await _eventService.GetEventByIdAsync(id);
                if (eventDto == null)
                {
                    _logger.LogWarning("Event not found for edit, ID: {EventId}", id);
                    return NotFound("Event not found");
                }

                ViewData["Title"] = $"Edytuj {eventDto.Name} - Wydarzenia - LeafLoop";
                ViewData["EventId"] = id;
                ViewData["ShowEditModal"] = true; // Signal to React to show edit modal
                
                return View("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accessing event edit page for EventID: {EventId}", id);
                return StatusCode(500, "An error occurred while loading the event for editing");
            }
        }

        /// <summary>
        /// Participants page - shows list of event participants
        /// </summary>
        /// <param name="id">Event ID</param>
        /// <returns>Participants view</returns>
        public async Task<IActionResult> Participants(int id)
        {
            _logger.LogInformation("Event participants page accessed for EventID: {EventId} by user: {UserName}", 
                id, User.Identity?.Name ?? "Anonymous");

            if (id <= 0)
            {
                _logger.LogWarning("Invalid EventID provided for participants: {EventId}", id);
                return BadRequest("Invalid event ID");
            }

            try
            {
                // Check if event exists
                var eventDto = await _eventService.GetEventByIdAsync(id);
                if (eventDto == null)
                {
                    _logger.LogWarning("Event not found for participants view, ID: {EventId}", id);
                    return NotFound("Event not found");
                }

                ViewData["Title"] = $"Uczestnicy - {eventDto.Name} - LeafLoop";
                ViewData["EventId"] = id;
                ViewData["ShowParticipantsView"] = true;
                
                return View("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accessing participants page for EventID: {EventId}", id);
                return StatusCode(500, "An error occurred while loading the participants");
            }
        }

        /// <summary>
        /// Register for event action - handles GET requests to register
        /// Redirects to event details after registration attempt
        /// </summary>
        /// <param name="id">Event ID</param>
        /// <returns>Redirect to event details</returns>
        [Authorize]
        public IActionResult Register(int id)
        {
            // This is for GET requests (like from email links)
            // The actual registration is handled by the API
            _logger.LogInformation("Event registration page accessed for EventID: {EventId} by user: {UserName}", 
                id, User.Identity?.Name ?? "Anonymous");

            return RedirectToAction(nameof(Details), new { id = id });
        }

        /// <summary>
        /// Error handling for events-related errors
        /// </summary>
        /// <returns>Error view</returns>
        public IActionResult Error()
        {
            _logger.LogWarning("Events error page accessed by user: {UserName}", User.Identity?.Name ?? "Anonymous");
            
            ViewData["Title"] = "Błąd - Wydarzenia - LeafLoop";
            
            return View("~/Views/Shared/Error.cshtml");
        }
    }
}