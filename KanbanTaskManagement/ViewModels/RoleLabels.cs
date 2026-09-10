using KanbanTaskManagement.Models;

namespace KanbanTaskManagement.ViewModels;

public static class RoleLabels {
	public static readonly GroupRole[] All = {
		GroupRole.READ_ONLY, GroupRole.WRITE, GroupRole.ADMIN, GroupRole.OWNER,
	};

	public static readonly GroupRole[] BoardAssignable = {
		GroupRole.READ_ONLY, GroupRole.WRITE, GroupRole.ADMIN,
	};

	public static readonly TaskPriority[] Priorities = {
		TaskPriority.LOW, TaskPriority.MEDIUM, TaskPriority.HIGH,
	};

	public static string PolishName(this GroupRole role) => role switch {
		GroupRole.READ_ONLY => "Odczyt",
		GroupRole.WRITE => "Zapis",
		GroupRole.ADMIN => "Administrator",
		GroupRole.OWNER => "Właściciel",
		_ => role.ToString(),
	};

	public static string PolishName(this TaskPriority priority) => priority switch {
		TaskPriority.LOW => "Niski",
		TaskPriority.MEDIUM => "Średni",
		TaskPriority.HIGH => "Wysoki",
		_ => priority.ToString(),
	};
}
