using KanbanTaskManagement.Services;
using MongoDB.Bson;

namespace KanbanTaskManagement.Tests.Support;

public class StubCurrentUser : ICurrentUser {
	public ObjectId? UserId { get; set; }
	public string? Username { get; set; }

	public bool IsAuthenticated => UserId is not null;
}
