using Microsoft.AspNetCore.Http;

namespace MP.DxpEnvironmentIndicator.Middleware;

// Injects the environment-indicator script into the Optimizely shell pages so the matched
// environment is badged into the top navigation bar. CMS 12 uses /EPiServer/ as the shell prefix;
// CMS 13 moved the editor to /Optimizely/. Both are covered here.
// Gated on Accept: text/html so the shell's many XHR/JSON and static-resource requests aren't
// buffered needlessly. EnvIndicator.js itself decides whether to render anything.
public class EnvIndicatorMiddleware(RequestDelegate next)
{
    private static readonly PathString ShellPathCms12 = "/EPiServer";
    private static readonly PathString ShellPathCms13 = "/Optimizely";
    private const string ScriptTag = "<script src=\"/EPiServer/DxpEnvironmentIndicator/ClientResources/Scripts/EnvIndicator.js\"></script>";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsShellPageRequest(context))
        {
            await next(context);
            return;
        }

        await HtmlBodyInjector.InjectBeforeBodyCloseAsync(context, next, ScriptTag);
    }

    private static bool IsShellPageRequest(HttpContext context)
    {
        var path = context.Request.Path;
        return HttpMethods.IsGet(context.Request.Method)
            && (path.StartsWithSegments(ShellPathCms12, StringComparison.OrdinalIgnoreCase)
                || path.StartsWithSegments(ShellPathCms13, StringComparison.OrdinalIgnoreCase))
            && context.Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }
}
