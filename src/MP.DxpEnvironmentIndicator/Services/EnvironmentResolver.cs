using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace MP.DxpEnvironmentIndicator.Services;

// Decides which badge (if any) to show for the running environment. Primary signal is the admin
// page: match the request host against the configured environment BaseUrls. If nothing matches
// (e.g. a developer on localhost that hasn't been added to the settings), fall back to
// ASPNETCORE_ENVIRONMENT so local dev still shows a badge. Production is badged unless opted out.
public class EnvironmentResolver : IEnvironmentResolver
{
    private readonly IEnvironmentSettingsService _settings;
    private readonly IWebHostEnvironment _hostEnvironment;

    public EnvironmentResolver(IEnvironmentSettingsService settings, IWebHostEnvironment hostEnvironment)
    {
        _settings = settings;
        _hostEnvironment = hostEnvironment;
    }

    public ResolvedEnvironment Resolve(string requestHost)
    {
        var settings = _settings.Get();
        var match = settings.DetectByHost(requestHost);

        if (match.Name != null)
        {
            bool disabled = match.Name switch
            {
                "Integration" => settings.IntegrationDisabled,
                "Preproduction" => settings.PreproductionDisabled,
                // Legacy ShowOnProduction=false is treated as disabled; new ProductionDisabled takes precedence.
                "Production" => settings.ProductionDisabled || !settings.ShowOnProduction,
                _ => false
            };

            if (disabled) return null;

            var color = string.IsNullOrWhiteSpace(match.Color) ? DefaultColor(match.Name) : match.Color;
            return color == null ? null : new ResolvedEnvironment(match.Name, EffectiveLabel(match.Name, match.Label), color);
        }

        // Unmatched host — only badge it when this is genuinely a local/dev run.
        if (_hostEnvironment.IsDevelopment())
            return new ResolvedEnvironment("Development", EffectiveLabel("Development", null), DefaultColor("Development"));

        return null;
    }

    // The pill text for an environment: the admin's custom label as typed, or the upper-cased
    // environment name when no override is set. Display only — never used for matching.
    public static string EffectiveLabel(string name, string customLabel) =>
        string.IsNullOrWhiteSpace(customLabel) ? name?.ToUpperInvariant() : customLabel.Trim();

    public static string DefaultColor(string name) => name?.ToLowerInvariant() switch
    {
        "integration" => "#d4651a",
        "preproduction" => "#7b2fff",
        "production" => "#c0392b",
        "development" => "#2e7d32",
        _ => null,
    };
}
