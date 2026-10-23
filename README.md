# Mutants

An original, hi-res night-terror action adventure for Android and iOS, built with
.NET 10 and MonoGame. It is a loving homage to the structure of 8-bit console
horror adventures: explore five districts, recover keys, open the gate, and put
the thing in the lair back to sleep.

All artwork, level layouts, names, text, and audio cues in this repository are
original. No Atari ROM data, sprites, maps, or other copyrighted game material is
included.

## Targets

| Platform | Minimum | Target |
| --- | --- | --- |
| Android | API 32 | API 36 (latest .NET 10 Android target) |
| iOS | 18.0 | latest installed SDK |

Bundle/application id: `uk.co.allthejohnsons.mutants`

## Layout

```
src/Mutants.Core        Pure game rules, levels, difficulty, high scores
src/Mutants.Game        MonoGame presentation, input, procedural art
src/Mutants.Android     Android shell, manifest, adaptive icon, splash
src/Mutants.iOS         iOS shell, Info.plist, asset catalogs, signing
tests/Mutants.Core.Tests xUnit tests for rules, levels, and persistence
tools/                  Asset generation and font licences
```

`Mutants.Core` has no MonoGame dependency, so the simulation, scoring, and
persistence are testable without a graphics device.

## Build and test

```bash
dotnet test tests/Mutants.Core.Tests/Mutants.Core.Tests.csproj
dotnet build src/Mutants.Android/Mutants.Android.csproj
dotnet build src/Mutants.iOS/Mutants.iOS.csproj
```

Run on a booted simulator/emulator:

```bash
dotnet build src/Mutants.iOS/Mutants.iOS.csproj \
  -t:Run -f net10.0-ios -p:_DeviceName=:v2:udid=<SIMULATOR_UDID>
```

Android debug APKs land in `src/Mutants.Android/bin/Debug/net10.0-android/`.

## Signing

The iOS project uses the profiles installed from `~/Downloads`:

- Debug: `devel-mutants`
- Release: `rel-mutants`

Profiles are copied into `~/Library/MobileDevice/Provisioning Profiles/` for
local builds; they are intentionally not committed.

## Controls

- Move: drag anywhere in the left half of the screen (virtual stick)
- Attack: tap the right half, or hold to keep firing
- Pause: the pause button, `Esc`, or `P`
- Menus: tap, or use arrow keys / `Enter`
- Score entry: tap a letter slot to change it, or type A-Z / 0-9

## High scores

The top eight runs are kept in a JSON board in the platform's local application
data folder:

```
<LocalApplicationData>/Mutants/highscores.json
```

Scores are keyed by initials, difficulty, level reached, and UTC timestamp.
Writes are atomic, and a damaged board falls back to an empty table rather than
blocking startup. Tests cover ordering, eight-entry trimming, sanitising
initials, round-tripping, and recovery from corrupt data.

## Assets

`tools/generate_assets.py` regenerates the app icon, splash art, title art, and
bitmap font atlases. The bundled Cinzel and Alegreya Sans SC fonts are used under
the SIL Open Font License; see `tools/fonts/LICENSE-*.txt`.
