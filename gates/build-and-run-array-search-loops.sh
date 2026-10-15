#!/usr/bin/env bash
# Cyclic and deep acyclic runtime Type construction and late signatures retain equality.
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
loop_prefix=$(awk '/^== array search deep types and late signatures ==$/ { exit } { print }' <<< "$loop_lines")
loop_before=$(run_bounded dotnet "$loop_app" before-array-search-additions)
assert_output "$loop_prefix" "$(strip_cr_win "$loop_before")"
loop_before=$(run_bounded "./$loop_root/gen/ArraySearchLoopsOnly$EXE_EXT" before-array-search-additions)
assert_output "$loop_prefix" "$(strip_cr_win "$loop_before")"
for line in '== array search loop provenance ==' \
        'loop-local=0:1:1' 'loop-argument=0:1:1' 'loop-type=0' \
        'loop-type-unwrapped=0:1' 'loop-unsearched-leaf=0' \
        'array search loop provenance end' \
        '== array search deep types and late signatures ==' \
        'deep-acyclic-type=0:1:True' 'late-signature-lookup=0:1' \
        'array search deep types and late signatures end' \
        '== array search shared callsite provenance ==' \
        'shared-callsite=0:0:1:1' \
        'array search shared callsite provenance end' \
        '== array search interface provenance ==' \
        'interface-arraylist=0:1' 'contravariant-array-search=0:1' \
        'array search interface provenance end' \
        '== array search shared read writes ==' \
        'shared-read-write-first=Int32:0:1:True' \
        'shared-read-write-second=String:0:1:True' \
        'array search shared read writes end'; do
    grep -Fxq -- "$line" <<< "$loop_lines" \
        || { echo "FAIL: array loop witness missing: $line" >&2; exit 1; }
done
for name in LoopFirstMatch LoopSecondMatch LoopTypeMatch DeepTypeMatch LateLookupMatch \
        SharedCallsiteFirstMatch SharedCallsiteSecondMatch \
        InterfaceListMatch ContravariantSearchMatch SharedReadWriteMatch; do
    grep -Eq "^int32_t ${name}_Equals_m[0-9]+\\(" "$loop_root/gen"/generated*.cpp \
        || { echo "FAIL: loop-selected equality was not emitted: $name" >&2; exit 1; }
done
echo 'PASS: array search loop provenance'
