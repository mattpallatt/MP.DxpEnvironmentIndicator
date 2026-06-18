using EPiServer.Shell.Navigation;

namespace MP.DxpEnvironmentIndicator.Menu;

// Registered via DI only (services.AddTransient<IMenuProvider, ...> in ServiceCollectionExtensions).
// The [MenuProvider] attribute is intentionally absent: on CMS 12, EPiServer both attribute-scans
// and resolves IMenuProvider from DI, so combining the attribute with an explicit DI registration
// produces duplicate menu entries.  DI-only gives exactly one entry on both CMS versions.
public class EnvironmentIndicatorMenuProvider : IMenuProvider
{
    private static readonly bool _isCms13 =
        typeof(EPiServer.Core.ContentReference).Assembly.GetName().Version?.Major >= 13;

    public IEnumerable<MenuItem> GetMenuItems()
    {
        if (_isCms13)
        {
            // Short /{module}/settings URL so the CMS 13 shell SPA router matches the
            // /{prefix}/{module}/{resource} pattern and iframe-loads it inside the shell
            // content area rather than doing a full-page navigation.
            // Point at the physical settings.html in the module directory.
            // The shell's module router can serve and iframe-load physical module files;
            // settings.html then redirects the iframe to our Razor controller.
            return new[]
            {
                new UrlMenuItem(
                    "Environment Labels",
                    MenuPaths.Global + "/cms/dxpenvindicator",
                    "/Optimizely/DxpEnvironmentIndicator/settings.html")
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
                "Environment Labels",
                "/global/cms/admin/tools/dxp.envindicator",
                "/EPiServer/EPiServer.Cms.UI.Admin/default#/EnvIndicator/Settings")
            {
                IsAvailable = _ => true,
                SortIndex = SortIndex.Last + 1
            }
        };
    }
}
