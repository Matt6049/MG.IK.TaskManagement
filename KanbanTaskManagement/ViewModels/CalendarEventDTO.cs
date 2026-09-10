namespace KanbanTaskManagement.ViewModels;

public class CalendarEventDTO {
	public required string TaskId { get; set; }
	public required string BoardId { get; set; }
	public required string BoardName { get; set; }
	public required string Title { get; set; }
	public required string Date { get; set; }
	public required string ColumnType { get; set; }
	public required string Priority { get; set; }
	public bool Done { get; set; }
}
