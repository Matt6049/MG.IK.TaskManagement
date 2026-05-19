using KanbanTaskManagement.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace KanbanTaskManagement.Data; 

public class MongoDBContext(IOptions<DbSettings> options) {

	public IMongoCollection<Table> TableCollection { get; private set; } = new MongoClient(options.Value.ConnectionString)
		.GetDatabase(options.Value.DatabaseName)
		.GetCollection<Table>(options.Value.TablesCollectionName);
}
