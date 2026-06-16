using EPiServer.Shell.Modules;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
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

        return services;
    }
}
