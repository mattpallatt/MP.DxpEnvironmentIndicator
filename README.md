# MP.DxpEnvironmentIndicator

An Optimizely CMS 12/13 add-in that badges the current DXP environment into the CMS shell top bar so editors always know where they are.

![Badge in the top bar](https://img.shields.io/badge/CMS-12%20%7C%2013-blue)

## How it works

The add-in runs in exactly one environment, so it has a single **label** and **colour** that always apply to the environment it's deployed in. Set them per slot on the settings page — an enable toggle (on by default) turns the badge on or off. Leave the label blank to fall back to `ASPNETCORE_ENVIRONMENT`.

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

- **CMS 12**: Admin → Tools → **Environment Labels**
- **CMS 13**: Add-ons → **Environment Labels**

| Field | Notes |
|-------|-------|
| **Show the environment badge** | Master on/off toggle. On by default. |
| **Label** | The pill text. Blank falls back to the server environment name (`ASPNETCORE_ENVIRONMENT`). |
| **Colour** | The pill background. Text colour (dark/white) is chosen automatically for contrast. A blank colour uses a neutral grey. |
| **Advanced → Top-bar selector** | CSS selector override if a CMS update moves the label. |

## Build from source

```bash
dotnet restore --source https://api.nuget.org/v3/index.json \
               --source https://nuget.optimizely.com/feed/packages.svc/
dotnet build
dotnet pack -c Release --no-build
```

Targets `net8.0`; compatible with CMS 12 (net8.0 host) and CMS 13 (net10.0 host).
