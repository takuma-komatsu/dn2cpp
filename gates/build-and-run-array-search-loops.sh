#!/usr/bin/env bash
# Loop-carried array searches and cyclic runtime Type construction retain equality.
set -euo pipefail
source "$(dirname "$0")/_common.sh"

loop_root=artifacts/arraycore-search-loops
dotnet build samples/dotnet/ArrayCore/ArraySearchLoopsOnly.csproj -c "$CONFIG" \
    --nologo -v q -o "$loop_root/app"
loop_app="$loop_root/app/ArraySearchLoopsOnly.dll"
corelib=$(locate_corelib)
run_with_watchdog 60 invoke_cli "$loop_app" -r "$corelib" --shared-generics -o "$loop_root/gen"
compile_console "$loop_root/gen" ArraySearchLoopsOnly
loop_native=$(run_bounded "./$loop_root/gen/ArraySearchLoopsOnly$EXE_EXT")
loop_oracle=$(run_bounded dotnet "$loop_app")
assert_output "$(strip_cr_win "$loop_native")" "$(strip_cr_win "$loop_oracle")"
loop_lines=$(strip_cr_win "$loop_native")
for line in '== array search loop provenance ==' \
        'loop-local=0:1:1' 'loop-argument=0:1:1' 'loop-type=0' \
        'loop-type-unwrapped=0:1' 'loop-unsearched-leaf=0' \
        'array search loop provenance end'; do
    grep -Fxq -- "$line" <<< "$loop_lines" \
        || { echo "FAIL: array loop witness missing: $line" >&2; exit 1; }
done
for name in LoopFirstMatch LoopSecondMatch LoopTypeMatch; do
    rg -q "^int32_t ${name}_Equals_m[0-9]+\\(" "$loop_root/gen"/generated*.cpp \
        || { echo "FAIL: loop-selected equality was not emitted: $name" >&2; exit 1; }
done
if rg -q '^int32_t .*LoopUnsearchedMatch.*_Equals_m[0-9]+\(' \
        "$loop_root/gen"/generated*.cpp; then
    echo 'FAIL: array wrappers rooted their unsearched value leaf' >&2
    exit 1
fi
echo 'PASS: array search loop provenance'
