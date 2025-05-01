using System.ComponentModel.DataAnnotations;
namespace LeafLoop.ViewModels.Account;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email jest wymagany")]
    [EmailAddress(ErrorMessage = "Podany adres email jest nieprawidłowy")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Hasło jest wymagane")]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [Display(Name = "Zapamiętaj mnie")]
    public bool RememberMe { get; set; }

    public string ReturnUrl { get; set; }
}