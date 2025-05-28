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
        // DbContext jest już wstrzykiwany przez klasę bazową Repository<T> i dostępny jako _context
        public EventRepository(LeafLoopDbContext context) : base(context)
        {
        }

        public async Task<Event> GetEventWithDetailsAsync(int eventId)
        {
            return await _context.Events
                .Include(e => e.Address)
                .Include(e => e.Participants)
                    .ThenInclude(p => p.User) // Załaduj powiązanego użytkownika dla każdego uczestnika
                .SingleOrDefaultAsync(e => e.Id == eventId);
        }

        public async Task<IEnumerable<Event>> GetUpcomingEventsAsync(int count)
        {
            var now = DateTime.UtcNow;
            return await _context.Events
                .Include(e => e.Address) // Dołącz adres, jeśli jest potrzebny w listingu
                .Where(e => e.StartDate > now)
                .OrderBy(e => e.StartDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetEventParticipantsAsync(int eventId)
        {
            // Upewnij się, że EventParticipants jest DbSet w Twoim DbContext
            return await _context.EventParticipants
                .Where(ep => ep.EventId == eventId)
                .Include(ep => ep.User) // Dołącz dane użytkownika
                .Select(ep => ep.User)  // Wybierz tylko obiekty User
                .ToListAsync();
        }

        public async Task<bool> AddParticipantAsync(int eventId, int userId)
        {
            var eventEntity = await _context.Events
                .Include(e => e.Participants) // Załaduj uczestników, aby sprawdzić ParticipantsLimit
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (eventEntity == null)
            {
                // _logger.LogWarning($"Event with ID {eventId} not found for adding participant."); // Można dodać logowanie
                return false; // Wydarzenie nie istnieje
            }

            var existingParticipant = await _context.EventParticipants
                .FirstOrDefaultAsync(ep => ep.EventId == eventId && ep.UserId == userId);

            if (existingParticipant != null)
            {
                // _logger.LogInformation($"User {userId} already registered for event {eventId}.");
                return true; // Użytkownik już jest uczestnikiem - traktujemy jako "sukces" w kontekście, że nie trzeba nic robić
            }

            // Sprawdź limit miejsc
            if (eventEntity.ParticipantsLimit > 0 && eventEntity.Participants.Count >= eventEntity.ParticipantsLimit)
            {
                // _logger.LogWarning($"Event {eventId} is full. Cannot register user {userId}.");
                return false; // Brak wolnych miejsc
            }

            // Sprawdź, czy wydarzenie już się nie zakończyło
            if (eventEntity.EndDate < DateTime.UtcNow)
            {
                // _logger.LogWarning($"Event {eventId} has already ended. Cannot register user {userId}.");
                return false; // Wydarzenie zakończone
            }

            var eventParticipant = new EventParticipant
            {
                EventId = eventId,
                UserId = userId,
                ParticipationStatus = ParticipationStatus.Registered // Ustaw domyślny status
            };

            await _context.EventParticipants.AddAsync(eventParticipant);
            // _logger.LogInformation($"User {userId} prepared for registration to event {eventId}. SaveChanges pending.");
            return true; // Encja dodana do kontekstu, gotowa do zapisu przez UnitOfWork.CompleteAsync()
        }

        public async Task<bool> RemoveParticipantAsync(int eventId, int userId)
        {
            var eventParticipant = await _context.EventParticipants
                .FirstOrDefaultAsync(ep => ep.EventId == eventId && ep.UserId == userId);

            if (eventParticipant == null)
            {
                // _logger.LogWarning($"Participant UserID {userId} for EventID {eventId} not found for removal.");
                return false; // Uczestnik nie znaleziony, nic do usunięcia
            }
            
            var eventEntity = await _context.Events.FindAsync(eventId);
            if (eventEntity != null && eventEntity.EndDate < DateTime.UtcNow)
            {
                // _logger.LogWarning($"Event {eventId} has already ended. Cannot unregister user {userId}.");
                // Zdecyduj, czy pozwolić na wypisanie się z zakończonego wydarzenia.
                // Zazwyczaj nie, ale zależy od logiki biznesowej. Poniżej przykład blokady.
                // return false;
            }


            _context.EventParticipants.Remove(eventParticipant);
            // _logger.LogInformation($"Participant UserID {userId} for EventID {eventId} marked for removal. SaveChanges pending.");
            return true; // Encja oznaczona do usunięcia, gotowa do zapisu przez UnitOfWork.CompleteAsync()
        }
    }
}