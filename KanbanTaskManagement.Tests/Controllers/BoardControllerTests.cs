using KanbanTaskManagement.Controllers;
using KanbanTaskManagement.Models;
using KanbanTaskManagement.Services;
using KanbanTaskManagement.Tests.Support;
using KanbanTaskManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KanbanTaskManagement.Tests.Controllers;

public class BoardControllerTests : IClassFixture<MongoTestContext> {
	private readonly MongoTestContext _mongo;
	private readonly StubCurrentUser _user = new();
	private readonly BoardController _controller;

	public BoardControllerTests(MongoTestContext mongo) {
		_mongo = mongo;
		_controller = new BoardController(_mongo.Db, _user, new PermissionService(_mongo.Db));
		ControllerFactory.WireUpTempData(_controller);
	}

	private async Task<Board> InsertBoard(ObjectId ownerId, params BoardMember[] members) {
		var board = new Board {
			Name = "Board",
			OwnerName = "owner",
			OwnerId = ownerId,
			IsUserOwned = true,
			Members = members.ToList(),
		};
		await _mongo.Db.BoardCollection.InsertOneAsync(board);
		return board;
	}

	private Task<Board> FetchBoard(ObjectId id)
		=> _mongo.Db.BoardCollection.Find(b => b.Id == id).FirstAsync();

	[Fact]
	public async Task CreateBoard_SetsOwnerAndDefaultColumns() {
		var ownerId = ObjectId.GenerateNewId();
		_user.UserId = ownerId;
		_user.Username = "owner";

		await _controller.CreateBoard("My board");

		var board = await _mongo.Db.BoardCollection.Find(b => b.Name == "My board").FirstAsync();
		Assert.Equal(ownerId, board.OwnerId);
		Assert.Equal(3, board.Columns.Count);
	}

	[Fact]
	public async Task Index_ForOwnerWithLinkedGroup_PopulatesAllSections() {
		var ownerId = ObjectId.GenerateNewId();
		var memberId = ObjectId.GenerateNewId();
		var groupId = ObjectId.GenerateNewId();
		await _mongo.Db.GroupCollection.InsertOneAsync(new Group {
			Id = groupId,
			Name = "Linked group",
			Members = [new GroupMember { UserId = ownerId, Role = GroupRole.OWNER }],
		});
		var board = await InsertBoard(ownerId, new BoardMember { UserId = memberId, Role = GroupRole.WRITE });
		await _mongo.Db.BoardCollection.UpdateOneAsync(
			b => b.Id == board.Id, Builders<Board>.Update.Set(b => b.GroupId, groupId));
		await _mongo.Db.UserCollection.InsertOneAsync(new KanbanUser {
			Id = memberId, Username = "writer", PasswordHash = "x", PasswordSalt = "x",
		});
		_user.UserId = ownerId;

		var result = await _controller.Index(board.Id.ToString());

		var view = Assert.IsType<ViewResult>(result);
		var vm = Assert.IsType<BoardDetailsViewModel>(view.Model);
		Assert.Contains("writer", vm.Assignable);
		Assert.Contains(vm.Members, m => m.IsOwner);
		Assert.Equal("Linked group", vm.LinkedGroupName);
	}

	[Fact]
	public async Task CreateTask_ByReadOnlyMember_IsForbidden() {
		var readerId = ObjectId.GenerateNewId();
		var board = await InsertBoard(ObjectId.GenerateNewId(), new BoardMember { UserId = readerId, Role = GroupRole.READ_ONLY });
		_user.UserId = readerId;

		var result = await _controller.CreateTask(board.Id.ToString(), (int) ColumnType.BACKLOG, "Task", "desc", null, null, 0, null);

		Assert.IsType<ForbidResult>(result);
		var reloaded = await FetchBoard(board.Id);
		Assert.All(reloaded.Columns, c => Assert.Empty(c.Tasks));
	}

