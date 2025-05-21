using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LeafLoop.Models;
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace LeafLoop.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<JwtTokenService> _logger;

    public JwtTokenService(
        IConfiguration configuration,
        UserManager<User> userManager,
        ILogger<JwtTokenService> logger)
    {
        _configuration = configuration;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<string> GenerateTokenAsync(User user)
    {
        try
        {
            if (user == null)
            {
                _logger.LogError("Cannot generate token for null user");
                return null;
            }

            var userClaims = await _userManager.GetClaimsAsync(user);
            var userRoles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.UserName), // Standard claim for username
                new(ClaimTypes.Email, user.Email),
                //FIXME Problemy z Unieważnianiem Tokenów ---
                // Add JTI (JWT ID) claim for blacklisting purposes
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var role in userRoles) claims.Add(new Claim(ClaimTypes.Role, role));
            claims.AddRange(userClaims);

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

          
            var expiryInMinutesText = _configuration["JwtSettings:ExpiryInMinutes"];
            if (string.IsNullOrEmpty(expiryInMinutesText) || 
                !double.TryParse(expiryInMinutesText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var expiryInMinutesValue))
            {
                _logger.LogError("ExpiryInMinutes is missing from JwtSettings or is not a valid double: '{ExpiryValue}'", expiryInMinutesText);
                throw new ArgumentException("ExpiryInMinutes configuration is invalid or missing.", "JwtSettings:ExpiryInMinutes");
            }
            var expiry = DateTime.UtcNow.AddMinutes(expiryInMinutesValue);
           
            var token = new JwtSecurityToken(
                _configuration["JwtSettings:Issuer"],
                _configuration["JwtSettings:Audience"],
                claims,
                expires: expiry, // Use UtcNow for consistency if generating expiry based on UtcNow
                signingCredentials: creds
            );

            _logger.LogInformation("Generated JWT token for user: {UserId}, Expires: {Expiry}", user.Id, expiry);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating token for user ID: {UserId}", user?.Id);
            return null;
        }
    }

    public ClaimsPrincipal
        ValidateToken(string token) // Keep using DateTime.Now here for validation against local clock if necessary
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["JwtSettings:Issuer"],
                ValidAudience = _configuration["JwtSettings:Audience"],
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero 
            };

            SecurityToken validatedToken;
            var principal = tokenHandler.ValidateToken(token, validationParameters, out validatedToken);
            _logger.LogInformation("Token validated successfully for principal: {Name}", principal?.Identity?.Name ?? "Unknown");
            return principal;
        }
        catch (SecurityTokenExpiredException ex) // Łap konkretny wyjątek dla wygasłego tokenu
        {
            _logger.LogWarning(ex, "Token validation failed: Token expired.");
            return null;
        }
        catch (SecurityTokenInvalidSignatureException ex) // Łap konkretny wyjątek dla nieprawidłowego podpisu
        {
            _logger.LogWarning(ex, "Token validation failed: Invalid signature.");
            return null;
        }
        catch (Exception ex) // Ogólny catch dla innych błędów walidacji
        {
            _logger.LogError(ex, "Error validating token (general exception).");
            return null;
        }
    }
}