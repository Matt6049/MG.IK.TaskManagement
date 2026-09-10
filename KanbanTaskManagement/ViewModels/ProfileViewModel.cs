using System.ComponentModel.DataAnnotations;

namespace KanbanTaskManagement.ViewModels;

public class ProfileViewModel {
	public required string Username { get; set; }
	public string? Email { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime LastActive { get; set; }
}

public class ChangePasswordInput {
	[Required(ErrorMessage = "Podaj obecne hasło.")]
	[DataType(DataType.Password)]
	[Display(Name = "Obecne hasło")]
	public string CurrentPassword { get; set; } = "";

	[Required(ErrorMessage = "Podaj nowe hasło.")]
	[StringLength(128, MinimumLength = 6, ErrorMessage = "Hasło musi mieć co najmniej 6 znaków.")]
	[DataType(DataType.Password)]
	[Display(Name = "Nowe hasło")]
	public string NewPassword { get; set; } = "";

	[DataType(DataType.Password)]
	[Compare(nameof(NewPassword), ErrorMessage = "Hasła nie są takie same.")]
	[Display(Name = "Powtórz nowe hasło")]
	public string ConfirmPassword { get; set; } = "";
}
