using KanbanTaskManagement.Models;

namespace KanbanTaskManagement.ViewModels;

public static class RoleLabels {
	public static readonly GroupRole[] All = {
		GroupRole.READ_ONLY, GroupRole.WRITE, GroupRole.ADMIN, GroupRole.OWNER,
	};

	public static readonly GroupRole[] BoardAssignable = {
		GroupRole.READ_ONLY, GroupRole.WRITE, GroupRole.ADMIN,
	};

	public static string PolishName(this GroupRole role) => role switch {
		GroupRole.READ_ONLY => "Odczyt",
		GroupRole.WRITE => "Zapis",
		GroupRole.ADMIN => "Administrator",
		GroupRole.OWNER => "Właściciel",
		_ => role.ToString(),
	};
}
