#!/usr/bin/env bash
# Two owned String entries: native byte-buffer reference and original UTF-16 scan IL.
# Method tokens keep their existing native-only unsupported boundary.
string_private_assert_parity_and_token_boundary() {
    local out="$1" oracle="$2"
    gate_output select "$out/native.stdout" "$out/positive.stdout" --through 'string private entry points end' --policy crlf || return $?
    gate_output compare "$out/positive.stdout" "$oracle" --policy crlf || return $?
    printf '%s\n' '== string method token boundary ==' 'token raw=PlatformNotSupportedException' \
        'token length=PlatformNotSupportedException' 'string method token boundary end' >"$out/token-boundary.expected" || return $?
    gate_output suffix "$out/native.stdout" "$out/token-boundary.expected" --after 'string private entry points end' --policy crlf || return $?
    gate_output select "$out/native.stdout" "$out/token-boundary.stdout" --from '== string method token boundary ==' --policy crlf || return $?
    gate_output compare "$out/token-boundary.stdout" "$out/token-boundary.expected" --policy crlf || return $?
}

string_private_entrypoints_gate() {
    local parent="${1:-artifacts/string-private-entrypoints-$CONFIG}" root corelib fixture compiler_dir source mode flag axis out
    corelib=$(locate_corelib) || return $?
    fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    compiler_dir=$(dirname "${DN2CPP_CLI_DLL:-$PWD/src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}")
    source="samples/dotnet/StringCore/bin/$CONFIG/$TFM"
    mkdir -p "$parent" || return $?
    root=$(mktemp -d "$parent/run-XXXXXX") || return $?
    echo "string-private-artifacts=$root"
    gate_capture_ok "$root/guards" run_bounded dotnet "$fixture" --string-private-entrypoints-guards \
        "$compiler_dir/Dn2Cpp.Transpiler.dll" "$corelib" "$compiler_dir/ildiet/ILDiet.dll" "$source/StringCore.dll" || return $?
    gate_output match-record "$root/guards.stdout" \
        '^string-private-guards=[1-9][0-9]*:two-owned,cold-purity,raw-decoded,MethodDef,adjacent-negative,Cecil-copy$' \
        --count 1 --policy crlf || return $?
    for mode in public prefix private noalloc bad-return bad-pointee bad-static; do
        mkdir -p "$root/$mode" || return $?
        cp "$source/"* "$root/$mode/" || return $?
        case "$mode" in
            public) flag=--string-private-entrypoints-public ;;
            prefix) flag=--string-private-entrypoints-prefix ;;
            private) flag=--string-private-entrypoints ;;
            noalloc) flag=--string-private-entrypoints-noalloc ;;
            *) flag=--string-private-$mode ;;
        esac
        gate_capture_ok "$root/rewrite-$mode" run_bounded dotnet "$fixture" "$flag" "$root/$mode/StringCore.dll" "$corelib" || return $?
    done
    gate_capture_ok "$root/public/oracle" run_bounded dotnet "$root/public/StringCore.dll" || return $?
    gate_capture_ok "$root/prefix/oracle" run_bounded dotnet "$root/prefix/StringCore.dll" || return $?
    gate_python -c 'pass' >/dev/null || return $?
    for axis in raw diet; do
        local flags=()
        [ "$axis" != raw ] || flags+=(--no-ildiet)
        out="$root/$axis"
        invoke_cli "$root/private/StringCore.dll" -r "$corelib" --auto-ref \
            --link-xml samples/dotnet/StringCore/keep-private-entrypoints.xml \
            ${flags[@]+"${flags[@]}"} -o "$out" || return $?
        gate_python - "$out" "$axis" "$corelib" <<'PY_WITNESS' || return $?
from pathlib import Path
import re, sys
sys.path.insert(0, "gates")
from ildiet_manifest import manifest
out = Path(sys.argv[1])
if sys.argv[2] == "diet":
    result, generation = manifest(out / "ildiet")
    assemblies = {row.get("name"): row for row in result.findall("assembly")}
    assert result.get("fallback") == "none" and result.get("cutsValidated") == "true"
    assert assemblies["StringCore"].get("mode") == "rewritten"
    assert Path(result.get("input")).name == "StringCore.dll"
    assert assemblies["System.Private.CoreLib"].get("mode") == "copied"
    assert (generation / "System.Private.CoreLib.dll").read_bytes() == Path(sys.argv[3]).read_bytes()
    print("string-private-ildiet=rewritten-input,copied-CoreLib,no-fallback,validated-cuts")
