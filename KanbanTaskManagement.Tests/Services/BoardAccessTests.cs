using KanbanTaskManagement.Models;
using KanbanTaskManagement.Services;

namespace KanbanTaskManagement.Tests.Services;

public class BoardAccessTests {
	[Fact]
	public void None_DeniesEverything() {
		var access = BoardAccess.None;

		Assert.False(access.CanView);
		Assert.False(access.CanEditTasks);
		Assert.False(access.CanManageMembers);
		Assert.False(access.CanManageBoard);
		Assert.False(access.CanDeleteBoard);
	}

	[Theory]
	[InlineData(GroupRole.READ_ONLY, true, false, false, false, false)]
	[InlineData(GroupRole.WRITE, true, true, false, false, false)]
	[InlineData(GroupRole.ADMIN, true, true, true, true, false)]
	[InlineData(GroupRole.OWNER, true, true, true, true, true)]
	public void Role_GrantsExpectedPermissions(
		GroupRole role, bool canView, bool canEditTasks, bool canManageMembers, bool canManageBoard, bool canDeleteBoard) {
		var access = new BoardAccess { Role = role };

		Assert.Equal(canView, access.CanView);
		Assert.Equal(canEditTasks, access.CanEditTasks);
		Assert.Equal(canManageMembers, access.CanManageMembers);
		Assert.Equal(canManageBoard, access.CanManageBoard);
		Assert.Equal(canDeleteBoard, access.CanDeleteBoard);
	}
}
