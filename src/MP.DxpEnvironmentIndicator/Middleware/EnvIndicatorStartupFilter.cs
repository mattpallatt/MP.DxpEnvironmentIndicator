using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace MP.DxpEnvironmentIndicator.Middleware;

// Registers both injecting middlewares into the request pipeline automatically, so a consuming host
// only needs to call AddDxpEnvironmentIndicator() — no manual app.UseMiddleware<>() wiring required.
// Both middlewares are idempotent, so this is harmless even if a host also registers them explicitly.
public class EnvIndicatorStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
        app =>
        {
            app.UseMiddleware<AdminScriptMiddleware>();
            app.UseMiddleware<EnvIndicatorMiddleware>();
            next(app);
        };
}
