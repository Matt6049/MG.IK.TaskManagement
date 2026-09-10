using KanbanTaskManagement.Models;
using MongoDB.Bson;

namespace KanbanTaskManagement.ViewModels;

public class KanbanColumnViewModel {
	public required ObjectId BoardId { get; init; }
	public required Column Column { get; init; }
	public bool CanEdit { get; init; }
}
