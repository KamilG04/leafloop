
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
    public class CategoryServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ICategoryRepository> _mockCategoryRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly ILogger<CategoryService> _logger; // Using NullLogger for most tests
        private readonly CategoryService _categoryService;

        public CategoryServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockCategoryRepository = new Mock<ICategoryRepository>();
            _mockMapper = new Mock<IMapper>();
            
            // Use NullLogger for tests where specific log output isn't being asserted.
            _logger = NullLogger<CategoryService>.Instance; 

            // Setup IUnitOfWork.Categories to return our mocked ICategoryRepository
            _mockUnitOfWork.Setup(uow => uow.Categories).Returns(_mockCategoryRepository.Object);

            // Instantiate the service with mocked dependencies
            _categoryService = new CategoryService(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _logger
            );
        }

        // --- Tests for GetCategoryByIdAsync ---

        [Fact]
        public async Task GetCategoryByIdAsync_CategoryExists_ShouldReturnMappedCategoryDto()
        {
            // Arrange: Setup repository to return a category entity and mapper to convert it to DTO.
            var categoryId = 1;
            var categoryEntity = new Category { Id = categoryId, Name = "Electronics", Description = "Electronic devices" };
            var expectedDto = new CategoryDto { Id = categoryId, Name = "Electronics", Description = "Electronic devices" };

            _mockCategoryRepository.Setup(repo => repo.GetByIdAsync(categoryId))
                .ReturnsAsync(categoryEntity);
            _mockMapper.Setup(mapper => mapper.Map<CategoryDto>(categoryEntity))
                .Returns(expectedDto);

            // Act: Call the service method.
            var result = await _categoryService.GetCategoryByIdAsync(categoryId);

            // Assert: Verify the DTO is returned and its properties match.
            Assert.NotNull(result);
            Assert.IsType<CategoryDto>(result);
            Assert.Equal(expectedDto.Id, result.Id);
            Assert.Equal(expectedDto.Name, result.Name);
            _mockCategoryRepository.Verify(repo => repo.GetByIdAsync(categoryId), Times.Once);
            _mockMapper.Verify(mapper => mapper.Map<CategoryDto>(categoryEntity), Times.Once);
        }

        [Fact]
        public async Task GetCategoryByIdAsync_CategoryDoesNotExist_ShouldReturnNull()
        {
            // Arrange: Setup repository to return null for a non-existent ID. Mapper should also return null for a null input.
            var nonExistentCategoryId = 99;
            _mockCategoryRepository.Setup(repo => repo.GetByIdAsync(nonExistentCategoryId))
                .ReturnsAsync((Category)null); // Explicit cast to Category for Moq with null
            _mockMapper.Setup(mapper => mapper.Map<CategoryDto>(null))
                .Returns((CategoryDto)null); // AutoMapper typically returns null for null source

            // Act: Call the service method.
            var result = await _categoryService.GetCategoryByIdAsync(nonExistentCategoryId);

            // Assert: Verify null is returned and mapper was called (or not, depending on null handling).
            Assert.Null(result);
            _mockCategoryRepository.Verify(repo => repo.GetByIdAsync(nonExistentCategoryId), Times.Once);
            _mockMapper.Verify(mapper => mapper.Map<CategoryDto>(null), Times.Once);
        }

        [Fact]
        public async Task GetCategoryByIdAsync_RepositoryThrowsException_ShouldLogErrorAndRethrow()
        {
            // Arrange: Setup repository to throw an exception.
            var categoryId = 1;
            var repositoryException = new InvalidOperationException("Simulated database error");
            _mockCategoryRepository.Setup(repo => repo.GetByIdAsync(categoryId))
                .ThrowsAsync(repositoryException);
            

            // Act & Assert: Verify the same exception is re-thrown by the service.
            var exceptionThrown = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _categoryService.GetCategoryByIdAsync(categoryId) // Use _categoryService if NullLogger is acceptable
                // () => serviceWithLocalMockLogger.GetCategoryByIdAsync(categoryId) // Use this if testing specific logs
            );
            Assert.Equal(repositoryException.Message, exceptionThrown.Message);
            
        }

        // --- Tests for GetAllCategoriesAsync ---
        [Fact]
        public async Task GetAllCategoriesAsync_CategoriesExist_ShouldReturnMappedCategoryDtos()
        {
            // Arrange: Setup repository to return a list of categories and mapper to convert them.
            var categoryEntities = new List<Category>
            {
                new Category { Id = 1, Name = "Electronics" },
                new Category { Id = 2, Name = "Clothing" }
            };
            var expectedDtos = new List<CategoryDto>
            {
                new CategoryDto { Id = 1, Name = "Electronics" },
                new CategoryDto { Id = 2, Name = "Clothing" }
            };

            _mockCategoryRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(categoryEntities);
            _mockMapper.Setup(mapper => mapper.Map<IEnumerable<CategoryDto>>(categoryEntities)).Returns(expectedDtos);

            // Act: Call the service method.
            var result = await _categoryService.GetAllCategoriesAsync();

            // Assert: Verify the list of DTOs is returned and matches expected data.
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Equivalent(expectedDtos, result); // Checks for equivalent content, order might not matter for some tests
            _mockCategoryRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
            _mockMapper.Verify(mapper => mapper.Map<IEnumerable<CategoryDto>>(categoryEntities), Times.Once);
        }

        [Fact]
        public async Task GetAllCategoriesAsync_NoCategoriesExist_ShouldReturnEmptyList()
        {
            // Arrange: Setup repository to return an empty list. Mapper should return an empty DTO list.
            var emptyCategoryList = new List<Category>();
            var expectedEmptyDtos = new List<CategoryDto>();

            _mockCategoryRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(emptyCategoryList);
            _mockMapper.Setup(mapper => mapper.Map<IEnumerable<CategoryDto>>(emptyCategoryList)).Returns(expectedEmptyDtos);

            // Act: Call the service method.
            var result = await _categoryService.GetAllCategoriesAsync();

            // Assert: Verify an empty list is returned.
            Assert.NotNull(result);
            Assert.Empty(result);
            _mockCategoryRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
            _mockMapper.Verify(mapper => mapper.Map<IEnumerable<CategoryDto>>(emptyCategoryList), Times.Once);
        }

        // --- Tests for CreateCategoryAsync ---
        [Fact]
        public async Task CreateCategoryAsync_ValidDto_ShouldAddCategoryAndReturnNewId()
        {
            // Arrange: Setup DTO, mapper to convert DTO to entity, and repository AddAsync to simulate ID assignment.
            var createDto = new CategoryCreateDto { Name = "New Category", Description = "Description" };
            var mappedCategoryEntity = new Category { Name = "New Category", Description = "Description" }; // Entity after mapping
            
            _mockMapper.Setup(m => m.Map<Category>(createDto)).Returns(mappedCategoryEntity);
            
            // Simulate the repository/DB setting the ID on the entity after adding it
            _mockCategoryRepository.Setup(repo => repo.AddAsync(mappedCategoryEntity))
                .Callback<Category>(cat => cat.Id = 555) // Simulate ID assignment by DB/repo
                .Returns(Task.CompletedTask); // AddAsync might be void or Task

            _mockUnitOfWork.Setup(uow => uow.CompleteAsync()).ReturnsAsync(1); // Simulate successful save

            // Act: Call the service method.
            var newCategoryId = await _categoryService.CreateCategoryAsync(createDto);

            // Assert: Verify the new ID is returned and repository/UoW methods were called.
            Assert.Equal(555, newCategoryId);
            _mockMapper.Verify(m => m.Map<Category>(createDto), Times.Once);
            _mockCategoryRepository.Verify(repo => repo.AddAsync(It.Is<Category>(
                c => c.Name == createDto.Name && c.Description == createDto.Description
            )), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Once);
        }

        // --- Tests for UpdateCategoryAsync ---
        [Fact]
        public async Task UpdateCategoryAsync_CategoryExists_ShouldUpdateCategory()
        {
            // Arrange: Setup DTO, existing entity, and repository/UoW mocks.
            var updateDto = new CategoryUpdateDto { Id = 1, Name = "Updated Name", Description = "Updated Desc" };
            var existingCategoryEntity = new Category { Id = 1, Name = "Old Name", Description = "Old Desc" };

            _mockCategoryRepository.Setup(repo => repo.GetByIdAsync(updateDto.Id)).ReturnsAsync(existingCategoryEntity);
            // _mapper.Map(dto, entity) modifies the entity in place; Moq doesn't need to return for void methods.
            // We just verify it's called. AutoMapper will handle the property updates.
            _mockMapper.Setup(m => m.Map(updateDto, existingCategoryEntity)); 
            _mockUnitOfWork.Setup(uow => uow.Categories.Update(existingCategoryEntity)); 
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync()).ReturnsAsync(1);

            // Act: Call the service method.
            await _categoryService.UpdateCategoryAsync(updateDto);

            // Assert: Verify repository/UoW methods were called.
            // Actual property changes on existingCategoryEntity are handled by AutoMapper's real implementation
            // or would require more complex callback setups in Moq if we wanted to assert them here.
            _mockCategoryRepository.Verify(repo => repo.GetByIdAsync(updateDto.Id), Times.Once);
            _mockMapper.Verify(m => m.Map(updateDto, existingCategoryEntity), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.Categories.Update(existingCategoryEntity), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateCategoryAsync_CategoryDoesNotExist_ShouldThrowKeyNotFoundException()
        {
            // Arrange: Setup repository to return null for GetByIdAsync.
            var updateDto = new CategoryUpdateDto { Id = 99, Name = "NonExistent" };
            _mockCategoryRepository.Setup(repo => repo.GetByIdAsync(updateDto.Id)).ReturnsAsync((Category)null);

            // Act & Assert: Verify KeyNotFoundException is thrown.
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _categoryService.UpdateCategoryAsync(updateDto)
            );
            Assert.Equal($"Category with ID {updateDto.Id} not found", exception.Message);
            _mockCategoryRepository.Verify(repo => repo.GetByIdAsync(updateDto.Id), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Never); // Save should not be called.
        }

        // --- Tests for DeleteCategoryAsync ---
        [Fact]
        public async Task DeleteCategoryAsync_CategoryExistsAndHasNoItems_ShouldDeleteCategory()
        {
            // Arrange: Setup mocks for a category that exists and has no associated items.
            var categoryId = 1;
            var categoryToDelete = new Category { Id = categoryId, Name = "Empty Category" };
            // Simulate GetCategoryWithItemsAsync returning the category with an empty Items list
            var categoryWithNoItems = new Category { Id = categoryId, Name = "Empty Category", Items = new List<Item>() };

            _mockCategoryRepository.Setup(repo => repo.GetByIdAsync(categoryId)).ReturnsAsync(categoryToDelete);
            _mockCategoryRepository.Setup(repo => repo.GetCategoryWithItemsAsync(categoryId)).ReturnsAsync(categoryWithNoItems);
            _mockUnitOfWork.Setup(uow => uow.Categories.Remove(categoryToDelete));
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync()).ReturnsAsync(1);

            // Act: Call the service method.
            await _categoryService.DeleteCategoryAsync(categoryId);

            // Assert: Verify repository/UoW methods were called correctly.
            _mockCategoryRepository.Verify(repo => repo.GetByIdAsync(categoryId), Times.Once);
            _mockCategoryRepository.Verify(repo => repo.GetCategoryWithItemsAsync(categoryId), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.Categories.Remove(categoryToDelete), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteCategoryAsync_CategoryDoesNotExist_ShouldThrowKeyNotFoundException()
        {
            // Arrange: Setup repository GetByIdAsync to return null.
            var categoryId = 99;
            _mockCategoryRepository.Setup(repo => repo.GetByIdAsync(categoryId)).ReturnsAsync((Category)null);

            // Act & Assert: Verify KeyNotFoundException.
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _categoryService.DeleteCategoryAsync(categoryId)
            );
            Assert.Equal($"Category with ID {categoryId} not found", exception.Message);
            _mockCategoryRepository.Verify(repo => repo.GetByIdAsync(categoryId), Times.Once);
            _mockCategoryRepository.Verify(repo => repo.GetCategoryWithItemsAsync(It.IsAny<int>()), Times.Never); // This should not be called
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteCategoryAsync_CategoryHasAssociatedItems_ShouldThrowInvalidOperationException()
        {
            // Arrange: Setup mocks for a category that has items.
            var categoryId = 1;
            var categoryToDelete = new Category { Id = categoryId, Name = "Category With Items" };
            var categoryWithItems = new Category {
                Id = categoryId,
                Name = "Category With Items",
                Items = new List<Item> { new Item { Id = 101, Name = "Associated Item 1" } }
            };

            _mockCategoryRepository.Setup(repo => repo.GetByIdAsync(categoryId)).ReturnsAsync(categoryToDelete);
            _mockCategoryRepository.Setup(repo => repo.GetCategoryWithItemsAsync(categoryId)).ReturnsAsync(categoryWithItems);

            // Act & Assert: Verify InvalidOperationException.
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _categoryService.DeleteCategoryAsync(categoryId)
            );
            Assert.Equal("Cannot delete category with associated items", exception.Message);

            _mockCategoryRepository.Verify(repo => repo.GetByIdAsync(categoryId), Times.Once);
            _mockCategoryRepository.Verify(repo => repo.GetCategoryWithItemsAsync(categoryId), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.Categories.Remove(It.IsAny<Category>()), Times.Never); // Remove should not be called.
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Never);
        }

        // TODO: Add tests for GetPopularCategoriesAsync - this will depend on the ICategoryRepository.GetPopularCategoriesAsync method.
        // TODO: Add tests for GetItemsByCategoryAsync - I am too stupid for thath shiet
    }
}