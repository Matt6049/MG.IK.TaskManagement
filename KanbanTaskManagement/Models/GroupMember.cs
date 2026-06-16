using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KanbanTaskManagement.Models;

public enum GroupRole {
	READ_ONLY,
	WRITE,
	ADMIN,
	OWNER
}

public class GroupMember {
	public required string Username { get; set; }

	[BsonRequired]
	[BsonRepresentation(BsonType.Int32)]
	public GroupRole Role { get; set; }
}
