#!/usr/bin/env bash
# Focused runner for the real factory block, also retained by array-core.
source "$(dirname "${BASH_SOURCE[0]}")/_common.sh"
source gates/_array-sort-factory.sh
build_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj
corelib=$(resolve_net10_corelib)
array_sort_factory_gate "${1:-artifacts/array-sort-factory}" \
    "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" "$corelib"
