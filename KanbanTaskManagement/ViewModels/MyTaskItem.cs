using KanbanTaskManagement.Models;

namespace KanbanTaskManagement.ViewModels;

public class MyTaskItem {
	public required string BoardId { get; set; }
	public required string BoardName { get; set; }
	public required string TaskId { get; set; }
	public required string TaskName { get; set; }
	public ColumnType Column { get; set; }
	public TaskPriority Priority { get; set; }
	public DateTime? DueDate { get; set; }
	public bool Done { get; set; }
}
