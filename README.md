# MP.DxpEnvironmentIndicator

An Optimizely CMS 12/13 add-in that badges the current DXP environment into the CMS shell top bar so editors always know where they are.

![Badge in the top bar](https://img.shields.io/badge/CMS-12%20%7C%2013-blue)

## How it works

The indicator matches the browser's request host against Base URLs you configure in the settings page. The same settings can be saved on every environment — each one identifies itself by host-matching, so nothing breaks when DXP copies database content between slots.

## Install

```bash
dotnet add package MP.DxpEnvironmentIndicator --source https://nuget.optimizely.com/feed/packages.svc/
```

Register in `Startup.cs` / `Program.cs`:

```csharp
using MP.DxpEnvironmentIndicator.Extensions;

services.AddDxpEnvironmentIndicator();
```

No `Configure()` changes needed — middleware is wired automatically via `IStartupFilter`.

## Host project setup

After installing the package, NuGet deploys `modules/_protected/DxpEnvironmentIndicator/` to your host project. This directory must exist for the Optimizely module finder to register the add-in with the shell.

## Settings

Navigate to the settings page:

- **CMS 12**: Admin → Tools → **Enviro-helper**
- **CMS 13**: Add-ons → **Enviro-helper**

| Field | Notes |
|-------|-------|
| **Base URLs** | One fully-qualified URL per line. The indicator shows when the request host matches any of them. |
| **Environment label** | The pill text for that environment. Blank uses the upper-cased environment name. |
| **Badge colour** | The pill background. Text colour (dark/white) is chosen automatically for contrast. |
| **Disable indicator** | Check to suppress the badge on a specific environment. |
| **Advanced → Top-bar selector** | CSS selector override if a CMS update moves the label. |

## Environments

| Environment | Default colour |
|-------------|---------------|
| Integration | Orange `#d4651a` |
| Preproduction | Purple `#7b2fff` |
| Production | Red `#c0392b` |
| Development (localhost fallback) | Green `#2e7d32` |

## Build from source

```bash
dotnet restore --source https://api.nuget.org/v3/index.json \
               --source https://nuget.optimizely.com/feed/packages.svc/
dotnet build
dotnet pack -c Release --no-build
```

Targets `net8.0`; compatible with CMS 12 (net8.0 host) and CMS 13 (net10.0 host).
