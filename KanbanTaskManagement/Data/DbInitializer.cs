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
			new CreateIndexModel<Board>(Builders<Board>.IndexKeys.Ascending("Columns.Tasks.DueDate")),
		});
	}

	private static async Task BackfillBoardOwnerIdsAsync(MongoDBContext db) {
		var missingOwnerId = Builders<Board>.Filter.Or(
			Builders<Board>.Filter.Exists(b => b.OwnerId, false),
			Builders<Board>.Filter.Eq(b => b.OwnerId, ObjectId.Empty));
		var boards = await db.BoardCollection.Find(missingOwnerId).ToListAsync();
		if (boards.Count == 0)
			return;

		var ownerNames = boards
			.Where(b => !string.IsNullOrEmpty(b.OwnerName))
			.Select(b => b.OwnerName)
			.Distinct()
			.ToList();
		var owners = await db.UserCollection
			.Find(Builders<KanbanUser>.Filter.In(u => u.Username, ownerNames))
			.ToListAsync();
		var ownerIdByName = owners.ToDictionary(u => u.Username, u => u.Id);

		var writes = new List<WriteModel<Board>>();
		foreach (var board in boards) {
			if (!ownerIdByName.TryGetValue(board.OwnerName, out var ownerId))
				continue;

			writes.Add(new UpdateOneModel<Board>(
				Builders<Board>.Filter.Eq(b => b.Id, board.Id),
				Builders<Board>.Update.Set(b => b.OwnerId, ownerId)));
		}
		if (writes.Count > 0)
			await db.BoardCollection.BulkWriteAsync(writes);
	}
}
