#!/usr/bin/env bash
# Focused entry to the factory block also run by the array-core gate.
set -euo pipefail
exec bash "$(dirname "$0")/../../run-array-sort-factory-fixture.sh" "$@"
