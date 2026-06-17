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
    // Call from the host's ConfigureServices. Registers everything the indicator needs and wires the
    // script-injecting middleware automatically (via IStartupFilter), so no Configure() changes are
    // required in the host.
    public static IServiceCollection AddDxpEnvironmentIndicator(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddSingleton<IEnvironmentSettingsService, EnvironmentSettingsService>();
        services.AddScoped<IEnvironmentResolver, EnvironmentResolver>();

        services.AddTransient<IStartupFilter, EnvIndicatorStartupFilter>();

        // Register the protected module so /EPiServer/DxpEnvironmentIndicator/* routes resolve.
        services.Configure<ProtectedModuleOptions>(opts =>
        {
            if (!opts.Items.Any(x => string.Equals(x.Name, "DxpEnvironmentIndicator", StringComparison.OrdinalIgnoreCase)))
            {
                opts.Items.Add(new ModuleDetails { Name = "DxpEnvironmentIndicator" });
            }
        });

        // Register as IMenuProvider via DI in addition to [MenuProvider] attribute scanning.
        // CMS 13 may discover providers from the DI container rather than (or in addition to)
        // assembly scanning, so this ensures the shell navigation item is registered regardless.
        services.AddTransient<IMenuProvider, EnvironmentIndicatorMenuProvider>();

        return services;
    }
}
