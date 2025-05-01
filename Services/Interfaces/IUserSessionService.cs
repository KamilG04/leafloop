using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;
using Microsoft.AspNetCore.Http;

namespace LeafLoop.Services.Interfaces
{
    public interface IUserSessionService
    {
        Task<UserSession> CreateSessionAsync(User user, string token, string refreshToken, HttpContext httpContext);
        Task<IEnumerable<UserSessionDto>> GetActiveSessionsForUserAsync(int userId);
        Task<bool> TerminateSessionAsync(int sessionId, int userId);
        Task<bool> TerminateAllSessionsExceptCurrentAsync(int userId, string currentToken);
        Task<bool> TerminateAllSessionsAsync(int userId);
        Task UpdateSessionActivityAsync(HttpContext httpContext);
        Task<bool> ValidateSessionAsync(string token);
    }
    
    // DTO dla sesji użytkownika
    public class UserSessionDto
    {
        public int Id { get; set; }
        public string UserAgent { get; set; }
        public string IpAddress { get; set; }
        public System.DateTime LoginTime { get; set; }
        public System.DateTime LastActivity { get; set; }
    }
}