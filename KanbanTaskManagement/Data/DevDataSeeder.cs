using KanbanTaskManagement.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KanbanTaskManagement.Data;

public static class DevDataSeeder {
	public static async Task SeedAsync(MongoDBContext db, IConfiguration config) {
		var section = config.GetSection("Seed");
		if (!section.GetValue("Enabled", false))
			return;

		var reset = section.GetValue("Reset", false);
		var alreadySeeded = await db.UserCollection.Find(u => u.Username == "izak").AnyAsync();

		if (alreadySeeded && !reset)
			return;

		if (reset) {
			await db.BoardCollection.DeleteManyAsync(FilterDefinition<Board>.Empty);
			await db.GroupCollection.DeleteManyAsync(FilterDefinition<Group>.Empty);
			await db.UserCollection.DeleteManyAsync(FilterDefinition<KanbanUser>.Empty);
		}

		var izak = NewUser("izak", "izak123");
		var test = NewUser("test", "test123");
		var kasia = NewUser("kasia", "kasia123");
		await db.UserCollection.InsertManyAsync(new[] { izak, test, kasia });

		var projekt = new Group {
			Name = "Zespół projektowy",
			Members = [
				new GroupMember { UserId = izak.Id, Role = GroupRole.OWNER },
				new GroupMember { UserId = test.Id, Role = GroupRole.WRITE },
				new GroupMember { UserId = kasia.Id, Role = GroupRole.READ_ONLY },
			],
		};
		var marketing = new Group {
			Name = "Marketing",
			Members = [
				new GroupMember { UserId = kasia.Id, Role = GroupRole.OWNER },
				new GroupMember { UserId = test.Id, Role = GroupRole.ADMIN },
			],
		};
		await db.GroupCollection.InsertManyAsync(new[] { projekt, marketing });

		var boards = new[] {
			BuildBoard("Tablica izaka", izak, ObjectId.Empty, [],
				["Zaplanować sprint", "Zebrać wymagania od promotora"],
				["Napisać dokumentację"],
				["Skonfigurować repozytorium"]),

			BuildBoard("Wspólna tablica", izak, projekt.Id, [],
				["Zaprojektować API kalendarza", "Makieta widoku tablicy"],
				["Model uprawnień do tablic"],
				["Logowanie i rejestracja"]),

			BuildBoard("Tablica z gościem", izak, ObjectId.Empty,
				[(test.Id, GroupRole.WRITE), (kasia.Id, GroupRole.READ_ONLY)],
				["Zadanie do konsultacji z zespołem"],
				[],
				[]),

			BuildBoard("Tablica Marketingu", kasia, marketing.Id, [],
				["Kampania jesienna"],
				["Grafiki na social media"],
				[]),
		};
		await db.BoardCollection.InsertManyAsync(boards);
	}

	private static KanbanUser NewUser(string username, string password) {
		var (hash, salt) = Auth.CreateCredential(password);
		return new KanbanUser {
			Username = username,
			Email = null,
			PasswordHash = hash,
			PasswordSalt = salt,
			CreatedAt = DateTime.UtcNow,
			LastActive = DateTime.UtcNow,
		};
	}

	private static Board BuildBoard(string name, KanbanUser owner, ObjectId groupId,
		(ObjectId UserId, GroupRole Role)[] members,
		string[] backlog, string[] inProgress, string[] done) {

		var board = new Board {
			Name = name,
			OwnerId = owner.Id,
			OwnerName = owner.Username,
			IsUserOwned = true,
			GroupId = groupId,
			Members = members.Select(m => new BoardMember { UserId = m.UserId, Role = m.Role }).ToList(),
		};

		AddTasks(board, ColumnType.BACKLOG, owner.Username, backlog);
		AddTasks(board, ColumnType.IN_PROGRESS, owner.Username, inProgress);
		AddTasks(board, ColumnType.DONE, owner.Username, done);
		return board;
	}

	private static void AddTasks(Board board, ColumnType type, string creator, string[] names) {
		var column = board.Columns.First(c => c.Type == type);
		foreach (var name in names)
			column.Tasks.Add(new KanbanTask { Name = name, CreatorName = creator });
	}
}
