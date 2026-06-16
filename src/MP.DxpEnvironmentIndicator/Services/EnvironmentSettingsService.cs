using EPiServer.Data.Dynamic;
using MP.DxpEnvironmentIndicator.Models;

namespace MP.DxpEnvironmentIndicator.Services;

// Loads/saves the indicator settings from the Dynamic Data Store, caching after the first load.
// DynamicDataStoreFactory uses IDatabaseExecutor, which has thread affinity — caching keeps reads
// cheap and off the DB after warm-up.
public class EnvironmentSettingsService : IEnvironmentSettingsService
{
    private EnvironmentIndicatorSettings _cached;
    private readonly object _lock = new();

    public EnvironmentIndicatorSettings Get()
    {
        lock (_lock)
        {
            if (_cached != null) return _cached;
        }

        var store = DynamicDataStoreFactory.Instance.GetStore(typeof(EnvironmentIndicatorSettings));
        var settings = store == null
            ? new EnvironmentIndicatorSettings()
            : store.LoadAll<EnvironmentIndicatorSettings>().FirstOrDefault() ?? new EnvironmentIndicatorSettings();

        lock (_lock)
        {
            _cached = settings;
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
        }
    }
}
