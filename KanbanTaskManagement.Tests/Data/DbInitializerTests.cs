using KanbanTaskManagement.Data;
using KanbanTaskManagement.Models;
using KanbanTaskManagement.Tests.Support;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KanbanTaskManagement.Tests.Data;

public class DbInitializerTests : IClassFixture<MongoTestContext> {
	private readonly MongoTestContext _mongo;

	public DbInitializerTests(MongoTestContext mongo) {
		_mongo = mongo;
	}

	[Fact]
	public async Task InitializeAsync_BackfillsOwnerIdFromMatchingUsername() {
		var ownerId = ObjectId.GenerateNewId();
		await _mongo.Db.UserCollection.InsertOneAsync(new KanbanUser {
			Id = ownerId, Username = "legacy-owner", PasswordHash = "x", PasswordSalt = "x",
		});
		var board = new Board { Name = "Legacy board", OwnerName = "legacy-owner", IsUserOwned = true };
		await _mongo.Db.BoardCollection.InsertOneAsync(board);

		await DbInitializer.InitializeAsync(_mongo.Db);

		var reloaded = await _mongo.Db.BoardCollection.Find(b => b.Id == board.Id).FirstAsync();
		Assert.Equal(ownerId, reloaded.OwnerId);
	}

	[Fact]
	public async Task InitializeAsync_LeavesBoardAlone_WhenOwnerNameMatchesNoUser() {
		var board = new Board { Name = "Orphaned board", OwnerName = "nobody-registered", IsUserOwned = true };
		await _mongo.Db.BoardCollection.InsertOneAsync(board);

		await DbInitializer.InitializeAsync(_mongo.Db);

		var reloaded = await _mongo.Db.BoardCollection.Find(b => b.Id == board.Id).FirstAsync();
		Assert.Equal(ObjectId.Empty, reloaded.OwnerId);
	}

	[Fact]
	public async Task InitializeAsync_BackfillsMultipleBoardsSharingTheSameOwnerName() {
		var ownerId = ObjectId.GenerateNewId();
		await _mongo.Db.UserCollection.InsertOneAsync(new KanbanUser {
			Id = ownerId, Username = "shared-owner", PasswordHash = "x", PasswordSalt = "x",
		});
		var boardA = new Board { Name = "Board A", OwnerName = "shared-owner", IsUserOwned = true };
		var boardB = new Board { Name = "Board B", OwnerName = "shared-owner", IsUserOwned = true };
		await _mongo.Db.BoardCollection.InsertManyAsync([boardA, boardB]);

		await DbInitializer.InitializeAsync(_mongo.Db);

		var reloadedA = await _mongo.Db.BoardCollection.Find(b => b.Id == boardA.Id).FirstAsync();
		var reloadedB = await _mongo.Db.BoardCollection.Find(b => b.Id == boardB.Id).FirstAsync();
		Assert.Equal(ownerId, reloadedA.OwnerId);
		Assert.Equal(ownerId, reloadedB.OwnerId);
	}

	[Fact]
	public async Task InitializeAsync_DoesNotTouchBoardsThatAlreadyHaveAnOwnerId() {
		var originalOwnerId = ObjectId.GenerateNewId();
		var board = new Board { Name = "Already owned", OwnerName = "someone-else", OwnerId = originalOwnerId, IsUserOwned = true };
		await _mongo.Db.BoardCollection.InsertOneAsync(board);

		await DbInitializer.InitializeAsync(_mongo.Db);

		var reloaded = await _mongo.Db.BoardCollection.Find(b => b.Id == board.Id).FirstAsync();
		Assert.Equal(originalOwnerId, reloaded.OwnerId);
	}
}
