#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
revision=9c1d4f5434ca4c5cba9934902acede8f6de757a4
destination="${1:-$root/artifacts/unrealsharp/plugin}"
if [[ -e "$destination" || -L "$destination" ]]; then
    echo "Refusing to modify an existing checkout: $destination" >&2
    exit 1
fi
mkdir -p "$(dirname "$destination")"
git clone --no-checkout https://github.com/takuma-komatsu/UnrealSharp-dn2cpp.git "$destination"
git -C "$destination" checkout --detach "$revision"
printf '%s\n' "Prepared $destination at $revision with dn2cpp integration."
