#!/usr/bin/env bash
# Closed integer Enum formatters use native metadata, while unsupported pairs keep their IL.
enum_static_integer_format_gate() {
    local parent="${1:-artifacts/enum-static-integer-$CONFIG}" root corelib fixture compiler_dir source kind flag axis out
    corelib=$(locate_corelib) || return $?
    fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    compiler_dir=$(dirname "${DN2CPP_CLI_DLL:-$PWD/src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}")
    source="samples/dotnet/EnumOps/bin/$CONFIG/$TFM"
    mkdir -p "$parent" || return $?
    root=$(mktemp -d "$parent/run-XXXXXX") || return $?
    echo "enum-static-integer-artifacts=$root"
    gate_capture_ok "$root/guards" run_bounded dotnet "$fixture" --enum-static-integer-guards \
        "$compiler_dir/Dn2Cpp.Transpiler.dll" "$corelib" "$compiler_dir/ildiet/ILDiet.dll" "$source/EnumOps.dll" || return $?
    gate_output match-record "$root/guards.stdout" \
        '^enum-static-integer-guards=[1-9][0-9]*:sixteen-closed,cold-purity,rare-negative,own-bodies,neighbor-sharing,copied-traversal,noalloc,lossless-nine,switch-dedupe$' \
        --count 1 --policy crlf || return $?
    for kind in public private noalloc; do
        mkdir -p "$root/$kind" || return $?
        cp "$source/"* "$root/$kind/" || return $?
        case "$kind" in
            public) flag=--enum-static-integer-public ;;
            private) flag=--enum-static-integer ;;
            noalloc) flag=--enum-static-integer-noalloc ;;
        esac
        dotnet "$fixture" "$flag" "$root/$kind/EnumOps.dll" "$corelib" || return $?
    done
    gate_capture_ok "$root/public/oracle" run_bounded dotnet "$root/public/EnumOps.dll" || return $?
    gate_python -c 'pass' >/dev/null || return $?
    for axis in raw diet; do
        local flags=()
        [ "$axis" != raw ] || flags+=(--no-ildiet)
        out="$root/$axis"
        invoke_cli "$root/private/EnumOps.dll" -r "$corelib" --auto-ref ${flags[@]+"${flags[@]}"} -o "$out" || return $?
        gate_python - "$out" "$axis" <<'PY_WITNESS' || return $?
from pathlib import Path
import ast, re, sys
sys.path.insert(0, "gates")
from ildiet_manifest import manifest
out = Path(sys.argv[1])
if sys.argv[2] == "diet":
    result, generation = manifest(out / "ildiet")
    assemblies = {row.get("name"): row for row in result.findall("assembly")}
    assert result.get("fallback") == "none" and result.get("cutsValidated") == "true"
    assert assemblies["EnumOps"].get("mode") == "rewritten"
    assert Path(result.get("input")).name == "EnumOps.dll"
source = "\n".join(p.read_text() for p in out.glob("generated*.cpp"))
bodies = re.findall(r"Dn2CppString\* (Enum_ToString_Tis\w+_m[0-9]+)\([^\n]+\)\n\{(.*?)\n\}", source, re.S)
assert len(bodies) == 16, "both declarations need eight closed callable bodies"
for name, body in bodies:
    assert "dn2cpp_enum_format_raw_integer(" in body, name
    assert "RuntimeType" not in body and "ToStringInlined" not in body, name
    assert "(void*)&" + name in source, "ldftn target missing: " + name
    assert name in "\n".join(p.read_text() for p in out.glob("generated_m*.cpp")), "retained metadata target missing: " + name
headers = "\n".join(p.read_text() for p in out.glob("generated*.h"))
pools = re.findall(r"extern const char md_name_pool\[\] =\s*(.*?)\n;", source, re.S)
assert len(pools) == 1, "one metadata name pool required"
pool = b"".join(ast.literal_eval(token).encode("utf-8") for token in
                re.findall(r'"(?:[^"\\]|\\.)*"', pools[0]))
names = {}
for key, offset in re.findall(r"(md_name_[0-9]+) = md_name_pool \+ ([0-9]+);", headers):
    start = int(offset)
    end = pool.index(b"\0", start) if b"\0" in pool[start:] else len(pool)
    names[key] = pool[start:end].decode("utf-8")
for family in ("I1", "U1", "I2", "U2", "I4", "U4", "I8", "U8", "Plain"):
    tables = re.findall(r"md_native_enummembers_EnumStaticIntegerFormatSubset_" + family
                       + r"\[\] = \{(.*?)\n\};", source, re.S)
    assert len(tables) == 1, "retained native enum table missing: " + family
    actual = [names[key] for key in re.findall(r"\{ (md_name_[0-9]+),", tables[0])]
    expected = ["Zero", "First", "Alias", "Two"] if family == "Plain" else ["Zero", "One", "Two", "Both", "Alias", "High"]
    assert actual == expected, "lossless alias metadata order: " + family
print("enum-static-integer-metadata=nine-lossless-tables,all-aliases,unsigned-order")
print("enum-static-integer-bodies=16-own-symbols,direct-and-address,shared-native-formatter")
PY_WITNESS
        compile_console "$out" EnumStaticInteger || return $?
        gate_capture_ok "$out/native" run_bounded "$out/EnumStaticInteger$EXE_EXT" native-controls || return $?
        gate_output select "$out/native.stdout" "$out/public-prefix.stdout" --before-prefix 'raw-null-before-format=' --policy crlf || return $?
        gate_output compare "$out/public-prefix.stdout" "$root/public/oracle.stdout" --policy crlf || return $?
        gate_output byte-witness "$out/native.stdout" '== enum static integer formatter ==' --count 1 --policy crlf || return $?
        gate_output match-record "$out/native.stdout" '^.*:storage=True$' --count 24 --policy crlf || return $?
        gate_output select "$out/native.stdout" "$out/native-safety.stdout" \
            --from 'raw-null-before-format=NullReferenceException' --policy crlf || return $?
        cat >"$out/expected-native-safety.stdout" <<'NATIVE_SAFETY'
raw-null-before-format=NullReferenceException
raw-null-before-decimal=NullReferenceException
invalid-format-before-type=FormatException
metadata-null-type=NullReferenceException
metadata-non-enum=ArgumentException
metadata-wrong-underlying=ArgumentException
native enum static safety controls end
NATIVE_SAFETY
        gate_output compare "$out/native-safety.stdout" "$out/expected-native-safety.stdout" --policy crlf || return $?
        gate_capture "$root/noalloc-$axis" invoke_cli "$root/noalloc/EnumOps.dll" -r "$corelib" --auto-ref \
            ${flags[@]+"${flags[@]}"} -o "$root/noalloc-$axis-output" || return $?
        gate_output status "$(cat "$root/noalloc-$axis.status")" 2 || return $?
        gate_output match-record "$root/noalloc-$axis.stderr" '.*NoAlloc.*dn2cpp_enum_format_raw_integer.*' --count any --policy crlf || return $?
    done
}
