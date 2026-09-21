#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
revision=d5ec8f27dd4c4597f8a6d51276da40e0732a9db7
destination="${1:-$root/artifacts/unrealsharp/plugin}"
if [[ -e "$destination" || -L "$destination" ]]; then
    echo "Refusing to modify an existing checkout: $destination" >&2
    exit 1
fi
mkdir -p "$(dirname "$destination")"
git clone --no-checkout https://github.com/takuma-komatsu/UnrealSharp-dn2cpp.git "$destination"
git -C "$destination" checkout --detach "$revision"
printf '%s\n' "Prepared $destination at $revision with dn2cpp integration."
