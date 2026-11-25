#!/usr/bin/env bash
# Erased readonly aliases retain raw MD storage identity on every allocator axis.
md_readonly_pending_asserts() {
    local out="$1" result
    result=$(awk '
        { sub(/\r$/, "") }
        /^\/\/ ArrayCovariantStoreSubset.Program::PendingIndirect$/ { method = 1; next }
        /^\/\/ ArrayCovariantStoreSubset.Program::PendingObject$/ { method = 2; next }
        /^\/\/ ArrayCovariantStoreSubset.Program::PendingArrayIndirect$/ { method = 3; next }
        /^\/\/ ArrayCovariantStoreSubset.Program::PendingArrayObject$/ { method = 4; next }
        /^\/\/ ArrayCovariantStoreSubset.Program::PendingFieldIndirect$/ { method = 5; next }
        /^\/\/ ArrayCovariantStoreSubset.Program::PendingFieldObject$/ { method = 6; next }
        /^\/\/ / { method = 0 }
        waiting {
            root = ""
            if (/DN2CPP_WEB_GC_LIVENESS\(/) {
                root = $0
                sub(/^.*DN2CPP_WEB_GC_LIVENESS\(/, "", root)
                sub(/\);.*$/, "", root)
            }
            waiting = 0
        }
        method && /dn2cpp_byref_load_ref\(/ { waiting = 1; root = "" }
        method && / = .*_ConsumePending.*_m[0-9]+\(/ {
            first = $0
            sub(/^.*_m[0-9]+\(/, "", first)
            sub(/,.*$/, "", first)
            if (root != "" && first == root) found[method]++
        }
        END {
            for (i = 1; i <= 6; i++) if (found[i] != 1) exit
            print "pending indirect references rooted"
        }
    ' "$out/generated.h" "$out"/generated*.cpp)
    [ "$result" = 'pending indirect references rooted' ] \
        || { echo 'FAIL: pending object/array/field references are not rooted across indirect/object reference reads' >&2; return 1; }
}

md_readonly_byrefs_gate() {
    local target="$1" source="artifacts/md-readonly-byrefs-$1"
    local DN2CPP_SAMPLE_PROJECT_DIR=gates/fixtures/md-readonly-byrefs
    local DN2CPP_NO_GC= DN2CPP_GC_BACKEND=unity WASM= _CG_CORELIB_IN=
    local axes axis out uses_gc file inputs=()
    case "$target" in
        native)
            axes=(unity nogc upstream)
            # This directory holds only this invocation's common build inputs.
            rm -rf "$source"
            ;;
        wasm)
            axes=(wasm)
            WASM=1
            _CG_CORELIB_IN=$(locate_corelib_cross_posix pinned) || return 1
            ;;
        *) echo "FAIL: unknown readonly byref target: $target" >&2; return 1 ;;
    esac
    export DN2CPP_NO_GC DN2CPP_GC_BACKEND WASM
    _corelib_gate_core MdReadonlyByrefs "$source" System.Collections
    md_readonly_pending_asserts "$source"
    awk '
        BEGIN { print "void dn2cpp_readonly_byref_runtime_checks();" }
        { print }
        /^[[:space:]]*dn2cpp_runtime_init\(/ {
            print "    dn2cpp_readonly_byref_runtime_checks();"; calls++
        }
        END { if (calls != 1) exit 1 }
    ' "$source/generated.cpp" > "$source/generated.checked.cpp.tmp" || return 1
    mv "$source/generated.checked.cpp.tmp" "$source/generated.cpp"
    gate_capture_ok "$source/clr" run_bounded dotnet "$_CG_APP"

    if [ "$target" = native ]; then
        # CMake consumes the flat emission and link sidecars. ILDiet's descriptor
        # retains the absolute paths of the single managed publication.
        for file in "$source"/*; do
            if [ -f "$file" ]; then inputs+=("$file"); fi
        done
    fi
    for axis in "${axes[@]}"; do
        out="artifacts/md-readonly-byrefs-$axis"
        DN2CPP_NO_GC= DN2CPP_GC_BACKEND=unity uses_gc=1
        case "$axis" in
            nogc) DN2CPP_NO_GC=1; uses_gc=0 ;;
            upstream) DN2CPP_GC_BACKEND=upstream ;;
        esac
        if [ "$target" = native ]; then
            mkdir -p "$out"
            # A smaller emission must remove stale TUs and link sidecars. Hidden
            # CMake directories keep their own toolchain and incremental build.
            rm -rf "$out"/*
            cp "${inputs[@]}" "$out/"
        fi
        printf '#define DN2CPP_EXPECT_RAW_MD_GC %s\n' "$uses_gc" > "$out/generated_readonly_checks.cpp"
        cat gates/fixtures/md-readonly-byrefs/runtime-checks.cpp >> "$out/generated_readonly_checks.cpp"
        if [ "$axis" = wasm ]; then
            compile_console_wasm "$out" MdReadonlyByrefs
            gate_capture_ok "$out/native" run_bounded node "$out/MdReadonlyByrefs.js"
            gate_output compare "$out/clr.stdout" "$out/native.stdout" --policy crlf
        else
            compile_console "$out" MdReadonlyByrefs
            gate_capture_ok "$out/native" run_bounded "$out/MdReadonlyByrefs$EXE_EXT"
            DN2CPP_GC_INCREMENTAL=1 gate_capture_ok "$out/incremental" run_bounded "$out/MdReadonlyByrefs$EXE_EXT"
            gate_output compare "$out/native.stdout" "$out/incremental.stdout"
            gate_output compare "$out/clr.stdout" "$out/native.stdout"
        fi
        echo "OK: readonly MD aliases and owner discrimination ($axis)"
    done
}
