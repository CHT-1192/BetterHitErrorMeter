# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

BetterHitErrorMeter — an ADOFAI (A Dance of Fire and Ice) UMM mod that improves upon the game's built-in `scrHitErrorMeter` with precise ms-level timing, statistical analysis, and a configurable overlay.

## Game Installation & Decompilation Tools

- **Game path**: `C:\Users\Xuemin Chen\Projects\ADOFAI\A Dance of Fire and Ice\`
- **Managed DLLs**: `<GamePath>\A Dance of Fire and Ice_Data\Managed\` — contains `Assembly-CSharp.dll` (main game code), `Assembly-CSharp-firstpass.dll`, `RDTools.dll`, etc.
- **dnSpy**: `C:\Users\Xuemin Chen\Projects\ADOFAI\dnSpy\dnSpy.Console.exe` — CLI decompiler for .NET assemblies
  ```bash
  # Decompile a specific type from the game DLL:
  cd "C:\Users\Xuemin Chen\Projects\ADOFAI\dnSpy"
  ./dnSpy.Console.exe -t "ClassName" --no-color "C:\Users\Xuemin Chen\Projects\ADOFAI\A Dance of Fire and Ice\A Dance of Fire and Ice_Data\Managed\Assembly-CSharp.dll"
  ```
- **AssetRipper**: `C:\Users\Xuemin Chen\Projects\ADOFAI\AssetRipper\` — for extracting Unity assets/prefabs

## Reference Repos

- **ADOFAI Mod Template**: `~/Projects/ADOFAIModTemplate` — UMM mod template (.NET 4.8.1, Harmony 2.3.3)
- **Overlayer**: `~/Projects/Overlayer` — feature-rich ADOFAI mod; reference for Harmony patches, overlay rendering, and exposing game data via `[Tag]` attributes

## Build & Run

```bash
dotnet build -c Debug    # or Release
```

The `.csproj` uses `ADOFAIMod.targets` which copies output + `Info.json` + `Resources/` to `out/`. `GameExePath` must be set in `.csproj` to point at the game exe for auto-deploy and auto-launch.

## Architecture

ADOFAI mods are C# class libraries targeting .NET Framework 4.8.1, loaded by UnityModManager (UMM).

### Entry Point & Lifecycle

- `Main.Load(UnityModManager.ModEntry)` — entry point
- `modEntry.OnToggle(bool)` — enable/disable; apply/unapply Harmony patches here
- `modEntry.OnGUI` — settings UI
- `modEntry.OnUpdate(float delta)` — every frame

## Reverse-Engineered Game Internals

### Hit Detection Flow

The full chain from keypress to hit margin:

1. **`scrController.UpdateInput()`** dequeues keyboard events from `AsyncInputManager.keyQueue`
2. When a key is pressed → `ProcessKeyInputs()` → `Simulated_PlayerControl_Update(targetTick)` (the main per-frame update)
3. `Simulated_PlayerControl_Update` calls several methods in order:
   - `CheckPostHoldFail()` — fails if planet has passed too far past exit angle (`angle - targetExitAngle > minAngleMargin * 2`)
   - `OttoHoldHit()` — auto-hit if `RDC.auto` or next tile is auto
   - `HitAutoFloors()` — on valid input trigger, calls `keyTimes.Add(Time.timeAsDouble)` to register keypress timing
   - `UpdateHoldBehavior()` — handles hold-until-near-exit mechanic
   - `HitHoldFloorsIfStartedAtHold()` — handles hitting hold floors from checkpoint
4. On actual hit → **`scrController.Hit(bool isAuto)`** (line 1318):
   - Saves `chosenPlanet.cachedAngle = chosenPlanet.angle`
   - Calls `HitInputEvent()` to validate input
   - Computes raw diff: `float num = chosenPlanet.cachedAngle - chosenPlanet.targetExitAngle`
   - If CCW, negates: `num *= -1f` — so positive = planet ahead of exit = early hit
   - Calls `errorMeter.AddHit(num, currFloor.nextfloor.marginScale)` (unless midspin infinite margin)
   - Calls `planet.SwitchChosen()` which:
     - Calls `scrMisc.GetHitMargin(cachedAngle, targetExitAngle, isCW, bpm*speed, song.pitch, marginScale)`
     - If valid hit: calls `mistakesManager.AddHit(hitMargin)` and moves planet to next tile
5. **`scrMistakesManager.AddHit(HitMargin)`** pushes to `hitMargins` list, increments `hitMarginsCount[(int)hit]`, recalculates percentAcc/percentXAcc

### `scrHitErrorMeter` (the built-in error meter)

Located in `Assembly-CSharp.dll`, namespace global. Key method:

```csharp
public void AddHit(float angleDiff, float marginScale = 1f)
```

**How it works:**
1. Converts angle to degrees: `angleDiff *= -57.29578f` (note: negated — positive = early, negative = late)
2. Gets the counted margin boundary via `scrMisc.GetAdjustedAngleBoundaryInDeg(HitMarginGeneral.Counted, bpm*speed, song.pitch, marginScale)`
3. Normalizes to a ±60 scale: `angleDiff *= (60.0 / adjustedAngleBoundaryInDeg)` — clamps to ±60 (randomized beyond)
4. Applies exponential smoothing: `averageAngle = Mathf.Lerp(averageAngle, angleDiff, sensitivity)` (sensitivity default 0.2)
5. Animates a hand indicator and decaying tick marks

**UI Structure (from decompiled code):**
- `straightMeter` / `curvedMeter` — static background GameObjects with pre-drawn scale textures (toggled by shape)
- `handImage` — needle Image, animated via DOTween (straight: `DOAnchorPos` at `-averageAngle * 2.5f`, curved: `DORotateQuaternion`)
- `tickPrefab` — individual hit marker Images, instantiated at startup (cached pool of `tickCacheSize=60`)
  - Curved: rotation = `Quaternion.Euler(0, 0, angle)`, color fades to alpha 0 over `tickLife=3s`
  - Straight: position = `(-angle * 2.5f, -62f)`, color fades to alpha 0 over `tickLife=3s`
- **Pixel mapping**: `2.5` pixels per degree on the normalized ±60 scale, so the full range is ±150 pixels
- `meterScale` multiplier: Small=0.75, Normal=1.0, Large=1.5, ExtraLarge=2.0

**Limitations of the built-in meter:**
- Shows only a smoothed visual gauge, no numerical readout
- The normalized ±60 scale obscures actual ms timing
- No statistical analysis (mean, stddev, histogram)
- No per-hit history beyond visual ticks
- Not accessible to other mods

### Key Classes (from Assembly-CSharp.dll)

#### `scrMistakesManager` — Hit accuracy tracking
```csharp
public static List<HitMargin> hitMargins;        // ordered list of every hit
public static int[] hitMarginsCount;              // count per HitMargin type
public static Difficulty hardestDifficulty;
public static int lastHitMarginsSize;
public float percentAcc;                          // accuracy %
public float percentXAcc;                         // X-accuracy % (weighted)
public float percentComplete;                     // level completion %

