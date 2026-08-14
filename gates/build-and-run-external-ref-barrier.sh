#!/usr/bin/env bash
# Write barriers for stores whose element type is EXTERNAL — a name the load set
# does not contain, so the GC-reference answer comes from the C++ type map rather
# than from a structural walk. The project is transpiled with no references at
# all (asserted below), which is the only way to reach that arm: with the real
# CoreLib loaded System.Exception is an ordinary Class and every store here is
# barriered for a different reason. Five store systems — stfld, whole-struct
# stobj, stelem of a reference-bearing struct element, Array.Fill of a reference
# element, rank-2 accessor Set — each grepped for its barrier in the generated
# C++ and then churned against real collection cycles in both GC modes.
# Former gates: none (new area).
source "$(dirname "$0")/_common.sh"

project=ExternalRefBarrier
OUT=artifacts/externalrefbarrier

echo "== 1/4 Building app assembly =="
build_proj "samples/dotnet/$project/$project.csproj"
app="samples/dotnet/$project/bin/$CONFIG/$TFM/$project.dll"

echo "== 2/4 Transpiling IL -> C++ (no references: the external-type lane) =="
invoke_cli "$app" -o "$OUT"

# The load set is the whole point of this bucket, so it is asserted rather than
# assumed: two assemblies, the app and the runtime's own, and no CoreLib.
if ! grep -qF 'const int32_t dn2cpp_assembly_registry_count = 2;' "$OUT/generated.cpp"; then
    echo "FAIL: $project no longer transpiles with an empty reference set — the" >&2
    echo "      stores below are then structural, not external, and cover nothing" >&2
    grep -n 'dn2cpp_assembly_registry' "$OUT/generated.cpp" >&2
    exit 1
fi

echo "== 3/4 Asserting a barrier at each external-typed store =="
# A store system passes when EVERY line matching its pattern carries a barrier on
# that line or the next; counting matches instead would pass on one barriered
# site among several. The two element-address patterns are anchored at the start
# of the line, which is what separates a store from a load of the same address.
barrier_follows() {
    local what="$1" pattern="$2"
    local bare
    # ENVIRON, not -v: awk expands escape sequences in a -v value, so the
    # backslashes these patterns need would reach the regex already eaten.
    bare=$(PAT="$pattern" awk '
        BEGIN { pat = ENVIRON["PAT"] }
        pending { if ($0 !~ /dn2cpp_gc_write_barrier/) print FILENAME ":" pendingline; pending = 0 }
        $0 ~ pat { if ($0 ~ /dn2cpp_gc_write_barrier/) next; pending = 1; pendingline = FNR }
        END { if (pending) print FILENAME ":" pendingline }
    ' "$OUT"/generated*.cpp)
    local hits
    hits=$(grep -cE -e "$pattern" "$OUT"/generated*.cpp | awk -F: '{ n += $NF } END { print n + 0 }')
    if [ "$hits" -eq 0 ]; then
        echo "FAIL: the $what store is gone from the generated output (pattern: $pattern)" >&2
        exit 1
    fi
    if [ -n "$bare" ]; then
        echo "FAIL: the $what store is emitted without a write barrier:" >&2
        printf '%s\n' "$bare" >&2
        exit 1
    fi
    echo "OK: $what ($hits site(s))"
}

barrier_follows 'external-typed field'          '->f_ExField = '
barrier_follows 'external-typed struct field'   '->f_Value = '
barrier_follows 'whole-struct field'            '->f_Pair = '
barrier_follows 'reference-bearing struct elem' '^ *\*\(t_ExternalWriteBarrierSubset_Program_ExPair\*\)dn2cpp_elem_addr\('
barrier_follows 'Array.Fill of a reference'     '->data\[__fi\] = '
barrier_follows 'rank-2 accessor Set'           '^ *\*\(Dn2CppObject\*\*\)dn2cpp_md_elem_addr2\('

ctx="external_ref_barrier|runs:DN2CPP_GC_INCREMENTAL=0+1|DN2CPP_GC_STATS=1"
ctx="$ctx|assert:empty-refset+generated-barriers+mode+diff+exit+collection-floor"
ctx="$ctx$(_gate_ctx_extras)"
if gate_cache_check "$OUT" "$ctx" \
        "$app" "${app%.dll}.runtimeconfig.json" "${app%.dll}.deps.json"; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 4/4 Compiling C++ and running (STW + incremental; exact diff vs real .NET) =="
compile_console "$OUT" "$project"

log_dir=$(mktemp -d "${TMPDIR:-/tmp}/dn2cpp_extrefbarrier.XXXXXX")
trap 'rm -rf "$log_dir"' EXIT

set +e
expected=$(run_bounded dotnet "$app"); expected_code=$?
stw=$(DN2CPP_GC_INCREMENTAL=0 DN2CPP_GC_STATS=1 \
    run_bounded "./$OUT/$project" 2>"$log_dir/stw.log"); stw_code=$?
incremental=$(DN2CPP_GC_INCREMENTAL=1 DN2CPP_GC_STATS=1 \
    run_bounded "./$OUT/$project" 2>"$log_dir/incremental.log"); incremental_code=$?
set -e
_gate_scratch_cleanup

assert_output "$stw" "$expected"
assert_exit_code "$stw_code" "$expected_code"
assert_output "$incremental" "$expected"
assert_exit_code "$incremental_code" "$expected_code"

if [ "$(grep -cF 'external reference stores:' <<<"$incremental")" -ne 1 ]; then
    echo "FAIL: the external-reference section did not run exactly once" >&2
    exit 1
fi
if ! grep -qF '[dn2cpp] GC mode: stop-the-world' "$log_dir/stw.log"; then
    echo "FAIL: $project STW arm did not report stop-the-world mode" >&2
    sed -n '1,40p' "$log_dir/stw.log" >&2
    exit 1
fi
if ! grep -qF '[dn2cpp] GC mode: incremental' "$log_dir/incremental.log"; then
    echo "FAIL: $project incremental arm did not report incremental mode" >&2
    sed -n '1,40p' "$log_dir/incremental.log" >&2
    exit 1
fi

# The churn is only coverage if it overlapped real cycles; a run that collected a
# handful of times would diff green having exercised nothing.
collections=$(sed -n 's/^collections (GC_no): *//p' "$log_dir/incremental.log")
if [ -z "$collections" ] || [ "$collections" -lt 100 ]; then
    echo "FAIL: the incremental arm collected ${collections:-no} times — too few for the" >&2
    echo "      mutation burst to have raced a mark phase" >&2
    exit 1
fi
echo "OK: $collections collections during the incremental arm"

gate_cache_commit
