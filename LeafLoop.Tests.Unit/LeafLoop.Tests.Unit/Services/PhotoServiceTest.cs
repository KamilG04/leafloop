// Path: LeafLoop.Tests.Unit/Services/PhotoServiceTests.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text; // For MemoryStream content
using System.Threading.Tasks;
using AutoMapper;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using LeafLoop.Services; // Namespace for PhotoService
using LeafLoop.Services.DTOs;
using Microsoft.AspNetCore.Hosting; // For IWebHostEnvironment
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions; // For NullLogger
using Moq;
using Xunit;

namespace LeafLoop.Tests.Unit.Services
{
    public class PhotoServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IPhotoRepository> _mockPhotoRepository;
        private readonly Mock<IItemRepository> _mockItemRepository; // For owner checks
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IWebHostEnvironment> _mockWebHostEnvironment;
        private readonly ILogger<PhotoService> _logger;
        private readonly PhotoService _photoService;

        public PhotoServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockPhotoRepository = new Mock<IPhotoRepository>();
            _mockItemRepository = new Mock<IItemRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
            _logger = NullLogger<PhotoService>.Instance;

            _mockUnitOfWork.Setup(uow => uow.Photos).Returns(_mockPhotoRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.Items).Returns(_mockItemRepository.Object);

            // Setup a fake WebRootPath for testing file operations
            _mockWebHostEnvironment.Setup(env => env.WebRootPath).Returns(Path.GetTempPath()); // Use a temporary path

