
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces;


namespace LeafLoop.Services
{
    public class EventService : IEventService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<EventService> _logger;
        // Wewnątrz pliku EventService.cs, np. na początku metody CreateEventAsync

        LeafLoop.Models.Address testAddressInstance; // Test 1: Użycie pełnej nazwy kwalifikowanej
        Address anotherTestAddressInstance;        // Test 2: Użycie krótkiej nazwy (jeśli 'using LeafLoop.Models;' jest obecne)

// Jeśli powyższe linie nie powodują błędu "Cannot resolve symbol 'Address'",
// to problem z 'Address' w _mapper.Map<Address>(...) jest bardziej specyficzny dla tej konstrukcji.

// Sprawdźmy też DTO:
        LeafLoop.Services.DTOs.AddressDto testAddressDtoInstance;
        AddressDto anotherTestAddressDtoInstance;
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

       // W EventService.cs
// Dla List<UserDto>
// ... inne usingi ...

public async Task<EventWithDetailsDto> GetEventWithDetailsAsync(int id)
{
    try
    {
        // GetEventWithDetailsAsync z repozytorium ładuje już Event.Participants.User
        var eventEntity = await _unitOfWork.Events.GetEventWithDetailsAsync(id);
        if (eventEntity == null)
        {
            _logger.LogWarning("Event with details not found for ID: {EventId}", id);
            return null;
        }

        // Wstępne mapowanie na DTO (Address i Participants zostaną zmapowane dzięki MappingProfile)
        var eventDto = _mapper.Map<EventWithDetailsDto>(eventEntity);

        // --- Uzupełnianie OrganizerName ---
        if (eventEntity.OrganizerType == OrganizerType.User)
        {
            var organizerUser = await _unitOfWork.Users.GetByIdAsync(eventEntity.OrganizerId);
            if (organizerUser != null)
            {
                eventDto.OrganizerName = $"{organizerUser.FirstName} {organizerUser.LastName}".Trim();
            }
            else
            {
                _logger.LogWarning("Organizer User with ID {OrganizerId} not found for Event {EventId}", eventEntity.OrganizerId, id);
                eventDto.OrganizerName = "Użytkownik nieznany";
            }
        }
        else if (eventEntity.OrganizerType == OrganizerType.Company)
        {
            // Załóżmy, że masz _unitOfWork.Companies.GetByIdAsync(id)
            var organizerCompany = await _unitOfWork.Companies.GetByIdAsync(eventEntity.OrganizerId);
            if (organizerCompany != null)
            {
                eventDto.OrganizerName = organizerCompany.Name;
            }
            else
            {
                _logger.LogWarning("Organizer Company with ID {OrganizerId} not found for Event {EventId}", eventEntity.OrganizerId, id);
                eventDto.OrganizerName = "Firma nieznana";
            }
        }
        else
        {
            eventDto.OrganizerName = "Organizator (typ nieznany)";
        }
        // --- Koniec uzupełniania OrganizerName ---

        // Mapowanie uczestników jest już obsługiwane przez AutoMapper dzięki zmianom w MappingProfile
        // pod warunkiem, że _unitOfWork.Events.GetEventWithDetailsAsync(id) poprawnie ładuje
        // eventEntity.Participants.Select(ep => ep.User)
        // Metoda GetEventWithDetailsAsync w EventRepository już to robi:
        // .Include(e => e.Participants).ThenInclude(p => p.User)

        return eventDto;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error occurred while getting event details for ID: {EventId}", id);
        throw; // Rzuć dalej, aby kontroler API mógł zwrócić odpowiedni błąd
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
                // Logika dla GetPastEventsAsync była wcześniej inna, użyjemy tej z repozytorium, jeśli istnieje
                // lub dostosujemy repozytorium. Załóżmy, że repozytorium ma odpowiednią metodę
                // lub implementacja z poprzedniej wersji EventService jest preferowana.
                // Poniżej używam logiki z poprzedniego EventService dla spójności, jeśli repo nie ma GetPastEventsAsync.
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

       // W EventService.cs, metoda CreateEventAsync
public async Task<int> CreateEventAsync(EventCreateDto eventDto)
{
    return await _unitOfWork.ExecuteInTransactionAsync(async () =>
    {
        var newEvent = _mapper.Map<Event>(eventDto);

        if (eventDto.Address != null &&
            (!string.IsNullOrWhiteSpace(eventDto.Address.Street) ||
             !string.IsNullOrWhiteSpace(eventDto.Address.City)))
        {
            // --- POCZĄTEK MODYFIKACJI TESTOWEJ ---
            LeafLoop.Services.DTOs.AddressDto sourceAddressDto = eventDto.Address;
            LeafLoop.Models.Address mappedAddressEntity = null; // Inicjalizuj jako null

            _logger.LogInformation("Próba mapowania AddressDto na Address. Typ źródłowy: {SourceType}", sourceAddressDto?.GetType().FullName ?? "null");

            try
            {
                // Test 1: Czy możemy uzyskać typ docelowy?
                Type targetAddressType = typeof(LeafLoop.Models.Address);
                _logger.LogInformation("Typ docelowy dla mapowania (encjA): {TargetType}", targetAddressType.FullName);

                // Test 2: Czy to konkretne wywołanie _mapper.Map powoduje błąd kompilacji "Cannot resolve symbol"?
                // Jeśli tak, a 'LeafLoop.Models.Address testInstance;' działało, to jest to skrajnie dziwne.
                mappedAddressEntity = _mapper.Map<LeafLoop.Models.Address>(sourceAddressDto);
                _logger.LogInformation("Mapowanie AddressDto na Address zakończone pomyślnie (bez błędu kompilacji na tej linii).");

                newEvent.Address = mappedAddressEntity;
                newEvent.AddressId = null; // Lub pozwól EF Core zarządzać, jeśli Address jest przypisany
            }
            catch (AutoMapperMappingException amEx) // To jest błąd RUNTIME, nie kompilacji
            {
                _logger.LogError(amEx, "BŁĄD WYKONANIA AutoMapper: Wystąpił problem podczas mapowania AddressDto na Address.");
                throw; // Rzuć dalej, aby transakcja została wycofana
            }
            catch (Exception ex) // Inne błędy wykonania
            {
                _logger.LogError(ex, "BŁĄD WYKONANIA: Inny problem podczas przetwarzania adresu.");
                throw; // Rzuć dalej
            }
            // --- KONIEC MODYFIKACJI TESTOWEJ ---
        }

        await _unitOfWork.Events.AddAsync(newEvent);
        await _unitOfWork.CompleteAsync();
        return newEvent.Id;
    });
}

        public async Task UpdateEventAsync(EventUpdateDto eventDto)
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var existingEvent = await _unitOfWork.Events.GetByIdAsync(eventDto.Id);
                if (existingEvent == null)
                {
                    _logger.LogWarning("Event with ID {EventId} not found for update.", eventDto.Id);
                    throw new KeyNotFoundException($"Event with ID {eventDto.Id} not found");
                }

                _mapper.Map(eventDto, existingEvent);

                // Jeśli adres jest aktualizowany
                if (eventDto.Address != null)
                {
                    if (existingEvent.Address != null) // Aktualizuj istniejący adres
                    {
                        _mapper.Map(eventDto.Address, existingEvent.Address);
                    }
                    else // Stwórz nowy adres, jeśli wydarzenie go nie miało, a DTO go dostarcza
                    {
                        var newAddress = _mapper.Map<Address>(eventDto.Address);
                        existingEvent.Address = newAddress; // EF Core powinien obsłużyć dodanie nowego adresu
                    }
                }
                else if (existingEvent.Address != null) // Jeśli DTO nie ma adresu, a wydarzenie miało, usuń stary adres (opcjonalne)
                {
                    // Zdecyduj, czy usuwać adres, czy pozostawić. Poniżej przykład usunięcia.
                    // _unitOfWork.Addresses.Remove(existingEvent.Address);
                    // existingEvent.Address = null; // Lub tylko nulluj referencję
                }

                _unitOfWork.Events.Update(existingEvent); // Oznacz event jako zmodyfikowany
                // _unitOfWork.CompleteAsync() zostanie wywołane przez ExecuteInTransactionAsync
            });
        }

        public async Task DeleteEventAsync(int id, int organizerId, OrganizerType organizerType)
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var existingEvent = await _unitOfWork.Events.GetByIdAsync(id);
                if (existingEvent == null)
                {
                    _logger.LogWarning("Event with ID {EventId} not found for deletion.", id);
                    throw new KeyNotFoundException($"Event with ID {id} not found");
                }

                if (existingEvent.OrganizerId != organizerId || existingEvent.OrganizerType != organizerType)
                {
                    _logger.LogWarning("User {OrganizerId} is not authorized to delete event {EventId}.", organizerId, id);
                    throw new UnauthorizedAccessException("User is not authorized to delete this event");
                }

                // Rozważ usunięcie powiązanych uczestników, jeśli kaskadowe usuwanie nie jest skonfigurowane
                var participants = await _unitOfWork.Events.GetEventParticipantsAsync(id); // Zakładając, że zwraca EventParticipant, a nie UserDto
                // Jeśli GetEventParticipantsAsync zwraca UserDto, potrzebujesz metody zwracającej EventParticipant
                // _unitOfWork.EventParticipants.RemoveRange(participants); // Jeśli masz repozytorium dla EventParticipant

                _unitOfWork.Events.Remove(existingEvent);
                // _unitOfWork.CompleteAsync() zostanie wywołane przez ExecuteInTransactionAsync
            });
        }

        // --- KLUCZOWA POPRAWKA TUTAJ ---
        public async Task<bool> RegisterForEventAsync(int eventId, int userId)
        {
            try
            {
                // Krok 1: Repozytorium przygotowuje encję w kontekście i wykonuje wstępne sprawdzenia.
                bool canRegisterAndEntityPrepared = await _unitOfWork.Events.AddParticipantAsync(eventId, userId);

                if (canRegisterAndEntityPrepared)
                {
                    // Krok 2: Jeśli encja została poprawnie przygotowana, zapisz wszystkie oczekujące zmiany.
                    int changesMade = await _unitOfWork.CompleteAsync();
                    _logger.LogInformation("Successfully registered UserID {UserId} for EventID {EventId}. Changes saved: {ChangesCount}", userId, eventId, changesMade);
                    return true; // Sukces, jeśli CompleteAsync nie rzucił wyjątku
                }
                else
                {
                    // AddParticipantAsync zwróciło false z powodu reguły biznesowej
                    _logger.LogWarning("Registration pre-check failed for UserID {UserId} for EventID {EventId}. AddParticipantAsync returned false.", userId, eventId);
                    return false;
                }
            }
            catch (DbUpdateException dbEx) // Bardziej szczegółowe logowanie dla błędów bazy danych
            {
                _logger.LogError(dbEx, "Database error occurred while registering UserID {UserId} for EventID {EventId}.", userId, eventId);
                // Możesz sprawdzić dbEx.InnerException dla bardziej szczegółowych informacji
                throw; // Rzuć dalej, aby kontroler mógł zwrócić odpowiedni błąd
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Generic error occurred while registering UserID {UserId} for EventID {EventId}", eventId, userId);
                throw;
            }
        }

        // --- KLUCZOWA POPRAWKA TUTAJ ---
        public async Task<bool> CancelEventRegistrationAsync(int eventId, int userId)
        {
            try
            {
                // Krok 1: Repozytorium oznacza encję do usunięcia w kontekście.
                bool canCancelAndEntityMarked = await _unitOfWork.Events.RemoveParticipantAsync(eventId, userId);

                if (canCancelAndEntityMarked)
                {
                    // Krok 2: Jeśli encja została poprawnie oznaczona, zapisz zmiany.
                    int changesMade = await _unitOfWork.CompleteAsync();
                    _logger.LogInformation("Successfully cancelled registration for UserID {UserId} for EventID {EventId}. Changes saved: {ChangesCount}", userId, eventId, changesMade);
                    return true;
                }
                else
                {
                    // RemoveParticipantAsync zwróciło false (np. użytkownik nie był zapisany)
                    _logger.LogWarning("Cancellation pre-check failed for UserID {UserId} for EventID {EventId}. RemoveParticipantAsync returned false.", userId, eventId);
                    return false;
                }
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while cancelling registration for UserID {UserId} for EventID {EventId}.", userId, eventId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Generic error occurred while cancelling registration for UserID {UserId} for EventID {EventId}", eventId, userId);
                throw;
            }
        }

        public async Task UpdateParticipationStatusAsync(int eventId, int userId, ParticipationStatus status)
        {
            // Ta metoda może również skorzystać z ExecuteInTransactionAsync dla pewności
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // Uwaga: GetEventWithDetailsAsync ładuje uczestników. Jeśli EventParticipant to oddzielna encja,
                // być może lepiej byłoby pobrać bezpośrednio EventParticipant.
                var eventParticipant = await _unitOfWork.Context.EventParticipants // Załóżmy, że masz dostęp do _context przez UoW
                    .FirstOrDefaultAsync(ep => ep.EventId == eventId && ep.UserId == userId);

                if (eventParticipant == null)
                {
                    _logger.LogWarning("Participant UserID {UserId} for EventID {EventId} not found for status update.", userId, eventId);
                    throw new KeyNotFoundException($"User with ID {userId} is not registered for event with ID {eventId}");
                }

                eventParticipant.ParticipationStatus = status;
                // _unitOfWork.UpdateEntity(eventParticipant); // Jeśli nie jest śledzona lub wymaga jawnego Update
                // _unitOfWork.CompleteAsync() zostanie wywołane przez ExecuteInTransactionAsync
                _logger.LogInformation("Participation status for UserID {UserId}, EventID {EventId} updated to {Status}.", userId, eventId, status);
            });
        }
        // Dodaj metodę do IUnitOfWork, aby uzyskać dostęp do kontekstu, jeśli potrzebne
        // public interface IUnitOfWork
        // {
        //     // ... inne interfejsy ...
        //     LeafLoopDbContext Context { get; } // Przykład
        // }
    }
}