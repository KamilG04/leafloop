using System;
using System.Collections.Generic;
using System.Linq; // Potrzebne dla Count() w GetUserWithDetailsAsync
using System.Threading.Tasks;
using AutoMapper;
using LeafLoop.Models; // Dla User, TransactionStatus itp.
using LeafLoop.Repositories.Interfaces; // Dla IUnitOfWork
using LeafLoop.Services.DTOs; // Dla DTOs
using LeafLoop.Services.Interfaces; // Dla IUserService, IRatingService
using Microsoft.AspNetCore.Identity; // Dla UserManager
using Microsoft.Extensions.Logging;

namespace LeafLoop.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;
        private readonly IRatingService _ratingService; // Zakładam, że ten serwis istnieje
        private readonly ILogger<UserService> _logger;

        public UserService(
            IUnitOfWork unitOfWork,
            UserManager<User> userManager,
            IMapper mapper,
            IRatingService ratingService, // Wstrzyknięcie
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
                // Zakładamy, że IUserRepository ma tę metodę
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
                // Zakładamy, że IUserRepository ma tę metodę
                var user = await _unitOfWork.Users.GetUserWithAddressAsync(id);
                if (user == null)
                {
                    return null;
                }

                var userDto = _mapper.Map<UserWithDetailsDto>(user);

                // Pobierz dodatkowe dane
                userDto.Badges = _mapper.Map<List<BadgeDto>>(await _unitOfWork.Users.GetUserBadgesAsync(id));
                userDto.AverageRating = await _ratingService.GetAverageRatingForUserAsync(id);

                // Pobierz liczniki transakcji (użyj CountAsync z repozytorium transakcji)
                // Upewnij się, że TransactionStatus jest poprawnym enumem
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
                 if (count <= 0) count = 10; // Domyślny limit
                // Zakładamy, że IUserRepository ma tę metodę
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
                user.UserName = registrationDto.Email; // Ustaw UserName
                user.CreatedDate = DateTime.UtcNow;
                user.LastActivity = DateTime.UtcNow;
                user.IsActive = true; // Domyślnie aktywuj

                var result = await _userManager.CreateAsync(user, registrationDto.Password);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("User creation failed for email {Email}: {Errors}", registrationDto.Email, errors);
                    // Rzuć wyjątek, aby kontroler mógł go złapać i zwrócić odpowiedni błąd API
                    throw new ApplicationException($"Failed to create user: {errors}");
                }
                 _logger.LogInformation("User registered successfully with ID: {UserId}", user.Id);
                return user.Id;
            }
            catch (Exception ex) // Złap też ApplicationException powyżej
            {
                // Loguj tylko jeśli to nie był ApplicationException z CreateAsync
                if (!(ex is ApplicationException))
                {
                    _logger.LogError(ex, "Unexpected error occurred while registering user: {Email}", registrationDto.Email);
                }
                throw; // Rzuć dalej, aby kontroler API mógł zwrócić 500 lub inny błąd
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

                _mapper.Map(userDto, user); // Zmapuj DTO na istniejącą encję
                user.LastActivity = DateTime.UtcNow;

                // _unitOfWork.Users.Update(user); // Zazwyczaj niepotrzebne, EF Core śledzi zmiany
                await _unitOfWork.CompleteAsync(); // Zapisz zmiany
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
                // Pobierz użytkownika razem z adresem
                var user = await _unitOfWork.Users.GetUserWithAddressAsync(userId);
                if (user == null)
                {
                    throw new KeyNotFoundException($"User with ID {userId} not found");
                }

                if (user.Address != null)
                {
                    // Aktualizuj istniejący adres
                    _mapper.Map(addressDto, user.Address);
                    // _unitOfWork.Addresses.Update(user.Address); // Niepotrzebne, jeśli śledzone
                }
                else
                {
                    // Utwórz nowy adres
                    var address = _mapper.Map<Address>(addressDto);
                    await _unitOfWork.Addresses.AddAsync(address);
                    // Musimy zapisać, aby uzyskać ID adresu, ale lepiej zrobić to w transakcji
                    // LUB przypisać obiekt adresu bezpośrednio do nawigacji użytkownika
                    user.Address = address; // Przypisz obiekt nawigacyjny
                    // user.AddressId = address.Id; // To zostanie ustawione przez EF Core
                }
                // Zapisz wszystkie zmiany (aktualizacja User lub dodanie Address)
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
                    // Zwróć false zamiast rzucać wyjątek, bo kontroler oczekuje bool
                    _logger.LogWarning("ChangePassword: User with ID {UserId} not found.", userId);
                    return false;
                    // throw new KeyNotFoundException($"User with ID {userId} not found");
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
                throw; // Rzuć dalej, bo to niespodziewany błąd
            }
        }

        public async Task<bool> DeactivateUserAsync(int userId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    // Zwróć false, kontroler zwróci BadRequest
                    _logger.LogWarning("DeactivateUser: User with ID {UserId} not found.", userId);
                    return false;
                    // throw new KeyNotFoundException($"User with ID {userId} not found");
                }
                 if (!user.IsActive)
                {
                     _logger.LogInformation("DeactivateUser: User with ID {UserId} is already inactive.", userId);
                     return true; // Już nieaktywny, uznajemy za sukces
                }

                user.IsActive = false;
                user.LastActivity = DateTime.UtcNow; // Zaktualizuj czas ostatniej aktywności

                // _unitOfWork.Users.Update(user); // Niepotrzebne
                await _unitOfWork.CompleteAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deactivating user with ID: {UserId}", userId);
                // Zwróć false w przypadku błędu, kontroler zwróci 500
                return false;
                // throw;
            }
        }

        public async Task<IEnumerable<BadgeDto>> GetUserBadgesAsync(int userId)
        {
            try
            {
                // Zakładamy, że IUserRepository ma tę metodę
                var badges = await _unitOfWork.Users.GetUserBadgesAsync(userId);
                return _mapper.Map<IEnumerable<BadgeDto>>(badges);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting badges for user ID: {UserId}", userId);
                throw;
            }
        }

        // === POPRAWIONA METODA ===
        public async Task<IEnumerable<ItemDto>> GetUserItemsAsync(int userId)
        {
            try
            {
                // Użyj generycznej metody FindAsync z IRepository<Item> (dostępnej przez IItemRepository)
                var items = await _unitOfWork.Items.FindAsync(i => i.UserId == userId);
                return _mapper.Map<IEnumerable<ItemDto>>(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting items for user ID: {UserId}", userId);
                throw;
            }
        }
        // === KONIEC POPRAWIONEJ METODY ===

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
                // Można dodać walidację, np. EcoScore nie może być ujemny
                if (user.EcoScore < 0) user.EcoScore = 0;

                user.LastActivity = DateTime.UtcNow;

                // _unitOfWork.Users.Update(user); // Niepotrzebne
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
