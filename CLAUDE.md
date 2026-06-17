# MP.DxpEnvironmentIndicator — Claude Code context

## What this is

A self-contained Razor class library (net8.0) that badges the active DXP environment into the Optimizely CMS shell top bar. Works on CMS 12 (net8.0 host) and CMS 13 (net10.0 host) from one build.

## Repository layout

```
src/MP.DxpEnvironmentIndicator/
├── Controllers/
│   ├── EnvironmentClientResourceController.cs  — serves EnvIndicator.js, AdminInit.js,
│   │                                             settings.html entry point, ClientResources/init.js
│   └── EnvironmentSettingsController.cs         — settings GET/POST; returns Index.cshtml (CMS 12)
│                                                  or Index13.cshtml (CMS 13)
├── Extensions/ServiceCollectionExtensions.cs   — AddDxpEnvironmentIndicator()
├── Menu/EnvironmentIndicatorMenuProvider.cs     — IMenuProvider; DI-only (no [MenuProvider] attr)
├── Middleware/
│   ├── AdminScriptMiddleware.cs                — injects AdminInit.js into CMS 12 admin pages only
│   ├── EnvIndicatorMiddleware.cs               — injects EnvIndicator.js into all shell pages
│   ├── EnvIndicatorStartupFilter.cs            — auto-wires middleware via IStartupFilter
│   └── HtmlBodyInjector.cs                     — buffers response and splices <script> before </body>
├── Models/
│   ├── EnvironmentIndicatorSettings.cs         — DDS-backed model; DetectByHost() splits URLs by \n
│   └── EnvironmentSettingsViewModel.cs
├── Services/
│   ├── EnvironmentResolver.cs                  — matches request host; checks per-env disabled flags
│   ├── EnvironmentSettingsService.cs           — DDS load/save
│   └── IEnvironmentResolver.cs / IEnvironmentSettingsService.cs
├── Views/EnvironmentSettings/
│   ├── Index.cshtml                            — standalone HTML (CMS 12 iframe overlay)
│   └── Index13.cshtml                          — shell-aware page with navigation.bundle.js (CMS 13)
└── modules/_protected/DxpEnvironmentIndicator/ — packaged as NuGet content → consumer's modules/
    ├── module.config                            — tags=EPiServerModulePackage, clientModule → CMS dep
    └── ClientResources/init.js                 — no-op AMD module; registers module in shell JS registry
```

## CMS version detection

Checked once at startup via:
```csharp
typeof(EPiServer.Core.ContentReference).Assembly.GetName().Version?.Major >= 13
```

Used in: `EnvironmentIndicatorMenuProvider`, `AdminScriptMiddleware`, `EnvironmentSettingsController`.

## CMS 12 settings navigation

- Menu item: Admin → Tools → Enviro-helper  
- URL: `/EPiServer/EPiServer.Cms.UI.Admin/default#/EnvIndicator/Settings` (hash route)
- `AdminInit.js` (injected into every admin page) watches `location.hash` and overlays a `position:fixed` iframe over the content pane pointing at `/EPiServer/DxpEnvironmentIndicator/settings`
- Settings controller returns `Index.cshtml` (standalone HTML, `Layout = null`)

## CMS 13 settings navigation

- Menu item: Add-ons → Enviro-helper (`MenuPaths.Global + "/cms/dxpenvindicator"`)
- URL: `/Optimizely/DxpEnvironmentIndicator/settings.html` (served by controller)
- Shell does full-page navigation; `settings.html` redirects to `/EPiServer/DxpEnvironmentIndicator/settings`
- Settings controller returns `Index13.cshtml`, which manually renders the Optimizely navigation bundle:
  - Shell version read from `typeof(EPiServer.Shell.Navigation.IMenuProvider).Assembly.GetName().Version`
  - Renders `epi-navigation-root` div with data attributes + `navigation.bundle.js` + `navigation.bundle.css`
  - Our settings form sits inside `epi-pn-navigation--fixed-adjust`

## Module registration (CMS 13)

- **No** `ProtectedModuleOptions` manual registration — doing so causes the module finder to look in
  `~/Optimizely/{Name}` (the virtual path) instead of the physical `~/modules/_protected/{Name}` path.
- Auto-discovery from `modules/_protected/DxpEnvironmentIndicator/module.config` handles registration.
- `<clientModule>` with `<moduleDependencies>CMS</moduleDependencies>` puts the module in the shell's
  JS registry, which causes the shell to treat `/Optimizely/DxpEnvironmentIndicator/*` URLs as
  module content (full-page with shell nav) rather than external URLs.

## Host app requirements

Each consuming host needs `modules/_protected/DxpEnvironmentIndicator/` present (NuGet deploys it automatically). For project references during development, create the directory manually.

Known host apps:
- CMS 13: `C:\Users\MattPallatt\source\repos\CMS13`
- CMS 12: `C:\Users\MattPallatt\source\repos\OptimizelyD`

## Key decisions

- `[MenuProvider]` attribute removed from `EnvironmentIndicatorMenuProvider` — CMS 12 performs both attribute scanning and DI resolution, causing duplicate menu entries. DI-only registration gives exactly one entry on both versions.
- `HtmlBodyInjector` skips body injection for HTTP 304/204 responses (no body allowed).
- Multi-URL support: `BaseUrl` fields store newline-separated URLs; `DetectByHost()` splits on `\n`.
- Per-environment disabled flags (`IntegrationDisabled` etc.) added to DDS model; legacy `ShowOnProduction` kept for backward compat.
- Badge is NOT clickable — `cursor:pointer`, `title`, and `onclick` removed from the pill element.
