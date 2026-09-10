using System.ComponentModel.DataAnnotations;

namespace KanbanTaskManagement.ViewModels;

public class LoginInput {
	[Required(ErrorMessage = "Podaj nazwę użytkownika.")]
	[Display(Name = "Użytkownik")]
	public string Username { get; set; } = "";

	[Required(ErrorMessage = "Podaj hasło.")]
	[DataType(DataType.Password)]
	[Display(Name = "Hasło")]
	public string Password { get; set; } = "";

	[Display(Name = "Zapamiętaj mnie")]
	public bool RememberMe { get; set; } = true;
}

public class RegisterInput {
	[Required(ErrorMessage = "Podaj nazwę użytkownika.")]
	[StringLength(32, MinimumLength = 3, ErrorMessage = "Nazwa użytkownika musi mieć od 3 do 32 znaków.")]
	[Display(Name = "Użytkownik")]
	public string Username { get; set; } = "";

	[EmailAddress(ErrorMessage = "Nieprawidłowy adres e-mail.")]
	[Display(Name = "E-mail (opcjonalnie)")]
	public string? Email { get; set; }

	[Required(ErrorMessage = "Podaj hasło.")]
	[StringLength(128, MinimumLength = 6, ErrorMessage = "Hasło musi mieć co najmniej 6 znaków.")]
	[DataType(DataType.Password)]
	[Display(Name = "Hasło")]
	public string Password { get; set; } = "";

	[DataType(DataType.Password)]
	[Compare(nameof(Password), ErrorMessage = "Hasła nie są takie same.")]
	[Display(Name = "Powtórz hasło")]
	public string ConfirmPassword { get; set; } = "";
}
