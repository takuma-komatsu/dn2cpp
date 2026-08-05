#!/usr/bin/env bash
# Regression gate for the UTF-16 -> UTF-8 transcode on the real System.Text.Json
# deserialize-from-string path. JsonSerializer.Deserialize(string) transcodes the
# whole UTF-16 input to UTF-8 (via UTF8Encoding.GetBytes -> Utf8Utility.TranscodeToUtf8)
# before parsing; the Utf8JsonProbe input "{\"name\":\"a•b\"}" carries one non-ASCII
# char (U+2022) after an ASCII run — the shape that tripped the real transcode body
# (it dropped the tail after the non-ASCII char, making the throwing UTF8 encoder
# fault on valid input). The native output ("len=3 c1=U+2022") must match real .NET.
#
# Lightweight (CoreLib + System.Text.Json only, no Godot / extension_api.json) so it
# always runs, giving the transcode fix permanent coverage where build-and-run-godot-
# bindgen.sh (the full binding-gen self-host) skips for lack of the engine API dump.
# Pins net10.0 like the other JSON gates (resolve_net10_corelib in _common.sh).
source "$(dirname "$0")/_common.sh"

net10_json_diff_gate Utf8JsonProbe