public void AddHit(HitMargin hit);               // called on each hit
public int GetHits(HitMargin hit);               // count for a specific margin
public int GetDeaths();                          // FailMiss + FailOverload
public float GetTotalHits();                     // hitMargins.Count
public void CalculatePercentAcc();               // recalculates percentAcc/percentXAcc
public bool IsAllPurePerfect();                  // all hits are Perfect or Auto
public void Reset();                             // clears all hit data
public void MarkCheckpoint(int checkpointTileOffset);
public void RevertToLastCheckpoint();
```

**percentAcc formula** (from decompiled):
```
num = GetHits(Perfect) + GetHits(EarlyPerfect) + GetHits(LatePerfect) + GetHits(Auto)
total = hitMargins.Count + GetHits(FailMiss) + GetHits(FailOverload)
percentAcc = (num == total ? 1.0 : num / total) + (GetHits(Perfect) + GetHits(Auto)) * 0.0001
```

**percentXAcc formula** (weighted accuracy):
```
weighted = 1.0*Perfect + 1.0*Auto + 0.75*EarlyPerfect + 0.75*LatePerfect
         + 0.4*VeryEarly + 0.4*VeryLate + 0.2*TooEarly + 0.2*TooLate
percentXAcc = weighted / hitMargins.Count * (0.9875 ^ checkpointsUsed)
```

#### `scrMisc` — Hit timing utilities (static)
```csharp
// Convert time (seconds) to angle (radians): time * pitch * PI / (60/bpm)
public static double TimeToAngleInRad(double timeinAbsoluteSpace, double bpmTimesSpeed,
    double conductorPitch, bool shrinkMarginsForHigherPitch = false);

