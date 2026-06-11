# Building

## Prerequisites

- .NET SDK 8.0+
- ADOFAI with [UnityModManager](https://www.nexusmods.com/site/mods/21) installed

## Setup

Clone and edit `BetterHitErrorMeter.csproj` — replace the `GameExePath` with your ADOFAI path:

```xml
<GameExePath>path\to\A Dance of Fire and Ice\A Dance of Fire and Ice.exe</GameExePath>
```

`BetterHitErrorMeter.csproj` is in `.gitignore` — your local path won't be committed.

## Build

```bash
dotnet build -c Release
```

Output goes to `out/`. Auto-deploys to `<GameDir>/Mods/BetterHitErrorMeter/`.

## Options

| Property | Default | Description |
|----------|---------|-------------|
| `GameExePath` | *(required)* | Path to the ADOFAI executable |
| `AutoLaunchGame` | `true` | Set to `false` to disable auto-launch after build |
