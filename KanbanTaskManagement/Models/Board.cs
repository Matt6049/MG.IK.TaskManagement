using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KanbanTaskManagement.Models;





//todo: activity logs

public class Board : IMongoDocument {
	[BsonId]
	public ObjectId Id { get; set; }

	[BsonRequired]
	public required string Name { get; set; }

	[BsonRequired]
	public bool IsUserOwned { get; set; }


    public ObjectId GroupId { get; set; }

    
	public required string OwnerName { get; set; }

	[BsonRequired]
	public List<Column> Columns { get; set; } = [
			new Column(){
				Type=ColumnType.BACKLOG
			},
			new Column(){
				Type=ColumnType.IN_PROGRESS
			},
			new Column(){
				Type=ColumnType.DONE
			}
		];
}
