using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace KanbanTaskManagement.Tests.Support;

public static class ControllerFactory {
	public static void WireUpTempData(Controller controller) {
		var httpContext = new DefaultHttpContext();
		controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
		controller.TempData = new TempDataDictionary(httpContext, new NullTempDataProvider());
	}
}
