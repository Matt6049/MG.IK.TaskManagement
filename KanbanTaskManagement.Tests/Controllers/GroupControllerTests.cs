using KanbanTaskManagement.Controllers;
using KanbanTaskManagement.Models;
using KanbanTaskManagement.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KanbanTaskManagement.Tests.Controllers;

public class GroupControllerTests : IClassFixture<MongoTestContext> {
	private readonly MongoTestContext _mongo;
	private readonly StubCurrentUser _user = new();
	private readonly GroupController _controller;

	public GroupControllerTests(MongoTestContext mongo) {
		_mongo = mongo;
		_controller = new GroupController(_mongo.Db, _user);
		ControllerFactory.WireUpTempData(_controller);
	}

	private async Task<Group> InsertGroup(params GroupMember[] members) {
		var group = new Group { Members = members.ToList() };
		await _mongo.Db.GroupCollection.InsertOneAsync(group);
		return group;
	}

	private Task<Group> FetchGroup(ObjectId id)
		=> _mongo.Db.GroupCollection.Find(g => g.Id == id).FirstAsync();

	[Fact]
	public async Task Create_AddsCreatorAsOwner() {
		var userId = ObjectId.GenerateNewId();
		_user.UserId = userId;

		await _controller.Create("New group");

		var group = await _mongo.Db.GroupCollection.Find(g => g.Name == "New group").FirstAsync();
		var member = Assert.Single(group.Members);
		Assert.Equal(userId, member.UserId);
		Assert.Equal(GroupRole.OWNER, member.Role);
	}

	[Fact]
	public async Task AddMember_ByAdmin_CannotGrantAboveAdmin() {
		var adminId = ObjectId.GenerateNewId();
		var newUserId = ObjectId.GenerateNewId();
		var group = await InsertGroup(new GroupMember { UserId = adminId, Role = GroupRole.ADMIN });
		await _mongo.Db.UserCollection.InsertOneAsync(new KanbanUser {
			Id = newUserId, Username = "newbie-admin-test", PasswordHash = "x", PasswordSalt = "x",
		});
		_user.UserId = adminId;

		await _controller.AddMember(group.Id.ToString(), "newbie-admin-test", (int) GroupRole.OWNER);

		var reloaded = await FetchGroup(group.Id);
		var added = reloaded.Members.Single(m => m.UserId == newUserId);
		Assert.Equal(GroupRole.ADMIN, added.Role);
	}

	[Fact]
	public async Task AddMember_ByNonAdmin_IsForbidden() {
		var writerId = ObjectId.GenerateNewId();
		var group = await InsertGroup(new GroupMember { UserId = writerId, Role = GroupRole.WRITE });
		await _mongo.Db.UserCollection.InsertOneAsync(new KanbanUser {
			Id = ObjectId.GenerateNewId(), Username = "newbie-forbidden-test", PasswordHash = "x", PasswordSalt = "x",
		});
		_user.UserId = writerId;

		var result = await _controller.AddMember(group.Id.ToString(), "newbie-forbidden-test", (int) GroupRole.WRITE);

		Assert.IsType<ForbidResult>(result);
		var reloaded = await FetchGroup(group.Id);
		Assert.Single(reloaded.Members);
	}

	[Fact]
	public async Task AddMember_UnknownUsername_SetsErrorAndAddsNobody() {
		var adminId = ObjectId.GenerateNewId();
		var group = await InsertGroup(new GroupMember { UserId = adminId, Role = GroupRole.ADMIN });
		_user.UserId = adminId;

		await _controller.AddMember(group.Id.ToString(), "ghost", (int) GroupRole.WRITE);

		Assert.Contains("ghost", (string) _controller.TempData["GroupError"]!);
		var reloaded = await FetchGroup(group.Id);
		Assert.Single(reloaded.Members);
	}

	[Fact]
	public async Task SetRole_CannotChangeOwnersRole_UnlessActorIsOwner() {
		var ownerId = ObjectId.GenerateNewId();
		var adminId = ObjectId.GenerateNewId();
		var group = await InsertGroup(
			new GroupMember { UserId = ownerId, Role = GroupRole.OWNER },
			new GroupMember { UserId = adminId, Role = GroupRole.ADMIN });
		_user.UserId = adminId;

		var result = await _controller.SetRole(group.Id.ToString(), ownerId.ToString(), (int) GroupRole.READ_ONLY);

		Assert.IsType<ForbidResult>(result);
		var reloaded = await FetchGroup(group.Id);
		Assert.Equal(GroupRole.OWNER, reloaded.Members.Single(m => m.UserId == ownerId).Role);
	}

	[Fact]
	public async Task SetRole_CannotDemoteTheLastOwner() {
		var ownerId = ObjectId.GenerateNewId();
		var group = await InsertGroup(new GroupMember { UserId = ownerId, Role = GroupRole.OWNER });
		_user.UserId = ownerId;

		await _controller.SetRole(group.Id.ToString(), ownerId.ToString(), (int) GroupRole.WRITE);

		var reloaded = await FetchGroup(group.Id);
		Assert.Equal(GroupRole.OWNER, reloaded.Members.Single().Role);
	}

	[Fact]
	public async Task SetRole_CanDemoteAnOwner_WhenAnotherOwnerRemains() {
		var owner1Id = ObjectId.GenerateNewId();
		var owner2Id = ObjectId.GenerateNewId();
		var group = await InsertGroup(
			new GroupMember { UserId = owner1Id, Role = GroupRole.OWNER },
			new GroupMember { UserId = owner2Id, Role = GroupRole.OWNER });
		_user.UserId = owner1Id;

		await _controller.SetRole(group.Id.ToString(), owner2Id.ToString(), (int) GroupRole.ADMIN);

		var reloaded = await FetchGroup(group.Id);
		Assert.Equal(GroupRole.ADMIN, reloaded.Members.Single(m => m.UserId == owner2Id).Role);
	}

	[Fact]
	public async Task RemoveMember_CannotRemoveTheLastOwner() {
		var ownerId = ObjectId.GenerateNewId();
		var group = await InsertGroup(new GroupMember { UserId = ownerId, Role = GroupRole.OWNER });
		_user.UserId = ownerId;

		await _controller.RemoveMember(group.Id.ToString(), ownerId.ToString());

		var reloaded = await FetchGroup(group.Id);
		Assert.Single(reloaded.Members);
	}

	[Fact]
	public async Task RemoveMember_ByNonAdmin_IsForbidden() {
		var writerId = ObjectId.GenerateNewId();
		var targetId = ObjectId.GenerateNewId();
		var group = await InsertGroup(
			new GroupMember { UserId = writerId, Role = GroupRole.WRITE },
			new GroupMember { UserId = targetId, Role = GroupRole.WRITE });
		_user.UserId = writerId;

		var result = await _controller.RemoveMember(group.Id.ToString(), targetId.ToString());

		Assert.IsType<ForbidResult>(result);
		var reloaded = await FetchGroup(group.Id);
		Assert.Equal(2, reloaded.Members.Count);
	}

	[Fact]
	public async Task GetGroupMembers_ForNonMember_IsForbidden() {
		var group = await InsertGroup(new GroupMember { UserId = ObjectId.GenerateNewId(), Role = GroupRole.OWNER });
		_user.UserId = ObjectId.GenerateNewId();

		var result = await _controller.GetGroupMembers(group.Id.ToString());

		Assert.IsType<ForbidResult>(result);
	}
}
