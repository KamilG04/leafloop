using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LeafLoop.Data;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LeafLoop.Repositories
{
    public class EventRepository : Repository<Event>, IEventRepository
    {
        public EventRepository(LeafLoopDbContext context) : base(context)
        {
        }

        public async Task<Event> GetEventWithDetailsAsync(int eventId)
        {
            return await _context.Events
                .Include(e => e.Address)
                .Include(e => e.Participants)
                    .ThenInclude(p => p.User)
                .SingleOrDefaultAsync(e => e.Id == eventId);
        }

        public async Task<IEnumerable<Event>> GetUpcomingEventsAsync(int count)
        {
            var now = DateTime.UtcNow;
            return await _context.Events
                .Include(e => e.Address)
                .Where(e => e.StartDate > now)
                .OrderBy(e => e.StartDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetEventParticipantsAsync(int eventId)
        {
            return await _context.EventParticipants
                .Where(ep => ep.EventId == eventId)
                .Include(ep => ep.User)
                .Select(ep => ep.User)
                .ToListAsync();
        }

        public async Task<bool> AddParticipantAsync(int eventId, int userId)
        {
            var eventEntity = await _context.Events
                .Include(e => e.Participants)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (eventEntity == null)
                return false;

            var existingParticipant = await _context.EventParticipants
                .FirstOrDefaultAsync(ep => ep.EventId == eventId && ep.UserId == userId);

            if (existingParticipant != null)
                return true; // User is already a participant

            // Check if there's space left
            if (eventEntity.ParticipantsLimit > 0 && 
                eventEntity.Participants.Count >= eventEntity.ParticipantsLimit)
                return false;

            var eventParticipant = new EventParticipant
            {
                EventId = eventId,
                UserId = userId,
                ParticipationStatus = ParticipationStatus.Registered
            };

            await _context.EventParticipants.AddAsync(eventParticipant);
            return true;
        }

        public async Task<bool> RemoveParticipantAsync(int eventId, int userId)
        {
            var eventParticipant = await _context.EventParticipants
                .FirstOrDefaultAsync(ep => ep.EventId == eventId && ep.UserId == userId);

            if (eventParticipant == null)
                return false;

            _context.EventParticipants.Remove(eventParticipant);
            return true;
        }
    }
}
    