using Microsoft.AspNetCore.Http;

namespace MP.DxpEnvironmentIndicator.Middleware;

// Injects the environment-indicator script into the Optimizely shell pages (anything served under
// /EPiServer as an HTML document) so the matched environment is badged into the top navigation bar.
// Gated on Accept: text/html so the shell's many XHR/JSON and static-resource requests under
// /EPiServer aren't buffered needlessly. EnvIndicator.js itself decides whether to render anything.
public class EnvIndicatorMiddleware(RequestDelegate next)
{
    private const string ShellPath = "/EPiServer";
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
        return HttpMethods.IsGet(context.Request.Method)
            && context.Request.Path.StartsWithSegments(ShellPath, StringComparison.OrdinalIgnoreCase)
            && context.Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }
}
