using KanbanTaskManagement.Data;
using KanbanTaskManagement.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KanbanTaskManagement.Services;

public class BoardAccess {
	public GroupRole? Role { get; init; }

	public bool CanView => Role is not null;
	public bool CanEditTasks => Role >= GroupRole.WRITE;
	public bool CanManageMembers => Role >= GroupRole.ADMIN;
	public bool CanManageBoard => Role >= GroupRole.ADMIN;
	public bool CanDeleteBoard => Role == GroupRole.OWNER;

	public static readonly BoardAccess None = new() { Role = null };
}

public interface IPermissionService {
	Task<BoardAccess> ResolveAsync(ObjectId userId, Board board);
}

public class PermissionService : IPermissionService {
	private readonly MongoDBContext _db;

	public PermissionService(MongoDBContext db) {
		_db = db;
	}

	public async Task<BoardAccess> ResolveAsync(ObjectId userId, Board board) {
		if (userId == ObjectId.Empty)
			return BoardAccess.None;

		if (board.OwnerId == userId)
			return new BoardAccess { Role = GroupRole.OWNER };

		GroupRole? role = board.Members
			.Where(m => m.UserId == userId)
			.Select(m => (GroupRole?) (m.Role >= GroupRole.OWNER ? GroupRole.ADMIN : m.Role))
			.FirstOrDefault();

		if (board.GroupId != ObjectId.Empty) {
			var group = await _db.GroupCollection
				.Find(g => g.Id == board.GroupId)
				.FirstOrDefaultAsync();

			GroupRole? groupRole = group?.Members
				.Where(m => m.UserId == userId)
				.Select(m => (GroupRole?) m.Role)
				.FirstOrDefault();

			if (groupRole is not null && (role is null || groupRole > role))
				role = groupRole;
		}

		return new BoardAccess { Role = role };
	}
}
