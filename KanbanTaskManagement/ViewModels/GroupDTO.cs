using MongoDB.Bson;

namespace KanbanTaskManagement.ViewModels;

public class GroupDTO {
	public ObjectId Id { get; set; }
	public string? Name { get; set; }
	public required DateTime CreatedAt { get; set; }
}
