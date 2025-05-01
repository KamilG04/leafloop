using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Services.DTOs;

namespace LeafLoop.Services.Interfaces
{
    public interface IRatingService
    {
        Task<RatingDto> GetRatingByIdAsync(int id);
        Task<IEnumerable<RatingDto>> GetRatingsByUserAsync(int userId, bool asRater = false);
        Task<IEnumerable<RatingDto>> GetRatingsByTransactionAsync(int transactionId);
        Task<double> GetAverageRatingForUserAsync(int userId);
        Task<double> GetAverageRatingForCompanyAsync(int companyId);
        Task<int> AddRatingAsync(RatingCreateDto ratingDto);
        Task UpdateRatingAsync(RatingUpdateDto ratingDto, int raterId);
        Task DeleteRatingAsync(int id, int userId);
    }
}
