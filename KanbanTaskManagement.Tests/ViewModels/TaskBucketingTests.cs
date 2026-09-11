using KanbanTaskManagement.Models;
using KanbanTaskManagement.ViewModels;

namespace KanbanTaskManagement.Tests.ViewModels;

public class TaskBucketingTests {
	private static readonly DateTime Today = new(2026, 9, 11);

	private static MyTaskItem NewTask(DateTime? due = null, bool done = false) => new() {
		BoardId = "b", BoardName = "Board", TaskId = "t", TaskName = "Task",
		Column = ColumnType.BACKLOG, Priority = TaskPriority.LOW, DueDate = due, Done = done,
	};

	[Fact]
	public void Of_DoneTask_IsDoneRegardlessOfDueDate() {
		var task = NewTask(due: Today.AddDays(-5), done: true);

		Assert.Equal(TaskBucket.Done, TaskBucketing.Of(task, Today));
	}

	[Fact]
	public void Of_NoDueDate_IsNoDueDate() {
		var task = NewTask(due: null);

		Assert.Equal(TaskBucket.NoDueDate, TaskBucketing.Of(task, Today));
	}

	[Fact]
	public void Of_PastDueDate_IsOverdue() {
		var task = NewTask(due: Today.AddDays(-1));

		Assert.Equal(TaskBucket.Overdue, TaskBucketing.Of(task, Today));
	}

	[Fact]
	public void Of_DueToday_IsToday() {
		var task = NewTask(due: Today);

		Assert.Equal(TaskBucket.Today, TaskBucketing.Of(task, Today));
	}

	[Theory]
	[InlineData(1)]
	[InlineData(7)]
	public void Of_DueWithinAWeek_IsThisWeek(int daysAhead) {
		var task = NewTask(due: Today.AddDays(daysAhead));

		Assert.Equal(TaskBucket.ThisWeek, TaskBucketing.Of(task, Today));
	}

	[Fact]
	public void Of_DueAfterAWeek_IsLater() {
		var task = NewTask(due: Today.AddDays(8));

		Assert.Equal(TaskBucket.Later, TaskBucketing.Of(task, Today));
	}

	[Fact]
	public void Summarize_CountsEachBucketAndKeepsFixedOrder() {
		var tasks = new List<MyTaskItem> {
			NewTask(due: Today.AddDays(-2)),
			NewTask(due: Today.AddDays(-1)),
			NewTask(due: Today),
			NewTask(due: Today.AddDays(3)),
			NewTask(due: null),
			NewTask(due: Today.AddDays(-10), done: true),
		};

		var summary = TaskBucketing.Summarize(tasks, Today);

		Assert.Equal(
			[TaskBucket.Overdue, TaskBucket.Today, TaskBucket.ThisWeek, TaskBucket.Later, TaskBucket.NoDueDate, TaskBucket.Done],
			summary.Select(b => b.Bucket));
		Assert.Equal(2, summary.Single(b => b.Bucket == TaskBucket.Overdue).Count);
		Assert.Equal(1, summary.Single(b => b.Bucket == TaskBucket.Today).Count);
		Assert.Equal(1, summary.Single(b => b.Bucket == TaskBucket.ThisWeek).Count);
		Assert.Equal(0, summary.Single(b => b.Bucket == TaskBucket.Later).Count);
		Assert.Equal(1, summary.Single(b => b.Bucket == TaskBucket.NoDueDate).Count);
		Assert.Equal(1, summary.Single(b => b.Bucket == TaskBucket.Done).Count);
	}

	[Fact]
	public void Summarize_WithNoTasks_ReturnsAllBucketsAtZero() {
		var summary = TaskBucketing.Summarize([], Today);

		Assert.Equal(6, summary.Count);
		Assert.All(summary, b => Assert.Equal(0, b.Count));
	}
}
