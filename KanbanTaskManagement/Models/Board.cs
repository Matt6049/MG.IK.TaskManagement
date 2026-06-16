using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KanbanTaskManagement.Models;





//todo: activity logs

public class Board {
	public ObjectId Id { get; set; }

	[BsonRequired]
	public required string Name { get; set; }

	[BsonRequired]
	public bool IsUserOwned { get; set; }

	[BsonRequired]
	public required string OwnerName { get; set; }

	[BsonRequired]
	public Column[] Columns { get; set; } = [
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
