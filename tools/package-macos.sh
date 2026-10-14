#!/bin/sh
set -eu
repo=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
sdk=${DMV_DOTNET:-dotnet}
cd "$repo"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
bundle="$repo/artifacts/dev/DB-Migration-Verifier.app"
"$sdk" publish src/Verifier.Desktop -m:1 -nodeReuse:false -p:UseSharedCompilation=false -c Release -r osx-arm64 --self-contained -p:RestoreLockedMode=true -o "$bundle/Contents/MacOS"
cp tools/Info.plist "$bundle/Contents/Info.plist"
cp tools/runtime-README.txt "$repo/artifacts/dev/README-macos.txt"
printf 'Local development app: %s\n' "$bundle"
