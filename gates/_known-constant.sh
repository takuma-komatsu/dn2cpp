#!/usr/bin/env bash
# Real IsKnownConstant false bodies, argument evaluation, and the affected SpanHelpers caller.
known_constant_asserts() {
    local output="$1" mode="$2" label
    if [[ "$mode" == generic-struct ]]; then
        gate_output witness "$output" 'known-constant-generic-struct:True' 'Known constant generic struct end' --count 1 --policy crlf
        return $?
    fi
    if [[ "$mode" == caller || "$mode" == all ]]; then
        gate_output witness "$output" 'known-constant-actual-caller:True' 'Known constant actual caller end' --count 1 --policy crlf || return $?
        [[ "$mode" != caller ]] || return 0
    fi
    if [[ "$mode" == all || "$mode" == generic-reflection ]]; then
        gate_output witness "$output" 'known-constant-generic-reflection-root:True' \
            'known-constant-generic-reflection:True' 'Known constant generic reflection end' --count 1 --policy crlf || return $?
        [[ "$mode" != generic-reflection ]] || return 0
    fi
    for label in type string char; do
        gate_output witness "$output" "known-constant-reflection-$label:True" --count 1 --policy crlf || return $?
    done
    gate_output witness "$output" 'Known constant reflection end' --count 1 --policy crlf || return $?
    if [[ "$mode" == all ]]; then
        gate_output witness "$output" '== Known constant conservative fallback ==' \
            '== Known constant public searches ==' 'Known constant public searches end' \
            'Known constant routes end' --count 1 --policy crlf || return $?
        for label in type string char generic effects-consumed effects-unused effects-throw calli delegate public-searches; do
            gate_output witness "$output" "known-constant-$label:True" --count 1 --policy crlf || return $?
        done
    fi
}

known_constant_emitted_asserts() {
    "$(resolve_python)" - "$1" "$2" "$3" <<'PY_WITNESS'
import pathlib, re, sys
root, mode, level = pathlib.Path(sys.argv[1]), sys.argv[2], sys.argv[3]
bodies = []
# Small managed bodies may be inline definitions in the header, not declarations.
for path in [root / 'generated.h', *root.glob('generated*.cpp')]:
    source = path.read_text()
    bodies.extend(re.findall(r'^([^\n;{}]+\([^\n]*\)\n\{\n.*?^\})', source, re.M | re.S))
hints = [body for body in bodies if re.search(r'\bRuntimeHelpers_IsKnownConstant\w*\(', body.split('\n', 1)[0])]
assert hints, 'real IsKnownConstant C++ definition missing'
for body in hints:
    assert re.search(r'\b(t\d+) = 0;\s+return \1;', body) or re.search(r'\breturn 0;', body), 'hint definition is not real false body'
if mode in ('caller', 'all'):
    callers = [body for body in bodies if re.search(r'\bSpanHelpers_IndexOfAnyValueType\w*\(', body.split('\n', 1)[0])]
    assert callers, 'actual SpanHelpers definition missing'
    folded = re.compile(r't\d+ = a1;\s+(t\d+) = \(\(int32_t\)\(0\)\);\s+if \(\1 == 0\) goto (IL_[0-9A-F]+);\s+'
                        r't\d+ = a2;\s+(t\d+) = \(\(int32_t\)\(0\)\);\s+if \(\3 == 0\) goto \2;')
    witnesses = []
    for body in callers:
        if len(re.findall(r'\bRuntimeHelpers_IsKnownConstant\w*\(', body)) >= 2:
            witnesses.append(body)
            continue
        match = folded.search(body)
        if match:
            # Managed trivial-body folding must retain both argument reads and
            # their false branches to the ordinary two-candidate packed route.
            label, visited, target = match[2], set(), None
            while label not in visited:
                visited.add(label)
                target = re.search(r'^' + label + r':;\n(.*?)(?=^IL_|^\})', body, re.M | re.S)
                jump = re.fullmatch(r'\s*goto (IL_[0-9A-F]+);\s*', target[1]) if target else None
                if not jump:
                    break
                label = jump[1]
            if target and re.search(r'\bPackedSpanHelpers_IndexOfAny(?:Except)?_\w*\(', target[1]):
                witnesses.append(body)
    assert len(witnesses) >= 2, 'both actual caller specializations need two hint calls or precise managed false-fold witnesses'
if mode == 'all':
    assert len(hints) >= 4, 'four overload bodies missing'
if mode == 'reflection' and level.lower() == 'high':
    header = (root / 'generated.h').read_text()
    assert not re.search(r'Program_(?:RunArtifact|PointerType|PointerString|PointerChar|PointerGeneric|Caller)[^\s(]*\(', header), 'reflection-only artifact retained direct roots'
print('known-constant-emitted-body-witness:True')
PY_WITNESS
}

known_constant_routes_gate() {
    local root="$1" original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM"
    shift
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib
    corelib=$(resolve_net10_corelib) || return $?
    local mode axis level flag oracle input out
    for mode in ${DN2CPP_KNOWN_CONSTANT_MODES:-all reflection}; do
        flag=--known-constant
        [[ "$mode" == all ]] || flag+="-$mode"
        oracle="$root/oracle-$mode"; input="$root/input-$mode"
        # Inputs and each axis are retained separately; no private patched artifact runs on CLR.
        [[ ! -e "$oracle" && ! -e "$input" ]] || { echo 'FAIL: choose a fresh known-constant artifact root' >&2; return 1; }
        mkdir -p "$oracle" "$input"
        cp "$original/"*.dll "$original/"*.json "$oracle/" || return $?
        cp "$original/"*.dll "$original/"*.json "$input/" || return $?
        dotnet "$fixture" "$flag-oracle" "$oracle/SelfHostPrimSubset.dll" "$corelib" || return $?
        gate_capture_ok "$oracle/result" run_bounded dotnet "$oracle/SelfHostPrimSubset.dll" || return $?
        known_constant_asserts "$oracle/result.stdout" "$mode" || return $?
        dotnet "$fixture" "$flag" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        for level in ${DN2CPP_KNOWN_CONSTANT_LEVELS:-High Medium}; do
        for axis in ${DN2CPP_KNOWN_CONSTANT_AXES:-raw diet}; do
            out="$root/$mode-$level-$axis"
            local flags=("$@" --stripping-level "$(printf '%s' "$level" | tr '[:upper:]' '[:lower:]')") runner=()
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
            known_constant_emitted_asserts "$out" "$mode" "$level" || return $?
            if [[ "${WASM:-0}" == 1 ]]; then
                compile_console_wasm "$out" KnownConstant || return $?
                runner=(node "$out/KnownConstant.js")
            else
                compile_console "$out" KnownConstant || return $?
                runner=("$out/KnownConstant$EXE_EXT")
            fi
            gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
            known_constant_asserts "$out/native.stdout" "$mode" || return $?
            gate_output compare "$oracle/result.stdout" "$out/native.stdout" --policy crlf || return $?
        done
        done
    done
}

known_constant_guards_gate() {
    local transpiler="$1" fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    dotnet "$fixture" --known-constant-guards "$transpiler" || return $?
    dotnet "$fixture" --known-constant-raw-guards "$transpiler"
}
