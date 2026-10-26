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
gate_capture_ok "$loop_root/native" run_bounded "./$loop_root/gen/ArraySearchLoopsOnly$EXE_EXT"
gate_capture_ok "$loop_root/oracle" run_bounded dotnet "$loop_app"
gate_output compare "$loop_root/native.stdout" "$loop_root/oracle.stdout"
gate_output select "$loop_root/native.stdout" "$loop_root/prefix.stdout" \
    --before '== array search deep types and late signatures ==' --policy crlf
gate_capture_ok "$loop_root/before-oracle" run_bounded dotnet "$loop_app" before-array-search-additions
gate_output compare "$loop_root/prefix.stdout" "$loop_root/before-oracle.stdout"
gate_capture_ok "$loop_root/before-native" run_bounded "./$loop_root/gen/ArraySearchLoopsOnly$EXE_EXT" before-array-search-additions
gate_output compare "$loop_root/prefix.stdout" "$loop_root/before-native.stdout"
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
    gate_output byte-witness "$loop_root/native.stdout" "$line" --count any --policy crlf \
        || { echo "FAIL: array loop witness missing: $line" >&2; exit 1; }
done
for name in LoopFirstMatch LoopSecondMatch LoopTypeMatch DeepTypeMatch LateLookupMatch \
        SharedCallsiteFirstMatch SharedCallsiteSecondMatch \
        InterfaceListMatch ContravariantSearchMatch SharedReadWriteMatch; do
    grep -Eq "^int32_t ${name}_Equals_m[0-9]+\\(" "$loop_root/gen"/generated*.cpp \
        || { echo "FAIL: loop-selected equality was not emitted: $name" >&2; exit 1; }
done
echo 'PASS: array search loop provenance'
