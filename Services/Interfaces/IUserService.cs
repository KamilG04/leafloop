using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Services.DTOs;

namespace LeafLoop.Services.Interfaces
{
    public interface IUserService
    {
        Task<UserDto> GetUserByIdAsync(int id);
        Task<UserDto> GetUserByEmailAsync(string email);
        Task<UserWithDetailsDto> GetUserWithDetailsAsync(int id);
        Task<IEnumerable<UserDto>> GetTopUsersByEcoScoreAsync(int count);
        Task<int> RegisterUserAsync(UserRegistrationDto registrationDto);
        Task UpdateUserProfileAsync(UserUpdateDto userDto);
        Task UpdateUserAddressAsync(int userId, AddressDto addressDto);
        Task<bool> ChangeUserPasswordAsync(int userId, string currentPassword, string newPassword);
        Task<bool> DeactivateUserAsync(int userId);
        Task<IEnumerable<BadgeDto>> GetUserBadgesAsync(int userId);
        Task<IEnumerable<ItemDto>> GetUserItemsAsync(int userId);
        Task<int> GetUserEcoScoreAsync(int userId);
        Task UpdateUserEcoScoreAsync(int userId, int scoreChange);
    }
}
