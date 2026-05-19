using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KanbanTaskManagement.Models; 

public class Table {
	[BsonId]
	[BsonRepresentation(BsonType.ObjectId)]
	public string? Id { get; set; }

	[BsonElement("idk")]
	public string? idk { get; set; }
}
