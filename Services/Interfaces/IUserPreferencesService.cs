using System.Threading.Tasks;
using LeafLoop.Models;

namespace LeafLoop.Services.Interfaces
{
    public interface IUserPreferencesService
    {
        Task<PreferencesData> GetUserPreferencesAsync(int userId);
        Task<bool> UpdateUserPreferencesAsync(int userId, PreferencesData preferences);
        Task<bool> UpdateUserThemeAsync(int userId, string theme);
        Task<bool> UpdateEmailNotificationsAsync(int userId, bool enabled);
        Task<bool> UpdateLanguagePreferenceAsync(int userId, string language);
        Task<string> GetUserThemeAsync(int userId);
    }
}