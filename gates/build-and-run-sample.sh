#!/usr/bin/env bash
# End-to-end pipeline: C# -> IL -> C++ -> native binary -> run.
source "$(dirname "$0")/_common.sh"

OUT=artifacts/console

echo "== 1/4 Building sample C# assembly =="
build_proj samples/dotnet/HelloWorld/HelloWorld.csproj
app="samples/dotnet/HelloWorld/bin/$CONFIG/$TFM/HelloWorld.dll"

echo "== 2/4 Transpiling IL -> C++ =="
transpile_log="$OUT.transpile.log"
mkdir -p "$(dirname "$transpile_log")"
invoke_cli "$app" -o "$OUT" | tee "$transpile_log"
# The bounded-native-import report's negative control, and it costs one grep: the
# line is UNCONDITIONAL (a silently wrong answer at run time deserves no opt-in),
# which only stays acceptable while it is silent on a program that bounds
# nothing. HelloWorld is that program — the simplest console closure there is,
# and the same negative control dist/nuget-smoke-test.sh uses for the default
# references. If this fires, the report has become noise on every build, and
# noise is how the real one goes unread.
#
# This is also the ONLY place the "silent at zero" property is asserted:
# build-and-run-file-real.sh's step 6 asserts a report instead — FileReal deliberately
# bounds six imports on every host, to cover the verdict column. So this grep is not
# a duplicate of anything.
if grep -q 'native imports bounded' "$transpile_log"; then
    echo "FAIL: HelloWorld bounded a native import to a default — either the bounded set widened" >&2
    echo "      or the report fires unconditionally; it must be silent at zero" >&2
    exit 1
fi
if gate_cache_check "$OUT" "sample|console" \
        "$app" "${app%.dll}.runtimeconfig.json" "${app%.dll}.deps.json"; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 3/4 Compiling C++ =="
compile_console "$OUT" HelloWorld

echo "== 4/4 Running native binary =="
"./$OUT/HelloWorld"
gate_cache_commit
