#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$ROOT_DIR/Diviner.csproj"

export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/tmp/sts2-diviner-dotnet}"
export DOTNET_CLI_TELEMETRY_OPTOUT="${DOTNET_CLI_TELEMETRY_OPTOUT:-1}"
export DOTNET_ROLL_FORWARD="${DOTNET_ROLL_FORWARD:-Major}"
export NUGET_PACKAGES="${NUGET_PACKAGES:-$ROOT_DIR/.nuget/packages}"

mkdir -p "$DOTNET_CLI_HOME" "$NUGET_PACKAGES"

echo "dotnet:"
dotnet --version

STS2_DATA_DIR="$(dotnet msbuild "$PROJECT" -nologo -getProperty:Sts2DataDir)"
MODS_DIR="$(dotnet msbuild "$PROJECT" -nologo -getProperty:ModsPath)"

echo "Sts2DataDir: $STS2_DATA_DIR"
echo "ModsPath: $MODS_DIR"

test -d "$STS2_DATA_DIR"
test -f "$STS2_DATA_DIR/sts2.dll"
test -f "$STS2_DATA_DIR/0Harmony.dll"
test -d "$MODS_DIR"

echo "Environment looks ready."
