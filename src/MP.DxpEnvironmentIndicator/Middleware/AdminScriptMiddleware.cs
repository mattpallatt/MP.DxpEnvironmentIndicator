using Microsoft.AspNetCore.Http;

namespace MP.DxpEnvironmentIndicator.Middleware;

// Injects the admin bootstrap script into every admin page, so the tools-menu item can overlay the
// settings page (the admin SPA has no route of its own for it). The buffer-and-splice logic lives
// in HtmlBodyInjector.
public class AdminScriptMiddleware(RequestDelegate next)
{
    private const string AdminPath = "/EPiServer/EPiServer.Cms.UI.Admin";
    private const string ScriptTag = "<script src=\"/EPiServer/DxpEnvironmentIndicator/ClientResources/Scripts/AdminInit.js\"></script>";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsAdminPageRequest(context))
        {
            await next(context);
            return;
        }

        await HtmlBodyInjector.InjectBeforeBodyCloseAsync(context, next, ScriptTag);
    }

    private static bool IsAdminPageRequest(HttpContext context)
    {
        return HttpMethods.IsGet(context.Request.Method)
            && context.Request.Path.StartsWithSegments(AdminPath, StringComparison.OrdinalIgnoreCase);
    }
}
