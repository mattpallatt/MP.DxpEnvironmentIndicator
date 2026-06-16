using EPiServer.Shell.Navigation;

namespace MP.DxpEnvironmentIndicator.Menu;

// Adds a "DXP Environment Indicator" entry under admin → Tools. It points at a hash route the admin
// SPA doesn't recognise; AdminInit.js watches for that hash and overlays the settings page in an
// iframe (the same overlay technique the content-transfer add-in uses).
[MenuProvider]
public class EnvironmentIndicatorMenuProvider : IMenuProvider
{
    public IEnumerable<MenuItem> GetMenuItems()
    {
        return new[]
        {
            new UrlMenuItem(
                "DXP Environment Indicator",
                "/global/cms/admin/tools/dxp.envindicator",
                "/EPiServer/EPiServer.Cms.UI.Admin/default#/EnvIndicator/Settings")
            {
                IsAvailable = _ => true,
                SortIndex = SortIndex.Last + 1
            }
        };
    }
}
