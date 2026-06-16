using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace KanbanTaskManagement.Models;


public class Group {
	public ObjectId Id { get; set; }

	public string? Name { get; set; } = null;

	[BsonRequired]
	[BsonDateTimeOptions(
		Kind = DateTimeKind.Utc,
		Representation = BsonType.DateTime
	)]
	public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	[BsonRequired]
	public GroupMember[] Members { get; set; } = [];
}
