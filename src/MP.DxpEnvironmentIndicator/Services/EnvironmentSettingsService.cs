using EPiServer.Data.Dynamic;
using MP.DxpEnvironmentIndicator.Models;

namespace MP.DxpEnvironmentIndicator.Services;

// Loads/saves the indicator settings from the Dynamic Data Store.
//
// A 30-second TTL cache is used because the middleware now reads settings on every shell HTML
// page navigation (the script is injected inline rather than as a separate HTTP resource).
// Without the cache, every editor navigation would hit DDS. The TTL is short enough that settings
// changes propagate across all nodes in a multi-instance DXP cloud deployment within 30 seconds.
// Save() resets the cache immediately so the saving node sees the change right away.
public class EnvironmentSettingsService : IEnvironmentSettingsService
{
    private EnvironmentIndicatorSettings _cached;
    private DateTimeOffset _cachedAt = DateTimeOffset.MinValue;
    private readonly object _lock = new();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    public EnvironmentIndicatorSettings Get()
    {
        lock (_lock)
        {
            if (_cached != null && DateTimeOffset.UtcNow - _cachedAt < CacheTtl)
                return _cached;
        }

        var store = DynamicDataStoreFactory.Instance.GetStore(typeof(EnvironmentIndicatorSettings));
        var settings = store == null
            ? new EnvironmentIndicatorSettings()
            : store.LoadAll<EnvironmentIndicatorSettings>().FirstOrDefault() ?? new EnvironmentIndicatorSettings();

        lock (_lock)
        {
            _cached = settings;
            _cachedAt = DateTimeOffset.UtcNow;
        }

        return settings;
    }

    public void Save(EnvironmentIndicatorSettings settings)
    {
        var store = DynamicDataStoreFactory.Instance.GetStore(typeof(EnvironmentIndicatorSettings))
                    ?? DynamicDataStoreFactory.Instance.CreateStore(typeof(EnvironmentIndicatorSettings));

        var existing = store.LoadAll<EnvironmentIndicatorSettings>().FirstOrDefault();
        if (existing != null)
            settings.Id = existing.Id;

        store.Save(settings);

        lock (_lock)
        {
            _cached = settings;
            _cachedAt = DateTimeOffset.UtcNow;
        }
    }
}
