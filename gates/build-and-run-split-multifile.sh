#!/usr/bin/env bash
# Split method bodies, class metadata and global data must link and preserve ArrayCore's
# exact CLR output. Re-emitting into the same directory must clear stale chunks even
# when an earlier cleanup stopped partway through.
source "$(dirname "$0")/_common.sh"

export DN2CPP_SPLIT_BYTES=100000

# ArrayCore is also transpiled by build-and-run-array-core.sh; claim a distinct
# artifacts dir so the two gates don't clobber each other under the parallel runner.
export DN2CPP_OUT_SUFFIX=-split

# The asserts run INSIDE the wrapper's cached region, through the gate_extra_asserts
# hook (`gates/_common.sh`), so a warm hit replays a green that includes them instead
# of re-running them against a build this run did not make. $1 is the OUT the wrapper
# transpiled into: spelling that path out again would agree with the DN2CPP_OUT_SUFFIX
# above by construction and disagree with the wrapper's -hwy/-scalar axes, which is how
# a chunk-count assert ends up reading another build's directory.
gate_extra_asserts() {
    local out="$1" n left sweep chunk chunk_files
    n=$(ls "$out"/generated*.cpp 2>/dev/null | wc -l | tr -d ' ')
    for chunk in generated_b1.cpp generated_m1.cpp generated_data_1.cpp generated_data_2.cpp; do
        if [ ! -s "$out/$chunk" ]; then
            echo "FAIL: DN2CPP_SPLIT_BYTES=$DN2CPP_SPLIT_BYTES expected $chunk (found $n TUs)" >&2
            exit 1
        fi
    done
    echo "OK: split into $n translation units; parallel multi-TU build matches real .NET"

    # Seed a sibling directory so the split output hashed by the gate cache stays intact.
    # Dropping the first data chunk models an interrupted cleanup.
    sweep="$out-sweep"
    rm -rf "$sweep"; mkdir -p "$sweep"
    cp "$out"/generated* "$sweep/"
    rm "$sweep/generated_data_1.cpp"
    touch "$sweep/generated_1.cpp" "$sweep/generated_data_custom.cpp"
    ( export DN2CPP_SPLIT_BYTES=0
      invoke_cli "samples/dotnet/ArrayCore/bin/$CONFIG/$TFM/ArrayCore.dll" \
          -r "$(locate_corelib)" -r "$(dirname "$(locate_corelib)")/System.Collections.dll" -o "$sweep" > /dev/null )
    # Only the numeric body/metadata/data namespaces and the legacy numeric namespace
    # are chunks. Fixed optional TUs also begin with generated_ and may correctly
    # be recreated by the unsplit run.
    chunk_files=$(find "$sweep" -maxdepth 1 -type f -name 'generated*.cpp' \
        | awk '/\/generated_(b|m|data_)?[0-9]+\.cpp$/')
    left=$(grep -c . <<<"$chunk_files" || true)
    if [ "$left" -ne 0 ]; then
        echo "FAIL: an unsplit re-transpile left $left stale chunk(s) behind:" >&2
        printf '%s\n' "$chunk_files" >&2
        exit 1
    fi
    if [ ! -f "$sweep/generated_data_custom.cpp" ]; then
        echo "FAIL: re-transpile removed a non-chunk source" >&2
        exit 1
    fi
    echo "OK: stale chunks from a previous run are swept before the new ones land"

    local definition
    local definitions=(
        '^static const char16_t str_data_[0-9]+\[\] ='
        '^Dn2CppString\* str_[0-9]+;'
        '^(static )?void dn2cpp_init_strings\(\)[[:space:]]*$'
        ' dn2cpp_virtual_method_targets\[\] =' ' dn2cpp_virtual_method_target_count ='
        ' dn2cpp_type_registry\[\] =' ' dn2cpp_type_registry_count ='
        '^(DN2CPP_GC_STATIC_ROOT )?\[\[maybe_unused\]\] [^=]+ sf_[^ =]+ ='
        '^const void\* const rgctx_' '^const uint8_t blob_[0-9]+\[\] ='
    )
    if grep -Fq ' dn2cpp_type_forwarders[] =' "$sweep/generated.cpp"; then
        definitions+=(' dn2cpp_type_forwarders\[\] =')
    fi
    for definition in "${definitions[@]}"; do
        if grep -Eq "$definition" "$out/generated.cpp" ||
            ! grep -Eq "$definition" "$out"/generated_data_[0-9]*.cpp ||
            ! grep -Eq "$definition" "$sweep/generated.cpp"; then
            echo "FAIL: expected $definition in split data chunks and the unsplit primary TU" >&2
            return 1
        fi
    done
}

# The sweep runs the CLI a SECOND time, and what it asserts — that a re-transpile deletes
# the chunks it finds — is invisible in the first transpile's output. So the key's surface
# term cannot see a regression in it: a transpiler that stopped sweeping would emit a
# byte-identical OUT and be served a cached green forever. The CLI hash is the term that
# closes that, the same stand-in a behavior gate uses. The chunk-count assert needs nothing
# extra — it only reads OUT, which the surface term already is.
export DN2CPP_GATE_EXTRA_CONTEXT="stale-chunk-sweep|cli:$(_gate_cli_hash)"

corelib_diff_gate ArrayCore System.Collections
