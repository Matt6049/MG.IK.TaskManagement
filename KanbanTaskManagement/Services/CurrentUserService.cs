using System.Security.Claims;
using MongoDB.Bson;

namespace KanbanTaskManagement.Services;

public static class AppClaims {
	public const string UserId = "UserId";
}

public interface ICurrentUser {
	bool IsAuthenticated { get; }
	ObjectId? UserId { get; }
	string? Username { get; }
}

public class CurrentUserService : ICurrentUser {
	private readonly ClaimsPrincipal? _principal;

	public CurrentUserService(IHttpContextAccessor accessor) {
		_principal = accessor.HttpContext?.User;
	}

	public bool IsAuthenticated => _principal?.Identity?.IsAuthenticated ?? false;

	public ObjectId? UserId {
		get {
			var raw = _principal?.FindFirst(AppClaims.UserId)?.Value;
			return ObjectId.TryParse(raw, out var id) ? id : null;
		}
	}

	public string? Username => _principal?.Identity?.Name;
}
