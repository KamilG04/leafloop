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
    public class ItemServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IItemRepository> _mockItemRepository; // Mock for _unitOfWork.Items
        private readonly Mock<ITagRepository> _mockTagRepository; // Mock for _unitOfWork.Tags (for Add/RemoveTag)
        private readonly Mock<IMapper> _mockMapper;
        private readonly ILogger<ItemService> _logger;
        private readonly ItemService _itemService;

        public ItemServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockItemRepository = new Mock<IItemRepository>();
            _mockTagRepository = new Mock<ITagRepository>(); // Initialize TagRepository mock
            _mockMapper = new Mock<IMapper>();
            _logger = NullLogger<ItemService>.Instance; // Use NullLogger for simplicity

            // Setup IUnitOfWork to return specific repository mocks
            _mockUnitOfWork.Setup(uow => uow.Items).Returns(_mockItemRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.Tags).Returns(_mockTagRepository.Object); // Setup for Tags

            _itemService = new ItemService(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _logger
            );
        }

        // --- Tests for GetItemByIdAsync ---
        [Fact]
        public async Task GetItemByIdAsync_ItemExists_ShouldReturnMappedItemDto()
        {
            // Arrange: Mock repository returns an item, mapper converts to DTO.
            var itemId = 1;
            var itemEntity = new Item { Id = itemId, Name = "Test Item" };
            var expectedDto = new ItemDto { Id = itemId, Name = "Test Item" };

            _mockItemRepository.Setup(repo => repo.GetByIdAsync(itemId)).ReturnsAsync(itemEntity);
            _mockMapper.Setup(mapper => mapper.Map<ItemDto>(itemEntity)).Returns(expectedDto);

            // Act
            var result = await _itemService.GetItemByIdAsync(itemId);

            // Assert: Result is correct DTO, mocks were called.
            Assert.NotNull(result);
            Assert.Equal(expectedDto.Name, result.Name);
            _mockItemRepository.Verify(repo => repo.GetByIdAsync(itemId), Times.Once);
            _mockMapper.Verify(mapper => mapper.Map<ItemDto>(itemEntity), Times.Once);
        }

        [Fact]
        public async Task GetItemByIdAsync_ItemDoesNotExist_ShouldReturnNull()
        {
            // Arrange: Mock repository returns null.
            var itemId = 99;
            _mockItemRepository.Setup(repo => repo.GetByIdAsync(itemId)).ReturnsAsync((Item)null);
            _mockMapper.Setup(mapper => mapper.Map<ItemDto>(null)).Returns((ItemDto)null);


            // Act
            var result = await _itemService.GetItemByIdAsync(itemId);

            // Assert: Result is null.
            Assert.Null(result);
            _mockItemRepository.Verify(repo => repo.GetByIdAsync(itemId), Times.Once);
        }

        // --- Tests for AddItemAsync ---
        [Fact]
        public async Task AddItemAsync_ValidDto_ShouldAddItemAndReturnNewId()
        {
            // Arrange: Setup DTO, mapper, and repository AddAsync/CompleteAsync calls.
            var userId = 1;
            var createDto = new ItemCreateDto { Name = "New Item", CategoryId = 1 };
            var mappedItemEntity = new Item { Name = "New Item", CategoryId = 1 }; // Entity after mapping

            _mockMapper.Setup(m => m.Map<Item>(createDto)).Returns(mappedItemEntity);
            _mockItemRepository.Setup(repo => repo.AddAsync(mappedItemEntity))
                .Callback<Item>(item => item.Id = 123) // Simulate DB setting the ID
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync()).ReturnsAsync(1); // Simulate successful save

            // Act
            var newItemId = await _itemService.AddItemAsync(createDto, userId);

            // Assert: Verify ID, UserId/DateAdded set on entity, and mock calls.
            Assert.Equal(123, newItemId);
            Assert.Equal(userId, mappedItemEntity.UserId);
            Assert.True(mappedItemEntity.IsAvailable);
            Assert.NotEqual(default(DateTime), mappedItemEntity.DateAdded); // Check DateAdded was set
            _mockMapper.Verify(m => m.Map<Item>(createDto), Times.Once);
            _mockItemRepository.Verify(repo => repo.AddAsync(mappedItemEntity), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Once);
        }

        // --- Tests for DeleteItemAsync ---
        [Fact]
        public async Task DeleteItemAsync_ItemExistsAndUserIsOwner_ShouldDeleteItem()
        {
            // Arrange: Item exists, and the provided userId is the owner.
            var itemId = 1;
            var userId = 5;
            var itemEntity = new Item { Id = itemId, Name = "Test Item", UserId = userId };

            _mockItemRepository.Setup(repo => repo.GetByIdAsync(itemId)).ReturnsAsync(itemEntity);
            _mockUnitOfWork.Setup(uow => uow.Items.Remove(itemEntity));
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync()).ReturnsAsync(1);

            // Act
            await _itemService.DeleteItemAsync(itemId, userId);

            // Assert: Verify repository/UoW calls.
            _mockItemRepository.Verify(repo => repo.GetByIdAsync(itemId), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.Items.Remove(itemEntity), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteItemAsync_ItemDoesNotExist_ShouldThrowKeyNotFoundException()
        {
            // Arrange: Item does not exist.
            var itemId = 99;
            var userId = 5;
            _mockItemRepository.Setup(repo => repo.GetByIdAsync(itemId)).ReturnsAsync((Item)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _itemService.DeleteItemAsync(itemId, userId));
            Assert.Equal($"Item with ID {itemId} not found.", ex.Message);
            _mockUnitOfWork.Verify(uow => uow.Items.Remove(It.IsAny<Item>()), Times.Never);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteItemAsync_UserIsNotOwner_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange: Item exists, but user is not the owner.
            var itemId = 1;
            var ownerId = 5;
            var requesterId = 6; // Different user
            var itemEntity = new Item { Id = itemId, Name = "Test Item", UserId = ownerId };

            _mockItemRepository.Setup(repo => repo.GetByIdAsync(itemId)).ReturnsAsync(itemEntity);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _itemService.DeleteItemAsync(itemId, requesterId));
            _mockUnitOfWork.Verify(uow => uow.Items.Remove(It.IsAny<Item>()), Times.Never);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Never);
        }

        // --- Tests for IsItemOwnerAsync ---
        [Fact]
        public async Task IsItemOwnerAsync_UserIsOwner_ShouldReturnTrue()
        {
            // Arrange
            var itemId = 1;
            var userId = 5;
            var itemEntity = new Item { Id = itemId, UserId = userId };
            _mockItemRepository.Setup(repo => repo.GetByIdAsync(itemId)).ReturnsAsync(itemEntity);

            // Act
            var result = await _itemService.IsItemOwnerAsync(itemId, userId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsItemOwnerAsync_UserIsNotOwner_ShouldReturnFalse()
        {
            // Arrange
            var itemId = 1;
            var ownerId = 5;
            var requesterId = 6;
            var itemEntity = new Item { Id = itemId, UserId = ownerId };
            _mockItemRepository.Setup(repo => repo.GetByIdAsync(itemId)).ReturnsAsync(itemEntity);

            // Act
            var result = await _itemService.IsItemOwnerAsync(itemId, requesterId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsItemOwnerAsync_ItemDoesNotExist_ShouldReturnFalse()
        {
            // Arrange
            var itemId = 99;
            var userId = 5;
            _mockItemRepository.Setup(repo => repo.GetByIdAsync(itemId)).ReturnsAsync((Item)null);

            // Act
            var result = await _itemService.IsItemOwnerAsync(itemId, userId);

            // Assert
            Assert.False(result);
        }

        // --- Tests for GetItemsNearLocationAsync ---
        [Fact]
        public async Task GetItemsNearLocationAsync_ItemsFound_ShouldReturnPagedResultOfItemSummaryDtos()
        {
            // Arrange: Setup parameters and mock repository/mapper responses.
            var lat = 50.0m; var lon = 20.0m; var radius = 10m;
            int page = 1; int pageSize = 5;
            var itemEntities = new List<Item> { new Item { Id = 1, Name = "Nearby Item 1" }, new Item { Id = 2, Name = "Nearby Item 2" } };
            var itemSummaryDtos = new List<ItemSummaryDto> { new ItemSummaryDto { Id = 1, Name = "Nearby Item 1" }, new ItemSummaryDto { Id = 2, Name = "Nearby Item 2" } };
            var totalCount = 2;

            _mockItemRepository.Setup(r => r.GetItemsNearLocationAsync(lat, lon, radius, null, null, page, pageSize))
                .ReturnsAsync(itemEntities);
            _mockItemRepository.Setup(r => r.CountItemsNearLocationAsync(lat, lon, radius, null, null))
                .ReturnsAsync(totalCount);
            _mockMapper.Setup(m => m.Map<IEnumerable<ItemSummaryDto>>(itemEntities))
                .Returns(itemSummaryDtos);

            // Act
            var result = await _itemService.GetItemsNearLocationAsync(lat, lon, radius, null, null, page, pageSize);

            // Assert: Verify PagedResult contents and mock calls.
            Assert.NotNull(result);
            Assert.Equal(totalCount, result.TotalCount);
            Assert.Equal(page, result.PageNumber);
            Assert.Equal(pageSize, result.PageSize);
            var expectedCount = itemSummaryDtos.Count;
            var actualItemCollection = result.Items; // Zobaczmy, jaki jest typ tej kolekcji
            var actualCount = actualItemCollection.Count(); // Jeśli .Count to właściwość, lub .Count() jeśli metoda

            _logger.LogDebug("DEBUG: Expected count: {ExpectedCount}, Type: {ExpectedType}", expectedCount, expectedCount.GetType().FullName);
            _logger.LogDebug("DEBUG: Actual item collection type: {ActualCollectionType}", actualItemCollection?.GetType().FullName ?? "null");
            _logger.LogDebug("DEBUG: Actual count: {ActualCount}, Type: {ActualType}", actualCount, actualCount.GetType().FullName);

// Assert.Equal(expectedCount, actualCount); // Zakomentuj na chwilę oryginalną linię
            Assert.Equal<int>(expectedCount, actualCount); // Spróbuj z jawnym typem
 
            Assert.Equivalent(itemSummaryDtos, result.Items); // Checks content equivalence

            _mockItemRepository.Verify(r => r.GetItemsNearLocationAsync(lat, lon, radius, null, null, page, pageSize), Times.Once);
            _mockItemRepository.Verify(r => r.CountItemsNearLocationAsync(lat, lon, radius, null, null), Times.Once);
            _mockMapper.Verify(m => m.Map<IEnumerable<ItemSummaryDto>>(itemEntities), Times.Once);
        }

        [Fact]
        public async Task GetItemsNearLocationAsync_NoItemsFound_ShouldReturnEmptyPagedResult()
        {
            // Arrange: Setup for no items found.
            var lat = 50.0m; var lon = 20.0m; var radius = 10m;
            int page = 1; int pageSize = 5;
            _mockItemRepository.Setup(r => r.GetItemsNearLocationAsync(lat, lon, radius, null, null, page, pageSize))
                .ReturnsAsync(new List<Item>());
            _mockItemRepository.Setup(r => r.CountItemsNearLocationAsync(lat, lon, radius, null, null))
                .ReturnsAsync(0);
            _mockMapper.Setup(m => m.Map<IEnumerable<ItemSummaryDto>>(It.IsAny<List<Item>>())) // It.IsAny for empty list
                .Returns(new List<ItemSummaryDto>());

            // Act
            var result = await _itemService.GetItemsNearLocationAsync(lat, lon, radius, null, null, page, pageSize);

            // Assert: Verify empty PagedResult.
            Assert.NotNull(result);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task GetItemsNearLocationAsync_RepositoryThrows_ShouldRethrow()
        {
            // Arrange: Setup one of the repository methods to throw.
            var lat = 50.0m; var lon = 20.0m; var radius = 10m;
            _mockItemRepository.Setup(r => r.GetItemsNearLocationAsync(lat, lon, radius, null, null, 1, 20))
                .ThrowsAsync(new Exception("Repo error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => 
                _itemService.GetItemsNearLocationAsync(lat, lon, radius, null, null, 1, 20)
            );
        }


       
    }
}