// Convert angle (radians) to time (seconds): angle / PI * (60/bpm)
public static double AngleToTime(double angle, double bpm);

// 60.0 / bpm = seconds per beat
public static double bpm2crotchet(double bpm);

// Get angle boundaries for each margin type (returns degrees)
public static double GetAdjustedAngleBoundaryInDeg(HitMarginGeneral marginType,
    double bpmTimesSpeed, double conductorPitch, double marginMult = 1.0);

// Classify a hit by angle difference (hitangle, refangle in radians)
public static HitMargin GetHitMargin(float hitangle, float refangle, bool isCW,
    float bpmTimesSpeed, float conductorPitch, double marginScale = 1.0);

public static double mod(double x, double m);
public static double GetAngleMoved(double entryAngle, double exitAngle, bool isCW);
public static bool IsValidHit(HitMargin margin);  // checks against GCS.hitMarginLimit
public static bool isDiffInMargin(double x, double y, double margin);
```

**`GetAdjustedAngleBoundaryInDeg` internal logic:**
- Base time thresholds per difficulty: Lenient=0.091s, Normal=0.065s, Strict=0.04s
- Perfect: Mobile=0.07s, Desktop=0.03s divided by `GCS.currentSpeedTrial`
- Pure: Mobile=0.05s, Desktop=0.02s divided by `GCS.currentSpeedTrial`
- All clamped to min 0.025s, then `TimeToAngleInRad(threshold, bpmTimesSpeed, conductorPitch, false) * 57.29578`
- Final = `Max(GCS.HITMARGIN_COUNTED * marginMult, computedFromThreshold)`

**`GetHitMargin` internal logic:**
```
angleDiff_deg = (hitangle - refangle) * (isCW ? 1 : -1) * 57.29578f
if (angleDiff_deg <= -countedDeg) → TooEarly
else if (angleDiff_deg <= -perfectDeg) → VeryEarly
else if (angleDiff_deg <= -pureDeg) → EarlyPerfect
else if (angleDiff_deg <= pureDeg) → Perfect
else if (angleDiff_deg <= perfectDeg) → LatePerfect
else if (angleDiff_deg <= countedDeg) → VeryLate
else → TooLate
```

#### `scrController` — Main game controller
```csharp
public static scrController instance;
public scrHitErrorMeter errorMeter;       // the built-in error meter
public scrMistakesManager mistakesManager;
public scrPlanet chosenPlanet;            // current active planet
public scrFloor currFloor;                // current floor/tile
public bool isCW;                         // rotation direction (CW = clockwise)
public bool gameworld;                    // true during actual gameplay
public bool midspinInfiniteMargin;        // true during midspin
public bool noFail;                       // no-fail mode
public bool noFailInfiniteMargin;         // no-fail infinite margin
public double speed;                      // current speed multiplier (1.0 = normal)
public int currentSeqID;                  // current tile index
public bool benchmarkMode;
public bool responsive;                   // can accept input
public List<AnyKeyCode> holdKeys;         // currently held keys
public int consecMultipressCounter;
public scrFailBar failbar;
public static int checkpointsUsed;

