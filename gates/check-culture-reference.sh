#!/usr/bin/env bash
# Verify the managed oracle, without adding ICU to the native runtime.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd -P)"
sdk=$(dotnet --version | tr -d '\r')
[ "$sdk" = 10.0.402 ] || { echo "FAIL: culture reference SDK: expected 10.0.402, got $sdk" >&2; exit 1; }
# The gate bootstrap selects the highest installed CoreLib, not just net10.
corelib=$(dotnet --list-runtimes | tr -d '\r' | awk '$1 == "Microsoft.NETCore.App" { version = $2 } END { print version }')
[ "$corelib" = 10.0.13 ] || { echo "FAIL: culture reference CoreLib: expected 10.0.13, got $corelib" >&2; exit 1; }
printf 'SDK: %s; CoreLib: %s\n' "$sdk" "$corelib"
dotnet run --project "$ROOT/tools/check-culture-reference/check-culture-reference.csproj" --configuration Release
