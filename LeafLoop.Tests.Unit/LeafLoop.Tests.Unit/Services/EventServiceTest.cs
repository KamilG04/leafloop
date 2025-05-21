using AutoMapper;
using LeafLoop.Models; 
using LeafLoop.Repositories.Interfaces;
using LeafLoop.Services;              
using LeafLoop.Services.DTOs;         
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions; 
using Moq;
using Xunit;

namespace LeafLoop.Tests.Unit.Services 
{
    public class EventServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IEventRepository> _mockEventRepository;
        private readonly Mock<IAddressRepository> _mockAddressRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly ILogger<EventService> _logger; // Using NullLogger for simplicity
        private readonly EventService _eventService;

        public EventServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockEventRepository = new Mock<IEventRepository>();
            _mockAddressRepository = new Mock<IAddressRepository>();
            _mockMapper = new Mock<IMapper>();
            _logger = NullLogger<EventService>.Instance;

            // Setup UnitOfWork mocks to return specific repository mocks
            _mockUnitOfWork.Setup(uow => uow.Events).Returns(_mockEventRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.Addresses).Returns(_mockAddressRepository.Object);

            // Instantiate the service with mocked dependencies
            _eventService = new EventService(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _logger
            );
        }

        // --- Tests for GetEventByIdAsync ---
        [Fact]
        public async Task GetEventByIdAsync_EventExists_ShouldReturnMappedEventDto()
        {
            // Arrange: Mock repository to return an event entity. Mock mapper to convert entity to DTO.
            var eventId = 1;
            var eventEntity = new Event { Id = eventId, Name = "Eco Meetup", StartDate = DateTime.UtcNow.AddDays(1) };
            var expectedDto = new EventDto { Id = eventId, Name = "Eco Meetup" };

            _mockEventRepository.Setup(repo => repo.GetByIdAsync(eventId)).ReturnsAsync(eventEntity);
            _mockMapper.Setup(mapper => mapper.Map<EventDto>(eventEntity)).Returns(expectedDto);

            // Act: Call the service method.
            var result = await _eventService.GetEventByIdAsync(eventId);

            // Assert: Verify the DTO is returned, is of correct type, and its properties match.
            Assert.NotNull(result);
            Assert.IsType<EventDto>(result);
            Assert.Equal(expectedDto.Id, result.Id);
            _mockEventRepository.Verify(repo => repo.GetByIdAsync(eventId), Times.Once);
            _mockMapper.Verify(mapper => mapper.Map<EventDto>(eventEntity), Times.Once);
        }

        [Fact]
        public async Task GetEventByIdAsync_EventDoesNotExist_ShouldReturnNull()
        {
            // Arrange: Mock repository to return null for a non-existent ID. Mapper setup for null input.
            var eventId = 99;
            _mockEventRepository.Setup(repo => repo.GetByIdAsync(eventId)).ReturnsAsync((Event)null);
            _mockMapper.Setup(mapper => mapper.Map<EventDto>(null)).Returns((EventDto)null);

            // Act: Call the service method.
            var result = await _eventService.GetEventByIdAsync(eventId);

            // Assert: Verify that null is returned.
            Assert.Null(result);
            _mockEventRepository.Verify(repo => repo.GetByIdAsync(eventId), Times.Once);
            _mockMapper.Verify(mapper => mapper.Map<EventDto>(null), Times.Once);
        }

        // --- Tests for GetEventWithDetailsAsync ---
        [Fact]
        public async Task GetEventWithDetailsAsync_EventExists_ShouldReturnMappedEventWithDetailsDto()
        {
            // Arrange: Mock repository to return an event entity with details.
            var eventId = 1;
            var eventEntity = new Event { Id = eventId, Name = "Detailed Eco Event", Description = "Full details." };
            var expectedDto = new EventWithDetailsDto { Id = eventId, Name = "Detailed Eco Event", Description = "Full details." };

            _mockEventRepository.Setup(repo => repo.GetEventWithDetailsAsync(eventId)).ReturnsAsync(eventEntity);
            _mockMapper.Setup(mapper => mapper.Map<EventWithDetailsDto>(eventEntity)).Returns(expectedDto);

            // Act
            var result = await _eventService.GetEventWithDetailsAsync(eventId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedDto.Description, result.Description); // Example property check
            _mockEventRepository.Verify(repo => repo.GetEventWithDetailsAsync(eventId), Times.Once);
            _mockMapper.Verify(mapper => mapper.Map<EventWithDetailsDto>(eventEntity), Times.Once);
        }

        [Fact]
        public async Task GetEventWithDetailsAsync_EventDoesNotExist_ShouldReturnNull()
        {
            // Arrange: Mock repository to return null.
            var eventId = 99;
            _mockEventRepository.Setup(repo => repo.GetEventWithDetailsAsync(eventId)).ReturnsAsync((Event)null);
            _mockMapper.Setup(mapper => mapper.Map<EventWithDetailsDto>(null)).Returns((EventWithDetailsDto)null);
            
            // Act
            var result = await _eventService.GetEventWithDetailsAsync(eventId);

            // Assert
            Assert.Null(result);
            _mockEventRepository.Verify(repo => repo.GetEventWithDetailsAsync(eventId), Times.Once);
        }

        // --- Tests for CreateEventAsync ---
        [Fact]
        public async Task CreateEventAsync_WithAddress_ShouldCreateEventAndAddress_ReturnsNewEventId()
        {
            // Arrange: DTO includes address. Mock mappers and repository Add/Complete calls.
            var eventCreateDto = new EventCreateDto
            {
                Name = "Park Cleanup", Address = new AddressDto { Street = "Main St 1", City = "Greenville" }
            };
            var mappedEventEntity = new Event(); // Mapper output for Event
            var mappedAddressEntity = new Address(); // Mapper output for Address

            _mockMapper.Setup(m => m.Map<Event>(eventCreateDto)).Returns(mappedEventEntity);
            _mockMapper.Setup(m => m.Map<Address>(eventCreateDto.Address)).Returns(mappedAddressEntity);

            // Simulate DB assigning ID to Address after AddAsync
            _mockAddressRepository.Setup(repo => repo.AddAsync(mappedAddressEntity))
                .Callback<Address>(addr => addr.Id = 10) 
                .Returns(Task.CompletedTask);

            // Simulate DB assigning ID to Event after AddAsync
            _mockEventRepository.Setup(repo => repo.AddAsync(mappedEventEntity))
                .Callback<Event>(ev => ev.Id = 100)
                .Returns(Task.CompletedTask);
            
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync()).ReturnsAsync(1); // Successful save

            // Act
            var newEventId = await _eventService.CreateEventAsync(eventCreateDto);

            // Assert: Check returned ID, AddressId linkage, and mock invocations.
            Assert.Equal(100, newEventId);
            Assert.Equal(10, mappedEventEntity.AddressId); // Verify foreign key linkage

            _mockMapper.Verify(m => m.Map<Event>(eventCreateDto), Times.Once);
            _mockMapper.Verify(m => m.Map<Address>(eventCreateDto.Address), Times.Once);
            _mockAddressRepository.Verify(repo => repo.AddAsync(mappedAddressEntity), Times.Once);
            _mockEventRepository.Verify(repo => repo.AddAsync(mappedEventEntity), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Exactly(2)); // Once for Address, once for Event
        }

        [Fact]
        public async Task CreateEventAsync_WithoutAddress_ShouldCreateEventOnly_ReturnsNewEventId()
        {
            // Arrange: DTO does not include address.
            var eventCreateDto = new EventCreateDto { Name = "Online Webinar" };
            var mappedEventEntity = new Event();
            _mockMapper.Setup(m => m.Map<Event>(eventCreateDto)).Returns(mappedEventEntity);
            _mockEventRepository.Setup(repo => repo.AddAsync(mappedEventEntity))
                .Callback<Event>(ev => ev.Id = 101)
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync()).ReturnsAsync(1);

            // Act
            var newEventId = await _eventService.CreateEventAsync(eventCreateDto);

            // Assert: Check ID, null AddressId, and that AddressRepository.AddAsync was not called.
            Assert.Equal(101, newEventId);
            Assert.Null(mappedEventEntity.AddressId); // Or 0 if AddressId is not nullable and not set
            _mockAddressRepository.Verify(repo => repo.AddAsync(It.IsAny<Address>()), Times.Never);
            _mockEventRepository.Verify(repo => repo.AddAsync(mappedEventEntity), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Once); // Only for Event
        }

        // --- Tests for DeleteEventAsync ---
        [Fact]
        public async Task DeleteEventAsync_UserIsOrganizer_ShouldDeleteEvent()
        {
            // Arrange: Event exists, and user is the correct organizer.
            var eventId = 1;
            var organizerId = 10;
            var organizerType = OrganizerType.User;
            var eventEntity = new Event { Id = eventId, OrganizerId = organizerId, OrganizerType = organizerType };

            _mockEventRepository.Setup(repo => repo.GetByIdAsync(eventId)).ReturnsAsync(eventEntity);
            _mockUnitOfWork.Setup(uow => uow.Events.Remove(eventEntity));
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync()).ReturnsAsync(1);

            // Act
            await _eventService.DeleteEventAsync(eventId, organizerId, organizerType);

            // Assert: Verify repository and UoW calls.
            _mockEventRepository.Verify(repo => repo.GetByIdAsync(eventId), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.Events.Remove(eventEntity), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteEventAsync_EventNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange: Event with given ID does not exist.
            var eventId = 99;
            _mockEventRepository.Setup(repo => repo.GetByIdAsync(eventId)).ReturnsAsync((Event)null);

            // Act & Assert: Expect KeyNotFoundException.
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _eventService.DeleteEventAsync(eventId, 10, OrganizerType.User)
            );
            Assert.Equal($"Event with ID {eventId} not found", ex.Message);
            _mockUnitOfWork.Verify(uow => uow.Events.Remove(It.IsAny<Event>()), Times.Never);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteEventAsync_UserIsNotOrganizer_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange: Event exists, but user is not the organizer.
            var eventId = 1;
            var actualOrganizerId = 10;
            var wrongOrganizerId = 20; // Different user trying to delete
            var organizerType = OrganizerType.User;
            var eventEntity = new Event { Id = eventId, OrganizerId = actualOrganizerId, OrganizerType = organizerType };

            _mockEventRepository.Setup(repo => repo.GetByIdAsync(eventId)).ReturnsAsync(eventEntity);

            // Act & Assert: Expect UnauthorizedAccessException.
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _eventService.DeleteEventAsync(eventId, wrongOrganizerId, organizerType)
            );
            _mockUnitOfWork.Verify(uow => uow.Events.Remove(It.IsAny<Event>()), Times.Never);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Never);
        }

        // --- Tests for RegisterForEventAsync ---
        [Fact]
        public async Task RegisterForEventAsync_WhenSuccessful_ShouldReturnTrue()
        {
            // Arrange: Mock repository's AddParticipantAsync to return true.
            var eventId = 1;
            var userId = 1;
            _mockEventRepository.Setup(repo => repo.AddParticipantAsync(eventId, userId)).ReturnsAsync(true);

            // Act
            var result = await _eventService.RegisterForEventAsync(eventId, userId);

            // Assert
            Assert.True(result);
            _mockEventRepository.Verify(repo => repo.AddParticipantAsync(eventId, userId), Times.Once);
        }

        [Fact]
        public async Task RegisterForEventAsync_WhenFails_ShouldReturnFalse()
        {
            // Arrange: Mock repository's AddParticipantAsync to return false (e.g., event full, user already registered).
            var eventId = 1;
            var userId = 1;
            _mockEventRepository.Setup(repo => repo.AddParticipantAsync(eventId, userId)).ReturnsAsync(false);

            // Act
            var result = await _eventService.RegisterForEventAsync(eventId, userId);

            // Assert
            Assert.False(result);
        }

        // --- Tests for UpdateParticipationStatusAsync ---
        [Fact]
        public async Task UpdateParticipationStatusAsync_EventNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var eventId = 99;
            _mockEventRepository.Setup(repo => repo.GetEventWithDetailsAsync(eventId)).ReturnsAsync((Event)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _eventService.UpdateParticipationStatusAsync(eventId, 1, ParticipationStatus.Confirmed)
            );
        }

        [Fact]
        public async Task UpdateParticipationStatusAsync_ParticipantNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange: Event exists, but participant is not in the event's list.
            var eventId = 1;
            var userIdNotInEvent = 99;
            var eventEntity = new Event { Id = eventId, Participants = new List<EventParticipant>() }; 
            _mockEventRepository.Setup(repo => repo.GetEventWithDetailsAsync(eventId)).ReturnsAsync(eventEntity);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _eventService.UpdateParticipationStatusAsync(eventId, userIdNotInEvent, ParticipationStatus.Confirmed)
            );
            Assert.Contains($"User with ID {userIdNotInEvent} is not registered for event with ID {eventId}", ex.Message);
        }
        
        [Fact]
        public async Task UpdateParticipationStatusAsync_ValidData_ShouldUpdateStatusAndComplete()
        {
            // Arrange: Event and participant exist.
            var eventId = 1;
            var userId = 1;
            var initialStatus = ParticipationStatus.Registered; // Corrected from Pending
            var newStatus = ParticipationStatus.Confirmed;
            var participantToUpdate = new EventParticipant { UserId = userId, EventId = eventId, ParticipationStatus = initialStatus };
            var eventEntity = new Event { 
                Id = eventId, 
                Participants = new List<EventParticipant> { participantToUpdate }
            };

            _mockEventRepository.Setup(repo => repo.GetEventWithDetailsAsync(eventId)).ReturnsAsync(eventEntity);
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync()).ReturnsAsync(1);

            // Act
            await _eventService.UpdateParticipationStatusAsync(eventId, userId, newStatus);

            // Assert: Verify status was changed on the entity and save was called.
            Assert.Equal(newStatus, participantToUpdate.ParticipationStatus);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Once);
        }
        
    }
}