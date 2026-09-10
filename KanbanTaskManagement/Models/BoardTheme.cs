namespace KanbanTaskManagement.Models;

public class BoardTheme {
	public string HeaderFrom { get; set; } = "#008080";
	public string HeaderTo { get; set; } = "#000000";
	public string BacklogColor { get; set; } = "#3b3b3b";
	public string InProgressColor { get; set; } = "#005f5f";
	public string DoneColor { get; set; } = "#004d26";

	public string ColumnColor(ColumnType type) => type switch {
		ColumnType.BACKLOG => BacklogColor,
		ColumnType.IN_PROGRESS => InProgressColor,
		ColumnType.DONE => DoneColor,
		_ => BacklogColor,
	};
}
