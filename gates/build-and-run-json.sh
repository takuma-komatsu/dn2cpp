#!/usr/bin/env bash
# End-to-end gate for the real System.Text.Json source-gen path: transpile
# the JsonProbe sample (a `JsonSerializerContext` deserializing a small
# List<T>-nested schema, including double and float properties) against the
# *real* tree-shaken CoreLib + real System.Text.Json IL, compile the C++, run the
# native binary, and diff its output against real .NET. The floating-point
# properties cover Utf8JsonReader.GetDouble/GetSingle and their Utf8Parser path.
#
# The flow (on the pinned CoreLib) lives in json_diff_gate; see
# resolve_pinned_corelib in _common.sh for why it is pinned.
source "$(dirname "$0")/_common.sh"

json_diff_gate JsonProbe
