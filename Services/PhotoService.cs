// Ścieżka: Services/PhotoService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; // Potrzebne dla Contains w SanitizeSubfolder
using System.Threading.Tasks;
using AutoMapper;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Hosting; // Potrzebne dla IWebHostEnvironment
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

        // --- Metody GetPhotoByIdAsync, GetPhotosByItemAsync, AddPhotoAsync, DeletePhotoAsync (by ID) ---
        // Pozostają takie same jak w kodzie, który wkleiłeś
        public async Task<PhotoDto> GetPhotoByIdAsync(int id) { /* ... jak poprzednio ... */
             try
            {
                var photo = await _unitOfWork.Photos.GetByIdAsync(id);
                 // Zwróć null jeśli nie znaleziono, zamiast rzucać wyjątek od razu
                return _mapper.Map<PhotoDto>(photo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting photo with ID: {PhotoId}", id);
                throw; // Rzuć dalej, aby kontroler API mógł zwrócić 500
            }
         }
        public async Task<IEnumerable<PhotoDto>> GetPhotosByItemAsync(int itemId) { /* ... jak poprzednio ... */
             try
            {
                var photos = await _unitOfWork.Photos.GetPhotosByItemAsync(itemId);
                // Zwróć pustą listę jeśli brak zdjęć
                return _mapper.Map<IEnumerable<PhotoDto>>(photos ?? new List<Photo>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting photos for item: {ItemId}", itemId);
                throw;
            }
         }
        public async Task<int> AddPhotoAsync(PhotoCreateDto photoDto, int userId) { /* ... jak poprzednio ... */
             try
            {
                var item = await _unitOfWork.Items.GetByIdAsync(photoDto.ItemId);
                if (item == null) throw new KeyNotFoundException($"Item with ID {photoDto.ItemId} not found");
                if (item.UserId != userId) throw new UnauthorizedAccessException("User is not authorized to add photos to this item");

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
         // Zmodyfikowane DeletePhotoAsync, aby używało DeletePhotoByPathAsync
        public async Task DeletePhotoAsync(int id, int userId)
        {
            _logger.LogInformation("Attempting to delete photo with ID: {PhotoId} by UserID: {UserId}", id, userId);
            try
            {
                var photo = await _unitOfWork.Photos.GetByIdAsync(id);
                if (photo == null)
                {
                    throw new KeyNotFoundException($"Photo with ID {id} not found");
                }

                var item = await _unitOfWork.Items.GetByIdAsync(photo.ItemId);
                if (item == null)
                {
                    // To nie powinno się zdarzyć, jeśli baza jest spójna, ale lepiej obsłużyć
                     _logger.LogWarning("Item with ID {ItemId} associated with Photo {PhotoId} not found during photo deletion.", photo.ItemId, id);
                     // Mimo wszystko usuń rekord zdjęcia z bazy
                }
                 else if (item.UserId != userId) // Sprawdź właściciela tylko jeśli item istnieje
                {
                    throw new UnauthorizedAccessException("User is not authorized to delete this photo");
                }

                // Usuń rekord z bazy danych
                _unitOfWork.Photos.Remove(photo);
                await _unitOfWork.CompleteAsync();
                 _logger.LogInformation("Photo record with ID: {PhotoId} deleted from database.", id);


                // Usuń plik fizyczny (po udanym usunięciu z bazy)
                if (!string.IsNullOrEmpty(photo.Path))
                {
                    await DeletePhotoByPathAsync(photo.Path); // Wywołaj nową metodę
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting photo with ID: {PhotoId}", id);
                throw;
            }
        }


        // --- ZMODYFIKOWANA METODA UploadPhotoAsync ---
        public async Task<string> UploadPhotoAsync(Stream fileStream, string fileName, string contentType, string subfolder = "items")
        {
            if (fileStream == null || fileStream.Length == 0)
                throw new ArgumentException("File stream cannot be null or empty.", nameof(fileStream));
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be empty.", nameof(fileName));

            subfolder = SanitizeSubfolder(subfolder); // Użyj helpera do oczyszczenia nazwy
            _logger.LogInformation("Uploading photo '{FileName}' to subfolder '{Subfolder}'. ContentType: {ContentType}, Stream Length: {Length}", fileName, subfolder, contentType, fileStream.Length);

            try
            {
                var uploadsRootDirectory = Path.Combine(_environment.WebRootPath, "uploads");
                var targetDirectory = Path.Combine(uploadsRootDirectory, subfolder);

                // Utwórz foldery
                Directory.CreateDirectory(targetDirectory); // CreateDirectory nie rzuca wyjątku, jeśli folder istnieje

                var extension = Path.GetExtension(fileName);
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var absoluteFilePath = Path.Combine(targetDirectory, uniqueFileName);

                _logger.LogInformation("Saving uploaded file to absolute path: {FilePath}", absoluteFilePath);

                // Zapisz strumień (upewnij się, że jest na początku)
                fileStream.Position = 0;
                using (var fileStreamOnDisk = new FileStream(absoluteFilePath, FileMode.Create))
                {
                    await fileStream.CopyToAsync(fileStreamOnDisk);
                }
                _logger.LogInformation("File saved successfully to disk: {FilePath}", absoluteFilePath);

                // Zwróć ścieżkę względną
                var relativePath = Path.Combine("uploads", subfolder, uniqueFileName).Replace("\\", "/");
                _logger.LogInformation("Returning relative path for database: {RelativePath}", relativePath);
                return relativePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during UploadPhotoAsync for file: {FileName}, subfolder: {Subfolder}", fileName, subfolder);
                throw;
            }
        }

        // --- NOWA METODA DeletePhotoByPathAsync ---
        public Task<bool> DeletePhotoByPathAsync(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                _logger.LogWarning("DeletePhotoByPathAsync called with null or empty path.");
                return Task.FromResult(false); // Nic do usunięcia
            }

            try
            {
                // Zabezpieczenie przed wychodzeniem poza wwwroot/uploads
                var safeRelativePath = relativePath.TrimStart('/', '\\').Replace("..", "");
                if (!safeRelativePath.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError("Attempted to delete file outside of allowed 'uploads' directory: {RelativePath}", relativePath);
                    return Task.FromResult(false);
                }

                var absolutePath = Path.Combine(_environment.WebRootPath, safeRelativePath);

                if (File.Exists(absolutePath))
                {
                    _logger.LogInformation("Deleting photo file at: {FilePath}", absolutePath);
                    File.Delete(absolutePath);
                    // Sprawdzenie czy plik faktycznie został usunięty (opcjonalne)
                    if (!File.Exists(absolutePath)) {
                         _logger.LogInformation("Photo file deleted successfully: {FilePath}", absolutePath);
                         return Task.FromResult(true);
                    } else {
                         _logger.LogError("Failed to delete photo file even though File.Delete did not throw: {FilePath}", absolutePath);
                         return Task.FromResult(false);
                    }
                }
                else
                {
                    _logger.LogWarning("Photo file not found for deletion at path: {FilePath}", absolutePath);
                    // Jeśli plik nie istniał, można uznać operację za "udaną" (nic nie trzeba było robić)
                    return Task.FromResult(true); // LUB false, jeśli chcesz jawnie wskazać, że pliku nie było
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting photo file by path: {RelativePath}", relativePath);
                return Task.FromResult(false); // Zwróć false w razie błędu
            }
        }

        // Prywatny helper do sanityzacji nazwy podfolderu
        private string SanitizeSubfolder(string subfolder)
        {
            if (string.IsNullOrWhiteSpace(subfolder)) return "general";
            var invalidChars = Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).Distinct().ToArray();
            string sanitized = new string(subfolder.Where(ch => !invalidChars.Contains(ch)).ToArray());
            sanitized = sanitized.Replace("..", ""); // Dodatkowe zabezpieczenie
            return string.IsNullOrWhiteSpace(sanitized) ? "general" : sanitized.Trim();
        }
    }
}