// Key methods:
public bool Hit(bool isAuto = false);                              // main hit method
public bool HitInputEvent(bool isAuto, InputEventState state);     // validates input
public bool OnDamage(bool multipress, bool applyMultipressDamage,
    bool skipDamage, HitMargin hitMargin);                         // damage on miss
public void FailAction(bool overload, bool multipress,
    string failMessage, bool hitbox);                              // death
public bool ValidInputWasTriggered();                               // any valid key pressed
public int CountValidKeysPressed();                                 // how many keys pressed
public void Simulated_PlayerControl_Update(ulong? targetTick);     // per-frame logic
public void Scrub(int floorNum, bool forceDontStartMusic);         // checkpoint restart

// Private properties used in hit detection:
private double _minAngleMargin  // GetAdjustedAngleBoundaryInDeg(Counted, bpm*speed, pitch, 1.0) in RADIANS
private float _marginScale      // currFloor.nextfloor.marginScale or 1f
private double _holdMargin      // 1.0 - _minAngleMargin * _marginScale / currFloor.angleLength
private bool _nextTileIsAuto    // currFloor.nextfloor?.auto
```

**Key hit detection code** (from `Hit()` method, decompiled line 1318-1400):
```csharp
public bool Hit(bool isAuto = false) {
    scrMisc.Vibrate(50L);
    if (!this.responsive) return false;
    if (ADOBase.isLevelEditor && ADOBase.controller.paused) return false;

    bool flag = this.chosenPlanet.currfloor.nextfloor?.auto ?? false;
    this.chosenPlanet.cachedAngle = this.chosenPlanet.angle;  // SAVE current angle

    if (!this.HitInputEvent(isAuto, InputEventState.Down)) return false;

    if (this.errorMeter && this.gameworld && Persistence.hitErrorMeterSize != ErrorMeterSize.Off) {
        float num = (float)(this.chosenPlanet.cachedAngle - this.chosenPlanet.targetExitAngle);
        if (!this.isCW) num *= -1f;  // positive = early
        if (!this.midspinInfiniteMargin) {
            if ((RDC.auto || flag) && !RDC.useOldAuto)
                this.errorMeter.AddHit(0f, 1f);  // auto-hit: report zero error
            else
                this.errorMeter.AddHit(num, (float)this.currFloor.nextfloor.marginScale);
        }
    }
    // ... planet switching, camera, etc.
}
```

**CheckPostHoldFail** (decompiled line 3002): Fails the player if:
```
angleDiff = chosenPlanet.angle - chosenPlanet.targetExitAngle
if (!isCW) angleDiff *= -1
if (angleDiff > Max(PI, _minAngleMargin * 2)) → FailAction()  // way past the tile
if (noFail || currFloor.isSafe) threshold drops to _minAngleMargin * 1.01
```

**OttoHoldHit** (decompiled line 3027): Auto-hits when `RDC.auto || benchmarkMode || nextTileIsAuto || (holding && currFloor.auto)`:
```csharp
while (num > 0 && chosenPlanet.AutoShouldHitNow()) {
    RDC.auto = true;
    if (currFloor.holdLength > -1) currFloor.holdRenderer.Hit();
    keyTimes.Clear();
    Hit(true);  // hit with isAuto=true
    RDC.auto = oldAuto;
    num--;
}
```

#### `scrConductor` — Song timing (instance via `scrConductor.instance`)
```csharp
// Properties:
public float bpm;                          // current BPM
public Song song;                          // AudioSource, has .pitch (usually 1.0, higher = faster)
public double songposition_minusi;         // song position in seconds (minus input offset calibration)
public double songposition_minusv;         // songposition_minusi + calibration_i - calibration_v
public double crotchetAtStart;             // 60.0 / initialBPM
public double deltaSongPos;                // change in song position since last frame
public bool isGameWorld;                   // true during gameplay
public bool hasSongStarted;
public float speed;                        // inherited from ADOBase

