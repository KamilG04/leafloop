using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace LeafLoop.Services
{
    public class RatingService : IRatingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<RatingService> _logger;

        public RatingService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<RatingService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<RatingDto> GetRatingByIdAsync(int id)
        {
            try
            {
                var rating = await _unitOfWork.Ratings.GetByIdAsync(id);
                return _mapper.Map<RatingDto>(rating);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting rating with ID: {RatingId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<RatingDto>> GetRatingsByUserAsync(int userId, bool asRater = false)
        {
            try
            {
                var ratings = await _unitOfWork.Ratings.GetRatingsByUserAsync(userId, asRater);
                return _mapper.Map<IEnumerable<RatingDto>>(ratings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting ratings for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<RatingDto>> GetRatingsByTransactionAsync(int transactionId)
        {
            try
            {
                var ratings = await _unitOfWork.Ratings.GetRatingsByTransactionAsync(transactionId);
                return _mapper.Map<IEnumerable<RatingDto>>(ratings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting ratings for transaction: {TransactionId}", transactionId);
                throw;
            }
        }

        public async Task<double> GetAverageRatingForUserAsync(int userId)
        {
            try
            {
                return await _unitOfWork.Ratings.GetAverageRatingForEntityAsync(userId, RatedEntityType.User);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting average rating for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<double> GetAverageRatingForCompanyAsync(int companyId)
        {
            try
            {
                return await _unitOfWork.Ratings.GetAverageRatingForEntityAsync(companyId, RatedEntityType.Company);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting average rating for company: {CompanyId}", companyId);
                throw;
            }
        }

        public async Task<int> AddRatingAsync(RatingCreateDto ratingDto)
        {
            try
            {
                // Validate the rating
                if (ratingDto.Value < 1 || ratingDto.Value > 5)
                {
                    throw new ArgumentException("Rating value must be between 1 and 5");
                }
                
                // If transaction is provided, check if it exists and is completed
                if (ratingDto.TransactionId.HasValue)
                {
                    var transaction = await _unitOfWork.Transactions.GetByIdAsync(ratingDto.TransactionId.Value);
                    
                    if (transaction == null)
                    {
                        throw new KeyNotFoundException($"Transaction with ID {ratingDto.TransactionId.Value} not found");
                    }
                    
                    if (transaction.Status != TransactionStatus.Completed)
                    {
                        throw new InvalidOperationException("Can only rate completed transactions");
                    }
                    
                    // Verify that the rater is part of the transaction
                    if (transaction.BuyerId != ratingDto.RaterId && transaction.SellerId != ratingDto.RaterId)
                    {
                        throw new UnauthorizedAccessException("User is not authorized to rate this transaction");
                    }
                    
                    // Verify that the rated entity is the other party in the transaction
                    bool isValidRatedEntity = false;
                    
                    if (ratingDto.RatedEntityType == RatedEntityType.User)
                    {
                        isValidRatedEntity = 
                            (transaction.BuyerId == ratingDto.RaterId && transaction.SellerId == ratingDto.RatedEntityId) ||
                            (transaction.SellerId == ratingDto.RaterId && transaction.BuyerId == ratingDto.RatedEntityId);
                    }
                    
                    if (!isValidRatedEntity)
                    {
                        throw new InvalidOperationException("Invalid rated entity for this transaction");
                    }
                    
                    // Check if user has already rated this entity for this transaction
                    var existingRating = await _unitOfWork.Ratings.SingleOrDefaultAsync(r =>
                        r.RaterId == ratingDto.RaterId &&
                        r.RatedEntityId == ratingDto.RatedEntityId &&
                        r.RatedEntityType == ratingDto.RatedEntityType &&
                        r.TransactionId == ratingDto.TransactionId);
                    
                    if (existingRating != null)
                    {
                        throw new InvalidOperationException("User has already rated this entity for this transaction");
                    }
                }
                
                var rating = _mapper.Map<Rating>(ratingDto);
                rating.RatingDate = DateTime.UtcNow;
                
                await _unitOfWork.Ratings.AddAsync(rating);
                await _unitOfWork.CompleteAsync();
                
                return rating.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding rating");
                throw;
            }
        }

        public async Task UpdateRatingAsync(RatingUpdateDto ratingDto, int raterId)
        {
            try
            {
                var rating = await _unitOfWork.Ratings.GetByIdAsync(ratingDto.Id);
                
                if (rating == null)
                {
                    throw new KeyNotFoundException($"Rating with ID {ratingDto.Id} not found");
                }
                
                // Verify that the user is the one who created the rating
                if (rating.RaterId != raterId)
                {
                    throw new UnauthorizedAccessException("User is not authorized to update this rating");
                }
                
                // Validate the rating value
                if (ratingDto.Value < 1 || ratingDto.Value > 5)
                {
                    throw new ArgumentException("Rating value must be between 1 and 5");
                }
                
                rating.Value = ratingDto.Value;
                rating.Comment = ratingDto.Comment;
                
                _unitOfWork.Ratings.Update(rating);
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating rating: {RatingId}", ratingDto.Id);
                throw;
            }
        }

        public async Task DeleteRatingAsync(int id, int userId)
        {
            try
            {
                var rating = await _unitOfWork.Ratings.GetByIdAsync(id);
                
                if (rating == null)
                {
                    throw new KeyNotFoundException($"Rating with ID {id} not found");
                }
                
                // Verify that the user is the one who created the rating
                if (rating.RaterId != userId)
                {
                    throw new UnauthorizedAccessException("User is not authorized to delete this rating");
                }
                
                _unitOfWork.Ratings.Remove(rating);
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting rating: {RatingId}", id);
                throw;
            }
        }
    }
}