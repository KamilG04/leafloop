using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace LeafLoop.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;
        private readonly IRatingService _ratingService;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IUnitOfWork unitOfWork,
            UserManager<User> userManager,
            IMapper mapper,
            IRatingService ratingService,
            ILogger<UserService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _ratingService = ratingService ?? throw new ArgumentNullException(nameof(ratingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<UserDto> GetUserByIdAsync(int id)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(id);
                return _mapper.Map<UserDto>(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting user with ID: {UserId}", id);
                throw;
            }
        }

        public async Task<UserDto> GetUserByEmailAsync(string email)
        {
            try
            {
                var user = await _unitOfWork.Users.GetUserByEmailAsync(email);
                return _mapper.Map<UserDto>(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting user with email: {Email}", email);
                throw;
            }
        }

        public async Task<UserWithDetailsDto> GetUserWithDetailsAsync(int id)
        {
            try
            {
                var user = await _unitOfWork.Users.GetUserWithAddressAsync(id);
                if (user == null)
                {
                    return null;
                }

                var userDto = _mapper.Map<UserWithDetailsDto>(user);

                // Get additional data
                userDto.Badges = _mapper.Map<List<BadgeDto>>(await _unitOfWork.Users.GetUserBadgesAsync(id));
                userDto.AverageRating = await _ratingService.GetAverageRatingForUserAsync(id);

                // Get completed transactions count
                var sellerTransactions = await _unitOfWork.Transactions.GetTransactionsByUserAsync(id, true);
                var buyerTransactions = await _unitOfWork.Transactions.GetTransactionsByUserAsync(id, false);

                userDto.CompletedTransactionsCount =
                    sellerTransactions.Count(t => t.Status == TransactionStatus.Completed) +
                    buyerTransactions.Count(t => t.Status == TransactionStatus.Completed);

                return userDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting user details with ID: {UserId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<UserDto>> GetTopUsersByEcoScoreAsync(int count)
        {
            try
            {
                var users = await _unitOfWork.Users.GetTopUsersByEcoScoreAsync(count);
                return _mapper.Map<IEnumerable<UserDto>>(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting top users by eco score");
                throw;
            }
        }

        public async Task<int> RegisterUserAsync(UserRegistrationDto registrationDto)
        {
            try
            {
                var user = _mapper.Map<User>(registrationDto);

                // The Identity UserManager will handle password hashing
                var result = await _userManager.CreateAsync(user, registrationDto.Password);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new ApplicationException($"Failed to create user: {errors}");
                }

                return user.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while registering user: {Email}", registrationDto.Email);
                throw;
            }
        }

        public async Task UpdateUserProfileAsync(UserUpdateDto userDto)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userDto.Id);
                if (user == null)
                {
                    throw new KeyNotFoundException($"User with ID {userDto.Id} not found");
                }

                // Update the user properties
                _mapper.Map(userDto, user);
                user.LastActivity = DateTime.UtcNow;

                _unitOfWork.Users.Update(user);
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating user profile with ID: {UserId}", userDto.Id);
                throw;
            }
        }

        public async Task UpdateUserAddressAsync(int userId, AddressDto addressDto)
        {
            try
            {
                var user = await _unitOfWork.Users.GetUserWithAddressAsync(userId);
                if (user == null)
                {
                    throw new KeyNotFoundException($"User with ID {userId} not found");
                }

                // If user already has an address, update it
                if (user.Address != null)
                {
                    _mapper.Map(addressDto, user.Address);
                    _unitOfWork.Addresses.Update(user.Address);
                }
                else
                {
                    // Create new address and link to user
                    var address = _mapper.Map<Address>(addressDto);
                    await _unitOfWork.Addresses.AddAsync(address);
                    await _unitOfWork.CompleteAsync(); // Save to get address ID

                    user.AddressId = address.Id;
                    user.Address = address;
                    _unitOfWork.Users.Update(user);
                }

                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating user address for user ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> ChangeUserPasswordAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    throw new KeyNotFoundException($"User with ID {userId} not found");
                }

                var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while changing password for user ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> DeactivateUserAsync(int userId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    throw new KeyNotFoundException($"User with ID {userId} not found");
                }

                user.IsActive = false;
                user.LastActivity = DateTime.UtcNow;

                _unitOfWork.Users.Update(user);
                await _unitOfWork.CompleteAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deactivating user with ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<BadgeDto>> GetUserBadgesAsync(int userId)
        {
            try
            {
                var badges = await _unitOfWork.Users.GetUserBadgesAsync(userId);
                return _mapper.Map<IEnumerable<BadgeDto>>(badges);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting badges for user ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<ItemDto>> GetUserItemsAsync(int userId)
        {
            try
            {
                var items = await _unitOfWork.Items.GetItemsByUserAsync(userId);
                return _mapper.Map<IEnumerable<ItemDto>>(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting items for user ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<int> GetUserEcoScoreAsync(int userId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    throw new KeyNotFoundException($"User with ID {userId} not found");
                }

                return user.EcoScore;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting eco score for user ID: {UserId}", userId);
                throw;
            }
        }

        public async Task UpdateUserEcoScoreAsync(int userId, int scoreChange)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    throw new KeyNotFoundException($"User with ID {userId} not found");
                }

                user.EcoScore += scoreChange;
                user.LastActivity = DateTime.UtcNow;

                _unitOfWork.Users.Update(user);
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating eco score for user ID: {UserId}", userId);
                throw;
            }
        }
    }
}