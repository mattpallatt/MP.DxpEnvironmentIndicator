using Microsoft.AspNetCore.Http;

namespace MP.DxpEnvironmentIndicator.Middleware;

// Injects the admin bootstrap script into CMS 12 admin pages so the settings link appears in the
// Admin → Tools section of the admin SPA. Not used for CMS 13 — the Add-ons menu item registered
// via IMenuProvider provides navigation to the settings page instead.
public class AdminScriptMiddleware(RequestDelegate next)
{
    private static readonly bool _isCms13 =
        typeof(EPiServer.Core.ContentReference).Assembly.GetName().Version?.Major >= 13;

    private const string AdminPath = "/EPiServer/EPiServer.Cms.UI.Admin";
    private const string ScriptTag = "<script src=\"/EPiServer/DxpEnvironmentIndicator/ClientResources/Scripts/AdminInit.js\"></script>";

    public async Task InvokeAsync(HttpContext context)
    {
        // CMS 13 uses the Add-ons menu item; no script injection needed there.
        // WebSocket upgrades must always pass through untouched.
        if (_isCms13 || context.WebSockets.IsWebSocketRequest || !IsAdminPageRequest(context))
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
