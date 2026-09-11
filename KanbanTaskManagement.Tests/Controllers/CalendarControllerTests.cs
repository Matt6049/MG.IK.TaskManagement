using KanbanTaskManagement.Controllers;
using KanbanTaskManagement.Models;
using KanbanTaskManagement.Services;
using KanbanTaskManagement.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KanbanTaskManagement.Tests.Controllers;

public class CalendarControllerTests : IClassFixture<MongoTestContext> {
	private readonly MongoTestContext _mongo;
	private readonly StubCurrentUser _user = new();
	private readonly CalendarController _controller;

	public CalendarControllerTests(MongoTestContext mongo) {
		_mongo = mongo;
		_controller = new CalendarController(_mongo.Db, _user, new PermissionService(_mongo.Db));
	}

	private async Task<Board> InsertBoard(ObjectId ownerId) {
		var board = new Board { Name = "Board", OwnerName = "owner", OwnerId = ownerId, IsUserOwned = true };
		await _mongo.Db.BoardCollection.InsertOneAsync(board);
		return board;
	}

	[Fact]
	public async Task Index_WithInaccessibleBoard_ReturnsNotFound() {
		var board = await InsertBoard(ObjectId.GenerateNewId());
		_user.UserId = ObjectId.GenerateNewId();

		var result = await _controller.Index(board.Id.ToString(), null);

		Assert.IsType<NotFoundResult>(result);
	}

	[Fact]
	public async Task Index_WithAccessibleBoard_SetsScopeName() {
		var ownerId = ObjectId.GenerateNewId();
		var board = await InsertBoard(ownerId);
		_user.UserId = ownerId;

		var result = await _controller.Index(board.Id.ToString(), null);

		Assert.IsType<ViewResult>(result);
		Assert.Equal("Board", _controller.ViewData["ScopeName"]);
	}

	[Fact]
	public async Task Index_WithNonexistentBoard_ReturnsNotFound() {
		_user.UserId = ObjectId.GenerateNewId();

		var result = await _controller.Index(ObjectId.GenerateNewId().ToString(), null);

		Assert.IsType<NotFoundResult>(result);
	}

	[Fact]
	public async Task Index_WithGroupTheUserIsNotIn_ReturnsNotFound() {
		var groupId = ObjectId.GenerateNewId();
		await _mongo.Db.GroupCollection.InsertOneAsync(new Group {
			Id = groupId,
			Members = [new GroupMember { UserId = ObjectId.GenerateNewId(), Role = GroupRole.OWNER }],
		});
		_user.UserId = ObjectId.GenerateNewId();

		var result = await _controller.Index(null, groupId.ToString());

		Assert.IsType<NotFoundResult>(result);
	}

	[Fact]
	public async Task Index_WithGroupMembership_SetsScopeName() {
		var groupId = ObjectId.GenerateNewId();
		var userId = ObjectId.GenerateNewId();
		await _mongo.Db.GroupCollection.InsertOneAsync(new Group {
			Id = groupId,
			Name = "My group",
			Members = [new GroupMember { UserId = userId, Role = GroupRole.WRITE }],
		});
		_user.UserId = userId;

		var result = await _controller.Index(null, groupId.ToString());

		Assert.IsType<ViewResult>(result);
		Assert.Equal("My group", _controller.ViewData["ScopeName"]);
	}

	[Fact]
	public async Task Index_WithNoScope_RendersWithoutScopeName() {
		_user.UserId = ObjectId.GenerateNewId();

		var result = await _controller.Index(null, null);

		Assert.IsType<ViewResult>(result);
		Assert.Null(_controller.ViewData["ScopeName"]);
	}
}
