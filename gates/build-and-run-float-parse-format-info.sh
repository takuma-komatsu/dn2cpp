#!/usr/bin/env bash
# Float parsing through IBinaryFloatParseAndFormatInfo<TSelf>: Utf8Parser.TryParse
# (double/float) and the span Parse/TryParse family, diffed against real .NET.
# Double/Single implement the interface's static abstract constants as explicit
# impls the constrained resolver cannot see, so they are lowered from the interface
# side (MethodCompiler.TryEmitBinaryFloatParseAndFormatInfoIntrinsic); a missing
# row surfaces here as a transpile error, a wrong value as an output diff.
source "$(dirname "$0")/_common.sh"

corelib_diff_gate FloatParseFormatInfo
