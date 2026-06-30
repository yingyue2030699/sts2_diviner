# Diviner

Diviner is a Slay the Spire 2 mod scaffold that requires BaseLib and is shaped for local development plus Steam Workshop packaging.

## Development

Use the scripts in `scripts/` so .NET first-run files and NuGet packages stay in controlled locations:

```bash
./scripts/verify-env.sh
./scripts/build.sh
./scripts/deploy.sh
```

`build.sh` compiles the mod and copies the DLL, manifest, resources, generated PCK, and BaseLib runtime files into the local Slay the Spire 2 mods folders when the game install is discoverable.

For machine-specific overrides, create `Directory.Build.props` from `Directory.Build.props.example`. That file is intentionally ignored by Git.

## Layout

- `Diviner.csproj`: Godot/C# project and local deploy targets.
- `Diviner.json`: mod manifest, including the BaseLib dependency.
- `DivinerCode/`: C# entry point and future gameplay code.
- `Diviner/`: resources, localization, images, and Godot assets packed into the mod PCK.
- `docs/`: planning, environment, and quality-control notes.
