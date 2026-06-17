using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace MP.DxpEnvironmentIndicator.Middleware;

// Registers both injecting middlewares into the request pipeline automatically, so a consuming host
// only needs to call AddDxpEnvironmentIndicator() — no manual app.UseMiddleware<>() wiring required.
// Both middlewares are idempotent, so this is harmless even if a host also registers them explicitly.
public class EnvIndicatorStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
        app =>
        {
            // Ensure this library's controllers are discoverable. We do this here (in the startup
            // filter) rather than in ConfigureServices because by this point the service provider
            // is built and we can get the real ApplicationPartManager instance that MapControllers()
            // will read from. Calling services.AddMvc() in ConfigureServices to achieve the same
            // thing breaks CMS 13's admin routing by unexpectedly enabling Razor Pages.
            var partManager = app.ApplicationServices.GetService<ApplicationPartManager>();
            if (partManager != null)
            {
                var assembly = typeof(EnvIndicatorStartupFilter).Assembly;
                if (!partManager.ApplicationParts.OfType<AssemblyPart>().Any(p => p.Assembly == assembly))
                    partManager.ApplicationParts.Add(new AssemblyPart(assembly));
            }

            app.UseMiddleware<AdminScriptMiddleware>();
            app.UseMiddleware<EnvIndicatorMiddleware>();
            next(app);
            // Register attribute-routed controllers (EnvIndicator.js, AdminInit.js, Admin/Settings)
            // so the host does not need to call MapControllers() or MapControllerRoute().
            // All UseEndpoints calls share the same ControllerActionEndpointDataSource, so calling
            // MapControllers() here is safe even when the host already calls it — no duplicate routes.
            // UseRouting (added by the host via next(app)) queries this shared data source at request
            // time, so placement after next(app) is fine.
            app.UseEndpoints(static endpoints => endpoints.MapControllers());
        };
}
