namespace MP.DxpEnvironmentIndicator.Models;

public class EnvironmentSettingsViewModel
{
    public bool Enabled { get; set; } = true;
    public string Label { get; set; }
    public string Color { get; set; }
    public string Selector { get; set; }

    public bool Saved { get; set; }
    public string ErrorMessage { get; set; }
}
