using System.Security.Claims;
using System.Threading.Tasks;
using LeafLoop.Models;

namespace LeafLoop.Services.Interfaces
{
    public interface IJwtTokenService
    {
        Task<string> GenerateTokenAsync(User user);
        ClaimsPrincipal ValidateToken(string token);
    }
}