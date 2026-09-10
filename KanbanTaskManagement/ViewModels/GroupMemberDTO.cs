using KanbanTaskManagement.Models;
using MongoDB.Bson;

namespace KanbanTaskManagement.ViewModels;

public class GroupMemberDTO {
	public ObjectId Id { get; set; }
	public required string Username { get; set; }
	public required GroupRole Role { get; set; }
	public DateTime LastActive { get; set; }
}