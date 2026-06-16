namespace KanbanTaskManagement.Data {
	public class DbSettings {
		public string ConnectionString { get; set; } = null!;
		public string DatabaseName { get; set; } = null!;
		public string TableCollectionName { get; set; } = null!;
		public string BoardCollectionName { get; set; } = null!;
		public string UserCollectionName { get; set; } = null!;
		public string GroupCollectionName { get; set; } = null!;
	}
}
