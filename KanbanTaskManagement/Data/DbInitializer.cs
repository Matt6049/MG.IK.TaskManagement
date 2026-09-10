using KanbanTaskManagement.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KanbanTaskManagement.Data;

public static class DbInitializer {
	public static async Task InitializeAsync(MongoDBContext db) {
		await EnsureIndexesAsync(db);
		await BackfillBoardOwnerIdsAsync(db);
	}

	private static async Task EnsureIndexesAsync(MongoDBContext db) {
		await db.BoardCollection.Indexes.CreateManyAsync(new[] {
			new CreateIndexModel<Board>(Builders<Board>.IndexKeys.Ascending(b => b.OwnerId)),
			new CreateIndexModel<Board>(Builders<Board>.IndexKeys.Ascending(b => b.GroupId)),
		});
	}

	private static async Task BackfillBoardOwnerIdsAsync(MongoDBContext db) {
		var missingOwnerId = Builders<Board>.Filter.Or(
			Builders<Board>.Filter.Exists(b => b.OwnerId, false),
			Builders<Board>.Filter.Eq(b => b.OwnerId, ObjectId.Empty));
		var boards = await db.BoardCollection.Find(missingOwnerId).ToListAsync();
		if (boards.Count == 0)
			return;

		foreach (var board in boards) {
			if (string.IsNullOrEmpty(board.OwnerName))
				continue;

			var owner = await db.UserCollection
				.Find(u => u.Username == board.OwnerName)
				.FirstOrDefaultAsync();
			if (owner is null)
				continue;

			await db.BoardCollection.UpdateOneAsync(
				b => b.Id == board.Id,
				Builders<Board>.Update.Set(b => b.OwnerId, owner.Id));
		}
	}
}