	[Fact]
	public async Task CreateTask_ClampsPriorityAndDropsIneligibleAssignees() {
		var writerId = ObjectId.GenerateNewId();
		var eligibleId = ObjectId.GenerateNewId();
		var board = await InsertBoard(
			ObjectId.GenerateNewId(),
			new BoardMember { UserId = writerId, Role = GroupRole.WRITE },
			new BoardMember { UserId = eligibleId, Role = GroupRole.READ_ONLY });
		await _mongo.Db.UserCollection.InsertOneAsync(new KanbanUser {
			Id = eligibleId, Username = "eligible", PasswordHash = "x", PasswordSalt = "x",
		});
		_user.UserId = writerId;
		_user.Username = "writer";

		await _controller.CreateTask(
			board.Id.ToString(), (int) ColumnType.BACKLOG, "Task", "desc", null, null, 99, ["eligible", "ghost"]);

		var reloaded = await FetchBoard(board.Id);
		var task = reloaded.Columns.Single(c => c.Type == ColumnType.BACKLOG).Tasks.Single();
		Assert.Equal(TaskPriority.HIGH, task.Priority);
		Assert.Equal(["eligible"], task.AssignedUsers);
	}

	[Fact]
	public async Task MoveTask_ToDone_SetsCompletedAt() {
		var ownerId = ObjectId.GenerateNewId();
		var board = await InsertBoard(ownerId);
		_user.UserId = ownerId;
		await _controller.CreateTask(board.Id.ToString(), (int) ColumnType.BACKLOG, "Task", "", null, null, 0, null);
		var created = await FetchBoard(board.Id);
		var taskId = created.Columns.Single(c => c.Type == ColumnType.BACKLOG).Tasks.Single().Id;

		await _controller.MoveTask(board.Id.ToString(), taskId.ToString(), (int) ColumnType.DONE);

		var reloaded = await FetchBoard(board.Id);
		var task = reloaded.Columns.Single(c => c.Type == ColumnType.DONE).Tasks.Single();
		Assert.NotNull(task.CompletedAt);
	}

	[Fact]
	public async Task MoveTask_AwayFromDone_ClearsCompletedAt() {
		var ownerId = ObjectId.GenerateNewId();
		var board = await InsertBoard(ownerId);
		_user.UserId = ownerId;
		await _controller.CreateTask(board.Id.ToString(), (int) ColumnType.BACKLOG, "Task", "", null, null, 0, null);
		var created = await FetchBoard(board.Id);
		var taskId = created.Columns.Single(c => c.Type == ColumnType.BACKLOG).Tasks.Single().Id;
		await _controller.MoveTask(board.Id.ToString(), taskId.ToString(), (int) ColumnType.DONE);

		await _controller.MoveTask(board.Id.ToString(), taskId.ToString(), (int) ColumnType.IN_PROGRESS);

		var reloaded = await FetchBoard(board.Id);
		var task = reloaded.Columns.Single(c => c.Type == ColumnType.IN_PROGRESS).Tasks.Single();
		Assert.Null(task.CompletedAt);
	}

	[Fact]
	public async Task DeleteTask_RemovesIt() {
		var ownerId = ObjectId.GenerateNewId();
		var board = await InsertBoard(ownerId);
		_user.UserId = ownerId;
		await _controller.CreateTask(board.Id.ToString(), (int) ColumnType.BACKLOG, "Task", "", null, null, 0, null);
		var created = await FetchBoard(board.Id);
		var taskId = created.Columns.Single(c => c.Type == ColumnType.BACKLOG).Tasks.Single().Id;

		await _controller.DeleteTask(board.Id.ToString(), taskId.ToString());

		var reloaded = await FetchBoard(board.Id);
		Assert.All(reloaded.Columns, c => Assert.Empty(c.Tasks));
	}

	[Fact]
	public async Task AddMember_CannotGrantAboveAdmin() {
		var ownerId = ObjectId.GenerateNewId();
		var newUserId = ObjectId.GenerateNewId();
		var board = await InsertBoard(ownerId);
		await _mongo.Db.UserCollection.InsertOneAsync(new KanbanUser {
			Id = newUserId, Username = "newbie", PasswordHash = "x", PasswordSalt = "x",
		});
		_user.UserId = ownerId;

		await _controller.AddMember(board.Id.ToString(), "newbie", (int) GroupRole.OWNER);

		var reloaded = await FetchBoard(board.Id);
		Assert.Equal(GroupRole.ADMIN, reloaded.Members.Single(m => m.UserId == newUserId).Role);
	}

