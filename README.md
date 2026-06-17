# MP.DxpEnvironmentIndicator

An Optimizely CMS 12 add-in that badges the current DXP environment into the shell's top
navigation bar, so editors always know whether they're on **Integration**, **Preproduction**, or
**Production**. Integration shows orange, Preproduction purple, and Production red. All three are
badged by default; you can turn the Production badge off so a missing badge becomes the "you're on
prod" signal.

It is fully standalone — it has **no dependency** on any other add-in.

![CMS [INTEGRATION] badge in the top bar](#)

## How it knows the environment

There's no per-environment config file or build variable to maintain. The running site identifies
itself by **matching the browser's request host** against the Base URLs you enter on a small admin
page (stored in the Dynamic Data Store). The same settings can be saved on every environment and
each one badges itself correctly.

Host-matching is deliberate: a stored "this is Integration" flag could be silently overwritten when
DXP copies database content from one environment to another, whereas the live request host can't be.
Only the host part of each URL is compared — port and scheme are ignored — so `https://localhost:5000`
matches a `https://localhost:5000` Base URL during local development.

If the request host matches nothing, the indicator falls back to `ASPNETCORE_ENVIRONMENT`: a
`Development` host shows a green **DEVELOPMENT** badge; anything else stays silent.

## Install

1. Add the package:
   ```bash
   dotnet add package MP.DxpEnvironmentIndicator
   ```
2. Register it in `Startup.ConfigureServices` (or `Program.cs`):
   ```csharp
   services.AddDxpEnvironmentIndicator();
   ```
   No `Configure()` changes are needed — the script injection is wired via an `IStartupFilter`.
3. Build and run. In the CMS, go to **Admin → Tools → DXP Environment Indicator** and enter each
   environment's Base URL (and optionally adjust the colours). Save.

## Configuration

| Field | Notes |
|-------|-------|
| Integration / Preproduction / Production **Base URL** | The host the running site is reached on for that environment. Used for host-matching. |
| **Environment label** | The pill text for that environment. Blank uses the upper-cased environment name; otherwise shown as typed. Keep it short. |
| Badge **colour** | The pill background colour for that environment. Text colour (dark/white) is chosen automatically for contrast. |
| **Show a badge on Production** | On by default. Untick to leave production unbadged. |
| **Advanced → Top-bar selector** | Optional CSS-selector override for where the badge is placed, in case a CMS update moves the label. Blank uses the built-in default. |

## How it works (internals)

- `EnvironmentClientResourceController` serves two scripts from controller routes (so there's no
  static-asset folder to deploy): `EnvIndicator.js` (the badge) and `AdminInit.js` (the settings
  overlay).
- `EnvIndicatorMiddleware` injects the badge script into shell HTML pages; `AdminScriptMiddleware`
  injects the settings overlay into admin pages. Both are registered automatically via
  `EnvIndicatorStartupFilter` and share `HtmlBodyInjector`.
- `EnvironmentResolver` resolves the badge server-side (host-match → `ASPNETCORE_ENVIRONMENT`
  fallback) and the colour/name are baked into `EnvIndicator.js` per request.
- The badge script finds the product label in the top bar and appends a coloured pill, retrying via
  a `MutationObserver` until the (async) shell renders.

## Build

The EPiServer packages live on the Optimizely feed, not nuget.org:

```bash
dotnet restore --source https://api.nuget.org/v3/index.json --source https://nuget.optimizely.com/feed/packages.svc/
dotnet build --no-restore
dotnet pack --no-build -c Release
```

Targets `net8.0` and Optimizely CMS 12 (`[12.0.0, 13.0.0)`).
