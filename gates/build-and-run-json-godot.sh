#!/usr/bin/env bash
# End-to-end gate for the real System.Text.Json source-gen path on the actual
# Godot extension_api.json schema shape: transpile the JsonGodotProbe
# sample — a `JsonSerializerContext` whose model graph mirrors the one
# Dn2Cpp.Godot.GodotApi.Load deserializes (classes/methods/properties +
# utility_functions + builtin_classes/members/constructors/constants/operators) —
# against the *real* tree-shaken CoreLib + real System.Text.Json IL, compile the
# C++, run the native binary, and diff its output against real .NET
# (`dotnet JsonGodotProbe.dll`). This puts the engine-binding load path's STJ
# source-gen surface under permanent regression. JsonGodotProbe splits into more
# TUs than JsonProbe, so this gate is heavier.
#
# The flow (on the pinned CoreLib) lives in json_diff_gate; see
# resolve_pinned_corelib in _common.sh for why it is pinned.
source "$(dirname "$0")/_common.sh"

json_diff_gate JsonGodotProbe
