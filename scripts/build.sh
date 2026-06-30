#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CONFIGURATION="${CONFIGURATION:-Debug}"

export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/tmp/sts2-diviner-dotnet}"
export DOTNET_CLI_TELEMETRY_OPTOUT="${DOTNET_CLI_TELEMETRY_OPTOUT:-1}"
export DOTNET_ROLL_FORWARD="${DOTNET_ROLL_FORWARD:-Major}"
export NUGET_PACKAGES="${NUGET_PACKAGES:-$ROOT_DIR/.nuget/packages}"

mkdir -p "$DOTNET_CLI_HOME" "$NUGET_PACKAGES"

restore_args=()
for arg in "$@"; do
    case "$arg" in
        /p:*|-p:*|/property:*|-property:*)
            restore_args+=("$arg")
            ;;
    esac
done

if ((${#restore_args[@]})); then
    dotnet restore "$ROOT_DIR/Diviner.csproj" --use-lock-file "${restore_args[@]}"
else
    dotnet restore "$ROOT_DIR/Diviner.csproj" --use-lock-file
fi

dotnet build "$ROOT_DIR/Diviner.csproj" --configuration "$CONFIGURATION" --no-restore /p:RequireQuickPck=true "$@"
