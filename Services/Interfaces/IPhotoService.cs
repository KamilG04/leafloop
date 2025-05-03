using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Services.DTOs;
using System.IO;

namespace LeafLoop.Services.Interfaces
{
    public interface IPhotoService
    {
        Task<PhotoDto> GetPhotoByIdAsync(int id);
        Task<IEnumerable<PhotoDto>> GetPhotosByItemAsync(int itemId);
        Task<int> AddPhotoAsync(PhotoCreateDto photoDto, int userId);
        Task DeletePhotoAsync(int id, int userId);
        Task<string> UploadPhotoAsync(Stream fileStream, string fileName, string contentType, string subfolder = "items");
        
        Task<bool> DeletePhotoByPathAsync(string relativePath);
    }
}
