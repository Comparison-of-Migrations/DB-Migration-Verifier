#!/bin/sh
set -eu
repo=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
sdk=${DMV_DOTNET:-dotnet}
cd "$repo"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
exec "$sdk" run --project src/Verifier.Desktop -p:RestoreLockedMode=true
