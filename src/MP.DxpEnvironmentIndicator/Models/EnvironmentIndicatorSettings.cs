using EPiServer.Data;
using EPiServer.Data.Dynamic;

namespace MP.DxpEnvironmentIndicator.Models;

// DDS-backed settings for the indicator: the host of each DXP environment plus the badge colour to
// show on it. The running environment is identified by matching the live request host against these
// — the same approach the content-transfer add-in uses, and the one that survives DXP copying DDS
// data between slots (a stored "I am X" flag could be overwritten by a content sync; a host match
// can't). Flat strings because DDS stores those most reliably.
[EPiServerDataStore(AutomaticallyRemapStore = true)]
public class EnvironmentIndicatorSettings : IDynamicData
{
    public Identity Id { get; set; }

    // BaseUrl fields store one URL per line (newline-separated) to support multiple hostnames
    // per environment (e.g. localhost + staging alias). DetectByHost splits on newlines.
    public string IntegrationBaseUrl { get; set; }
    public string IntegrationColor { get; set; }
    public string IntegrationLabel { get; set; }
    public bool IntegrationDisabled { get; set; } = false;

    public string PreproductionBaseUrl { get; set; }
    public string PreproductionColor { get; set; }
    public string PreproductionLabel { get; set; }
    public bool PreproductionDisabled { get; set; } = false;

    public string ProductionBaseUrl { get; set; }
    public string ProductionColor { get; set; }
    public string ProductionLabel { get; set; }
    public bool ProductionDisabled { get; set; } = false;

    // Legacy opt-out flag kept for DDS backward compatibility. New UI uses ProductionDisabled.
    // The resolver treats production as disabled when either this is false OR ProductionDisabled is true.
    public bool ShowOnProduction { get; set; } = true;

    // Optional override for the CSS selector the badge is placed next to.
    public string Selector { get; set; }

    // Returns the environment whose configured BaseUrl host matches the request host, or default
    // when nothing matches. Each BaseUrl may contain multiple fully-qualified URLs, one per line.
    public (string Name, string BaseUrl, string Color, string Label) DetectByHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host)) return default;
        foreach (var env in All())
        {
            if (string.IsNullOrWhiteSpace(env.BaseUrl)) continue;
            var urls = env.BaseUrl.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var url in urls)
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                    string.Equals(uri.Host, host, StringComparison.OrdinalIgnoreCase))
                    return env;
            }
        }
        return default;
    }

    public IEnumerable<(string Name, string BaseUrl, string Color, string Label)> All() => new[]
    {
        ("Integration", IntegrationBaseUrl, IntegrationColor, IntegrationLabel),
        ("Preproduction", PreproductionBaseUrl, PreproductionColor, PreproductionLabel),
        ("Production", ProductionBaseUrl, ProductionColor, ProductionLabel),
    };
}
