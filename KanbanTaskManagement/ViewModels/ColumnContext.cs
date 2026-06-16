using KanbanTaskManagement.Models;
using MongoDB.Bson;

namespace KanbanTaskManagement.ViewModels;

public class ColumnContext {
	public required ObjectId BoardId { get; set; }
	public required ColumnType Col { get; set; }
}
