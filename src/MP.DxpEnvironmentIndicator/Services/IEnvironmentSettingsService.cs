using MP.DxpEnvironmentIndicator.Models;

namespace MP.DxpEnvironmentIndicator.Services;

public interface IEnvironmentSettingsService
{
    EnvironmentIndicatorSettings Get();
    void Save(EnvironmentIndicatorSettings settings);
}
