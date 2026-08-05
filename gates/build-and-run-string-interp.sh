#!/usr/bin/env bash
# Consolidated string concat/interpolation gate: String.Concat (string/char/value
# operands, join-list), interpolated string handlers, interpolation via newobj,
# AppendFormat, and custom format providers.
#
# A LIVE `dotnet $app` diff. It was a frozen snapshot because the custom-format
# section is culture-dependent — percent and currency formatting, where real .NET
# used the host's culture and the transpiler a fixed invariant one. The driver
# pins CurrentCulture, which makes both sides invariant and removes the
# asymmetry, so the expectation moves
# from "whatever the transpiler printed" to "what real .NET prints".
# Former gates: concat, concat-value, concat-join-list, interp, interp-handler,
# interpolation-newobj, append-format, custom-format.
source "$(dirname "$0")/_common.sh"

corelib_diff_gate StringInterp
