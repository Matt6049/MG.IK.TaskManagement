using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KanbanTaskManagement.Models;

public enum TaskPriority {
	LOW,
	MEDIUM,
	HIGH
}

public class KanbanTask {
	[BsonRequired]
	public required string Name { get; set; }

	public string? Description { get; set; } = null!;

	[BsonRequired]
	[BsonRepresentation(BsonType.Int32)]
	public required TaskPriority Priority { get; set; } = TaskPriority.LOW;

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	[BsonRequired]
	public required string CreatorName { get; set; }

	[BsonRequired]
	public string[] AssignedUsers { get; set; } = [];
}