using EPiServer.Data.Dynamic;
using MP.DxpEnvironmentIndicator.Models;

namespace MP.DxpEnvironmentIndicator.Services;

// Loads/saves the indicator settings from the Dynamic Data Store. Not cached: the settings script is
// only fetched on full shell/admin page loads (not a hot path), and reading fresh each time means a
// colour or selector change takes effect on the very next load with no process-wide stale state.
// (Unlike the content-transfer add-in, nothing here runs on a background thread, so there is no
// thread-affinity reason to cache.)
public class EnvironmentSettingsService : IEnvironmentSettingsService
{
    public EnvironmentIndicatorSettings Get()
    {
        var store = DynamicDataStoreFactory.Instance.GetStore(typeof(EnvironmentIndicatorSettings));
        return store == null
            ? new EnvironmentIndicatorSettings()
            : store.LoadAll<EnvironmentIndicatorSettings>().FirstOrDefault() ?? new EnvironmentIndicatorSettings();
    }

    public void Save(EnvironmentIndicatorSettings settings)
    {
        var store = DynamicDataStoreFactory.Instance.GetStore(typeof(EnvironmentIndicatorSettings))
                    ?? DynamicDataStoreFactory.Instance.CreateStore(typeof(EnvironmentIndicatorSettings));

        var existing = store.LoadAll<EnvironmentIndicatorSettings>().FirstOrDefault();
        if (existing != null)
            settings.Id = existing.Id;

        store.Save(settings);
    }
}
