#!/bin/sh
set -eu
repo=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
sdk=${DMV_DOTNET:-dotnet}
cd "$repo"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
bundle="$repo/artifacts/dev/win-x64"
"$sdk" publish src/Verifier.Desktop -m:1 -nodeReuse:false -p:UseSharedCompilation=false -c Release -r win-x64 --self-contained -p:RestoreLockedMode=true -o "$bundle"
cp tools/runtime-README.txt "$bundle/README.txt"
printf 'Local development app: %s\n' "$bundle/Verifier.Desktop.exe"
