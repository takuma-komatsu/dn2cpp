#!/usr/bin/env bash
# Attributed reachable implementations retain noinline bodies and deterministic
# symbol-to-TU mappings under stripping and canonical generic sharing, and under
# --no-shared-generics a target only a compile round reaches keeps its row; a
# refused target set leaves no generated sources in the output directory.
source "$(dirname "$0")/_common.sh"

# Keep native parity under the standard wrapper; the hook exercises opt-in emission.
export DN2CPP_GATE_EXTRA_CONTEXT="obfuscation|strict|ildiet-on-off|no-shared-generics|cli:$(_gate_cli_hash)"
PYTHON="$(resolve_python)" || gate_skip "obfuscation validation requires Python"
gate_extra_asserts() {
    local baseline="$1" mode out
    for mode in stripped original; do
        out="$baseline-$mode"
        local flags=()
        [ "$mode" = original ] && flags+=(--no-ildiet)
        DN2CPP_STRICT_COMPLETION=1 invoke_cli "$_CG_APP" -r "$_CG_CORELIB" \
            --obfuscate ${flags[@]+"${flags[@]}"} -o "$out"
        "$PYTHON" - "$out" <<'PY'
import json, pathlib, re, sys
root = pathlib.Path(sys.argv[1])
rows = json.loads((root / 'obfuscation-targets.json').read_text(encoding='utf-8'))['targets']
assert rows
names = '\n'.join(row['managedMethod'] for row in rows)
for name in ('::.ctor', '::Mix', '::Tiny', '::Pick', '::ArrayName'):
    assert name in names, (name, names)
assert 'Unreachable' not in names and 'PlainTiny' not in names
symbols = {row['implementationSymbol'] for row in rows}
assert len([s for s in symbols if 'Mix' in s]) == 2
pick_rows = [row for row in rows if '::Pick' in row['managedMethod']]
assert len({row['implementationSymbol'] for row in pick_rows}) < len(pick_rows), pick_rows
header = (root / 'generated.h').read_text(encoding='utf-8')
for row in rows:
    symbol = row['implementationSymbol']
    assert row['symbolPattern'] == f'^_Z{len(symbol)}{symbol}.*$'
    assert pathlib.Path(row['cppFile']).name == row['cppFile']
    source = (root / row['cppFile']).read_text(encoding='utf-8')
    assert re.search(r'DN2CPP_NOINLINE [^\n]*\b' + re.escape(symbol) + r'\([^\n]*\)\n\{', source), row
    assert not re.search(r'inline [^\n]*\b' + re.escape(symbol) + r'\(', header), row
assert re.search(r'inline [^\n]*PlainTiny', header)
array_rows = [row for row in rows if '::ArrayName' in row['managedMethod']]
assert len(array_rows) == 2 and len({row['implementationSymbol'] for row in array_rows}) == 1, array_rows
row = array_rows[0]
source = (root / row['cppFile']).read_text(encoding='utf-8')
assert re.search(r'DN2CPP_NOINLINE [^\n]*' + re.escape(row['implementationSymbol']) + r'\([^\n]*__rgctx', source), row
PY
        cp "$out/obfuscation-targets.json" "$out/targets-first.json"
        DN2CPP_STRICT_COMPLETION=1 invoke_cli "$_CG_APP" -r "$_CG_CORELIB" \
            --obfuscate ${flags[@]+"${flags[@]}"} -o "$out"
        cmp "$out/targets-first.json" "$out/obfuscation-targets.json"
        compile_console "$out" Obfuscation
        assert_output "$(run_bounded "./$out/Obfuscation")" "$(run_bounded dotnet "$_CG_APP")"
    done

    local negative="$baseline-negative"
    mkdir -p "$negative"
    "$PYTHON" - "$negative" <<'PYGEN'
import pathlib, sys
root = pathlib.Path(sys.argv[1])
cases = {
    'empty': ('public static int Target() => 1;', 'Target()'),
    'intrinsic': ('', 'System.Math.Abs(-1)'),
    'bodyreplace': ('', 'new Grpc.Net.Client.GrpcChannel().get_HttpHandlerType()'),
    'pinvoke': ('[Dn2Cpp.Runtime.Obfuscate, System.Runtime.InteropServices.DllImport("missing")] public static extern int Target();', 'Target()'),
    'async': ('[Dn2Cpp.Runtime.Obfuscate] public static async System.Threading.Tasks.Task<int> Target() { await System.Threading.Tasks.Task.Yield(); return 1; }', 'Target().GetAwaiter().GetResult()'),
    'iterator': ('[Dn2Cpp.Runtime.Obfuscate] public static System.Collections.Generic.IEnumerable<int> Target() { yield return 1; }', 'Target().GetEnumerator().MoveNext() ? 1 : 0'),
    'abstract': ('public abstract class Base { [Dn2Cpp.Runtime.Obfuscate] public abstract int Target(); } public sealed class Child : Base { public override int Target() => 1; } public static int Call(Base value) => value.Target();', 'Call(new Child())'),
}
for name, (body, call) in cases.items():
    directory = root / name
    directory.mkdir(exist_ok=True)
    (directory / 'Probe.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>disable</ImplicitUsings></PropertyGroup></Project>', encoding='utf-8')
    prefix = 'namespace System { static class Math { [Dn2Cpp.Runtime.Obfuscate] public static int Abs(int value) => value < 0 ? -value : value; } }' if name == 'intrinsic' else ''
    if name == 'bodyreplace':
        prefix = 'namespace Grpc.Net.Client { public class GrpcChannel { [Dn2Cpp.Runtime.Obfuscate] public int get_HttpHandlerType() => 1; } }'
    (directory / 'Program.cs').write_text(prefix + 'namespace Dn2Cpp.Runtime { [System.AttributeUsage(System.AttributeTargets.Method)] sealed class ObfuscateAttribute : System.Attribute {} }\n'
        + 'class Program { ' + body + ' public static void Main() { System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture; System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture; System.Console.WriteLine(' + call + '); } }', encoding='utf-8')
PYGEN
    local kind reason log
    for kind in empty pinvoke async iterator abstract intrinsic bodyreplace; do
        build_gate_proj "$negative/$kind/Probe.csproj"
        case "$kind" in
            empty) reason='requires at least one reachable' ;;
            intrinsic|bodyreplace) reason='implementation is replaced by an intrinsic or backend' ;;
            pinvoke) reason='P/Invoke has no managed implementation body' ;;
            async) reason='async methods are not supported' ;;
            iterator) reason='iterator methods are not supported' ;;
            abstract) reason='abstract methods have no implementation body' ;;
        esac
        for mode in stripped original; do
            local flags=()
            [ "$mode" = original ] && flags+=(--no-ildiet)
            log="$negative/$kind/$mode.log"
            rm -rf "$negative/$kind/$mode"
            # Seed -o with a complete earlier transpile: the refusal must remove its
            # header too, not leave it beside chunks the refused run streamed.
            if [ "$kind" = empty ]; then
                invoke_cli "$negative/$kind/bin/$CONFIG/$TFM/Probe.dll" -r "$_CG_CORELIB" \
                    ${flags[@]+"${flags[@]}"} -o "$negative/$kind/$mode" >"$log" 2>&1 \
                    && [ -f "$negative/$kind/$mode/generated.h" ] || { cat "$log"; return 1; }
            fi
            if invoke_cli "$negative/$kind/bin/$CONFIG/$TFM/Probe.dll" -r "$_CG_CORELIB" \
                --obfuscate ${flags[@]+"${flags[@]}"} -o "$negative/$kind/$mode" >"$log" 2>&1; then
                echo "FAIL: obfuscation accepted $kind ($mode)" >&2
                return 1
            fi
            grep -F "$reason" "$log" || { cat "$log"; return 1; }
            ! compgen -G "$negative/$kind/$mode/generated*" >/dev/null \
                || { echo "FAIL: refused $kind ($mode) left C++: $(ls -1 "$negative/$kind/$mode" | tr '\n' ' ')" >&2; return 1; }
        done
    done

    "$PYTHON" - "$negative" <<'PYCROSS'
