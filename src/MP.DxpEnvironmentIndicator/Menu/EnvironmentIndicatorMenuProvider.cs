using EPiServer.Shell.Navigation;

namespace MP.DxpEnvironmentIndicator.Menu;

// [MenuProvider] attribute is required for CMS 12, which discovers IMenuProvider implementations
// via assembly attribute-scanning rather than DI. CMS 13 dropped attribute scanning and resolves
// IMenuProvider from DI instead — so DI registration is conditionally added for CMS 13 only in
// ServiceCollectionExtensions, avoiding duplicate entries on either version.
[MenuProvider]
public class EnvironmentIndicatorMenuProvider : IMenuProvider
{
    private static readonly bool _isCms13 =
        typeof(EPiServer.Core.ContentReference).Assembly.GetName().Version?.Major >= 13;

    public IEnumerable<MenuItem> GetMenuItems()
    {
        if (_isCms13)
        {
            // Docs: "Add a second-level menu to the existing admin menu" uses /global/cms/admin/...
            // In CMS 13 the admin menu is labelled "Settings" in the UI. Use the same path
            // hierarchy as CMS 12 so the item lands in the same section on both versions.
            // Point directly at the Razor controller — no intermediate redirect.
            return new[]
            {
                new UrlMenuItem(
                    "Environment Label",
                    MenuPaths.Global + "/cms/admin/tools/dxp.envindicator",
                    "/EPiServer/DxpEnvironmentIndicator/settings")
                {
                    IsAvailable = _ => true,
                    SortIndex = SortIndex.Last + 1
                }
            };
        }

        // CMS 12: point at the admin SPA with a hash route so the shell stays visible.
        // AdminInit.js (injected into every admin page) watches for this hash and overlays
        // the settings iframe over the content pane — no full-page navigation occurs.
        return new[]
        {
            new UrlMenuItem(
                "Environment Label",
                "/global/cms/admin/tools/dxp.envindicator",
                "/EPiServer/EPiServer.Cms.UI.Admin/default#/EnvIndicator/Settings")
            {
                IsAvailable = _ => true,
                SortIndex = SortIndex.Last + 1
            }
        };
    }
}
