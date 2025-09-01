using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace ldap_tels.Extensions;

public static class ControllerExtensions
{
    public static async Task<string> RenderViewAsync(this Controller controller, string viewName, object model, bool partial = false)
    {
        controller.ViewData.Model = model;

        var serviceProvider = controller.HttpContext.RequestServices;
        var viewEngine = (IRazorViewEngine)serviceProvider.GetService(typeof(IRazorViewEngine))!;
        var tempDataProvider = (ITempDataProvider)serviceProvider.GetService(typeof(ITempDataProvider))!;

        await using var writer = new StringWriter();
        var actionContext = new ActionContext(controller.HttpContext, controller.RouteData, controller.ControllerContext.ActionDescriptor, new ModelStateDictionary());

        ViewEngineResult viewResult;
        if (partial)
        {
            viewResult = viewEngine.GetView(null, viewName, false);
            if (!viewResult.Success)
            {
                // Попытка найти относительно папки контроллера
                var partialViewName = $"Views/{controller.ControllerContext.ActionDescriptor.ControllerName}/{viewName}.cshtml";
                viewResult = viewEngine.GetView(null, partialViewName, false);
            }
        }
        else
        {
            viewResult = viewEngine.FindView(actionContext, viewName, false);
        }

        if (!viewResult.Success)
        {
            throw new FileNotFoundException($"Не удалось найти представление '{viewName}'");
        }

        var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            Model = model
        };

        var tempData = new TempDataDictionary(controller.HttpContext, tempDataProvider);
        var viewContext = new ViewContext(actionContext, viewResult.View, viewDictionary, tempData, writer, new HtmlHelperOptions());

        await viewResult.View.RenderAsync(viewContext);
        return writer.ToString();
    }
}


