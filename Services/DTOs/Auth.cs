using System.ComponentModel.DataAnnotations;

namespace LeafLoop.Services.DTOs.Auth
{
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        
        [Required]
        public string Password { get; set; }
        
        public bool RememberMe { get; set; }
    }
    
    public class TokenResponseDto
    {
        public string Token { get; set; }
        public UserDto User { get; set; }
    }
    
    public class PasswordChangeDto
    {
        [Required]
        public string CurrentPassword { get; set; }
        
        [Required]
        [MinLength(8, ErrorMessage = "Hasło musi mieć co najmniej 8 znaków")]
        public string NewPassword { get; set; }
        
        [Required]
        [Compare("NewPassword", ErrorMessage = "Hasła nie są identyczne")]
        public string ConfirmNewPassword { get; set; }
    }
}