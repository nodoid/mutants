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
tools/                  Asset generation, font licences, store asset build
releases/               Signed .aab, .apk and .ipa (generated, not tracked)
stores/                 Store artwork and listing copy (generated, not tracked)
Movies/                 Gameplay footage for the listings (generated, not tracked)
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

## Deploy to a physical iPhone

`rel-mutants` is a distribution profile and provisions no devices, so a build
that can be installed on a connected iPhone is compiled in Release and signed
with the development profile instead:

```bash
dotnet build src/Mutants.iOS/Mutants.iOS.csproj -c Release -f net10.0-ios -r ios-arm64 \
  -p:CodesignProvision=devel-mutants \
  -p:CodesignKey="Apple Development: <your name> (<TEAMID>)"

xcrun devicectl device install app --device <DEVICE_UDID> \
  src/Mutants.iOS/bin/Release/net10.0-ios/ios-arm64/Mutants.iOS.app
xcrun devicectl device process launch --device <DEVICE_UDID> --console uk.co.allthejohnsons.mutants
```

`Info.plist` edits are not picked up incrementally; add `-t:Rebuild` after
changing it or the built `AppManifest.plist` stays stale.

Signing identities are deliberately not baked into the repository. The release
key is whichever `Apple Distribution` identity and profile you install locally;
override them on the command line when you need a specific one:

```bash
-p:CodesignKey="Apple Distribution: <your name> (<TEAMID>)"
```

## Signing

The iOS project uses the profiles installed from `~/Downloads`:

- Debug: `devel-mutants`
- Release: `rel-mutants`

Profiles are copied into `~/Library/MobileDevice/Provisioning Profiles/` for
local builds; they are intentionally not committed.

## Audio

Every sound is synthesised on the device at startup (`Mutants.Core/Audio`), so the
build carries no sampled audio and nothing is derived from the original ROM:

- `SfxLibrary` renders short square/noise effects for shots, hits, kills, pickups,
  the gate, the boss, menu movement and the score board.
- `MusicLibrary` renders three looping cues: title, district (transposed per level
  theme) and boss.

The opening screen has a `SOUND ON` / `SOUND OFF` button (or press `M`). The choice
is stored in `<LocalApplicationData>/Mutants/settings.json` and survives relaunches.
On iOS the audio session uses the Playback category, so the game is audible with the
Ring/Silent switch on.

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

## Distribution

Signed builds live in `releases/` (see `releases/README.md` for the signing
identities and the exact commands):

| File | Purpose |
| --- | --- |
| `Mutants-1.0.0-android.aab` | Play Console upload |
| `Mutants-1.0.0-android.apk` | device testing |
| `Mutants-1.0.0-ios.ipa` | App Store Connect, signed with `rel-mutants` |

Store artwork and copy are in `stores/`, generated from simulator captures of
the release build:

```bash
python3 tools/build_store_assets.py /tmp/cap   # captures -> stores/
python3 tools/check_store_copy.py              # sizes, copy limits, hosted pages
```

`stores/README.md` documents the capture setup and the exact screenshot sizes.
The iPad footage is in `Movies/`; `Movies/README.md` has the recording and
encode commands.

For capture, the game reads two optional environment variables at launch
(`MUTANTS_SCREEN`, `MUTANTS_DEMO`); they are inert in a normal run.

The copyright line on the title screen reads `2026 PAUL F JOHNSON`.

## Not in the repository

`stores/`, `releases/` and `Movies/` are generated locally and ignored by git:
signed binaries and store artwork do not belong in a source checkout. Rebuild
them with the commands above.

## Licence

DILLIGAF - see `LICENSE`. The bundled Cinzel and Alegreya Sans SC fonts stay
under the SIL Open Font License, as recorded in `tools/fonts/`.
