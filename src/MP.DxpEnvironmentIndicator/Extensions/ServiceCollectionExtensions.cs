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
    private static readonly bool _isCms13 =
        typeof(EPiServer.Core.ContentReference).Assembly.GetName().Version?.Major >= 13;

    public static IServiceCollection AddDxpEnvironmentIndicator(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddSingleton<IEnvironmentSettingsService, EnvironmentSettingsService>();
        services.AddScoped<IEnvironmentResolver, EnvironmentResolver>();

        services.AddTransient<IStartupFilter, EnvIndicatorStartupFilter>();

        // CMS 12 only: register the protected module so the shell picks up the
        // [MenuProvider] attribute via its module scanner. In CMS 13 the module
        // registration is not needed for menu discovery (IMenuProvider DI registration
        // below is sufficient) and it creates an unwanted Add-ons sidebar entry.
        if (!_isCms13)
        {
            services.Configure<ProtectedModuleOptions>(opts =>
            {
                if (!opts.Items.Any(x => string.Equals(x.Name, "DxpEnvironmentIndicator", StringComparison.OrdinalIgnoreCase)))
                    opts.Items.Add(new ModuleDetails { Name = "DxpEnvironmentIndicator" });
            });
        }

        // CMS 12: [MenuProvider] is the discovery signal; DI is the instantiation path — both required.
        // CMS 13: DI registration alone is sufficient; attribute scanning is not used.
        services.AddTransient<IMenuProvider, EnvironmentIndicatorMenuProvider>();

        return services;
    }
}
