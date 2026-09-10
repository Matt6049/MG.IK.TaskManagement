using KanbanTaskManagement.Models;
using KanbanTaskManagement.Services;
using MongoDB.Bson;

namespace KanbanTaskManagement.ViewModels;

public class BoardDetailsViewModel {
	public required Board Board { get; init; }
	public required BoardAccess Access { get; init; }
	public List<BoardMemberView> Members { get; set; } = [];
	public List<GroupOption> Groups { get; set; } = [];
	public string? LinkedGroupName { get; set; }
}

public class BoardMemberView {
	public ObjectId UserId { get; set; }
	public required string Username { get; set; }
	public GroupRole Role { get; set; }
	public bool IsOwner { get; set; }
}

public class GroupOption {
	public ObjectId Id { get; set; }
	public required string Name { get; set; }
}
