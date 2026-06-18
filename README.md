# MP.DxpEnvironmentIndicator

An Optimizely CMS 12/13 add-in that badges the current DXP environment into the CMS shell top bar so editors always know where they are.

![Badge in the top bar](https://img.shields.io/badge/CMS-12%20%7C%2013-blue)

## How it works

The add-in shows a configurable UI element in the header bar, so that CMS users know which environment they are working in.

## Install

```bash
dotnet add package MP.DxpEnvironmentIndicator --source https://nuget.optimizely.com/feed/packages.svc/
```

Register in `Startup.cs` / `Program.cs`:

```csharp
using MP.DxpEnvironmentIndicator.Extensions;

services.AddDxpEnvironmentIndicator();
```

## Settings

Navigate to the settings page:

- **CMS 12**: Admin → Tools → **Environment Label**
- **CMS 13**: Add-ons → **Environment Label**

| Field | Notes |
|-------|-------|
| **Show the environment badge** | Master on/off toggle. On by default. |
| **Label** | The pill text. Blank falls back to the server environment name (`ASPNETCORE_ENVIRONMENT`). |
| **Colour** | The pill background. Text colour (dark/white) is chosen automatically for contrast. A blank colour uses a neutral grey. |
| **Advanced → Top-bar selector** | CSS selector override if a CMS update moves the label. |
