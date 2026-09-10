using KanbanTaskManagement.Models;

namespace KanbanTaskManagement.ViewModels;

public class DashboardViewModel {
	public required IReadOnlyList<Board> Boards { get; init; }
	public int OverdueCount { get; init; }
	public int TodayCount { get; init; }
}
