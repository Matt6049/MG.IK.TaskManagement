using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace KanbanTaskManagement.Tests.Support;

public class NullTempDataProvider : ITempDataProvider {
	public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();

	public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
}
