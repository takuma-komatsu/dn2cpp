#!/usr/bin/env bash
# Scalar-axis variant of build-and-run-utf8-json.sh. Runs the real System.Text.Json
# deserialize-from-string path with the Highway SIMD backend opted out
# (-DDN2CPP_USE_HIGHWAY=OFF, set here via SCALAR=1). With the HW-accel gate folded
# to 0 the faithful SpanHelpers / JSON SIMD reachability stays behind the fold and
# the scalar fallbacks run instead — the path the default (Highway) build no longer
# exercises. (The UTF-16 -> UTF-8 transcode itself is a wholesale intercept whose
# call-graph edge is cut, so it is unaffected either way.)
#
# Output is diffed EXACT vs real .NET — the JSON parse result is deterministic and
# byte-identical regardless of vectorization. The SCALAR axis uses separate
# runtime/app build dirs and a -scalar output dir (gates/_common.sh), so it runs
# alongside the default utf8-json gate without clobbering it. Pins net10.0.
source "$(dirname "$0")/_common.sh"

export SCALAR=1
net10_json_diff_gate Utf8JsonProbe
