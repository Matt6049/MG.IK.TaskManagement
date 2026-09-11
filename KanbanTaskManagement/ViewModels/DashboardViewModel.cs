using KanbanTaskManagement.Models;

namespace KanbanTaskManagement.ViewModels;

public class DashboardViewModel {
	public required IReadOnlyList<Board> Boards { get; init; }
	public List<TaskBucketCount> TaskBuckets { get; init; } = [];
	public int TaskTotal => TaskBuckets.Sum(b => b.Count);
}
