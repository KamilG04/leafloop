
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
            _logger.LogInformation("GetUserByIdAsync: Attempting to get user with ID: {UserId}", id);
            try
            {
                // Assuming _unitOfWork.Users.GetByIdAsync(id) is the correct way to get user by int ID
                var user = await _unitOfWork.Users.GetByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("GetUserByIdAsync: User with ID {UserId} not found.", id);
                    return null;
                }
                _logger.LogInformation("GetUserByIdAsync: Successfully retrieved user with ID: {UserId}", id);
                return _mapper.Map<UserDto>(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUserByIdAsync: Error occurred while getting user with ID: {UserId}", id);
                throw;
            }
        }

        public async Task<UserDto> GetUserByEmailAsync(string email)
        {
            _logger.LogInformation("GetUserByEmailAsync: Attempting to get user with email: {Email}", email);
            try
            {
                var user = await _unitOfWork.Users.GetUserByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("GetUserByEmailAsync: User with email {Email} not found.", email);
                    return null;
                }
                _logger.LogInformation("GetUserByEmailAsync: Successfully retrieved user with email: {Email}", email);
                return _mapper.Map<UserDto>(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUserByEmailAsync: Error occurred while getting user with email: {Email}", email);
                throw;
            }
        }

        public async Task<UserWithDetailsDto> GetUserWithDetailsAsync(int id)
        {
            _logger.LogInformation("GetUserWithDetailsAsync: Attempting to get user details for ID: {UserId}", id);
            try
            {
                var user = await _unitOfWork.Users.GetUserWithAddressAsync(id); 
                if (user == null) // <<<< (C)
                {
                    _logger.LogWarning("GetUserWithDetailsAsync: User with ID {UserId} not found.", id);
                    return null; // <<<< (D) Jeśli użytkownik nie istnieje, serwis zwraca null
                }

                var userDto = _mapper.Map<UserWithDetailsDto>(user); // <<<< (E) Mapowanie
                // Jeśli userDto jest null po mapowaniu, to też może być problem
        
                // Jeśli userDto jest null, poniższe linie rzucą NullReferenceException
                userDto.Badges = _mapper.Map<List<BadgeDto>>(await _unitOfWork.Users.GetUserBadgesAsync(id));
                userDto.AverageRating = await _ratingService.GetAverageRatingForUserAsync(id);
                // ... reszta ...
        
                return userDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUserWithDetailsAsync: Error occurred while getting user details for ID: {UserId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<UserDto>> GetTopUsersByEcoScoreAsync(int count)
        {
            _logger.LogInformation("GetTopUsersByEcoScoreAsync: Attempting to get top {Count} users by eco score.", count);
            try
            {
                if (count <= 0) count = 10;
                var users = await _unitOfWork.Users.GetTopUsersByEcoScoreAsync(count);
                _logger.LogInformation("GetTopUsersByEcoScoreAsync: Successfully retrieved {UserCount} top users.", users.Count());
                return _mapper.Map<IEnumerable<UserDto>>(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetTopUsersByEcoScoreAsync: Error occurred while getting top users by eco score.");
                throw;
            }
        }

        public async Task<int> RegisterUserAsync(UserRegistrationDto registrationDto)
        {
            _logger.LogInformation("RegisterUserAsync: Attempting to register user with email: {Email}", registrationDto.Email);
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
                    _logger.LogError("RegisterUserAsync: User creation failed for email {Email}: {Errors}", registrationDto.Email, errors);
                    throw new ApplicationException($"Failed to create user: {errors}");
                }
                _logger.LogInformation("RegisterUserAsync: User registered successfully with ID: {UserId} for email: {Email}", user.Id, registrationDto.Email);
                return user.Id;
            }
            catch (Exception ex)
            {
                if (!(ex is ApplicationException))
                {
                    _logger.LogError(ex, "RegisterUserAsync: Unexpected error occurred while registering user with email: {Email}", registrationDto.Email);
                }
                throw;
            }
        }

        public async Task UpdateUserProfileAsync(UserUpdateDto userDto)
        {
            _logger.LogInformation("UpdateUserProfileAsync: Attempting to update profile for UserID: {UserId}", userDto.Id);
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userDto.Id);
                if (user == null)
                {
                    _logger.LogWarning("UpdateUserProfileAsync: User with ID {UserId} not found for update.", userDto.Id);
                    throw new KeyNotFoundException($"User with ID {userDto.Id} not found.");
                }

                _mapper.Map(userDto, user);
                user.LastActivity = DateTime.UtcNow;
                _unitOfWork.Users.Update(user);
                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("UpdateUserProfileAsync: Successfully updated profile for UserID: {UserId}", userDto.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateUserProfileAsync: Error occurred while updating user profile for ID: {UserId}", userDto.Id);
                throw;
            }
        }
        
        private bool IsAddressDtoEffectivelyEmpty(AddressDto dto)
        {
            if (dto == null) return true;
            return string.IsNullOrWhiteSpace(dto.Street) &&
                   string.IsNullOrWhiteSpace(dto.City) &&
                   string.IsNullOrWhiteSpace(dto.PostalCode) &&
                   string.IsNullOrWhiteSpace(dto.Country) &&
                   !dto.Latitude.HasValue && 
                   !dto.Longitude.HasValue;
        }

        public async Task UpdateUserAddressAsync(int userId, AddressDto addressDto)
        {
            _logger.LogInformation("UpdateUserAddressAsync START for UserID: {UserId}. AddressDto: {@AddressDto}", userId, addressDto);
            try
            {
                var user = await _unitOfWork.Users.GetUserWithAddressAsync(userId); 
                if (user == null)
                {
                    _logger.LogWarning("UpdateUserAddressAsync: User with ID {UserId} not found.", userId);
                    throw new KeyNotFoundException($"User with ID {userId} not found.");
                }

                if (user.Address != null)
                {
                    if (addressDto == null || IsAddressDtoEffectivelyEmpty(addressDto))
                    {
                        _logger.LogInformation("UpdateUserAddressAsync: AddressDto is empty/null for UserID {UserId}. Removing existing address with ID {AddressId}.", userId, user.Address.Id);
                        _unitOfWork.Addresses.Remove(user.Address);
                        user.Address = null; 
                        user.AddressId = null; 
                    }
                    else
                    {
                        _logger.LogInformation("UpdateUserAddressAsync: Updating existing address ID {AddressId} for UserID {UserId}.", user.Address.Id, userId);
                        _mapper.Map(addressDto, user.Address);
                    }
                }
                else if (addressDto != null && !IsAddressDtoEffectivelyEmpty(addressDto))
                {
                    _logger.LogInformation("UpdateUserAddressAsync: Creating new address for UserID {UserId}.", userId);
                    var newAddress = _mapper.Map<Address>(addressDto);
                    user.Address = newAddress; 
                }
                else
                {
                    _logger.LogInformation("UpdateUserAddressAsync: UserID {UserId} has no current address and provided DTO is empty/null. No address changes made.", userId);
                }

                user.LastActivity = DateTime.UtcNow;
                _unitOfWork.Users.Update(user); // Mark user as updated to ensure AddressId/Address changes are persisted
                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("UpdateUserAddressAsync: Successfully processed address for UserID {UserId}.", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateUserAddressAsync: Error updating address for UserID: {UserId}. AddressDto: {@AddressDto}", userId, addressDto);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task UpdateUserLocationAsync(int userId, LocationUpdateDto locationData)
        {
            _logger.LogInformation("UpdateUserLocationAsync START for UserID: {UserId}. LocationData: {@LocationData}", userId, locationData);

            // Retrieve the user and their current Address (if any)
            // This method should exist in IUserRepository and eager load User.Address
            var user = await _unitOfWork.Users.GetUserWithAddressAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("UpdateUserLocationAsync: User with ID {UserId} not found.", userId);
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            Address address = user.Address; // Get existing address from the user
            bool isNewAddress = address == null;

            if (isNewAddress)
            {
                _logger.LogInformation("UpdateUserLocationAsync: No existing address found for UserID {UserId} (for location settings). Creating a new one.", userId);
                address = new Address(); // Create a new Address instance
                user.Address = address; // Associate it with the user. EF Core will handle FK (user.AddressId) and add the new Address.
            }
            else
            {
                _logger.LogInformation("UpdateUserLocationAsync: Found existing address (ID: {AddressId}) for UserID {UserId}. Updating it for location settings.", address.Id, userId);
            }

            // Update Address properties
            address.Latitude = locationData.Latitude;
            address.Longitude = locationData.Longitude;
            // Note: Address.SearchRadius is not set here as it's assumed to be on the User entity.

            // Update User's SearchRadius (assuming User entity has this property)
            // Ensure User.cs has: public decimal SearchRadius { get; set; }
            user.SearchRadius = locationData.SearchRadius;
            user.LastActivity = DateTime.UtcNow;


            if (!string.IsNullOrWhiteSpace(locationData.LocationName))
            {
                _logger.LogInformation("UpdateUserLocationAsync: LocationName '{LocationName}' provided for UserID {UserId}. Attempting to parse for City/Country.", locationData.LocationName, userId);
                var parts = locationData.LocationName.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    address.City = parts[0];
                    _logger.LogDebug("UpdateUserLocationAsync: Set City to '{City}' for UserID {UserId} based on LocationName.", address.City, userId);
                    if (parts.Length > 1)
                    {
                        address.Country = parts[1];
                        _logger.LogDebug("UpdateUserLocationAsync: Set Country to '{Country}' for UserID {UserId} based on LocationName.", address.Country, userId);
                    }
                }
            }
            else
            {
                _logger.LogInformation("UpdateUserLocationAsync: No LocationName provided for UserID {UserId}. City/Country not updated from LocationName.", userId);
            }
            
            // Mark the user entity as modified. This will ensure that if user.Address was new,
            // it gets added, and if user.AddressId was changed (by assigning a new Address object),
            // or if user.SearchRadius changed, these changes are saved.
            _unitOfWork.Users.Update(user);

            await _unitOfWork.CompleteAsync();
            _logger.LogInformation("UpdateUserLocationAsync SUCCESS for UserID: {UserId}. Address (ID: {AddressId}) and User (SearchRadius: {SearchRadius}) updated.",
                userId, address?.Id, user.SearchRadius );
        }


        public async Task<bool> ChangeUserPasswordAsync(int userId, string currentPassword, string newPassword)
        {
            _logger.LogInformation("ChangeUserPasswordAsync: Attempting to change password for UserID: {UserId}", userId);
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    _logger.LogWarning("ChangeUserPasswordAsync: User with ID {UserId} not found.", userId);
                    return false; 
                }

                var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("ChangeUserPasswordAsync: Password change failed for UserID {UserId}. Errors: {Errors}", userId, errors);
                    return false;
                }
                _logger.LogInformation("ChangeUserPasswordAsync: Successfully changed password for UserID: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChangeUserPasswordAsync: Error occurred while changing password for UserID: {UserId}", userId);
                throw; 
            }
        }

        public async Task<bool> DeactivateUserAsync(int userId)
        {
            _logger.LogInformation("DeactivateUserAsync: Attempting to deactivate UserID: {UserId}", userId);
            try
            {
                // Verify this call if the "int vs Expression" error persists from line 416.
                var user = await _unitOfWork.Users.GetByIdAsync(userId); 
                if (user == null)
                {
                    _logger.LogWarning("DeactivateUserAsync: User with ID {UserId} not found.", userId);
                    return false; 
                }
                if (!user.IsActive)
                {
                    _logger.LogInformation("DeactivateUserAsync: User with ID {UserId} is already inactive.", userId);
                    return true; 
                }

                user.IsActive = false;
                user.LastActivity = DateTime.UtcNow;
                _unitOfWork.Users.Update(user);
                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("DeactivateUserAsync: Successfully deactivated UserID: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeactivateUserAsync: Error occurred while deactivating UserID: {UserId}", userId);
                return false; 
            }
        }

        public async Task<IEnumerable<BadgeDto>> GetUserBadgesAsync(int userId)
        {
            _logger.LogInformation("GetUserBadgesAsync: Attempting to get badges for UserID: {UserId}", userId);
            try
            {
                // Assuming _unitOfWork.Users.ExistsAsync(int id) exists.
                // If not, fetch the user and check for null.
                var userExists = await _unitOfWork.Users.GetByIdAsync(userId) != null; // Or an ExistsAsync method
                if (!userExists)
                {
                     _logger.LogWarning("GetUserBadgesAsync: User with ID {UserId} not found when trying to get badges.", userId);
                     throw new KeyNotFoundException($"User with ID {userId} not found.");
                }
                var badges = await _unitOfWork.Users.GetUserBadgesAsync(userId);
                _logger.LogInformation("GetUserBadgesAsync: Successfully retrieved {BadgeCount} badges for UserID: {UserId}", badges.Count(), userId);
                return _mapper.Map<IEnumerable<BadgeDto>>(badges);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUserBadgesAsync: Error occurred while getting badges for UserID: {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<ItemDto>> GetUserItemsAsync(int userId)
        {
            _logger.LogInformation("GetUserItemsAsync: Attempting to get items for UserID: {UserId}", userId);
            try
            {
                var userExists = await _unitOfWork.Users.GetByIdAsync(userId) != null; // Or an ExistsAsync method
                if (!userExists)
                {
                     _logger.LogWarning("GetUserItemsAsync: User with ID {UserId} not found when trying to get items.", userId);
                     throw new KeyNotFoundException($"User with ID {userId} not found.");
                }
                var items = await _unitOfWork.Items.FindAsync(i => i.UserId == userId); 
                _logger.LogInformation("GetUserItemsAsync: Successfully retrieved {ItemCount} items for UserID: {UserId}", items.Count(), userId);
                return _mapper.Map<IEnumerable<ItemDto>>(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUserItemsAsync: Error occurred while getting items for UserID: {UserId}", userId);
                throw;
            }
        }

        public async Task<int> GetUserEcoScoreAsync(int userId)
        {
            _logger.LogInformation("GetUserEcoScoreAsync: Attempting to get EcoScore for UserID: {UserId}", userId);
            try
            {
                // Verify this call if the "int vs Expression" error persists.
                var user = await _unitOfWork.Users.GetByIdAsync(userId); 
                if (user == null)
                {
                    _logger.LogWarning("GetUserEcoScoreAsync: User with ID {UserId} not found.", userId);
                    throw new KeyNotFoundException($"User with ID {userId} not found.");
                }
                _logger.LogInformation("GetUserEcoScoreAsync: EcoScore for UserID {UserId} is {EcoScore}.", userId, user.EcoScore);
                return user.EcoScore;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUserEcoScoreAsync: Error occurred while getting EcoScore for UserID: {UserId}", userId);
                throw;
            }
        }

        public async Task UpdateUserEcoScoreAsync(int userId, int scoreChange)
        {
            _logger.LogInformation("UpdateUserEcoScoreAsync: Attempting to update EcoScore for UserID {UserId} by {ScoreChange}.", userId, scoreChange);
            try
            {
                // Verify this call due to the reported error: "Argument type 'int' is not assignable..."
                // If GetByIdAsync(int) is not found, this needs to change, e.g., to FirstOrDefaultAsync with an expression.
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                // Alternative if GetByIdAsync(int) is problematic:
                // var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    _logger.LogWarning("UpdateUserEcoScoreAsync: User with ID {UserId} not found.", userId);
                    throw new KeyNotFoundException($"User with ID {userId} not found.");
                }

                user.EcoScore += scoreChange;
                if (user.EcoScore < 0) user.EcoScore = 0; 

                user.LastActivity = DateTime.UtcNow;
                _unitOfWork.Users.Update(user);
                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("UpdateUserEcoScoreAsync: Successfully updated EcoScore for UserID {UserId} to {NewEcoScore}.", userId, user.EcoScore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateUserEcoScoreAsync: Error occurred while updating EcoScore for UserID: {UserId}", userId);
                throw;
            }
        }
    }
}