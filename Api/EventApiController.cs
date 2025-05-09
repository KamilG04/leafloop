using LeafLoop.Models;
using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;


namespace LeafLoop.Api;

[Route("api/[controller]")]
[ApiController]
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

    [HttpGet]
    public async Task<IActionResult> GetAllEvents()
    {
        try
        {
            var events = await _eventService.GetAllEventsAsync();
            return this.ApiOk(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events");
            return this.ApiInternalError("Error retrieving events", ex);
        }
    }

    [HttpGet("upcoming")]
    public async Task<IActionResult> GetUpcomingEvents([FromQuery] int count = 10)
    {
        if (count <= 0) count = 10;
        try
        {
            var events = await _eventService.GetUpcomingEventsAsync(count);
            return this.ApiOk(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving upcoming events");
            return this.ApiInternalError("Error retrieving upcoming events", ex);
        }
    }

    [HttpGet("past")]
    public async Task<IActionResult> GetPastEvents([FromQuery] int count = 10)
    {
        if (count <= 0) count = 10;
        try
        {
            var events = await _eventService.GetPastEventsAsync(count);
            return this.ApiOk(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving past events");
            return this.ApiInternalError("Error retrieving past events", ex);
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetEvent(int id)
    {
        if (id <= 0) return this.ApiBadRequest("Invalid Event ID.");

        try
        {
            var eventDetails = await _eventService.GetEventWithDetailsAsync(id);

            if (eventDetails == null) return this.ApiNotFound($"Event with ID {id} not found.");

            return this.ApiOk(eventDetails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving event details. EventId: {EventId}", id);
            return this.ApiInternalError("Error retrieving event details", ex);
        }
    }

    [HttpGet("{id:int}/participants")]
    public async Task<IActionResult> GetEventParticipants(int id)
    {
        if (id <= 0) return this.ApiBadRequest("Invalid Event ID.");

        try
        {
            var participants = await _eventService.GetEventParticipantsAsync(id);
            return this.ApiOk(participants);
        }
        catch (KeyNotFoundException)
        {
            return this.ApiNotFound($"Event with ID {id} not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving event participants. EventId: {EventId}", id);
            return this.ApiInternalError("Error retrieving participants", ex);
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateEvent([FromBody] EventCreateDto eventDto)
    {
        if (!ModelState.IsValid) return this.ApiBadRequest(ModelState);

        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return this.ApiUnauthorized("User not found.");

            eventDto.OrganizerId = user.Id;
            eventDto.OrganizerType = OrganizerType.User;

            var eventId = await _eventService.CreateEventAsync(eventDto);

            var createdEvent = await _eventService.GetEventWithDetailsAsync(eventId);
            if (createdEvent == null)
            {
                _logger.LogError("Could not retrieve event (ID: {EventId}) immediately after creation.", eventId);
                return this.ApiInternalError("Failed to retrieve event details after creation.");
            }

            return this.ApiCreatedAtAction(
                createdEvent,
                nameof(GetEvent),
                "Events",
                new { id = eventId }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating event with Name: {EventName}", eventDto?.Name);
            return this.ApiInternalError("Error creating event", ex);
        }
    }

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] EventUpdateDto eventDto)
    {
        if (id != eventDto.Id) return this.ApiBadRequest("Event ID mismatch in URL and body.");

        if (!ModelState.IsValid) return this.ApiBadRequest(ModelState);

        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return this.ApiUnauthorized("User not found.");
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            var eventToCheck = await _eventService.GetEventByIdAsync(id);
            if (eventToCheck == null) return this.ApiNotFound($"Event with ID {id} not found.");
            var isOwner = eventToCheck.OrganizerId == user.Id && eventToCheck.OrganizerType == OrganizerType.User;
            if (!isOwner && !isAdmin) return this.ApiForbidden("You are not authorized to update this event.");

            await _eventService.UpdateEventAsync(eventDto);

            return this.ApiNoContent();
        }
        catch (KeyNotFoundException)
        {
            return this.ApiNotFound($"Event with ID {id} not found during update.");
        }
        catch (UnauthorizedAccessException)
        {
            return this.ApiForbidden("Authorization error during update.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating event. EventId: {EventId}", id);
            return this.ApiInternalError("Error updating event", ex);
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        if (id <= 0) return this.ApiBadRequest("Invalid Event ID.");

        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return this.ApiUnauthorized("User not found.");

            await _eventService.DeleteEventAsync(id, user.Id, OrganizerType.User);

            return this.ApiNoContent();
        }
        catch (KeyNotFoundException)
        {
            return this.ApiNotFound($"Event with ID {id} not found.");
        }
        catch (UnauthorizedAccessException)
        {
            return this.ApiForbidden("You are not authorized to delete this event.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting event. EventId: {EventId}", id);
            return this.ApiInternalError("Error deleting event", ex);
        }
    }

    [HttpPost("{id:int}/register")]
    [Authorize]
    public async Task<IActionResult> RegisterForEvent(int id)
    {
        if (id <= 0) return this.ApiBadRequest("Invalid Event ID.");
        User? user = null;

        try
        {
            user = await _userManager.GetUserAsync(User);
            if (user == null) return this.ApiUnauthorized("User not found.");

            var success = await _eventService.RegisterForEventAsync(id, user.Id);

            if (!success)
                return this.ApiBadRequest(
                    "Unable to register for event. The event might be full, past, or not available.");

            return this.ApiNoContent();
        }
        catch (KeyNotFoundException)
        {
            return this.ApiNotFound($"Event with ID {id} not found.");
        }
        catch (InvalidOperationException ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering for event. EventId: {EventId}, UserId: {UserId}", id, user?.Id);
            return this.ApiInternalError("Error registering for event", ex);
        }
    }

    [HttpDelete("{id:int}/register")]
    [Authorize]
    public async Task<IActionResult> CancelEventRegistration(int id)
    {
        if (id <= 0) return this.ApiBadRequest("Invalid Event ID.");
        User?
            user = null; // https://stackoverflow.com/questions/55492214/the-annotation-for-nullable-reference-types-should-only-be-used-in-code-within-a

        try
        {
            user = await _userManager.GetUserAsync(User);
            if (user == null) return this.ApiUnauthorized("User not found.");

            var success = await _eventService.CancelEventRegistrationAsync(id, user.Id);

            if (!success)
                return this.ApiBadRequest(
                    "Unable to cancel registration. You might not be registered or the event is not active.");

            return this.ApiNoContent();
        }
        catch (KeyNotFoundException)
        {
            return this.ApiNotFound($"Event with ID {id} not found.");
        }
        catch (InvalidOperationException ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling event registration. EventId: {EventId}, UserId: {UserId}", id,
                user?.Id);
            return this.ApiInternalError("Error cancelling registration", ex);
        }
    }

    [HttpPut("{eventId:int}/participants/{userId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateParticipationStatus(int eventId, int userId,
        [FromBody] ParticipationStatusUpdateDto statusUpdateDto)
    {
        if (eventId <= 0 || userId <= 0) return this.ApiBadRequest("Invalid Event or User ID.");
        if (!ModelState.IsValid) return this.ApiBadRequest(ModelState);

        try
        {
            await _eventService.UpdateParticipationStatusAsync(eventId, userId, statusUpdateDto.Status);

            return this.ApiNoContent();
        }
        catch (KeyNotFoundException)
        {
            return this.ApiNotFound("Event or participant not found.");
        }
        catch (ArgumentException ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating participation status. EventId: {EventId}, UserId: {UserId}", eventId,
                userId);
            return this.ApiInternalError("Error updating participation status", ex);
        }
    }
}