using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace LeafLoop.Services
{
    public class EventService : IEventService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<EventService> _logger;

        public EventService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<EventService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<EventDto> GetEventByIdAsync(int id)
        {
            try
            {
                var eventEntity = await _unitOfWork.Events.GetByIdAsync(id);
                return _mapper.Map<EventDto>(eventEntity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting event with ID: {EventId}", id);
                throw;
            }
        }

        public async Task<EventWithDetailsDto> GetEventWithDetailsAsync(int id)
        {
            try
            {
                var eventEntity = await _unitOfWork.Events.GetEventWithDetailsAsync(id);
                
                if (eventEntity == null)
                {
                    return null;
                }
                
                return _mapper.Map<EventWithDetailsDto>(eventEntity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting event details for ID: {EventId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<EventDto>> GetAllEventsAsync()
        {
            try
            {
                var events = await _unitOfWork.Events.GetAllAsync();
                return _mapper.Map<IEnumerable<EventDto>>(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all events");
                throw;
            }
        }

        public async Task<IEnumerable<EventDto>> GetUpcomingEventsAsync(int count)
        {
            try
            {
                var events = await _unitOfWork.Events.GetUpcomingEventsAsync(count);
                return _mapper.Map<IEnumerable<EventDto>>(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting upcoming events");
                throw;
            }
        }

        public async Task<IEnumerable<EventDto>> GetPastEventsAsync(int count)
        {
            try
            {
                var now = DateTime.UtcNow;
                var events = await _unitOfWork.Events.FindAsync(e => e.EndDate < now);
                
                return _mapper.Map<IEnumerable<EventDto>>(
                    events.OrderByDescending(e => e.EndDate).Take(count)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting past events");
                throw;
            }
        }

        public async Task<IEnumerable<EventDto>> GetEventsByOrganizerAsync(int organizerId, OrganizerType organizerType)
        {
            try
            {
                var events = await _unitOfWork.Events.FindAsync(
                    e => e.OrganizerId == organizerId && e.OrganizerType == organizerType
                );
                
                return _mapper.Map<IEnumerable<EventDto>>(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting events for organizer: {OrganizerId}", organizerId);
                throw;
            }
        }

        public async Task<IEnumerable<UserDto>> GetEventParticipantsAsync(int eventId)
        {
            try
            {
                var participants = await _unitOfWork.Events.GetEventParticipantsAsync(eventId);
                return _mapper.Map<IEnumerable<UserDto>>(participants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting participants for event: {EventId}", eventId);
                throw;
            }
        }

        public async Task<int> CreateEventAsync(EventCreateDto eventDto)
        {
            try
            {
                var newEvent = _mapper.Map<Event>(eventDto);
                
                // If the event has an address, check if it exists or create it
                if (eventDto.Address != null)
                {
                    var address = _mapper.Map<Address>(eventDto.Address);
                    await _unitOfWork.Addresses.AddAsync(address);
                    await _unitOfWork.CompleteAsync();
                    
                    newEvent.AddressId = address.Id;
                }
                
                await _unitOfWork.Events.AddAsync(newEvent);
                await _unitOfWork.CompleteAsync();
                
                return newEvent.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating event");
                throw;
            }
        }

        public async Task UpdateEventAsync(EventUpdateDto eventDto)
        {
            try
            {
                var existingEvent = await _unitOfWork.Events.GetByIdAsync(eventDto.Id);
                
                if (existingEvent == null)
                {
                    throw new KeyNotFoundException($"Event with ID {eventDto.Id} not found");
                }
                
                _mapper.Map(eventDto, existingEvent);
                
                _unitOfWork.Events.Update(existingEvent);
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating event: {EventId}", eventDto.Id);
                throw;
            }
        }

        public async Task DeleteEventAsync(int id, int organizerId, OrganizerType organizerType)
        {
            try
            {
                var existingEvent = await _unitOfWork.Events.GetByIdAsync(id);
                
                if (existingEvent == null)
                {
                    throw new KeyNotFoundException($"Event with ID {id} not found");
                }
                
                // Verify that the user is the organizer
                if (existingEvent.OrganizerId != organizerId || existingEvent.OrganizerType != organizerType)
                {
                    throw new UnauthorizedAccessException("User is not authorized to delete this event");
                }
                
                _unitOfWork.Events.Remove(existingEvent);
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting event: {EventId}", id);
                throw;
            }
        }

        public async Task<bool> RegisterForEventAsync(int eventId, int userId)
        {
            try
            {
                return await _unitOfWork.Events.AddParticipantAsync(eventId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while registering for event: {EventId}, User: {UserId}", eventId, userId);
                throw;
            }
        }

        public async Task<bool> CancelEventRegistrationAsync(int eventId, int userId)
        {
            try
            {
                return await _unitOfWork.Events.RemoveParticipantAsync(eventId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cancelling registration for event: {EventId}, User: {UserId}", eventId, userId);
                throw;
            }
        }

        public async Task UpdateParticipationStatusAsync(int eventId, int userId, ParticipationStatus status)
        {
            try
            {
                var eventEntity = await _unitOfWork.Events.GetEventWithDetailsAsync(eventId);
                
                if (eventEntity == null)
                {
                    throw new KeyNotFoundException($"Event with ID {eventId} not found");
                }
                
                var participant = eventEntity.Participants.FirstOrDefault(p => p.UserId == userId);
                
                if (participant == null)
                {
                    throw new KeyNotFoundException($"User with ID {userId} is not registered for event with ID {eventId}");
                }
                
                participant.ParticipationStatus = status;
                
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating participation status. EventId: {EventId}, UserId: {UserId}", eventId, userId);
                throw;
            }
        }
    }
}