source = "\n".join(p.read_text() for p in (*out.glob("generated*.cpp"), out / "generated.h"))
metadata_pools = "\n".join(line for line in source.splitlines() if re.match(r"extern const void\* const md_ptr_[0-9]+\[\] =", line))
for name in ("GetRawStringDataAsUInt8", "wcslen"):
    rows = re.findall(r"(?:uint8_t\*|int32_t) (String_" + name + r"_m[0-9]+)\([^\n]*\)\n\{(.*?)\n\}", source, re.S)
    assert len(rows) == 1, "one callable String body required: " + name
    symbol, body = rows[0]
    assert source.count("(void*)&" + symbol) >= 2, "address and metadata routes required: " + name
    assert "(const void*)((void*)&" + symbol + ")" in metadata_pools, "retained function pointer absent: " + name
    table = "md_record_" + ("stringrawbytetab" if name == "GetRawStringDataAsUInt8" else "stringlengthtab") + "_System_String"
    assert len(re.findall(re.escape(table) + r"\[\] =", source)) == 1, "one private method table required: " + name
    assert "f__firstChar" not in body, "native String must not alias CLR inline data"
    if name == "GetRawStringDataAsUInt8":
        assert "uint8_t*" in body and "->chars" in body and "dn2cpp_null_check" in body
        assert body.index("dn2cpp_null_check") < body.index("->chars"), "null policy precedes buffer read"
        assert "dn2cpp_alloc" not in body and "dn2cpp_string_from" not in body
    else:
        assert "SpanHelpers_IndexOfNullCharacter" in body, "wcslen must retain its original managed forwarding body"
for route in ("RawDirect", "RawVirtual", "RawPointer"):
    callers = re.findall(r"uint8_t\* (Probe_" + route + r"_m[0-9]+)\(Dn2CppString\* a0\)\n\{(.*?)\n\}", source, re.S)
    assert len(callers) == 1, "one native String receiver caller required: " + route
    body = callers[0][1]
    assert "t_System_String" not in body, "generated String layout is not the callable receiver ABI: " + route
    assert "String_GetRawStringDataAsUInt8_m" in body, "owned raw-byte entry call absent: " + route
    if route == "RawVirtual":
        assert "dn2cpp_null_check" in body, "callvirt receiver check required before entry"
    elif route == "RawPointer":
        assert "uint8_t* (*)(Dn2CppString*)" in body, "address/calli must match the native String receiver ABI"
attachments = [line for line in source.splitlines() if "dn2cpp_set_string_private_rows(" in line]
assert len(attachments) == 1 and all("md_record_" + table + "_System_String + 0 + 1" in attachments[0]
    for table in ("stringrawbytetab", "stringlengthtab")), "both canonical named-row installations required"
print("string-private-bodies=two-owned,direct-address,retained,raw-buffer,original-wcslen,canonical-names,NoAlloc-byte-ref")
PY_WITNESS
        compile_console "$out" StringPrivateEntryPoints || return $?
        gate_capture_ok "$out/native" run_bounded "$out/StringPrivateEntryPoints$EXE_EXT" || return $?
        string_private_assert_parity_and_token_boundary "$out" "$root/public/oracle.stdout" || return $?
        gate_output select "$out/native.stdout" "$out/public-prefix.stdout" --before '== string private entry points ==' --policy crlf || return $?
        gate_output compare "$out/public-prefix.stdout" "$root/prefix/oracle.stdout" --policy crlf || return $?
        for line in '== string private entry points ==' 'named declaring=True:True' 'named flags=True:True:True' 'named case=True' \
            'named signature=True:True:True:True' 'null direct=NullReferenceException' \
            'null virtual=NullReferenceException' 'null pointer=NullReferenceException' 'string private entry points end'; do
            gate_output byte-witness "$out/native.stdout" "$line" --count 1 --policy crlf || return $?
        done
        gate_output match-record "$out/native.stdout" '^raw [0-5] identity=True:nonnull=True$' --count 6 --policy crlf || return $?
        gate_output match-record "$out/native.stdout" '^length [0-7]:[0-9]+=.*:sentinel=True$' --count 120 --policy crlf || return $?
        gate_capture "$root/noalloc-$axis" invoke_cli "$root/noalloc/StringCore.dll" -r "$corelib" --auto-ref \
            ${flags[@]+"${flags[@]}"} -o "$root/noalloc-$axis-output" || return $?
        gate_output status "$(cat "$root/noalloc-$axis.status")" 2 || return $?
        gate_output match-record "$root/noalloc-$axis.stderr" '.*NoAlloc.*dn2cpp_string_repeat_char.*' --count any --policy crlf || return $?
        for mode in bad-return bad-pointee bad-static; do
            gate_capture "$root/$mode-$axis" invoke_cli "$root/$mode/StringCore.dll" -r "$corelib" --auto-ref \
                ${flags[@]+"${flags[@]}"} -o "$root/$mode-$axis-output" || return $?
            gate_output status "$(cat "$root/$mode-$axis.status")" 2 || return $?
            gate_output match-record "$root/$mode-$axis.stderr" \
                '.*String private entry point requires its exact byte-reference or UTF-16 pointer signature.*' \
                --count any --policy crlf || return $?
        done
    done
}
