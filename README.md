# BetterHitErrorMeter

A Dance of Fire and Ice (ADOFAI) mod that replaces the built-in hit error meter with a clean, high-contrast design rendered at runtime.

## Features

- **Clean vector-accurate textures** — generated at runtime from CAD-measured geometry
- **Both shapes supported** — straight bar and curved gauge, matching the original layout
- **All 4 sizes** — Small, Normal, Large, ExtraLarge
- **Zero performance cost** — texture generated once per size/shape change, normal sprite rendering thereafter
- **Toggle on/off** — original meter textures restored when mod is disabled

## Installation

1. Install [UnityModManager](https://www.nexusmods.com/site/mods/21) for ADOFAI
2. Download the latest release from [Releases](../../releases)
3. Extract to `Mods/BetterHitErrorMeter/` in your ADOFAI directory
4. Enable the mod in UMM (Ctrl+F10 in-game)

## Building from Source

```bash
# Set your game path in BetterHitErrorMeter.csproj:
#   <GameExePath>path\to\A Dance of Fire and Ice.exe</GameExePath>

dotnet build -c Release
# Output: out/BetterHitErrorMeter.dll
```

Copy `out/` contents to `<GameDir>/Mods/BetterHitErrorMeter/`.

## How It Works

Instead of shipping pre-rendered PNG textures, the mod generates them at runtime using `Texture2D.SetPixels()`:

- **Straight meter**: filled rectangles layered in draw order
- **Curved meter**: per-pixel polar coordinate check (`atan2` + ring radius test)

Texture resolution scales with the selected meter size, ensuring crisp edges at all zoom levels. No per-frame cost — just a standard Unity sprite render.

## License

GPL-3.0-or-later
