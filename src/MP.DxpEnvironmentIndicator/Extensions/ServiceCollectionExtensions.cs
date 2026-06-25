using EPiServer.Shell.Modules;
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

        // Register the protected module so Optimizely's shell recognises our URLs and the
        // [MenuProvider] attribute on EnvironmentIndicatorMenuProvider is picked up by the
        // module scanner. Without this registration the menu item does not appear in CMS 12.
        services.Configure<ProtectedModuleOptions>(opts =>
        {
            if (!opts.Items.Any(x => string.Equals(x.Name, "DxpEnvironmentIndicator", StringComparison.OrdinalIgnoreCase)))
                opts.Items.Add(new ModuleDetails { Name = "DxpEnvironmentIndicator" });
        });

        // The Optimizely docs show IMenuProvider implementations using constructor injection,
        // meaning Optimizely instantiates them via DI (not Activator.CreateInstance). The
        // [MenuProvider] attribute on the class is the *discovery* signal; DI registration is
        // the *instantiation* path. Both are required for the menu item to appear.
        services.AddTransient<IMenuProvider, EnvironmentIndicatorMenuProvider>();

        return services;
    }
}
