using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace LeafLoop.Services
{
    public class UserSessionService : IUserSessionService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserSessionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<UserSession> CreateSessionAsync(User user, string token, string refreshToken, HttpContext httpContext)
        {
            var session = new UserSession
            {
                UserId = user.Id,
                Token = token,
                RefreshToken = refreshToken,
                UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
                IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
                LoginTime = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow,
                IsActive = true
            };

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _unitOfWork.AddEntityAsync(session);
                await _unitOfWork.CompleteAsync();
            });

            return session;
        }

        public async Task<IEnumerable<UserSessionDto>> GetActiveSessionsForUserAsync(int userId)
        {
            var sessions = await _unitOfWork.FindEntitiesAsync<UserSession>(s => s.UserId == userId && s.IsActive);
            
            return sessions.Select(s => new UserSessionDto
            {
                Id = s.Id,
                UserAgent = s.UserAgent,
                IpAddress = s.IpAddress,
                LoginTime = s.LoginTime,
                LastActivity = s.LastActivity
            });
        }

        public async Task<bool> TerminateSessionAsync(int sessionId, int userId)
        {
            var session = await _unitOfWork.SingleOrDefaultEntityAsync<UserSession>(
                s => s.Id == sessionId && s.UserId == userId);

            if (session == null)
                return false;

            session.IsActive = false;
            session.LogoutTime = DateTime.UtcNow;
            
            _unitOfWork.UpdateEntity(session);
            await _unitOfWork.CompleteAsync();
            
            return true;
        }

        public async Task<bool> TerminateAllSessionsExceptCurrentAsync(int userId, string currentToken)
        {
            var sessions = await _unitOfWork.FindEntitiesAsync<UserSession>(
                s => s.UserId == userId && s.IsActive && s.Token != currentToken);

            foreach (var session in sessions)
            {
                session.IsActive = false;
                session.LogoutTime = DateTime.UtcNow;
                _unitOfWork.UpdateEntity(session);
            }

            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task<bool> TerminateAllSessionsAsync(int userId)
        {
            var sessions = await _unitOfWork.FindEntitiesAsync<UserSession>(
                s => s.UserId == userId && s.IsActive);

            foreach (var session in sessions)
            {
                session.IsActive = false;
                session.LogoutTime = DateTime.UtcNow;
                _unitOfWork.UpdateEntity(session);
            }

            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task UpdateSessionActivityAsync(HttpContext httpContext)
        {
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
                return;

            var userId = int.Parse(httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            var token = httpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            var session = await _unitOfWork.SingleOrDefaultEntityAsync<UserSession>(
                s => s.UserId == userId && s.Token == token && s.IsActive);

            if (session != null)
            {
                session.LastActivity = DateTime.UtcNow;
                _unitOfWork.UpdateEntity(session);
                await _unitOfWork.CompleteAsync();
            }
        }

        public async Task<bool> ValidateSessionAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            var session = await _unitOfWork.SingleOrDefaultEntityAsync<UserSession>(
                s => s.Token == token && s.IsActive);

            return session != null;
        }
    }
}