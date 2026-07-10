using KanbanTaskManagement.Data;
using KanbanTaskManagement.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace KanbanTaskManagement.Controllers {
	public class LoginController : Controller {
		private readonly MongoDBContext _db;

		public LoginController(MongoDBContext db) {
			_db = db;
		}

		public async Task<IActionResult> Login(string username, string password) {
			var userRes = await _db.UserCollection.FindAsync<KanbanUser>(u => u.Username == username);
			var user = userRes.FirstOrDefault();

			if (user != null && user.PasswordHash == Auth.GetHash(password, user.PasswordSalt)) {
				var claims = new List<Claim> {
					new Claim(ClaimTypes.Name, user.Username),
					new Claim(ClaimTypes.Email, user.Email ?? ""),
					new Claim(ClaimTypes.Role, "User"),
					new Claim("UserId", user.Id.ToString())
				};
				var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
				var authProperties = new AuthenticationProperties { IsPersistent = true };

				await HttpContext.SignInAsync(
					CookieAuthenticationDefaults.AuthenticationScheme,
					new ClaimsPrincipal(claimsIdentity),
					authProperties);

				return RedirectToAction("Index", "Dashboard");
			}

			return View();
		}
	}
}