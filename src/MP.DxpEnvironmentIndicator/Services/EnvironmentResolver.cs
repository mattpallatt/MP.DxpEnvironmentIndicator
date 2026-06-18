using Microsoft.AspNetCore.Hosting;

namespace MP.DxpEnvironmentIndicator.Services;

// Decides the badge for the running environment. The add-in runs in one environment, so this is just
// the single configured label + colour, shown when enabled. A blank label falls back to
// ASPNETCORE_ENVIRONMENT so local dev still shows something; a blank colour falls back to neutral grey.
public class EnvironmentResolver : IEnvironmentResolver
{
    // Neutral default pill colour when none is configured.
    public const string DefaultColor = "#6b7280";

    private readonly IEnvironmentSettingsService _settings;
    private readonly IWebHostEnvironment _hostEnvironment;

    public EnvironmentResolver(IEnvironmentSettingsService settings, IWebHostEnvironment hostEnvironment)
    {
        _settings = settings;
        _hostEnvironment = hostEnvironment;
    }

    public ResolvedEnvironment Resolve()
    {
        var s = _settings.Get();
        if (!s.Enabled) return null;

        var label = string.IsNullOrWhiteSpace(s.Label)
            ? (_hostEnvironment.EnvironmentName ?? "Environment").ToUpperInvariant()
            : s.Label.Trim();
        var color = string.IsNullOrWhiteSpace(s.Color) ? DefaultColor : s.Color.Trim();
        return new ResolvedEnvironment(label, color);
    }
}
