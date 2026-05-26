using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KanbanTaskManagement.Models;

public class User {
	public ObjectId Id { get; set; }

	[BsonRequired]
	public required string Username { get; set; }

	public string? Email { get; set; } = null!;

	[BsonRequired]
	public required string PasswordHash { get; set; }

	[BsonRequired]
	public required string PasswordSalt { get; set; }

	[BsonRequired]
	[BsonDateTimeOptions(
		Kind = DateTimeKind.Utc,
		Representation = BsonType.DateTime)]
	public DateTime Created_at { get; set; }
}

