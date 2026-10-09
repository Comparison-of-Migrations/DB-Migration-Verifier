#!/bin/sh
set -eu
repo=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
sdk=${DMV_DOTNET:-dotnet}
cd "$repo"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
bundle="$repo/artifacts/dev/DB-Migration-Verifier.app"
"$sdk" publish src/Verifier.Desktop -c Release -r osx-arm64 --self-contained -p:RestoreLockedMode=true -o "$bundle/Contents/MacOS"
cp tools/Info.plist "$bundle/Contents/Info.plist"
printf 'Local development app: %s\n' "$bundle"
