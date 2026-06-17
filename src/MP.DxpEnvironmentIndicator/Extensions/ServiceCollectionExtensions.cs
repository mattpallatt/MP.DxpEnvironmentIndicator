using EPiServer.Shell.Navigation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MP.DxpEnvironmentIndicator.Menu;
using MP.DxpEnvironmentIndicator.Middleware;
using MP.DxpEnvironmentIndicator.Services;

namespace MP.DxpEnvironmentIndicator.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDxpEnvironmentIndicator(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddSingleton<IEnvironmentSettingsService, EnvironmentSettingsService>();
        services.AddScoped<IEnvironmentResolver, EnvironmentResolver>();

        services.AddTransient<IStartupFilter, EnvIndicatorStartupFilter>();

        // The module is discovered automatically by the Optimizely module finder scanning
        // ~/modules/_protected/DxpEnvironmentIndicator/module.config in the host app.
        // No manual ProtectedModuleOptions registration is needed — and adding one causes the
        // module finder to look in ~/Optimizely/{Name} (the virtual path) instead of the correct
        // physical ~/modules/_protected/{Name} path, producing the "couldn't find directory" error.

        // Register via DI for all CMS versions.  The [MenuProvider] attribute has been removed from
        // the provider class because CMS 12 both attribute-scans and resolves IMenuProvider from DI,
        // causing duplicate menu entries when both mechanisms are active.
        services.AddTransient<IMenuProvider, EnvironmentIndicatorMenuProvider>();

        return services;
    }
}
