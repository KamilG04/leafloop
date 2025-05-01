using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Services.DTOs;

namespace LeafLoop.Services.Interfaces
{
    public interface IEventService
    {
        Task<EventDto> GetEventByIdAsync(int id);
        Task<EventWithDetailsDto> GetEventWithDetailsAsync(int id);
        Task<IEnumerable<EventDto>> GetAllEventsAsync();
        Task<IEnumerable<EventDto>> GetUpcomingEventsAsync(int count);
        Task<IEnumerable<EventDto>> GetPastEventsAsync(int count);
        Task<IEnumerable<EventDto>> GetEventsByOrganizerAsync(int organizerId, OrganizerType organizerType);
        Task<IEnumerable<UserDto>> GetEventParticipantsAsync(int eventId);
        Task<int> CreateEventAsync(EventCreateDto eventDto);
        Task UpdateEventAsync(EventUpdateDto eventDto);
        Task DeleteEventAsync(int id, int organizerId, OrganizerType organizerType);
        Task<bool> RegisterForEventAsync(int eventId, int userId);
        Task<bool> CancelEventRegistrationAsync(int eventId, int userId);
        Task UpdateParticipationStatusAsync(int eventId, int userId, ParticipationStatus status);
    }
}
