using KanbanTaskManagement.Data;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace KanbanTaskManagement.Tests.Support;

public class MongoTestContext : IAsyncLifetime {
	private readonly IMongoClient _client;
	private readonly string _databaseName;

	public MongoDBContext Db { get; }

	public MongoTestContext() {
		_databaseName = $"KanbanTest_{Guid.NewGuid():N}";
		_client = new MongoClient("mongodb://127.0.0.1:27017");

		var options = Options.Create(new DbSettings {
			ConnectionString = "mongodb://127.0.0.1:27017",
			DatabaseName = _databaseName,
			TableCollectionName = "Tables",
			BoardCollectionName = "Boards",
			UserCollectionName = "Users",
			GroupCollectionName = "Groups",
		});
		Db = new MongoDBContext(options);
	}

	public Task InitializeAsync() => Task.CompletedTask;

	public Task DisposeAsync() => _client.DropDatabaseAsync(_databaseName);
}