import pathlib, sys
root = pathlib.Path(sys.argv[1])
lib = root / 'cross-lib'
lib.mkdir(exist_ok=True)
(lib / 'RefLibrary.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>', encoding='utf-8')
(lib / 'Library.cs').write_text("""namespace Dn2Cpp.Runtime { [System.AttributeUsage(System.AttributeTargets.Method)] public sealed class ObfuscateAttribute : System.Attribute {} }
namespace System { public static class Math { [Dn2Cpp.Runtime.Obfuscate] public static int Abs(int value) => value < 0 ? -value : value; } }
public interface IValue { [Dn2Cpp.Runtime.Obfuscate] int Target(); }
public sealed class Value : IValue { public int Target() => 1; }
""", encoding='utf-8')
for name, call in [('intrinsic', 'obfuscated::System.Math.Abs(-1)'), ('interface', '((obfuscated::IValue)new obfuscated::Value()).Target()')]:
    app = root / ('cross-' + name)
    app.mkdir(exist_ok=True)
    (app / 'Probe.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework></PropertyGroup><ItemGroup><ProjectReference Include="../cross-lib/RefLibrary.csproj" Aliases="obfuscated" /></ItemGroup></Project>', encoding='utf-8')
    (app / 'Program.cs').write_text('extern alias obfuscated; class Program { static void Main() { System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture; System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture; System.Console.WriteLine(' + call + '); } }', encoding='utf-8')
