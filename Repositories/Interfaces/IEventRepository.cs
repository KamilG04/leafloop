using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;

namespace LeafLoop.Repositories.Interfaces
{
    public interface IEventRepository : IRepository<Event>
    {
        Task<Event> GetEventWithDetailsAsync(int eventId);
        Task<IEnumerable<Event>> GetUpcomingEventsAsync(int count);
        Task<IEnumerable<User>> GetEventParticipantsAsync(int eventId);
        Task<bool> AddParticipantAsync(int eventId, int userId);
        Task<bool> RemoveParticipantAsync(int eventId, int userId);
    }
}
