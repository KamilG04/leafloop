using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LeafLoop.Api
{
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
            _eventService = eventService;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: api/events
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EventDto>>> GetAllEvents()
        {
            try
            {
                var events = await _eventService.GetAllEventsAsync();
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving events");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving events");
            }
        }

        // GET: api/events/upcoming
        [HttpGet("upcoming")]
        public async Task<ActionResult<IEnumerable<EventDto>>> GetUpcomingEvents([FromQuery] int count = 10)
        {
            try
            {
                var events = await _eventService.GetUpcomingEventsAsync(count);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving upcoming events");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving events");
            }
        }

        // GET: api/events/past
        [HttpGet("past")]
        public async Task<ActionResult<IEnumerable<EventDto>>> GetPastEvents([FromQuery] int count = 10)
        {
            try
            {
                var events = await _eventService.GetPastEventsAsync(count);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving past events");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving events");
            }
        }

        // GET: api/events/5
        [HttpGet("{id}")]
        public async Task<ActionResult<EventWithDetailsDto>> GetEvent(int id)
        {
            try
            {
                var eventDetails = await _eventService.GetEventWithDetailsAsync(id);
                
                if (eventDetails == null)
                {
                    return NotFound();
                }
                
                return Ok(eventDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving event details. EventId: {EventId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving event details");
            }
        }

        // GET: api/events/5/participants
        [HttpGet("{id}/participants")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetEventParticipants(int id)
        {
            try
            {
                var participants = await _eventService.GetEventParticipantsAsync(id);
                return Ok(participants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving event participants. EventId: {EventId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving participants");
            }
        }

        // POST: api/events
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<int>> CreateEvent(EventCreateDto eventDto)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                
                // Setting the organizer as the current user
                eventDto.OrganizerId = user.Id;
                eventDto.OrganizerType = OrganizerType.User;
                
                var eventId = await _eventService.CreateEventAsync(eventDto);
                
                return CreatedAtAction(nameof(GetEvent), new { id = eventId }, eventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error creating event");
            }
        }

        // PUT: api/events/5
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateEvent(int id, EventUpdateDto eventDto)
        {
            if (id != eventDto.Id)
            {
                return BadRequest("Event ID mismatch");
            }
            
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                
                // Get event to check ownership
                var eventEntity = await _eventService.GetEventByIdAsync(id);
                if (eventEntity == null)
                {
                    return NotFound();
                }
                
                // Check if user is the organizer or an admin
                if (eventEntity.OrganizerId != user.Id && eventEntity.OrganizerType != OrganizerType.User && !isAdmin)
                {
                    return Forbid();
                }
                
                await _eventService.UpdateEventAsync(eventDto);
                
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Event with ID {id} not found");
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event. EventId: {EventId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error updating event");
            }
        }

        // DELETE: api/events/5
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                
                // Get event to check ownership
                var eventEntity = await _eventService.GetEventByIdAsync(id);
                if (eventEntity == null)
                {
                    return NotFound();
                }
                
                await _eventService.DeleteEventAsync(id, user.Id, OrganizerType.User);
                
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Event with ID {id} not found");
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event. EventId: {EventId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error deleting event");
            }
        }

        // POST: api/events/5/register
        [HttpPost("{id}/register")]
        [Authorize]
        public async Task<IActionResult> RegisterForEvent(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                
                var success = await _eventService.RegisterForEventAsync(id, user.Id);
                
                if (!success)
                {
                    return BadRequest("Unable to register for event. The event might be full or not available for registration.");
                }
                
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Event with ID {id} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering for event. EventId: {EventId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error registering for event");
            }
        }

        // DELETE: api/events/5/register
        [HttpDelete("{id}/register")]
        [Authorize]
        public async Task<IActionResult> CancelEventRegistration(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                
                var success = await _eventService.CancelEventRegistrationAsync(id, user.Id);
                
                if (!success)
                {
                    return BadRequest("Unable to cancel registration. You might not be registered for this event.");
                }
                
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Event with ID {id} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling event registration. EventId: {EventId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error cancelling registration");
            }
        }

        // PUT: api/events/5/participants/3
        [HttpPut("{eventId}/participants/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateParticipationStatus(int eventId, int userId, [FromBody] ParticipationStatusUpdateDto statusUpdateDto)
        {
            try
            {
                await _eventService.UpdateParticipationStatusAsync(eventId, userId, statusUpdateDto.Status);
                
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Event or participant not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating participation status. EventId: {EventId}, UserId: {UserId}", eventId, userId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error updating participation status");
            }
        }
    }

    // Helper class for participation status update
    public class ParticipationStatusUpdateDto
    {
        public ParticipationStatus Status { get; set; }
    }
}