# Development Environment

## Verified Inputs

- Slay the Spire 2 is installed under the default Steam path for macOS.
- The local install contains both macOS data folders; this project prefers `data_sts2_macos_arm64` when present.
- .NET SDK `10.0.201` is available and can build the `net9.0` mod project.
- The scripts set `DOTNET_ROLL_FORWARD=Major` so the net9 PckPacker tool can run on the installed .NET 10 runtime.
- The upstream ModTemplate-StS2 project currently defines the expected Godot/C# project shape and uses BaseLib as a default dependency.
- BaseLib is pulled from NuGet and pinned in `Diviner.csproj`; `Diviner.json` mirrors that minimum version.

## Local Paths

The project discovers paths in `Sts2PathDiscovery.props`.

- `Sts2Path`: Steam game install root.
- `Sts2DataDir`: game data folder containing `sts2.dll` and `0Harmony.dll`.
- `ModsPath`: folder where DLL/manifest/PCK files are deployed.
- `ModResourceOutputPath`: resource-root mirror used by local builds.
- `SecondaryModResourceOutputPath`: macOS x86_64 resource mirror when that data folder is present alongside arm64.

If discovery fails, set `Sts2Path` in an ignored `Directory.Build.props`.

## Commands

```bash
./scripts/verify-env.sh
./scripts/build.sh
./scripts/deploy.sh
```

`deploy.sh` is a Release build wrapper. The MSBuild targets handle deployment into the local mods folder.

The script build path requires PckPacker to produce a fresh `Diviner.pck`. If future assets require a full Godot export, use `dotnet publish` with `GodotPath` set instead of relying on the quick packer.

## Publishing Notes

`dotnet publish` requires `GodotPath` to point to MegaDot / Godot `4.5.1` mono. The project keeps the `BasicExport` preset from the STS2 template and excludes `Diviner.json` from the PCK because the manifest lives beside the DLL/PCK in the mod folder. Godot export failures fail the publish instead of leaving a stale PCK.
