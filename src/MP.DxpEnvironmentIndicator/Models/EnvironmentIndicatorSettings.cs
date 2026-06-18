using EPiServer.Data;
using EPiServer.Data.Dynamic;

namespace MP.DxpEnvironmentIndicator.Models;

// DDS-backed settings for the indicator. The add-in runs in exactly one environment, so there is a
// single label + colour (and an enable toggle) that always applies to the environment it is deployed
// in — set per slot. No host-matching: each environment's DDS holds its own values.
[EPiServerDataStore(AutomaticallyRemapStore = true)]
public class EnvironmentIndicatorSettings : IDynamicData
{
    public Identity Id { get; set; }

    // The badge is shown when enabled. On by default, so a fresh install shows something immediately.
    public bool Enabled { get; set; } = true;

    // The pill text. Blank falls back to ASPNETCORE_ENVIRONMENT (e.g. "DEVELOPMENT").
    public string Label { get; set; }

    // The pill background colour (hex). Blank falls back to a neutral default.
    public string Color { get; set; }

    // Optional override for the CSS selector the badge is placed next to (advanced).
    public string Selector { get; set; }
}