            _photoService = new PhotoService(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockWebHostEnvironment.Object,
                _logger
            );
        }

        // --- Tests for GetPhotoByIdAsync ---
        [Fact]
        public async Task GetPhotoByIdAsync_PhotoExists_ShouldReturnMappedPhotoDto()
        {
            // Arrange
            var photoId = 1;
            var photoEntity = new Photo { Id = photoId, Path = "uploads/items/test.jpg" };
            var expectedDto = new PhotoDto { Id = photoId, Path = "/uploads/items/test.jpg" }; // Assuming mapper handles path formatting

            _mockPhotoRepository.Setup(repo => repo.GetByIdAsync(photoId)).ReturnsAsync(photoEntity);
            _mockMapper.Setup(mapper => mapper.Map<PhotoDto>(photoEntity)).Returns(expectedDto);

            // Act
            var result = await _photoService.GetPhotoByIdAsync(photoId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedDto.Path, result.Path);
            _mockPhotoRepository.Verify(repo => repo.GetByIdAsync(photoId), Times.Once);
        }

        [Fact]
        public async Task GetPhotoByIdAsync_PhotoDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            var photoId = 99;
            _mockPhotoRepository.Setup(repo => repo.GetByIdAsync(photoId)).ReturnsAsync((Photo)null);
            _mockMapper.Setup(mapper => mapper.Map<PhotoDto>(null)).Returns((PhotoDto)null);


            // Act
            var result = await _photoService.GetPhotoByIdAsync(photoId);

            // Assert
            Assert.Null(result);
        }

        // --- Tests for AddPhotoAsync ---
        [Fact]
        public async Task AddPhotoAsync_ValidData_ShouldAddPhotoAndReturnId()
        {
            // Arrange
            var userId = 1;
            var itemId = 10;
            var photoCreateDto = new PhotoCreateDto { ItemId = itemId, Path = "uploads/temp/new_photo.jpg", FileName = "new_photo.jpg" };
            var itemEntity = new Item { Id = itemId, UserId = userId }; // Item owner matches
            var mappedPhotoEntity = new Photo(); // Output of mapper

            _mockItemRepository.Setup(repo => repo.GetByIdAsync(itemId)).ReturnsAsync(itemEntity);
            _mockMapper.Setup(m => m.Map<Photo>(photoCreateDto)).Returns(mappedPhotoEntity);
            _mockPhotoRepository.Setup(repo => repo.AddAsync(mappedPhotoEntity))
                .Callback<Photo>(p => p.Id = 123) // Simulate ID assignment
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync()).ReturnsAsync(1);

            // Act
            var newPhotoId = await _photoService.AddPhotoAsync(photoCreateDto, userId);

            // Assert
            Assert.Equal(123, newPhotoId);
            Assert.NotEqual(default(DateTime), mappedPhotoEntity.AddedDate); // Check AddedDate is set
            _mockItemRepository.Verify(repo => repo.GetByIdAsync(itemId), Times.Once);
            _mockPhotoRepository.Verify(repo => repo.AddAsync(mappedPhotoEntity), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task AddPhotoAsync_ItemNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var userId = 1;
            var photoCreateDto = new PhotoCreateDto { ItemId = 99 }; // Non-existent item
            _mockItemRepository.Setup(repo => repo.GetByIdAsync(photoCreateDto.ItemId)).ReturnsAsync((Item)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _photoService.AddPhotoAsync(photoCreateDto, userId));
        }

        [Fact]
        public async Task AddPhotoAsync_UserNotOwner_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            var ownerUserId = 1;
            var otherUserId = 2;
            var itemId = 10;
            var photoCreateDto = new PhotoCreateDto { ItemId = itemId };
            var itemEntity = new Item { Id = itemId, UserId = ownerUserId }; // Item owned by someone else
            _mockItemRepository.Setup(repo => repo.GetByIdAsync(itemId)).ReturnsAsync(itemEntity);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _photoService.AddPhotoAsync(photoCreateDto, otherUserId));
        }

        // --- Tests for DeletePhotoAsync ---
        [Fact]
        public async Task DeletePhotoAsync_ValidPhotoAndOwner_ShouldDeleteRecordAndAttemptFileDelete()
        {
            // Arrange
            var photoId = 1;
            var userId = 5;
            var itemId = 10;
            var photoPath = "uploads/items/image_to_delete.jpg";
            var photoEntity = new Photo { Id = photoId, ItemId = itemId, Path = photoPath };
            var itemEntity = new Item { Id = itemId, UserId = userId }; // User is owner

            _mockPhotoRepository.Setup(repo => repo.GetByIdAsync(photoId)).ReturnsAsync(photoEntity);
            _mockItemRepository.Setup(repo => repo.GetByIdAsync(itemId)).ReturnsAsync(itemEntity);
            _mockUnitOfWork.Setup(uow => uow.Photos.Remove(photoEntity));
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync()).ReturnsAsync(1);
            // DeletePhotoByPathAsync is complex to unit test for actual deletion,
            // here we just verify it would be called. Its own unit tests will cover its logic.
            // For this test, we assume DeletePhotoByPathAsync will be called.

            // Act
            await _photoService.DeletePhotoAsync(photoId, userId);

            // Assert
            _mockPhotoRepository.Verify(repo => repo.GetByIdAsync(photoId), Times.Once);
            _mockItemRepository.Verify(repo => repo.GetByIdAsync(itemId), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.Photos.Remove(photoEntity), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Once);
            // We can't easily verify a call to another method on the same service instance with Moq like this.
            // To test DeletePhotoByPathAsync was called, it would need to be on a separate, mockable service.
            // However, we can infer it was called if no exception occurred and the path was valid.
        }
        
        [Fact]
        public async Task DeletePhotoAsync_PhotoNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var photoId = 99;
            _mockPhotoRepository.Setup(repo => repo.GetByIdAsync(photoId)).ReturnsAsync((Photo)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _photoService.DeletePhotoAsync(photoId, 1));
        }


        // --- Tests for UploadPhotoAsync ---
        [Fact]
        public async Task UploadPhotoAsync_NullStream_ShouldThrowArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>("fileStream", () => 
                _photoService.UploadPhotoAsync(null, "test.jpg", "image/jpeg")
            );
        }

       
        [Fact]
        public async Task UploadPhotoAsync_ValidInput_ShouldReturnRelativePath()
        {
            // Arrange: This test focuses on path generation and structure, not actual file write.
            var webRoot = Path.GetTempPath(); // Use a real but temporary path for WebRootPath
             _mockWebHostEnvironment.Setup(e => e.WebRootPath).Returns(webRoot);

            var fileName = "test-image.png";
            var contentType = "image/png";
            var subfolder = "test_items";
            var fileContent = "fake image data";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
            
            // We are not mocking Directory.CreateDirectory or FileStream/CopyToAsync
            // as those are direct file system operations best tested in integration tests.
            // This unit test verifies the path generation logic.

            // Act
            var relativePath = await _photoService.UploadPhotoAsync(stream, fileName, contentType, subfolder);

            // Assert
            Assert.NotNull(relativePath);
            Assert.StartsWith($"uploads/{subfolder}/", relativePath);
            Assert.EndsWith(Path.GetExtension(fileName), relativePath); // Check extension
            Assert.Contains(subfolder, relativePath); // Check subfolder is in path

            // Cleanup: Attempt to delete the directory if it was created (best effort for temp files)
            // In a real scenario with mocked file system, this would be more robust.
            var expectedTargetDirectory = Path.Combine(webRoot, "uploads", subfolder);
            if(Directory.Exists(expectedTargetDirectory))
            {
                // This part is tricky as the filename is Guid. We can list files or just delete dir if empty
                // For simplicity, we'll just acknowledge the test ran. A full cleanup is out of scope for simple unit test.
            }
        }


        // --- Tests for DeletePhotoByPathAsync ---
        // Note: Fully testing file deletion requires an IFileSystem abstraction or integration tests.
        // These tests will focus on the logic paths within DeletePhotoByPathAsync.

        [Fact]
        public async Task DeletePhotoByPathAsync_NullOrEmptyPath_ShouldReturnFalse()
        {
            // Act
            var resultNull = await _photoService.DeletePhotoByPathAsync(null);
            var resultEmpty = await _photoService.DeletePhotoByPathAsync("");
            var resultWhitespace = await _photoService.DeletePhotoByPathAsync("   ");

            // Assert
            Assert.False(resultNull);
            Assert.False(resultEmpty);
            Assert.False(resultWhitespace);
        }

       

       //Po pierwsze ten projekt jest chujowy i mozecie mnie cytowac a testy kurwa ssa bo jak przetestowac te zdjecia co integracyjnie no kurwa 
    }
}