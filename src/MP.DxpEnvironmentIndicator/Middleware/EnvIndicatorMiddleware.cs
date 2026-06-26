using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MP.DxpEnvironmentIndicator.Controllers;
using MP.DxpEnvironmentIndicator.Services;

namespace MP.DxpEnvironmentIndicator.Middleware;

// Injects the environment-indicator script into Optimizely shell pages so the matched environment
// is badged into the top navigation bar. CMS 12 uses /EPiServer/ as the shell prefix; CMS 13 uses
// /Optimizely/. Both are covered here.
//
// The script is injected INLINE (not as a <script src>) so the badge values are embedded in the
// HTML response. A separate JS resource can be cached by CDN/reverse-proxy infrastructure in front
// of the app server even when the app returns no-store; inline content is part of the HTML response
// which is always served fresh. Settings are read via IEnvironmentSettingsService on every request
// (with a 30 s TTL so all nodes in a multi-instance deployment pick up changes within half a minute).
public class EnvIndicatorMiddleware(RequestDelegate next)
{
    private static readonly PathString ShellPathCms12 = "/EPiServer";
    private static readonly PathString ShellPathCms13 = "/Optimizely";

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.WebSockets.IsWebSocketRequest || !IsShellPageRequest(context))
        {
            await next(context);
            return;
        }

        var resolver = context.RequestServices.GetRequiredService<IEnvironmentResolver>();
        var settingsSvc = context.RequestServices.GetRequiredService<IEnvironmentSettingsService>();

        var env = resolver.Resolve();
        if (env == null)
        {
            await next(context);
            return;
        }

        var selector = settingsSvc.Get().Selector;
        if (string.IsNullOrWhiteSpace(selector)) selector = EnvironmentClientResourceController.DefaultSelector;

        var prelude = $"var __DXP_LABEL={JsonSerializer.Serialize(env.Label)};"
                    + $"var __DXP_COLOR={JsonSerializer.Serialize(env.Color)};"
                    + $"var __DXP_TEXT={JsonSerializer.Serialize(ContrastColor.Text(env.Color))};"
                    + $"var __DXP_SELECTOR={JsonSerializer.Serialize(selector)};\n";

        var scriptTag = $"<script>{prelude}{EnvironmentClientResourceController.EnvIndicatorScript}</script>";
        await HtmlBodyInjector.InjectBeforeBodyCloseAsync(context, next, scriptTag);
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
