# MP.DxpEnvironmentIndicator

An Optimizely CMS 12 add-in that badges the current DXP environment into the shell's top
navigation bar, so editors always know whether they're on **Integration**, **Preproduction**, or
**Production**. 

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

## Build

The EPiServer packages live on the Optimizely feed, not nuget.org:

```bash
dotnet restore --source https://api.nuget.org/v3/index.json --source https://nuget.optimizely.com/feed/packages.svc/
dotnet build --no-restore
dotnet pack --no-build -c Release
```

Targets `net8.0` and Optimizely CMS 12 (`[12.0.0, 13.0.0)`).
