using MongoDB.Bson;

namespace KanbanTaskManagement.Models;

public interface IMongoDocument {
	public ObjectId Id { get; set; }
}
