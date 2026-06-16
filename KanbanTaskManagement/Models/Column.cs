using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KanbanTaskManagement.Models;

public enum ColumnType {
	BACKLOG,
	IN_PROGRESS,
	DONE
};

public class Column {
	[BsonRequired]
	[BsonRepresentation(BsonType.Int32)]
	public required ColumnType Type { get; set; }

	public List<KanbanTask> Tasks { get; set; } = [];
}