// Calibration:
public static float calibration_i;         // input offset in seconds
public static float calibration_v;         // visual offset in seconds
```

#### `scrPlanet` — Planet/player character
```csharp
public double angle;                       // current planet angle (radians), updates every frame
public double targetExitAngle;             // exit angle of current tile (radians)
public double cachedAngle;                 // snapshot of angle at hit time
public double visualAngle;                 // angle + calibration_v - calibration_i
public scrFloor currfloor;                 // current floor/tile
public scrFloor conditionalFloor;          // controller.conditionalFloor
public scrPlanet next;                     // next planet in rotation

// Key method:
public bool AutoShouldHitNow() {
    float threshold = (RDC.useOldAuto ? 10f : 0.5f) * 0.017453292f;  // degrees → rad
    if (isCW) return angle > targetExitAngle - threshold;
    else      return angle < targetExitAngle + threshold;
}

public scrPlanet SwitchChosen() {
    // Called on hit. Computes hitMargin via GetHitMargin, then:
    // if (IsValidHit(hitMargin) || auto || midspinInfiniteMargin || noFailInfiniteMargin)
    //     Advance to next tile, return next planet
    // On miss: does NOT advance, returns same planet
}
```

#### `scrFloor` — Tile/floor
```csharp
public double entryangle;                  // entry angle (radians)
public double exitangle;                   // exit angle (radians)
public double angleLength;                 // total angle length of this tile
public double marginScale;                 // hit window multiplier (1.0 = normal, lowers = tighter)
public scrFloor nextfloor;                 // next tile
public scrFloor prevfloor;                 // previous tile
public float speed;                        // speed multiplier
public bool auto;                          // auto-hit tile (no input needed)
public int holdLength;                     // -1 = not hold, 0+ = hold tile
public int countdownTicks;                 // countdown beats
public float extraBeats;                   // extra beats
public bool midSpin;
public bool isportal;
public double entryTime;                   // song time when tile is reached
public double entryTimePitchAdj;           // entryTime adjusted for pitch
public bool isSafe;                        // safe tile (can't die here)
```

#### `scrFailBar` — Overload/multipress death mechanic
```csharp
// overloadCounter increases by overloadDamagePerMiss (0.5) on each miss
// multipressCounter increases by multipressDamage (0.35) on each multipress
// Both decay over time based on overloadCooldown (0.4) / multipressCooldown (0.2)
// If either counter > 1.0 → FailAction(overload: true, multipress: ...)
```

#### `GCS` — Game constants (static fields)
```csharp
public static Difficulty difficulty = Difficulty.Normal;
public static float currentSpeedTrial = 1f;    // speed trial multiplier
public static float HITMARGIN_COUNTED = 60f;   // default counted margin in degrees
public const float HITMARGIN_PERFECT = 45f;
public const float HITMARGIN_PURE = 30f;
public const float HITMARGIN_MINIMUM_SECONDS_HARD = 0.04f;    // Strict
public const float HITMARGIN_MINIMUM_SECONDS_NORMAL = 0.065f; // Normal
public const float HITMARGIN_MINIMUM_SECONDS_EASY = 0.091f;   // Lenient
public const float HITMARGIN_ABSOLUTE_MINIMUM_SECONDS = 0.025f;
public static HitMarginLimit hitMarginLimit;   // None, PerfectsOnly, PurePerfectOnly
public static bool speedTrialMode;
public static bool practiceMode;
public static int practiceLength;
public static float practiceSpeed = 0.75f;
```

#### `RDC` — Runtime debug/config constants
```csharp
public static bool auto;              // auto-play mode
public static bool useOldAuto;        // use old (lenient) auto timing
public static bool debug;             // debug mode
public static bool practice;          // practice mode toggle
public static bool noHud;
```

#### `ADOBase` — Static convenience accessors
```csharp
public static scrConductor conductor;     // scrConductor.instance
public static scrController controller;   // scrController.instance
public static scrLevelMaker lm;           // scrLevelMaker.instance
public static bool isMobile;
public static bool isLevelEditor;
public static bool isScnGame;             // in custom level gameplay
public static bool isOfficialLevel;
public static bool customLevel;
```

#### `Persistence` — Settings storage (hit error meter related)
```csharp
public static ErrorMeterSize hitErrorMeterSize {
    get => (ErrorMeterSize)generalPrefs.GetInt("hitErrorMeterSize", 0);
    set => generalPrefs.SetInt("hitErrorMeterSize", (int)value);
}
public static ErrorMeterShape hitErrorMeterShape {
    get => (ErrorMeterShape)generalPrefs.GetInt("hitErrorMeterShape", 0);
    set => generalPrefs.SetInt("hitErrorMeterShape", (int)value);
}
```

### Enums

```csharp
enum HitMargin {
    TooEarly=0, VeryEarly=1, EarlyPerfect=2, Perfect=3,
    LatePerfect=4, VeryLate=5, TooLate=6,
    Multipress=7, FailMiss=8, FailOverload=9, Auto=10, OverPress=11
}
enum HitMarginGeneral { Counted, Perfect, Pure }
enum HitMarginLimit { None, PerfectsOnly, PurePerfectOnly }
enum Difficulty { Lenient=0, Normal=1, Strict=2 }
enum ErrorMeterSize { Off, Small, Normal, Large, ExtraLarge }
enum ErrorMeterShape { Curved, Straight }
```

### Hit Error to Milliseconds Conversion

The raw `angleDiff` passed to `scrHitErrorMeter.AddHit(angleDiff, marginScale)` is in radians (CW-world, positive = early). To convert to milliseconds:

```
// The raw angle diff is: cachedAngle - targetExitAngle (negated if !isCW)
// So positive = planet has passed the exit = EARLY

