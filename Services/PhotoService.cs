using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AutoMapper;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace LeafLoop.Services
{
    public class PhotoService : IPhotoService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<PhotoService> _logger;

        public PhotoService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IWebHostEnvironment environment,
            ILogger<PhotoService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PhotoDto> GetPhotoByIdAsync(int id)
        {
            try
            {
                var photo = await _unitOfWork.Photos.GetByIdAsync(id);
                return _mapper.Map<PhotoDto>(photo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting photo with ID: {PhotoId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<PhotoDto>> GetPhotosByItemAsync(int itemId)
        {
            try
            {
                var photos = await _unitOfWork.Photos.GetPhotosByItemAsync(itemId);
                return _mapper.Map<IEnumerable<PhotoDto>>(photos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting photos for item: {ItemId}", itemId);
                throw;
            }
        }

        public async Task<int> AddPhotoAsync(PhotoCreateDto photoDto, int userId)
        {
            try
            {
                // Verify that the user owns the item
                var item = await _unitOfWork.Items.GetByIdAsync(photoDto.ItemId);
                
                if (item == null)
                {
                    throw new KeyNotFoundException($"Item with ID {photoDto.ItemId} not found");
                }
                
                if (item.UserId != userId)
                {
                    throw new UnauthorizedAccessException("User is not authorized to add photos to this item");
                }
                
                var photo = _mapper.Map<Photo>(photoDto);
                photo.AddedDate = DateTime.UtcNow;
                
                await _unitOfWork.Photos.AddAsync(photo);
                await _unitOfWork.CompleteAsync();
                
                return photo.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding photo to item: {ItemId}", photoDto.ItemId);
                throw;
            }
        }

        public async Task DeletePhotoAsync(int id, int userId)
        {
            try
            {
                var photo = await _unitOfWork.Photos.GetByIdAsync(id);
                
                if (photo == null)
                {
                    throw new KeyNotFoundException($"Photo with ID {id} not found");
                }
                
                // Verify that the user owns the item this photo belongs to
                var item = await _unitOfWork.Items.GetByIdAsync(photo.ItemId);
                
                if (item == null)
                {
                    throw new KeyNotFoundException($"Item with ID {photo.ItemId} not found");
                }
                
                if (item.UserId != userId)
                {
                    throw new UnauthorizedAccessException("User is not authorized to delete this photo");
                }
                
                // Delete the physical file if it exists
                if (!string.IsNullOrEmpty(photo.Path) && File.Exists(Path.Combine(_environment.WebRootPath, photo.Path)))
                {
                    File.Delete(Path.Combine(_environment.WebRootPath, photo.Path));
                }
                
                _unitOfWork.Photos.Remove(photo);
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting photo with ID: {PhotoId}", id);
                throw;
            }
        }

        public async Task<string> UploadPhotoAsync(Stream fileStream, string fileName, string contentType)
        {
            try
            {
                // Create uploads directory if it doesn't exist
                var uploadsDirectory = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsDirectory))
                {
                    Directory.CreateDirectory(uploadsDirectory);
                }
                
                // Create items directory if it doesn't exist
                var itemsDirectory = Path.Combine(uploadsDirectory, "items");
                if (!Directory.Exists(itemsDirectory))
                {
                    Directory.CreateDirectory(itemsDirectory);
                }
                
                // Generate a unique file name to prevent overwriting
                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
                var filePath = Path.Combine(itemsDirectory, uniqueFileName);
                
                // Save the file
                using (var fileStream2 = new FileStream(filePath, FileMode.Create))
                {
                    await fileStream.CopyToAsync(fileStream2);
                }
                
                // Return the relative path to be stored in the database
                return Path.Combine("uploads", "items", uniqueFileName).Replace("\\", "/");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while uploading photo: {FileName}", fileName);
                throw;
            }
        }
    }
}