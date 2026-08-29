#!/usr/bin/env bash
# Platform-ISA reachability owns its native compile surface: no ISA use emits no
# detector TU, while one reached family emits its detector and that family only.
# The full PlatformIsaProbe remains the all-family end of this 0/1/all contract.
source "$(dirname "$0")/_common.sh"

unset DN2CPP_CPU_FEATURES DN2CPP_ENABLE_AVX10V2

project=gates/fixtures/platform-isa-header/PlatformIsaHeaderProbe.csproj
corelib=$(locate_corelib)
scratch=$(mktemp -d "${TMPDIR:-/tmp}/dn2cpp_isa_headers.XXXXXX")
trap 'rm -rf "$scratch"' EXIT

extract_platform_isa_includes() {
    tr -d '\r' | sed -nE 's/^#include "(isa\/[^\"]+)"$/\1/p'
}

include_fixture='#include "isa/x86/dn2cpp_isa_x86_sse2.h"'
lf_selected=$(printf '%s\n' "$include_fixture" | extract_platform_isa_includes)
crlf_selected=$(printf '%s\r\n' "$include_fixture" | extract_platform_isa_includes)
if [ "$lf_selected" != isa/x86/dn2cpp_isa_x86_sse2.h ] \
        || [ "$crlf_selected" != "$lf_selected" ]; then
    echo "FAIL: platform-ISA include extraction differs between LF and CRLF" >&2
    exit 1
fi

check_axis() {
    local axis="$1" define="$2" header="$3" token="$4" helper="$5"
    local managed="$scratch/managed-$axis" obj="$scratch/obj-$axis"
    local out="artifacts/platformisaheader-$axis"

    echo "== $axis: building the one-family managed fixture =="
    dotnet build "$project" -c "$CONFIG" --nologo -v q \
        -p:DefineConstants="$define" \
        -p:BaseIntermediateOutputPath="$obj/" \
        -o "$managed"
    local app="$managed/PlatformIsaHeaderProbe.dll"
    [ -f "$app" ] || { echo "FAIL: fixture assembly missing: $app" >&2; return 1; }

    echo "== $axis: transpiling and checking the selected include closure =="
    invoke_cli "$app" -r "$corelib" -o "$out"
    [ -f "$out/generated_platform_isa.cpp" ] \
        || { echo "FAIL: $axis did not emit generated_platform_isa.cpp" >&2; return 1; }
    grep -qxF '#include "intrinsics/dn2cpp_cpu_features.cpp"' "$out/generated_platform_isa.cpp" \
        || { echo "FAIL: $axis detector TU is not the canonical one-line include" >&2; return 1; }
    grep -qF '#include "dn2cpp_cpu_features.h"' "$out/generated.h" \
        || { echo "FAIL: $axis generated.h lacks the feature-token header" >&2; return 1; }
    grep -qF "#include \"$header\"" "$out/generated.h" \
        || { echo "FAIL: $axis generated.h lacks $header" >&2; return 1; }
    if grep -qF '#include "isa/dn2cpp_isa.h"' "$out/generated.h"; then
        echo "FAIL: $axis fell back to the all-family umbrella" >&2
        return 1
    fi

    local selected
    selected=$(extract_platform_isa_includes < "$out/generated.h")
    if [ "$selected" != "$header" ]; then
        printf 'FAIL: %s selected family headers [%s], expected only [%s]\n' \
            "$axis" "$(tr '\n' ' ' <<<"$selected")" "$header" >&2
        return 1
    fi
    grep -qF "$token" "$out"/generated*.cpp \
        || { echo "FAIL: $axis emitted no IsSupported token $token" >&2; return 1; }
    grep -qF "$helper" "$out"/generated*.cpp \
        || { echo "FAIL: $axis emitted no instruction helper $helper" >&2; return 1; }

    if gate_cache_check "$out" "platform-isa-includes|$axis|$header|$token|$helper" \
            "$app" "$corelib" "$project" gates/fixtures/platform-isa-header/Program.cs; then
        gate_cache_hit_msg
        return 0
    fi

    echo "== $axis: compiling the selected family and detector =="
    compile_console "$out" "PlatformIsaHeaderProbe-$axis"
    local builddir; builddir="$(_cmake_app_builddir "$out")"
    local ninja="$builddir/build.ninja"
    [ -f "$ninja" ] || { echo "FAIL: native build manifest missing: $ninja" >&2; return 1; }

    local detector_rule
    detector_rule=$(awk '
        { sub(/\r$/, "") }
        /^build / && /generated_platform_isa\.cpp\.(o|obj):/ { inside = 1 }
        inside { print }
        inside && /^$/ { exit }
    ' "$ninja")
    [ -n "$detector_rule" ] \
        || { echo "FAIL: $axis native manifest does not compile generated_platform_isa.cpp" >&2; return 1; }
    if grep -qF 'cmake_pch' <<<"$detector_rule"; then
        echo "FAIL: $axis detector TU is compiled through the app PCH" >&2
        return 1
    fi
    gate_cache_commit
}

check_axis x86 PROBE_X86 \
    isa/x86/dn2cpp_isa_x86_sse2.h \
    DN2CPP_ISA_X86_Sse2 \
    dn2cpp_isa_x86_sse2_add_v128i32_v128i32
check_axis arm PROBE_ARM \
    isa/arm/dn2cpp_isa_arm_advsimd.h \
    DN2CPP_ISA_Arm_AdvSimd \
    dn2cpp_isa_arm_advsimd_add_v128i32_v128i32
check_axis wasm PROBE_WASM \
    isa/wasm/dn2cpp_isa_wasm_packedsimd.h \
    DN2CPP_ISA_Wasm_PackedSimd \
    dn2cpp_isa_wasm_packedsimd_add_v128i32_v128i32

echo "PASS: each reached platform-ISA family owns exactly its helper and detector compile surface"