	[Fact]
	public async Task DeleteBoard_ByAdminMember_IsForbidden_OnlyOwnerCanDelete() {
		var ownerId = ObjectId.GenerateNewId();
		var adminId = ObjectId.GenerateNewId();
		var board = await InsertBoard(ownerId, new BoardMember { UserId = adminId, Role = GroupRole.ADMIN });
		_user.UserId = adminId;

		var result = await _controller.DeleteBoard(board.Id.ToString());

		Assert.IsType<ForbidResult>(result);
		Assert.NotNull(await _mongo.Db.BoardCollection.Find(b => b.Id == board.Id).FirstOrDefaultAsync());
	}

	[Fact]
	public async Task DeleteBoard_ByOwner_Succeeds() {
		var ownerId = ObjectId.GenerateNewId();
		var board = await InsertBoard(ownerId);
		_user.UserId = ownerId;

		await _controller.DeleteBoard(board.Id.ToString());

		Assert.Null(await _mongo.Db.BoardCollection.Find(b => b.Id == board.Id).FirstOrDefaultAsync());
	}

	[Fact]
	public async Task MoveTask_ToUndefinedColumn_IsRejectedAndTaskStays() {
		var ownerId = ObjectId.GenerateNewId();
		var board = await InsertBoard(ownerId);
		_user.UserId = ownerId;
		await _controller.CreateTask(board.Id.ToString(), (int) ColumnType.BACKLOG, "Task", "", null, null, 0, null);
		var created = await FetchBoard(board.Id);
		var taskId = created.Columns.Single(c => c.Type == ColumnType.BACKLOG).Tasks.Single().Id;

		var result = await _controller.MoveTask(board.Id.ToString(), taskId.ToString(), 99);

		Assert.IsType<BadRequestResult>(result);
		var reloaded = await FetchBoard(board.Id);
		Assert.Single(reloaded.Columns.Single(c => c.Type == ColumnType.BACKLOG).Tasks);
		Assert.All(reloaded.Columns.Where(c => c.Type != ColumnType.BACKLOG), c => Assert.Empty(c.Tasks));
	}

	[Fact]
	public async Task CreateTask_ToUndefinedColumn_IsRejected() {
		var ownerId = ObjectId.GenerateNewId();
		var board = await InsertBoard(ownerId);
		_user.UserId = ownerId;

		var result = await _controller.CreateTask(board.Id.ToString(), 99, "Task", "", null, null, 0, null);

		Assert.IsType<BadRequestResult>(result);
		var reloaded = await FetchBoard(board.Id);
		Assert.All(reloaded.Columns, c => Assert.Empty(c.Tasks));
	}

	[Fact]
	public async Task UpdateTask_WithTaskIdFromAnotherBoard_DoesNotLeakOrEditIt() {
		var ownerId = ObjectId.GenerateNewId();
		var boardA = await InsertBoard(ownerId);
		var boardB = await InsertBoard(ObjectId.GenerateNewId());
		_user.UserId = ownerId;
		await _controller.CreateTask(boardA.Id.ToString(), (int) ColumnType.BACKLOG, "Task A", "", null, null, 0, null);

		_user.UserId = boardB.OwnerId;
		await _controller.CreateTask(boardB.Id.ToString(), (int) ColumnType.BACKLOG, "Task B (secret)", "", null, null, 0, null);
		var boardBReloaded = await FetchBoard(boardB.Id);
		var secretTaskId = boardBReloaded.Columns.Single(c => c.Type == ColumnType.BACKLOG).Tasks.Single().Id;

		_user.UserId = ownerId;
		var result = await _controller.UpdateTask(boardA.Id.ToString(), secretTaskId.ToString(), "hijacked", "", null, null, 0, null);

		Assert.IsType<NotFoundResult>(result);
		var boardBAfter = await FetchBoard(boardB.Id);
		Assert.Equal("Task B (secret)", boardBAfter.Columns.Single(c => c.Type == ColumnType.BACKLOG).Tasks.Single().Name);
	}

	[Fact]
	public async Task SetTheme_InvalidHex_KeepsPreviousColor() {
		var ownerId = ObjectId.GenerateNewId();
		var board = await InsertBoard(ownerId);
		var originalBacklog = board.Theme.BacklogColor;
		_user.UserId = ownerId;

		await _controller.SetTheme(board.Id.ToString(), "#111111", "#222222", "not-a-color", "#444444", "#555555");

		var reloaded = await FetchBoard(board.Id);
		Assert.Equal(originalBacklog, reloaded.Theme.BacklogColor);
		Assert.Equal("#111111", reloaded.Theme.HeaderFrom);
	}
}
