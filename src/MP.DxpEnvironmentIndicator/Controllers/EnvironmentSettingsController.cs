using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MP.DxpEnvironmentIndicator.Models;
using MP.DxpEnvironmentIndicator.Services;

namespace MP.DxpEnvironmentIndicator.Controllers;

[Authorize(Roles = "CmsAdmins,Administrators,WebAdmins")]
public class EnvironmentSettingsController : Controller
{
    private static readonly bool _isCms13 =
        typeof(EPiServer.Core.ContentReference).Assembly.GetName().Version?.Major >= 13;

    private readonly IEnvironmentSettingsService _settingsService;

    public EnvironmentSettingsController(IEnvironmentSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    [Route("~/EPiServer/DxpEnvironmentIndicator/settings")]
    [Route("~/EPiServer/DxpEnvironmentIndicator/Admin/Settings")]
    [Route("~/Optimizely/DxpEnvironmentIndicator/settings")]
    [Route("~/Optimizely/DxpEnvironmentIndicator/Admin/Settings")]
    public IActionResult Index()
    {
        Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
        var settings = _settingsService.Get();
        // CMS 13: shell-aware view that includes the navigation bundle so the sidebar remains visible.
        // CMS 12: standalone HTML displayed inside the AdminInit.js iframe overlay.
        var view = _isCms13
            ? "~/Views/EnvironmentSettings/Index13.cshtml"
            : "~/Views/EnvironmentSettings/Index.cshtml";
        return View(view, MapToViewModel(settings));
    }

    [HttpPost]
    [Route("~/EPiServer/DxpEnvironmentIndicator/settings")]
    [Route("~/EPiServer/DxpEnvironmentIndicator/Admin/Settings")]
    [Route("~/Optimizely/DxpEnvironmentIndicator/settings")]
    [Route("~/Optimizely/DxpEnvironmentIndicator/Admin/Settings")]
    [ValidateAntiForgeryToken]
    public IActionResult Save(EnvironmentSettingsViewModel model)
    {
        try
        {
            _settingsService.Save(new EnvironmentIndicatorSettings
            {
                Enabled = model.Enabled,
                Label = Clean(model.Label),
                Color = model.Color?.Trim(),
                Selector = Clean(model.Selector)
            });
            model.Saved = true;
        }
        catch (Exception ex)
        {
            model.ErrorMessage = $"Failed to save settings: {ex.Message}";
        }

        var view = _isCms13
            ? "~/Views/EnvironmentSettings/Index13.cshtml"
            : "~/Views/EnvironmentSettings/Index.cshtml";
        return View(view, model);
    }

    private static string Clean(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static EnvironmentSettingsViewModel MapToViewModel(EnvironmentIndicatorSettings s) => new()
    {
        Enabled = s.Enabled,
        Label = s.Label,
        Color = string.IsNullOrWhiteSpace(s.Color) ? EnvironmentResolver.DefaultColor : s.Color,
        Selector = s.Selector
    };
}
