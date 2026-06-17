using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MP.DxpEnvironmentIndicator.Models;
using MP.DxpEnvironmentIndicator.Services;

namespace MP.DxpEnvironmentIndicator.Controllers;

[Authorize(Roles = "CmsAdmins,Administrators,WebAdmins")]
public class EnvironmentSettingsController : Controller
{
    private readonly IEnvironmentSettingsService _settingsService;

    public EnvironmentSettingsController(IEnvironmentSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    [Route("~/EPiServer/DxpEnvironmentIndicator/Admin/Settings")]
    public IActionResult Index()
    {
        Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
        var settings = _settingsService.Get();
        return View("~/Views/EnvironmentSettings/Index.cshtml", MapToViewModel(settings));
    }

    [HttpPost]
    [Route("~/EPiServer/DxpEnvironmentIndicator/Admin/Settings")]
    [ValidateAntiForgeryToken]
    public IActionResult Save(EnvironmentSettingsViewModel model)
    {
        try
        {
            _settingsService.Save(new EnvironmentIndicatorSettings
            {
                IntegrationBaseUrl = model.IntegrationBaseUrl?.Trim(),
                IntegrationColor = model.IntegrationColor?.Trim(),
                IntegrationLabel = Clean(model.IntegrationLabel),
                PreproductionBaseUrl = model.PreproductionBaseUrl?.Trim(),
                PreproductionColor = model.PreproductionColor?.Trim(),
                PreproductionLabel = Clean(model.PreproductionLabel),
                ProductionBaseUrl = model.ProductionBaseUrl?.Trim(),
                ProductionColor = model.ProductionColor?.Trim(),
                ProductionLabel = Clean(model.ProductionLabel),
                ShowOnProduction = model.ShowOnProduction,
                Selector = Clean(model.Selector)
            });
            model.Saved = true;
        }
        catch (Exception ex)
        {
            model.ErrorMessage = $"Failed to save settings: {ex.Message}";
        }

        return View("~/Views/EnvironmentSettings/Index.cshtml", model);
    }

    private static string Clean(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static EnvironmentSettingsViewModel MapToViewModel(EnvironmentIndicatorSettings s) => new()
    {
        IntegrationBaseUrl = s.IntegrationBaseUrl,
        IntegrationColor = string.IsNullOrWhiteSpace(s.IntegrationColor) ? EnvironmentResolver.DefaultColor("Integration") : s.IntegrationColor,
        IntegrationLabel = s.IntegrationLabel,
        PreproductionBaseUrl = s.PreproductionBaseUrl,
        PreproductionColor = string.IsNullOrWhiteSpace(s.PreproductionColor) ? EnvironmentResolver.DefaultColor("Preproduction") : s.PreproductionColor,
        PreproductionLabel = s.PreproductionLabel,
        ProductionBaseUrl = s.ProductionBaseUrl,
        ProductionColor = string.IsNullOrWhiteSpace(s.ProductionColor) ? EnvironmentResolver.DefaultColor("Production") : s.ProductionColor,
        ProductionLabel = s.ProductionLabel,
        ShowOnProduction = s.ShowOnProduction,
        Selector = s.Selector
    };
}
