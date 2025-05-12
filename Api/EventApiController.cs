using LeafLoop.Models;
using LeafLoop.Models.API; // For ApiResponse and ApiResponse<T>
using LeafLoop.Services.DTOs; // For Event DTOs
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LeafLoop.Api
{
    /// <summary>
    /// Manages community events.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<EventsController> _logger;

        public EventsController(
            IEventService eventService,
            UserManager<User> userManager,
            ILogger<EventsController> logger)
        {
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Retrieves all events.
        /// </summary>
        /// <returns>A list of all events.</returns>
        /// <response code="200">Returns the list of events.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet]
        [AllowAnonymous] // Assuming public visibility for all events list
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<EventDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllEvents()
        {
            _logger.LogInformation("API GetAllEvents START");
            try
            {
                var events = await _eventService.GetAllEventsAsync();
                _logger.LogInformation("API GetAllEvents SUCCESS. Count: {Count}", events?.Count() ?? 0);
                return this.ApiOk(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API GetAllEvents ERROR");
                return this.ApiInternalError("Error retrieving events.", ex);
            }
        }

        /// <summary>
        /// Retrieves upcoming events.
        /// </summary>
        /// <param name="count">The maximum number of upcoming events to retrieve. Defaults to 10.</param>
        /// <returns>A list of upcoming events.</returns>
        /// <response code="200">Returns the list of upcoming events.</response>
        /// <response code="400">If the count parameter is invalid.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet("upcoming")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<EventDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUpcomingEvents([FromQuery] int count = 10)
        {
            _logger.LogInformation("API GetUpcomingEvents START. Count: {Count}", count);
            if (count <= 0 || count > 100) // Added a reasonable upper limit for count
            {
                _logger.LogWarning("API GetUpcomingEvents BAD_REQUEST: Invalid count: {Count}. Adjusted to 10.", count);
                count = 10; // Default to 10 if invalid
            }
            try
            {
                var events = await _eventService.GetUpcomingEventsAsync(count);
                _logger.LogInformation("API GetUpcomingEvents SUCCESS. Retrieved: {RetrievedCount}", events?.Count() ?? 0);
                return this.ApiOk(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API GetUpcomingEvents ERROR");
                return this.ApiInternalError("Error retrieving upcoming events.", ex);
            }
        }

        /// <summary>
        /// Retrieves past events.
        /// </summary>
        /// <param name="count">The maximum number of past events to retrieve. Defaults to 10.</param>
        /// <returns>A list of past events.</returns>
        /// <response code="200">Returns the list of past events.</response>
        /// <response code="400">If the count parameter is invalid.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet("past")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<EventDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPastEvents([FromQuery] int count = 10)
        {
            _logger.LogInformation("API GetPastEvents START. Count: {Count}", count);
            if (count <= 0 || count > 100) // Added a reasonable upper limit for count
            {
                _logger.LogWarning("API GetPastEvents BAD_REQUEST: Invalid count: {Count}. Adjusted to 10.", count);
                count = 10; // Default to 10 if invalid
            }
            try
            {
                var events = await _eventService.GetPastEventsAsync(count);
                _logger.LogInformation("API GetPastEvents SUCCESS. Retrieved: {RetrievedCount}", events?.Count() ?? 0);
                return this.ApiOk(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API GetPastEvents ERROR");
                return this.ApiInternalError("Error retrieving past events.", ex);
            }
        }

        /// <summary>
        /// Retrieves a specific event by its ID, including details.
        /// </summary>
        /// <param name="id">The ID of the event to retrieve.</param>
        /// <returns>The event with the specified ID.</returns>
        /// <response code="200">Returns the requested event details.</response>
        /// <response code="400">If the event ID is invalid.</response>
        /// <response code="404">If the event with the specified ID is not found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet("{id:int}", Name = "GetEventById")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<EventWithDetailsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEvent(int id)
        {
            _logger.LogInformation("API GetEvent START for ID: {EventId}", id);
            if (id <= 0)
            {
                _logger.LogWarning("API GetEvent BAD_REQUEST: Invalid Event ID: {EventId}", id);
                return this.ApiBadRequest("Invalid Event ID.");
            }

            try
            {
                var eventDetails = await _eventService.GetEventWithDetailsAsync(id);
                if (eventDetails == null)
                {
                    _logger.LogWarning("API GetEvent NOT_FOUND: Event with ID {EventId} not found.", id);
                    return this.ApiNotFound($"Event with ID {id} not found.");
                }
                _logger.LogInformation("API GetEvent SUCCESS for ID: {EventId}", id);
                return this.ApiOk(eventDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API GetEvent ERROR for ID: {EventId}", id);
                return this.ApiInternalError("Error retrieving event details.", ex);
            }
        }

        /// <summary>
        /// Retrieves participants for a specific event.
        /// </summary>
        /// <param name="id">The ID of the event.</param>
        /// <returns>A list of participants for the event.</returns>
        /// <response code="200">Returns the list of event participants.</response>
        /// <response code="400">If the event ID is invalid.</response>
        /// <response code="404">If the event with the specified ID is not found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet("{id:int}/participants")]
        [AllowAnonymous] // Assuming participant lists for public events are also public
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEventParticipants(int id)
        {
            _logger.LogInformation("API GetEventParticipants START for EventID: {EventId}", id);
            if (id <= 0)
            {
                _logger.LogWarning("API GetEventParticipants BAD_REQUEST: Invalid Event ID: {EventId}", id);
                return this.ApiBadRequest("Invalid Event ID.");
            }

            try
            {
                // Service method GetEventParticipantsAsync should handle the case where event doesn't exist by throwing KeyNotFoundException or returning null.
                var participants = await _eventService.GetEventParticipantsAsync(id);
                // If service returns null for a non-existent event (instead of throwing KeyNotFoundException), handle it:
                // if (participants == null && await _eventService.GetEventByIdAsync(id) == null) {
                //     return this.ApiNotFound($"Event with ID {id} not found.");
                // }
                _logger.LogInformation("API GetEventParticipants SUCCESS for EventID: {EventId}. Participant count: {Count}", id, participants?.Count() ?? 0);
                return this.ApiOk(participants); // If event not found and service returns null, this will be ApiOk(null)
            }
            catch (KeyNotFoundException) // This is good if service throws it for non-existent event
            {
                _logger.LogWarning("API GetEventParticipants NOT_FOUND: Event with ID {EventId} not found.", id);
                return this.ApiNotFound($"Event with ID {id} not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API GetEventParticipants ERROR for EventID: {EventId}", id);
                return this.ApiInternalError("Error retrieving participants.", ex);
            }
        }

        /// <summary>
        /// Creates a new event. Requires authentication.
        /// The organizer will be the authenticated user.
        /// </summary>
        /// <param name="eventDto">The data for the new event.</param>
        /// <returns>The newly created event details.</returns>
        /// <response code="201">Returns the newly created event and its location.</response>
        /// <response code="400">If the event data is invalid.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpPost]
        [Authorize] // Requires authentication
        [ProducesResponseType(typeof(ApiResponse<EventWithDetailsDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateEvent([FromBody] EventCreateDto eventDto)
        {
            _logger.LogInformation("API CreateEvent START by User: {UserName}, Event Name: {EventName}", User.Identity?.Name ?? "N/A", eventDto?.Name ?? "N/A");
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API CreateEvent BAD_REQUEST: Invalid model state by User: {UserName}. Errors: {@ModelStateErrors}", User.Identity?.Name, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return this.ApiBadRequest(ModelState);
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    // This should ideally not happen if [Authorize] is effective.
                    _logger.LogWarning("API CreateEvent UNAUTHORIZED: User not found though endpoint is authorized.");
                    return this.ApiUnauthorized("User not found.");
                }

                eventDto.OrganizerId = user.Id;
                eventDto.OrganizerType = OrganizerType.User; // Explicitly set organizer type

                var eventId = await _eventService.CreateEventAsync(eventDto);
                var createdEvent = await _eventService.GetEventWithDetailsAsync(eventId);

                if (createdEvent == null)
                {
                    _logger.LogError("API CreateEvent ERROR: Could not retrieve event (ID: {EventId}) immediately after creation by User: {UserId}.", eventId, user.Id);
                    return this.ApiInternalError("Failed to retrieve event details after creation.");
                }
                _logger.LogInformation("API CreateEvent SUCCESS. New EventID: {EventId}, Name: {EventName}, OrganizerUserID: {UserId}", eventId, createdEvent.Name, user.Id);
                return this.ApiCreatedAtAction(
                    createdEvent,
                    nameof(GetEvent),
                    this.ControllerContext.ActionDescriptor.ControllerName,
                    new { id = eventId },
                    "Event created successfully."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API CreateEvent ERROR by User: {UserName}, Event Name: {EventName}", User.Identity?.Name, eventDto?.Name);
                return this.ApiInternalError("Error creating event.", ex);
            }
        }

        /// <summary>
        /// Updates an existing event. Requires authentication and user must be the organizer or an Admin.
        /// </summary>
        /// <param name="id">The ID of the event to update.</param>
        /// <param name="eventDto">The updated event data.</param>
        /// <returns>A 204 No Content response if successful.</returns>
        /// <response code="204">Event updated successfully.</response>
        /// <response code="400">If the event data is invalid or ID mismatch.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user is not authorized to update this event.</response>
        /// <response code="404">If the event with the specified ID is not found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpPut("{id:int}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] EventUpdateDto eventDto)
        {
            _logger.LogInformation("API UpdateEvent START for EventID: {EventId} by User: {UserName}", id, User.Identity?.Name ?? "N/A");
            if (id != eventDto.Id)
            {
                _logger.LogWarning("API UpdateEvent BAD_REQUEST: Event ID mismatch in URL ({UrlId}) and body ({BodyId}) by User: {UserName}.", id, eventDto.Id, User.Identity?.Name);
                return this.ApiBadRequest("Event ID mismatch in URL and body.");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API UpdateEvent BAD_REQUEST: Invalid model state for EventID {EventId} by User: {UserName}. Errors: {@ModelStateErrors}", id, User.Identity?.Name, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return this.ApiBadRequest(ModelState);
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                     _logger.LogWarning("API UpdateEvent UNAUTHORIZED: User not found though endpoint is authorized. EventID: {EventId}", id);
                    return this.ApiUnauthorized("User not found.");
                }

                var eventToUpdate = await _eventService.GetEventByIdAsync(id); // Get basic event for auth check
                if (eventToUpdate == null)
                {
                    _logger.LogWarning("API UpdateEvent NOT_FOUND: Event with ID {EventId} not found for update by User: {UserName}.", id, User.Identity?.Name);
                    return this.ApiNotFound($"Event with ID {id} not found.");
                }

                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                var isOwner = eventToUpdate.OrganizerId == user.Id && eventToUpdate.OrganizerType == OrganizerType.User;

                if (!isOwner && !isAdmin)
                {
                    _logger.LogWarning("API UpdateEvent FORBIDDEN: User {UserId} ({UserName}) is not owner or admin for EventID {EventId}.", user.Id, user.UserName, id);
                    return this.ApiForbidden("You are not authorized to update this event.");
                }

                await _eventService.UpdateEventAsync(eventDto); // Service should handle its own KeyNotFoundException if DTO's ID is not found by service
                _logger.LogInformation("API UpdateEvent SUCCESS for EventID: {EventId} by User: {UserName}", id, User.Identity?.Name);
                return this.ApiNoContent();
            }
            catch (KeyNotFoundException) // This might be redundant if GetEventByIdAsync above handles it, but good for safety if UpdateEventAsync can also throw it.
            {
                _logger.LogWarning("API UpdateEvent NOT_FOUND: Event with ID {EventId} not found during update process by User: {UserName}.", id, User.Identity?.Name);
                return this.ApiNotFound($"Event with ID {id} not found during update.");
            }
            // UnauthorizedAccessException might be thrown by the service layer if additional checks are done there.
            // However, the controller already performs primary authorization. (KamilG): PawelS consider validation enhance validation like in file  
            catch (UnauthorizedAccessException authEx)
            {
                 _logger.LogWarning(authEx, "API UpdateEvent FORBIDDEN (from service) for EventID: {EventId} by User: {UserName}.", id, User.Identity?.Name);
                return this.ApiForbidden("Authorization error during update.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API UpdateEvent ERROR for EventID: {EventId} by User: {UserName}", id, User.Identity?.Name);
                return this.ApiInternalError("Error updating event.", ex);
            }
        }

        /// <summary>
        /// Deletes an event. Requires authentication and user must be the organizer or an Admin.
        /// </summary>
        /// <param name="id">The ID of the event to delete.</param>
        /// <returns>A 204 No Content response if successful.</returns>
        /// <response code="204">Event deleted successfully.</response>
        /// <response code="400">If the event ID is invalid.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user is not authorized to delete this event.</response>
        /// <response code="404">If the event with the specified ID is not found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpDelete("{id:int}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            _logger.LogInformation("API DeleteEvent START for EventID: {EventId} by User: {UserName}", id, User.Identity?.Name ?? "N/A");
            if (id <= 0)
            {
                _logger.LogWarning("API DeleteEvent BAD_REQUEST: Invalid Event ID: {EventId} by User: {UserName}.", id, User.Identity?.Name);
                return this.ApiBadRequest("Invalid Event ID.");
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("API DeleteEvent UNAUTHORIZED: User not found though endpoint is authorized. EventID: {EventId}", id);
                    return this.ApiUnauthorized("User not found.");
                }
                // The service layer DeleteEventAsync handles authorization (isOwner or Admin)
                await _eventService.DeleteEventAsync(id, user.Id, OrganizerType.User); // Assuming user organizer type for this call
                _logger.LogInformation("API DeleteEvent SUCCESS for EventID: {EventId} by User: {UserName}", id, User.Identity?.Name);
                return this.ApiNoContent();
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning("API DeleteEvent NOT_FOUND: Event with ID {EventId} not found for deletion by User: {UserName}.", id, User.Identity?.Name);
                return this.ApiNotFound($"Event with ID {id} not found.");
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("API DeleteEvent FORBIDDEN: User {UserName} is not authorized to delete EventID {EventId}.", User.Identity?.Name, id);
                return this.ApiForbidden("You are not authorized to delete this event.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API DeleteEvent ERROR for EventID: {EventId} by User: {UserName}", id, User.Identity?.Name);
                return this.ApiInternalError("Error deleting event.", ex);
            }
        }

        /// <summary>
        /// Registers the authenticated user for an event.
        /// </summary>
        /// <param name="id">The ID of the event to register for.</param>
        /// <returns>A 204 No Content response if successful registration.</returns>
        /// <response code="204">Successfully registered for the event.</response>
        /// <response code="400">If the event ID is invalid or registration is not possible (e.g., event full, past).</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="404">If the event with the specified ID is not found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpPost("{id:int}/register")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RegisterForEvent(int id)
        {
            _logger.LogInformation("API RegisterForEvent START for EventID: {EventId} by User: {UserName}", id, User.Identity?.Name ?? "N/A");
            if (id <= 0)
            {
                _logger.LogWarning("API RegisterForEvent BAD_REQUEST: Invalid Event ID: {EventId} by User: {UserName}.", id, User.Identity?.Name);
                return this.ApiBadRequest("Invalid Event ID.");
            }
            
            User? user = null; // Declare here to use in final catch block if needed
            try
            {
                user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("API RegisterForEvent UNAUTHORIZED: User not found though endpoint is authorized. EventID: {EventId}", id);
                    return this.ApiUnauthorized("User not found.");
                }

                var success = await _eventService.RegisterForEventAsync(id, user.Id);

                if (!success)
                {
                    _logger.LogWarning("API RegisterForEvent BAD_REQUEST: Unable to register UserID {UserId} for EventID {EventId}. Event might be full, past, or other issue.", user.Id, id);
                    return this.ApiBadRequest("Unable to register for event. The event might be full, past, or not available.");
                }
                _logger.LogInformation("API RegisterForEvent SUCCESS for EventID: {EventId} by UserID: {UserId}", id, user.Id);
                return this.ApiNoContent();
            }
            catch (KeyNotFoundException) // If event or user (less likely for user due to GetUserAsync) not found in service
            {
                _logger.LogWarning("API RegisterForEvent NOT_FOUND: Event with ID {EventId} not found for registration by User: {UserName}.", id, User.Identity?.Name);
                return this.ApiNotFound($"Event with ID {id} not found.");
            }
            catch (InvalidOperationException ex) // e.g., already registered, event not open for registration
            {
                _logger.LogWarning(ex, "API RegisterForEvent BAD_REQUEST: Invalid operation for EventID {EventId}, UserID {UserId}.", id, user?.Id);
                return this.ApiBadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API RegisterForEvent ERROR for EventID: {EventId}, UserID: {UserId}", id, user?.Id);
                return this.ApiInternalError("Error registering for event.", ex);
            }
        }

        /// <summary>
        /// Cancels the authenticated user's registration for an event.
        /// </summary>
        /// <param name="id">The ID of the event to cancel registration for.</param>
        /// <returns>A 204 No Content response if successful cancellation.</returns>
        /// <response code="204">Successfully cancelled registration for the event.</response>
        /// <response code="400">If the event ID is invalid or cancellation is not possible.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="404">If the event with the specified ID is not found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpDelete("{id:int}/register")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CancelEventRegistration(int id)
        {
            _logger.LogInformation("API CancelEventRegistration START for EventID: {EventId} by User: {UserName}", id, User.Identity?.Name ?? "N/A");
            if (id <= 0)
            {
                _logger.LogWarning("API CancelEventRegistration BAD_REQUEST: Invalid Event ID: {EventId} by User: {UserName}.", id, User.Identity?.Name);
                return this.ApiBadRequest("Invalid Event ID.");
            }
            
            User? user = null;
            try
            {
                user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("API CancelEventRegistration UNAUTHORIZED: User not found though endpoint is authorized. EventID: {EventId}", id);
                    return this.ApiUnauthorized("User not found.");
                }

                var success = await _eventService.CancelEventRegistrationAsync(id, user.Id);

                if (!success)
                {
                    _logger.LogWarning("API CancelEventRegistration BAD_REQUEST: Unable to cancel registration for UserID {UserId} for EventID {EventId}. User might not be registered or other issue.", user.Id, id);
                    return this.ApiBadRequest("Unable to cancel registration. You might not be registered or the event is not active.");
                }
                _logger.LogInformation("API CancelEventRegistration SUCCESS for EventID: {EventId} by UserID: {UserId}", id, user.Id);
                return this.ApiNoContent();
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning("API CancelEventRegistration NOT_FOUND: Event with ID {EventId} not found for cancellation by User: {UserName}.", id, User.Identity?.Name);
                return this.ApiNotFound($"Event with ID {id} not found.");
            }
            catch (InvalidOperationException ex) // e.g., not registered
            {
                _logger.LogWarning(ex, "API CancelEventRegistration BAD_REQUEST: Invalid operation for EventID {EventId}, UserID {UserId}.", id, user?.Id);
                return this.ApiBadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API CancelEventRegistration ERROR for EventID: {EventId}, UserID: {UserId}", id, user?.Id);
                return this.ApiInternalError("Error cancelling registration.", ex);
            }
        }

        /// <summary>
        /// Updates the participation status of a user for an event. Requires Admin role.
        /// </summary>
        /// <param name="eventId">The ID of the event.</param>
        /// <param name="userId">The ID of the user whose participation status is to be updated.</param>
        /// <param name="statusUpdateDto">The DTO containing the new participation status.</param>
        /// <returns>A 204 No Content response if successful.</returns>
        /// <response code="204">Participation status updated successfully.</response>
        /// <response code="400">If IDs are invalid, DTO is invalid, or status is invalid.</response>
        /// <response code="401">If the admin is not authenticated.</response>
        /// <response code="403">If the authenticated user is not an Admin.</response>
        /// <response code="404">If the event or participant is not found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpPut("{eventId:int}/participants/{userId:int}/status")] // Added /status for clarity
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateParticipationStatus(int eventId, int userId, [FromBody] ParticipationStatusUpdateDto statusUpdateDto)
        {
            _logger.LogInformation("API UpdateParticipationStatus START for EventID: {EventId}, UserID: {TargetUserId}, NewStatus: {Status} by Admin: {AdminUserName}",
                eventId, userId, statusUpdateDto?.Status, User.Identity?.Name ?? "N/A");

            if (eventId <= 0 || userId <= 0)
            {
                _logger.LogWarning("API UpdateParticipationStatus BAD_REQUEST: Invalid Event or User ID. EventID: {EventId}, UserID: {TargetUserId}", eventId, userId);
                return this.ApiBadRequest("Invalid Event or User ID.");
            }
            if (!ModelState.IsValid) // Checks DTO validation attributes
            {
                 _logger.LogWarning("API UpdateParticipationStatus BAD_REQUEST: Invalid model state. EventID: {EventId}, UserID: {TargetUserId}. Errors: {@ModelStateErrors}", eventId, userId, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return this.ApiBadRequest(ModelState);
            }
            // Enum validation for statusUpdateDto.Status should ideally be handled by model binding or DTO validation attributes.
            // If not, an additional check here might be needed.

            try
            {
                await _eventService.UpdateParticipationStatusAsync(eventId, userId, statusUpdateDto.Status);
                _logger.LogInformation("API UpdateParticipationStatus SUCCESS for EventID: {EventId}, UserID: {TargetUserId}, NewStatus: {Status}", eventId, userId, statusUpdateDto.Status);
                return this.ApiNoContent();
            }
            catch (KeyNotFoundException) // If event or user/participant not found in service
            {
                _logger.LogWarning("API UpdateParticipationStatus NOT_FOUND: Event or participant not found. EventID: {EventId}, UserID: {TargetUserId}", eventId, userId);
                return this.ApiNotFound("Event or participant not found.");
            }
            catch (ArgumentException ex) // For invalid status or other argument issues from service
            {
                _logger.LogWarning(ex, "API UpdateParticipationStatus BAD_REQUEST: Argument error. EventID: {EventId}, UserID: {TargetUserId}", eventId, userId);
                return this.ApiBadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API UpdateParticipationStatus ERROR. EventID: {EventId}, UserID: {TargetUserId}", eventId, userId);
                return this.ApiInternalError("Error updating participation status.", ex);
            }
        }
    }
}
