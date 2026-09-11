using KanbanTaskManagement.Models;
using KanbanTaskManagement.Services;
using KanbanTaskManagement.Tests.Support;
using MongoDB.Bson;

namespace KanbanTaskManagement.Tests.Services;

public class PermissionServiceTests : IClassFixture<MongoTestContext> {
	private readonly MongoTestContext _mongo;
	private readonly PermissionService _service;

	public PermissionServiceTests(MongoTestContext mongo) {
		_mongo = mongo;
		_service = new PermissionService(_mongo.Db);
	}

	private static Board NewBoard(ObjectId ownerId) => new() {
		Name = "Board",
		OwnerName = "owner",
		OwnerId = ownerId,
		IsUserOwned = true,
	};

	[Fact]
	public async Task Owner_GetsOwnerRole_EvenWithoutMembership() {
		var ownerId = ObjectId.GenerateNewId();
		var board = NewBoard(ownerId);

		var access = await _service.ResolveAsync(ownerId, board);

		Assert.Equal(GroupRole.OWNER, access.Role);
	}

	[Fact]
	public async Task NonMember_GetsNoAccess() {
		var board = NewBoard(ObjectId.GenerateNewId());
		var strangerId = ObjectId.GenerateNewId();

		var access = await _service.ResolveAsync(strangerId, board);

		Assert.Null(access.Role);
		Assert.False(access.CanView);
	}

	[Fact]
	public async Task EmptyUserId_GetsNoAccess() {
		var board = NewBoard(ObjectId.GenerateNewId());

		var access = await _service.ResolveAsync(ObjectId.Empty, board);

		Assert.Null(access.Role);
	}

	[Fact]
	public async Task DirectBoardMember_GetsTheirBoardRole() {
		var board = NewBoard(ObjectId.GenerateNewId());
		var memberId = ObjectId.GenerateNewId();
		board.Members.Add(new BoardMember { UserId = memberId, Role = GroupRole.WRITE });

		var access = await _service.ResolveAsync(memberId, board);

		Assert.Equal(GroupRole.WRITE, access.Role);
	}

	[Fact]
	public async Task GroupMember_GetsRoleFromLinkedGroup() {
		var groupId = ObjectId.GenerateNewId();
		var memberId = ObjectId.GenerateNewId();
		await _mongo.Db.GroupCollection.InsertOneAsync(new Group {
			Id = groupId,
			Members = [new GroupMember { UserId = memberId, Role = GroupRole.ADMIN }],
		});

		var board = NewBoard(ObjectId.GenerateNewId());
		board.GroupId = groupId;

		var access = await _service.ResolveAsync(memberId, board);

		Assert.Equal(GroupRole.ADMIN, access.Role);
	}

	[Fact]
	public async Task UserInLinkedGroup_ButNotAMember_GetsNoAccess() {
		var groupId = ObjectId.GenerateNewId();
		await _mongo.Db.GroupCollection.InsertOneAsync(new Group {
			Id = groupId,
			Members = [new GroupMember { UserId = ObjectId.GenerateNewId(), Role = GroupRole.ADMIN }],
		});

		var board = NewBoard(ObjectId.GenerateNewId());
		board.GroupId = groupId;
		var strangerId = ObjectId.GenerateNewId();

		var access = await _service.ResolveAsync(strangerId, board);

		Assert.Null(access.Role);
	}

	[Fact]
	public async Task EffectiveRole_IsMaxOfBoardRoleAndGroupRole_WhenGroupRoleIsHigher() {
		var groupId = ObjectId.GenerateNewId();
		var userId = ObjectId.GenerateNewId();
		await _mongo.Db.GroupCollection.InsertOneAsync(new Group {
			Id = groupId,
			Members = [new GroupMember { UserId = userId, Role = GroupRole.ADMIN }],
		});

		var board = NewBoard(ObjectId.GenerateNewId());
		board.GroupId = groupId;
		board.Members.Add(new BoardMember { UserId = userId, Role = GroupRole.READ_ONLY });

		var access = await _service.ResolveAsync(userId, board);

		Assert.Equal(GroupRole.ADMIN, access.Role);
	}

	[Fact]
	public async Task BoardMemberWithOwnerRole_IsCappedToAdmin() {
		var board = NewBoard(ObjectId.GenerateNewId());
		var memberId = ObjectId.GenerateNewId();
		board.Members.Add(new BoardMember { UserId = memberId, Role = GroupRole.OWNER });

		var access = await _service.ResolveAsync(memberId, board);

		Assert.Equal(GroupRole.ADMIN, access.Role);
		Assert.False(access.CanDeleteBoard);
	}

	[Fact]
	public async Task EffectiveRole_IsMaxOfBoardRoleAndGroupRole_WhenBoardRoleIsHigher() {
		var groupId = ObjectId.GenerateNewId();
		var userId = ObjectId.GenerateNewId();
		await _mongo.Db.GroupCollection.InsertOneAsync(new Group {
			Id = groupId,
			Members = [new GroupMember { UserId = userId, Role = GroupRole.READ_ONLY }],
		});

		var board = NewBoard(ObjectId.GenerateNewId());
		board.GroupId = groupId;
		board.Members.Add(new BoardMember { UserId = userId, Role = GroupRole.ADMIN });

		var access = await _service.ResolveAsync(userId, board);

		Assert.Equal(GroupRole.ADMIN, access.Role);
	}
}
