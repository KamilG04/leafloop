using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;          // Dla User, KeyNotFoundException, UnauthorizedAccessException, ParticipationStatus, OrganizerType
using LeafLoop.Models.API;      // Dla ApiResponse<T> i ApiResponse
using LeafLoop.Services.DTOs;   // Dla DTOs
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;      // Dla StatusCodes
using Microsoft.AspNetCore.Identity;  // Dla UserManager
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LeafLoop.Api;             // <<<=== DODAJ TEN USING dla ApiControllerExtensions

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
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: api/events
        [HttpGet]
        public async Task<IActionResult> GetAllEvents() // Zmieniono sygnaturę na IActionResult
        {
            try
            {
                var events = await _eventService.GetAllEventsAsync();
                return this.ApiOk(events); // Użyj ApiOk<T>
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving events");
                return this.ApiInternalError("Error retrieving events", ex); // Użyj ApiInternalError
            }
        }

        // GET: api/events/upcoming
        [HttpGet("upcoming")]
        public async Task<IActionResult> GetUpcomingEvents([FromQuery] int count = 10) // Zmieniono sygnaturę na IActionResult
        {
            if (count <= 0) count = 10;
            try
            {
                var events = await _eventService.GetUpcomingEventsAsync(count);
                return this.ApiOk(events); // Użyj ApiOk<T>
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving upcoming events");
                return this.ApiInternalError("Error retrieving upcoming events", ex); // Użyj ApiInternalError
            }
        }

        // GET: api/events/past
        [HttpGet("past")]
        public async Task<IActionResult> GetPastEvents([FromQuery] int count = 10) // Zmieniono sygnaturę na IActionResult
        {
            if (count <= 0) count = 10;
            try
            {
                var events = await _eventService.GetPastEventsAsync(count);
                return this.ApiOk(events); // Użyj ApiOk<T>
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving past events");
                return this.ApiInternalError("Error retrieving past events", ex); // Użyj ApiInternalError
            }
        }

        // GET: api/events/{id:int}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetEvent(int id) // Zmieniono sygnaturę na IActionResult
        {
            if (id <= 0) return this.ApiBadRequest("Invalid Event ID.");

            try
            {
                var eventDetails = await _eventService.GetEventWithDetailsAsync(id); // Zakładamy, że zwraca EventWithDetailsDto

                if (eventDetails == null)
                {
                    return this.ApiNotFound($"Event with ID {id} not found."); // Użyj ApiNotFound
                }

                return this.ApiOk(eventDetails); // Użyj ApiOk<T>
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving event details. EventId: {EventId}", id);
                return this.ApiInternalError("Error retrieving event details", ex); // Użyj ApiInternalError
            }
        }

        // GET: api/events/{id:int}/participants
        [HttpGet("{id:int}/participants")]
        public async Task<IActionResult> GetEventParticipants(int id) // Zmieniono sygnaturę na IActionResult
        {
             if (id <= 0) return this.ApiBadRequest("Invalid Event ID.");

            try
            {
                // Zakładamy, że serwis rzuca KeyNotFoundException, jeśli event nie istnieje
                var participants = await _eventService.GetEventParticipantsAsync(id); // Zakładamy, że zwraca IEnumerable<UserDto>
                return this.ApiOk(participants); // Użyj ApiOk<T>
            }
            catch (KeyNotFoundException) // Jawnie łap KeyNotFoundException z serwisu
            {
                return this.ApiNotFound($"Event with ID {id} not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving event participants. EventId: {EventId}", id);
                return this.ApiInternalError("Error retrieving participants", ex); // Użyj ApiInternalError
            }
        }

        // POST: api/events
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateEvent([FromBody] EventCreateDto eventDto) // Zmieniono sygnaturę na IActionResult
        {
             if (!ModelState.IsValid) return this.ApiBadRequest(ModelState);

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return this.ApiUnauthorized("User not found.");

                // Ustawienie organizatora - upewnij się, że OrganizerType enum jest poprawny
                eventDto.OrganizerId = user.Id;
                eventDto.OrganizerType = Models.OrganizerType.User; // Użyj pełnej nazwy, jeśli jest konflikt

                var eventId = await _eventService.CreateEventAsync(eventDto);

                var createdEvent = await _eventService.GetEventWithDetailsAsync(eventId); // Pobierz DTO do odpowiedzi
                if(createdEvent == null)
                {
                     _logger.LogError("Could not retrieve event (ID: {EventId}) immediately after creation.", eventId);
                     return this.ApiInternalError("Failed to retrieve event details after creation.");
                }

                // Użyj ApiCreatedAtAction
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
                return this.ApiInternalError("Error creating event", ex); // Użyj ApiInternalError
            }
        }

        // PUT: api/events/{id:int}
        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] EventUpdateDto eventDto)
        {
            if (id != eventDto.Id)
            {
                return this.ApiBadRequest("Event ID mismatch in URL and body."); // Użyj ApiBadRequest
            }

            if (!ModelState.IsValid) return this.ApiBadRequest(ModelState);

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return this.ApiUnauthorized("User not found.");
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

                // Sprawdź, czy użytkownik może edytować ten event (jest właścicielem lub adminem)
                // Zakładamy, że GetEventByIdAsync zwraca podstawowy Event lub null
                var eventToCheck = await _eventService.GetEventByIdAsync(id);
                if (eventToCheck == null)
                {
                     return this.ApiNotFound($"Event with ID {id} not found.");
                }
                bool isOwner = (eventToCheck.OrganizerId == user.Id && eventToCheck.OrganizerType == Models.OrganizerType.User);
                if (!isOwner && !isAdmin)
                {
                    return this.ApiForbidden("You are not authorized to update this event.");
                }

                // Jeśli wszystko ok, wykonaj aktualizację
                // Zakładamy, że UpdateEventAsync rzuca KeyNotFoundException, jeśli wewnętrznie nie znajdzie eventu
                await _eventService.UpdateEventAsync(eventDto);

                return this.ApiNoContent(); // Użyj ApiNoContent
            }
            catch (KeyNotFoundException) // Jeśli UpdateEventAsync rzuci ten błąd
            {
                 return this.ApiNotFound($"Event with ID {id} not found during update.");
            }
            catch (UnauthorizedAccessException) // Chociaż sprawdzamy wcześniej, dla pewności
            {
                 return this.ApiForbidden("Authorization error during update.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event. EventId: {EventId}", id);
                return this.ApiInternalError("Error updating event", ex); // Użyj ApiInternalError
            }
        }

        // DELETE: api/events/{id:int}
        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            if (id <= 0) return this.ApiBadRequest("Invalid Event ID.");

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return this.ApiUnauthorized("User not found.");

                // Zakładamy, że DeleteEventAsync wewnętrznie sprawdza uprawnienia
                // i rzuca UnauthorizedAccessException lub KeyNotFoundException
                await _eventService.DeleteEventAsync(id, user.Id, Models.OrganizerType.User);

                return this.ApiNoContent(); // Użyj ApiNoContent
            }
            catch (KeyNotFoundException)
            {
                return this.ApiNotFound($"Event with ID {id} not found."); // Użyj ApiNotFound
            }
            catch (UnauthorizedAccessException)
            {
                 return this.ApiForbidden("You are not authorized to delete this event."); // Użyj ApiForbidden
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event. EventId: {EventId}", id);
                return this.ApiInternalError("Error deleting event", ex); // Użyj ApiInternalError
            }
        }

        // POST: api/events/{id:int}/register
        [HttpPost("{id:int}/register")]
        [Authorize]
        public async Task<IActionResult> RegisterForEvent(int id)
        {
            if (id <= 0) return this.ApiBadRequest("Invalid Event ID.");
            User? user = null; // Deklaracja przed try

            try
            {
                user = await _userManager.GetUserAsync(User);
                if (user == null) return this.ApiUnauthorized("User not found.");

                var success = await _eventService.RegisterForEventAsync(id, user.Id);

                if (!success)
                {
                    return this.ApiBadRequest("Unable to register for event. The event might be full, past, or not available."); // Użyj ApiBadRequest
                }

                return this.ApiNoContent(); // Użyj ApiNoContent
            }
            catch (KeyNotFoundException)
            {
                return this.ApiNotFound($"Event with ID {id} not found."); // Użyj ApiNotFound
            }
            catch (InvalidOperationException ex) // Np. już zarejestrowany
            {
                 return this.ApiBadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering for event. EventId: {EventId}, UserId: {UserId}", id, user?.Id);
                return this.ApiInternalError("Error registering for event", ex); // Użyj ApiInternalError
            }
        }

        // DELETE: api/events/{id:int}/register
        [HttpDelete("{id:int}/register")]
        [Authorize]
        public async Task<IActionResult> CancelEventRegistration(int id)
        {
            if (id <= 0) return this.ApiBadRequest("Invalid Event ID.");
            User? user = null; // Deklaracja przed try

            try
            {
                user = await _userManager.GetUserAsync(User);
                if (user == null) return this.ApiUnauthorized("User not found.");

                var success = await _eventService.CancelEventRegistrationAsync(id, user.Id);

                if (!success)
                {
                    return this.ApiBadRequest("Unable to cancel registration. You might not be registered or the event is not active."); // Użyj ApiBadRequest
                }

                return this.ApiNoContent(); // Użyj ApiNoContent
            }
            catch (KeyNotFoundException)
            {
                return this.ApiNotFound($"Event with ID {id} not found."); // Użyj ApiNotFound
            }
            catch (InvalidOperationException ex) // Np. nie był zarejestrowany
            {
                 return this.ApiBadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling event registration. EventId: {EventId}, UserId: {UserId}", id, user?.Id);
                return this.ApiInternalError("Error cancelling registration", ex); // Użyj ApiInternalError
            }
        }

        // PUT: api/events/{eventId:int}/participants/{userId:int}
        [HttpPut("{eventId:int}/participants/{userId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateParticipationStatus(int eventId, int userId, [FromBody] ParticipationStatusUpdateDto statusUpdateDto)
        {
            if (eventId <= 0 || userId <= 0) return this.ApiBadRequest("Invalid Event or User ID.");
            if (!ModelState.IsValid) return this.ApiBadRequest(ModelState);

            try
            {
                // Zakładamy, że serwis rzuca KeyNotFoundException lub ArgumentException
                await _eventService.UpdateParticipationStatusAsync(eventId, userId, statusUpdateDto.Status);

                return this.ApiNoContent(); // Użyj ApiNoContent
            }
            catch (KeyNotFoundException)
            {
                return this.ApiNotFound("Event or participant not found."); // Użyj ApiNotFound
            }
            catch (ArgumentException ex) // Np. nieprawidłowy status
            {
                 return this.ApiBadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating participation status. EventId: {EventId}, UserId: {UserId}", eventId, userId);
                return this.ApiInternalError("Error updating participation status", ex); // Użyj ApiInternalError
            }
        }
    }
}