angleDiffRad = |cachedAngle - targetExitAngle|  // raw radians
msError = angleDiffRad * 1000 * 60.0 / (PI * bpm * speed * pitch)

// Using scrMisc utilities:
timeInSeconds = scrMisc.AngleToTime(angleDiffRad, bpm * speed) / pitch
msError = timeInSeconds * 1000

// The game's conversion (in scrHitErrorMeter.AddHit):
angleDiff *= -57.29578f;  // rad → deg, negated (positive = early)
adjustedBoundary = GetAdjustedAngleBoundaryInDeg(Counted, bpm*speed, song.pitch, marginScale);
normalized = angleDiff * (60.0 / adjustedBoundary);  // map to ±60 scale
// outside ±60: randomized to [-63, -60) or (60, 63]
```

**Note on sign convention**: After `AddHit` does `angleDiff *= -57.29578f`:
- Positive = early (planet arrived at exit point before the beat)
- Negative = late (planet arrived after the beat)
- Zero = perfect

## Implementation Strategy

### Where to Hook (Harmony Patches)

**Best patch point**: `scrHitErrorMeter.AddHit(float angleDiff, float marginScale)` Postfix.

Why: It receives the raw angle difference on every valid hit. From there, access:
- `scrConductor.instance.bpm` — current BPM
- `scrController.instance.speed` — speed multiplier
- `scrConductor.instance.song.pitch` — pitch adjustment
- `scrConductor.instance.songposition_minusi` — current song time
- `scrController.instance.currFloor` — current tile info

### What to Track

- Per-hit ms error (signed, early/late) with timestamp
- Running statistics: mean, median, standard deviation, min/max, range
- Hit distribution by timing buckets (e.g., ±10ms, ±20ms, ±50ms, etc.)
- Early/late ratio (percentage of hits that are early vs late)
- Session history (last N hits) for trend analysis
- Per-difficulty stats (track separately for Lenient/Normal/Strict)

### Mod File Structure

```
ProjectRoot/
├── BetterHitErrorMeter.csproj
├── ADOFAIMod.targets
├── Main.cs              # Entry point
├── Info.json            # UMM metadata
├── Settings.cs          # Mod configuration
├── Patches.cs           # Harmony Postfix on scrHitErrorMeter.AddHit
├── HitStats.cs          # Statistics tracking (mean, stddev, histogram)
├── Overlay.cs           # On-screen display rendering
└── Properties/
    └── AssemblyInfo.cs
```
