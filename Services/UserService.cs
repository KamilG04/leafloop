using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LeafLoop.Models; // For User, Address, TransactionStatus etc.
using LeafLoop.Repositories.Interfaces; // For IUnitOfWork
using LeafLoop.Services.DTOs; // For DTOs
using LeafLoop.Services.Interfaces; // For IUserService, IRatingService
using Microsoft.AspNetCore.Identity; // For UserManager
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
                // Assumes IUserRepository has GetUserByEmailAsync
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
                // Assumes IUserRepository has GetUserWithAddressAsync
                var user = await _unitOfWork.Users.GetUserWithAddressAsync(id);
                if (user == null)
                {
                    return null;
                }

                var userDto = _mapper.Map<UserWithDetailsDto>(user);

                userDto.Badges = _mapper.Map<List<BadgeDto>>(await _unitOfWork.Users.GetUserBadgesAsync(id));
                userDto.AverageRating = await _ratingService.GetAverageRatingForUserAsync(id);

                // Ensure TransactionStatus is a valid enum
                int sellingCount = await _unitOfWork.Transactions.CountAsync(t => t.SellerId == id && t.Status == TransactionStatus.Completed);
                int buyingCount = await _unitOfWork.Transactions.CountAsync(t => t.BuyerId == id && t.Status == TransactionStatus.Completed);
                userDto.CompletedTransactionsCount = sellingCount + buyingCount;

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
                 if (count <= 0) count = 10; // Default limit
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
                user.UserName = registrationDto.Email;
                user.CreatedDate = DateTime.UtcNow;
                user.LastActivity = DateTime.UtcNow;
                user.IsActive = true;

                var result = await _userManager.CreateAsync(user, registrationDto.Password);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("User creation failed for email {Email}: {Errors}", registrationDto.Email, errors);
                    throw new ApplicationException($"Failed to create user: {errors}");
                }
                _logger.LogInformation("User registered successfully with ID: {UserId}", user.Id);
                return user.Id;
            }
            catch (Exception ex)
            {
                if (!(ex is ApplicationException))
                {
                    _logger.LogError(ex, "Unexpected error occurred while registering user: {Email}", registrationDto.Email);
                }
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

                _mapper.Map(userDto, user);
                user.LastActivity = DateTime.UtcNow;
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating user profile with ID: {UserId}", userDto.Id);
                throw;
            }
        }

        // Helper method to check if AddressDto is effectively empty
        // TODO: Define this based on the actual structure of AddressDto
        private bool IsAddressDtoEffectivelyEmpty(AddressDto dto)
        {
            if (dto == null) return true;
            // Example check, adjust to your AddressDto properties:
            return string.IsNullOrWhiteSpace(dto.Street) &&
                   string.IsNullOrWhiteSpace(dto.City) &&
                   string.IsNullOrWhiteSpace(dto.PostalCode) &&
                   string.IsNullOrWhiteSpace(dto.Country);
        }

        public async Task UpdateUserAddressAsync(int userId, AddressDto addressDto)
        {
            _logger.LogInformation("UpdateUserAddressAsync START for UserID: {UserId}", userId);
            try
            {
                var user = await _unitOfWork.Users.GetUserWithAddressAsync(userId); // Ensure Address is eager loaded
                if (user == null)
                {
                    _logger.LogWarning("UpdateUserAddressAsync: User with ID {UserId} not found.", userId);
                    throw new KeyNotFoundException($"User with ID {userId} not found");
                }

                // Scenario 1: User has an existing address.
                if (user.Address != null)
                {
                    // Client might send empty/null DTO to indicate removal of address.
                    if (addressDto == null || IsAddressDtoEffectivelyEmpty(addressDto))
                    {
                        _logger.LogInformation("UpdateUserAddressAsync: AddressDto is empty/null for UserID {UserId}. Removing existing address with ID {AddressId}.", userId, user.Address.Id);
                        _unitOfWork.Addresses.Remove(user.Address); // Mark the existing address for removal
                        user.Address = null;    // Disassociate from user
                        user.AddressId = null;  // Clear the foreign key
                    }
                    else
                    {
                        _logger.LogInformation("UpdateUserAddressAsync: Updating existing address for UserID {UserId}, AddressID {AddressId}.", userId, user.Address.Id);
                        // CRITICAL: AutoMapper configuration for AddressDto -> Address
                        // MUST ignore mapping the Id property to prevent changing the PK.
                        // Example in MappingProfile: CreateMap<AddressDto, Address>().ForMember(dest => dest.Id, opt => opt.Ignore());
                        _mapper.Map(addressDto, user.Address);
                        // EF Core will track changes to the user.Address entity.
                    }
                }
                // Scenario 2: User does not have an address, and DTO provides new address data.
                else if (addressDto != null && !IsAddressDtoEffectivelyEmpty(addressDto))
                {
                    _logger.LogInformation("UpdateUserAddressAsync: Creating new address for UserID {UserId}.", userId);
                    var newAddress = _mapper.Map<Address>(addressDto); // Create a new Address entity
                    // The newAddress.Id will be 0 (or default). The database will assign it upon insertion.
                    // Ensure AutoMapper does not try to set newAddress.Id from a potentially non-zero addressDto.Id.

                    user.Address = newAddress; // Associate the new address with the user.
                                               // EF Core will detect this new entity linked to a tracked entity (user)
                                               // and will add it to the Addresses DbSet.
                }
                // Scenario 3: User has no address, and DTO is also empty/null. No action needed for address.
                else
                {
                    _logger.LogInformation("UpdateUserAddressAsync: UserID {UserId} has no current address and provided DTO is empty/null. No address changes made.", userId);
                }

                user.LastActivity = DateTime.UtcNow;
                // _unitOfWork.Users.Update(user); // Typically not needed if EF Core tracks 'user' and detects changes to Address navigation or AddressId.
                                                // However, if only user.AddressId (FK) changes and user.Address (navigation) is not loaded or set,
                                                // then marking 'user' as modified might be necessary. With navigation property assignment, it should be fine.

                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("UpdateUserAddressAsync: Successfully processed address for UserID {UserId}.", userId);
            }
            catch (Exception ex)
            {
                // Log the full exception details, which was very helpful.
                _logger.LogError(ex, "Error occurred while updating user address for user ID: {UserId}. AddressDto: {@AddressDto}", userId, addressDto);
                throw; // Re-throw for the controller/middleware to handle.
            }
        }


        public async Task<bool> ChangeUserPasswordAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    _logger.LogWarning("ChangePassword: User with ID {UserId} not found.", userId);
                    return false;
                }

                var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
                if (!result.Succeeded)
                {
                     _logger.LogWarning("ChangePassword failed for UserID {UserId}. Errors: {@Errors}", userId, result.Errors);
                }
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
                    _logger.LogWarning("DeactivateUser: User with ID {UserId} not found.", userId);
                    return false;
                }
                 if (!user.IsActive)
                {
                     _logger.LogInformation("DeactivateUser: User with ID {UserId} is already inactive.", userId);
                     return true;
                }

                user.IsActive = false;
                user.LastActivity = DateTime.UtcNow;
                await _unitOfWork.CompleteAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deactivating user with ID: {UserId}", userId);
                return false;
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
                var items = await _unitOfWork.Items.FindAsync(i => i.UserId == userId);
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
                if (user.EcoScore < 0) user.EcoScore = 0;
                user.LastActivity = DateTime.UtcNow;
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