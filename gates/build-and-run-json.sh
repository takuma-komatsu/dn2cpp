#!/usr/bin/env bash
# End-to-end gate for the real System.Text.Json source-gen path: transpile
# the JsonProbe sample (a `JsonSerializerContext` deserializing a small
# List<T>-nested schema) against the *real* tree-shaken CoreLib + real
# System.Text.Json IL, compile the C++, run the native binary, and diff its output
# against real .NET (`dotnet JsonProbe.dll` -> "a 1").
#
# The flow (net10.0-pinned to avoid an 11.0 CoreLib shape skew) lives in
# net10_json_diff_gate; see _common.sh for why net10.0 is pinned here.
source "$(dirname "$0")/_common.sh"

net10_json_diff_gate JsonProbe
