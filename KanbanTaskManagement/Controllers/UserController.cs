using KanbanTaskManagement.Data;
using KanbanTaskManagement.Models;
using KanbanTaskManagement.Services;
using KanbanTaskManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace KanbanTaskManagement.Controllers;

[Authorize]
public class UserController : Controller {
	private readonly MongoDBContext _db;
	private readonly ICurrentUser _currentUser;

	public UserController(MongoDBContext db, ICurrentUser currentUser) {
		_db = db;
		_currentUser = currentUser;
	}

	[HttpGet]
	public async Task<IActionResult> Profile() {
		var user = await LoadCurrentUser();
		if (user is null)
			return Forbid();

		return View(new ProfileViewModel {
			Username = user.Username,
			Email = user.Email,
			CreatedAt = user.CreatedAt,
			LastActive = user.LastActive,
		});
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> ChangeEmail(string? email) {
		var user = await LoadCurrentUser();
		if (user is null)
			return Forbid();

		var trimmed = email?.Trim();
		if (!string.IsNullOrEmpty(trimmed) && !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(trimmed)) {
			TempData["ProfileError"] = "Nieprawidłowy adres e-mail.";
			return RedirectToAction(nameof(Profile));
		}

		await _db.UserCollection.UpdateOneAsync(
			u => u.Id == user.Id,
			Builders<KanbanUser>.Update.Set(u => u.Email, string.IsNullOrEmpty(trimmed) ? null : trimmed));

		TempData["ProfileMessage"] = "Adres e-mail został zaktualizowany.";
		return RedirectToAction(nameof(Profile));
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> ChangePassword(ChangePasswordInput model) {
		var user = await LoadCurrentUser();
		if (user is null)
			return Forbid();

		if (!ModelState.IsValid) {
			TempData["ProfileError"] = "Sprawdź poprawność wprowadzonych haseł.";
			return RedirectToAction(nameof(Profile));
		}

		if (!Auth.Verify(model.CurrentPassword, user.PasswordHash, user.PasswordSalt)) {
			TempData["ProfileError"] = "Obecne hasło jest nieprawidłowe.";
			return RedirectToAction(nameof(Profile));
		}

		var (hash, salt) = Auth.CreateCredential(model.NewPassword);
		await _db.UserCollection.UpdateOneAsync(
			u => u.Id == user.Id,
			Builders<KanbanUser>.Update
				.Set(u => u.PasswordHash, hash)
				.Set(u => u.PasswordSalt, salt));

		TempData["ProfileMessage"] = "Hasło zostało zmienione.";
		return RedirectToAction(nameof(Profile));
	}

	private async Task<KanbanUser?> LoadCurrentUser() {
		if (_currentUser.UserId is not { } userId)
			return null;
		return await _db.UserCollection.Find(u => u.Id == userId).FirstOrDefaultAsync();
	}
}
