using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KanbanTaskManagement.Models;

public class BoardMember {
	public required ObjectId UserId { get; set; }

	[BsonRequired]
	[BsonRepresentation(BsonType.Int32)]
	public GroupRole Role { get; set; }
}
