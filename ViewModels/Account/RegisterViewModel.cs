using System.ComponentModel.DataAnnotations;

namespace LeafLoop.ViewModels.Account;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Email jest wymagany")]
    [EmailAddress(ErrorMessage = "Podany adres email jest nieprawidłowy")]
    [Display(Name = "Email")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Imię jest wymagane")]
    [Display(Name = "Imię")]
    public string FirstName { get; set; }

    [Required(ErrorMessage = "Nazwisko jest wymagane")]
    [Display(Name = "Nazwisko")]
    public string LastName { get; set; }

    [Required(ErrorMessage = "Hasło jest wymagane")]
    [StringLength(100, ErrorMessage = "{0} musi mieć co najmniej {2} znaków.", MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Hasło")]
    public string Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Potwierdź hasło")]
    [Compare("Password", ErrorMessage = "Hasła nie są identyczne.")]
    public string ConfirmPassword { get; set; }
}