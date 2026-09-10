using KanbanTaskManagement.Data;
using KanbanTaskManagement.Models;
using KanbanTaskManagement.Services;
using KanbanTaskManagement.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;

namespace KanbanTaskManagement.Controllers;

[AllowAnonymous]
public class AccountController : Controller {
	private readonly MongoDBContext _db;

	public AccountController(MongoDBContext db) => _db = db;

	[HttpGet]
	public IActionResult Login(string? returnUrl = null) {
		if (User.Identity?.IsAuthenticated == true)
			return RedirectToLocal(returnUrl);

		ViewData["ReturnUrl"] = returnUrl;
		return View(new LoginInput());
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Login(LoginInput model, string? returnUrl = null) {
		ViewData["ReturnUrl"] = returnUrl;
		if (!ModelState.IsValid)
			return View(model);

		var user = await (await _db.UserCollection.FindAsync(u => u.Username == model.Username)).FirstOrDefaultAsync();
		if (user == null || !Auth.Verify(model.Password, user.PasswordHash, user.PasswordSalt)) {
			ModelState.AddModelError(string.Empty, "Nieprawidłowa nazwa użytkownika lub hasło.");
			return View(model);
		}

		await SignInAsync(user, model.RememberMe);
		await _db.UserCollection.UpdateOneAsync(
			u => u.Id == user.Id,
			Builders<KanbanUser>.Update.Set(u => u.LastActive, DateTime.UtcNow));

		return RedirectToLocal(returnUrl);
	}

	[HttpGet]
	public IActionResult Register() {
		if (User.Identity?.IsAuthenticated == true)
			return RedirectToAction("Index", "Dashboard");

		return View(new RegisterInput());
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Register(RegisterInput model) {
		if (!ModelState.IsValid)
			return View(model);

		var taken = await (await _db.UserCollection.FindAsync(u => u.Username == model.Username)).AnyAsync();
		if (taken) {
			ModelState.AddModelError(nameof(model.Username), "Ta nazwa użytkownika jest już zajęta.");
			return View(model);
		}

		var (hash, salt) = Auth.CreateCredential(model.Password);
		var user = new KanbanUser {
			Username = model.Username.Trim(),
			Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim(),
			PasswordHash = hash,
			PasswordSalt = salt,
			CreatedAt = DateTime.UtcNow,
			LastActive = DateTime.UtcNow,
		};
		await _db.UserCollection.InsertOneAsync(user);

		await SignInAsync(user, isPersistent: true);
		return RedirectToAction("Index", "Dashboard");
	}

	[HttpPost]
	[Authorize]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Logout() {
		await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
		return RedirectToAction(nameof(Login));
	}

	private async Task SignInAsync(KanbanUser user, bool isPersistent) {
		var claims = new List<Claim> {
			new(ClaimTypes.NameIdentifier, user.Id.ToString()),
			new(ClaimTypes.Name, user.Username),
			new(ClaimTypes.Email, user.Email ?? ""),
			new(ClaimTypes.Role, "User"),
			new(AppClaims.UserId, user.Id.ToString()),
		};
		var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

		await HttpContext.SignInAsync(
			CookieAuthenticationDefaults.AuthenticationScheme,
			new ClaimsPrincipal(identity),
			new AuthenticationProperties { IsPersistent = isPersistent });
	}

	private IActionResult RedirectToLocal(string? returnUrl)
		=> Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl!) : RedirectToAction("Index", "Dashboard");
}
