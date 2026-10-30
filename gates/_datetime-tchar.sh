#!/usr/bin/env bash
# Compiler-only controls supplement the aggregate's real span lifetime execution.
datetime_tchar_negative_gate() {
    local root="$1" original="$2" corelib mode axis rc
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    for mode in ${DN2CPP_DATETIME_TCHAR_NEGATIVES:-return-owner unsupported-int address-return-owner address-unsupported-int}; do
        mkdir -p "$root/$mode/input"
        cp -R "$original/." "$root/$mode/input/" || return $?
        dotnet "$fixture" --datetime-tchar-negative "$root/$mode/input/DateTimeTChar.dll" "$corelib" "$mode" || return $?
        local diagnostic='DateTimeFormatInfo TChar accessor requires its exact MethodSpec MemberRef signature'
        case "$mode" in
            unsupported-int|address-unsupported-int) diagnostic='DateTimeFormatInfo TChar accessor requires its exact Char or Byte signature' ;;
            return-owner|address-return-owner|static|address-static) ;;
            *) echo "unmeasured TChar negative mode: $mode" >&2; return 1 ;;
        esac
        for axis in raw diet; do
            local flags=(); rc=0
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$root/$mode/input/DateTimeTChar.dll" --auto-ref "${flags[@]}" -o "$root/$mode/$axis" > "$root/$mode/$axis.log" 2>&1 || rc=$?
            printf '%s\n' "$rc" > "$root/$mode/$axis.status"
            [[ "$rc" == 2 ]] || return 1
            grep -Fq "$diagnostic" "$root/$mode/$axis.log" || return 1
        done
    done
}

datetime_tchar_closure_gate() {
    local root="$1" original="$2" corelib kind width axis rc
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    for kind in discard noalloc; do
        for width in char byte; do
            mkdir -p "$root/$kind-$width/input"
            cp -R "$original/." "$root/$kind-$width/input/" || return $?
            dotnet "$fixture" "--datetime-tchar-$kind" "$root/$kind-$width/input/DateTimeTChar.dll" "$corelib" "$width" || return $?
            for axis in raw diet; do
                local flags=(); rc=0
                [[ "$axis" != raw ]] || flags+=(--no-ildiet)
                local out="$root/$kind-$width/$axis"
                invoke_cli "$root/$kind-$width/input/DateTimeTChar.dll" --auto-ref "${flags[@]}" -o "$out" > "$out.log" 2>&1 || rc=$?
                printf '%s\n' "$rc" > "$out.status"
                if [[ "$kind-$width" == noalloc-byte ]]; then
                    [[ "$rc" == 2 ]] || return 1
                    grep -Fq '[HotPath(NoAlloc)] closure of DateTimeTChar.Program::ByteDirect allocates: DateTimeTChar.Program::ByteDirect emits dn2cpp_alloc_atomic(' "$out.log" || return 1
                else
                    [[ "$rc" == 0 ]] || return 1
                fi
                if [[ "$kind" == discard ]] && grep -Eq 'Program_(CharDirect|ByteDirect|CharPointer|BytePointer)_m[0-9]+\(' "$out/generated.h"; then
                    echo 'FAIL: direct-discard TChar probe retained an alternate wrapper root' >&2; return 1
                fi
            done
        done
    done
}
