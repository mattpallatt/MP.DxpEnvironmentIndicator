namespace MP.DxpEnvironmentIndicator.Models;

public class EnvironmentSettingsViewModel
{
    public string IntegrationBaseUrl { get; set; }
    public string IntegrationColor { get; set; }

    public string PreproductionBaseUrl { get; set; }
    public string PreproductionColor { get; set; }

    public string ProductionBaseUrl { get; set; }
    public string ProductionColor { get; set; }

    public bool ShowOnProduction { get; set; }

    public bool Saved { get; set; }
    public string ErrorMessage { get; set; }
}