PYCROSS
    for kind in intrinsic interface; do
        build_gate_proj "$negative/cross-$kind/Probe.csproj"
        reason='implementation is replaced by an intrinsic or backend'
        [ "$kind" = interface ] && reason='abstract methods have no implementation body'
        for mode in stripped original; do
            local flags=()
            [ "$mode" = original ] && flags+=(--no-ildiet)
            log="$negative/cross-$kind/$mode.log"
            rm -rf "$negative/cross-$kind/$mode"
            if invoke_cli "$negative/cross-$kind/bin/$CONFIG/$TFM/Probe.dll" \
                -r "$negative/cross-lib/bin/$CONFIG/$TFM/RefLibrary.dll" -r "$_CG_CORELIB" \
                --obfuscate ${flags[@]+"${flags[@]}"} -o "$negative/cross-$kind/$mode" >"$log" 2>&1; then
                echo "FAIL: obfuscation accepted cross-assembly $kind ($mode)" >&2
                return 1
            fi
            grep -F "$reason" "$log" || { cat "$log"; return 1; }
            ! compgen -G "$negative/cross-$kind/$mode/generated*" >/dev/null \
                || { echo "FAIL: refused cross-assembly $kind ($mode) left C++: $(ls -1 "$negative/cross-$kind/$mode" | tr '\n' ' ')" >&2; return 1; }
        done
    done
    echo "refusal OK: every refused --obfuscate run left no generated sources, including over an earlier transpile"

    # Without a planning pass the real pass's compile rounds are the first to reach this
    # target: compiling the field copy mints Cell<int>, and the Array.Initialize lowering
    # reaches each minted value type's parameterless constructor. Its row, and the
    # refusal of a set with no target, follow those rounds.
    local late="$baseline-late" unplanned="$negative/empty-unplanned"
    mkdir -p "$late"
    "$PYTHON" - "$late" <<'PYLATE'
import pathlib, sys
root = pathlib.Path(sys.argv[1])
(root / 'Probe.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>disable</ImplicitUsings></PropertyGroup></Project>', encoding='utf-8')
(root / 'Program.cs').write_text("""namespace Dn2Cpp.Runtime { [System.AttributeUsage(System.AttributeTargets.Method | System.AttributeTargets.Constructor)] sealed class ObfuscateAttribute : System.Attribute {} }
struct Cell<T> { public int V; [Dn2Cpp.Runtime.Obfuscate] public Cell() { V = 7; } }
sealed class Holder { public Cell<int> F; public Cell<int> G; }
class Program {
    static int Move(Holder holder) { holder.G = holder.F; return 1; }
    static void Main() {
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
        var values = new int[2];
        values.Initialize();
        System.Console.WriteLine(values[0] + Move(new Holder()));
    }
}
""", encoding='utf-8')
PYLATE
    build_gate_proj "$late/Probe.csproj"
    for mode in stripped original; do
        local flags=()
        [ "$mode" = original ] && flags+=(--no-ildiet)
        out="$late/$mode"
        DN2CPP_STRICT_COMPLETION=1 invoke_cli "$late/bin/$CONFIG/$TFM/Probe.dll" -r "$_CG_CORELIB" \
            --obfuscate --no-shared-generics ${flags[@]+"${flags[@]}"} -o "$out"
        "$PYTHON" - "$out" <<'PY'
import json, pathlib, re, sys
root = pathlib.Path(sys.argv[1])
rows = json.loads((root / 'obfuscation-targets.json').read_text(encoding='utf-8'))['targets']
assert len(rows) == 1 and rows[0]['managedMethod'].endswith('::.ctor():Void'), rows
source = (root / rows[0]['cppFile']).read_text(encoding='utf-8')
assert re.search(r'DN2CPP_NOINLINE [^\n]*\b' + re.escape(rows[0]['implementationSymbol']) + r'\([^\n]*\)\n\{', source), rows
PY
        log="$unplanned-$mode.log"
        rm -rf "$unplanned-$mode"
        invoke_cli "$negative/empty/bin/$CONFIG/$TFM/Probe.dll" -r "$_CG_CORELIB" --no-shared-generics \
            ${flags[@]+"${flags[@]}"} -o "$unplanned-$mode" >"$log" 2>&1 \
            && [ -f "$unplanned-$mode/generated.h" ] || { cat "$log"; return 1; }
        if invoke_cli "$negative/empty/bin/$CONFIG/$TFM/Probe.dll" -r "$_CG_CORELIB" --no-shared-generics \
            --obfuscate ${flags[@]+"${flags[@]}"} -o "$unplanned-$mode" >"$log" 2>&1; then
            echo "FAIL: obfuscation accepted empty under --no-shared-generics ($mode)" >&2
            return 1
        fi
        grep -F 'requires at least one reachable' "$log" || { cat "$log"; return 1; }
        ! compgen -G "$unplanned-$mode/generated*" >/dev/null \
            || { echo "FAIL: refused empty under --no-shared-generics ($mode) left C++: $(ls -1 "$unplanned-$mode" | tr '\n' ' ')" >&2; return 1; }
    done
    compile_console "$late/stripped" Probe
    assert_output "$(run_bounded "./$late/stripped/Probe")" "$(run_bounded dotnet "$late/bin/$CONFIG/$TFM/Probe.dll")"
    echo "compile-round target OK: under --no-shared-generics a target a compile round reaches first keeps its row, and an empty target set leaves no generated sources"
}
corelib_diff_gate Obfuscation
