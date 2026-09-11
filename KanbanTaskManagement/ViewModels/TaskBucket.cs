namespace KanbanTaskManagement.ViewModels;

public enum TaskBucket {
	Overdue,
	Today,
	ThisWeek,
	Later,
	NoDueDate,
	Done,
}

public class TaskBucketCount {
	public required TaskBucket Bucket { get; init; }
	public required string Label { get; init; }
	public required string Color { get; init; }
	public string? Icon { get; init; }
	public int Count { get; init; }
}

public static class TaskBucketing {
	public static TaskBucket Of(MyTaskItem task, DateTime today) {
		if (task.Done)
			return TaskBucket.Done;
		if (task.DueDate is not { } due)
			return TaskBucket.NoDueDate;

		var date = due.Date;
		if (date < today)
			return TaskBucket.Overdue;
		if (date == today)
			return TaskBucket.Today;
		if (date <= today.AddDays(7))
			return TaskBucket.ThisWeek;
		return TaskBucket.Later;
	}

	public static List<TaskBucketCount> Summarize(IReadOnlyCollection<MyTaskItem> tasks, DateTime today) {
		var counts = tasks
			.GroupBy(t => Of(t, today))
			.ToDictionary(g => g.Key, g => g.Count());

		return Definitions
			.Select(d => new TaskBucketCount {
				Bucket = d.Bucket,
				Label = d.Label,
				Color = d.Color,
				Icon = d.Icon,
				Count = counts.GetValueOrDefault(d.Bucket, 0),
			})
			.ToList();
	}

	private static readonly (TaskBucket Bucket, string Label, string Color, string? Icon)[] Definitions = [
		(TaskBucket.Overdue, "Zaległe", "#d03b3b", "bi-exclamation-triangle-fill"),
		(TaskBucket.Today, "Na dziś", "#fab219", "bi-alarm-fill"),
		(TaskBucket.ThisWeek, "W tym tygodniu", "#898781", null),
		(TaskBucket.Later, "Później", "#c3c2b7", null),
		(TaskBucket.NoDueDate, "Bez terminu", "#e1e0d9", null),
		(TaskBucket.Done, "Ukończone", "#0ca30c", "bi-check-circle-fill"),
	];
}
