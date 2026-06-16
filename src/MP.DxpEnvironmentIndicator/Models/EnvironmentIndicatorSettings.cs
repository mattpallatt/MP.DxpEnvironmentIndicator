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

    public string IntegrationBaseUrl { get; set; }
    public string IntegrationColor { get; set; }

    public string PreproductionBaseUrl { get; set; }
    public string PreproductionColor { get; set; }

    public string ProductionBaseUrl { get; set; }
    public string ProductionColor { get; set; }

    // Production is silent by default — a missing badge is the "you're on prod" signal. Opt in here.
    public bool ShowOnProduction { get; set; }

    // Returns the environment whose configured BaseUrl host matches the request host, or default
    // ((null, null, null)) when nothing matches. Host-only comparison, so port/scheme don't matter.
    public (string Name, string BaseUrl, string Color) DetectByHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host)) return default;
        foreach (var env in All())
        {
            if (string.IsNullOrWhiteSpace(env.BaseUrl)) continue;
            if (Uri.TryCreate(env.BaseUrl, UriKind.Absolute, out var uri) &&
                string.Equals(uri.Host, host, StringComparison.OrdinalIgnoreCase))
                return env;
        }
        return default;
    }

    public IEnumerable<(string Name, string BaseUrl, string Color)> All() => new[]
    {
        ("Integration", IntegrationBaseUrl, IntegrationColor),
        ("Preproduction", PreproductionBaseUrl, PreproductionColor),
        ("Production", ProductionBaseUrl, ProductionColor),
    };
}
