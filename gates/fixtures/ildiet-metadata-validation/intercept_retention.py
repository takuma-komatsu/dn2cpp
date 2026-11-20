"""Retained member-specific cuts keep callable bodies without scanning replaced IL."""
from pathlib import Path
import json
import os
import re
import subprocess


def validate_intercept_retention(probe, cli, original, directory, modes=("raw", "full")):
    def run(arguments):
        result = subprocess.run(["dotnet", "exec", *map(str, arguments)], capture_output=True, text=True,
                                env={**os.environ, "DN2CPP_STRICT_COMPLETION": "1"})
        assert result.returncode == 0, (arguments, result.returncode, result.stdout, result.stderr)
        return result

    records = []
    for kind in ("retained", "called"):
        source = directory / ("intercept-" + kind)
        result = run([probe, "--create-stripping-intercept-fixture", original, source, kind])
        assert "stripping-fixture=intercept:" + kind in result.stdout
        for level in ("high", "medium"):
            for mode in modes:
                output = directory / ("emission-intercept-" + kind + "-" + level + "-" + mode)
                arguments = [cli, source / "MediumApp.dll", "-r", source / "System.Private.CoreLib.dll",
                             "-r", source / "MediumLib.dll", "--stripping-level", level,
                             "--no-metadata-compression", "--no-shared-generics", "-o", output]
                if mode == "raw":
                    arguments.append("--no-ildiet")
                result = run(arguments)
                text = "\n".join(p.read_text() for p in [output / "generated.h", *output.glob("generated*.cpp")])
                assert "RawBodyOnly" not in text, "Retention scanned the replaced body"
                for member in ("BlockCopy", "ByteLength", "GetByte", "SetByte"):
                    body = re.search(r"\bBuffer_" + member + r"_\w+\([^;]*?\)\s*\{", text)
                    assert bool(body) == (level == "medium" or kind == "called"), (kind, level, member)
                    if level == "medium":
                        assert re.search(r"\(void\*\)&Buffer_" + member + r"_\w+, \(void\*\)&inv_", text), \
                            "Retained cut member lacks a reflection invoker: " + member
                if kind == "retained":
                    assert not re.search(r"\bTplEventSource_CreateGuidForTaskID_\w+", text), \
                        "Type-wide retention emitted a refused provider callable"
                    scope = re.search(r"\bTplEventSource_ScopeId_\w+\([^;]*?\)\s*\{", text)
                    assert bool(scope) == (level == "medium"), "Wrong callable policy for a safe provider return"
                    casing = re.findall(r"\b(Rune_ChangeCaseCultureAware_\w+)\(([^;()]*)\)\s*\{", text)
                    assert len(casing) == (level == "medium") and all(not args for _, args in casing), \
                        "Rune's non-invariant helper became callable or its namesake was dropped"
                    if level == "medium":
                        table = re.search(r"\bmd_native_methtab_System_Threading_Tasks_TplEventSource\[\] = \{([^;]*)\};", text)
                        assert table, "Retained provider has no method metadata table"
                        # The GUID helper is the fixture's only one-argument Guid-returning provider member.
                        assert re.search(r"\{\s*md_name_\d+, &ti_System_Threading_Tasks_TplEventSource, &ti_System_Guid, "
                                         r"parmpool_\d+, 1LL, [^,]+, -1LL, 0, 0,", table[1]), \
                            "Refused provider helper lost its signature row or became invocable"
                        assert re.search(r"\(void\*\)&TplEventSource_ScopeId_\w+, \(void\*\)&inv_", table[1]), \
                            "Safe provider wrapper lost its reflection invoker"
                        rune = re.search(r"\bmd_native_methtab_System_Text_Rune\[\] = \{([^;]*)\};", text)
                        assert rune, "Retained Rune has no method metadata table"
                        helper = re.search(r"\{\s*md_name_\d+, &ti_System_Text_Rune, &ti_System_Text_Rune, "
                                           r"(parmpool_\d+), 3LL, [^,]+, -1LL, 0, 0,", rune[1])
                        assert helper, "Rune's non-invariant helper lost its signature row or became invocable"
                        parameters = re.search(r"\bmd_native_" + helper[1] + r"\[\] = \{([^;]*)\};", text)
                        assert parameters and re.findall(r"\{\s*(&\w+),", parameters[1]) == [
                            "&ti_System_Text_Rune", "&dn2cpp_textinfo_type", "&dn2cpp_bool_type"], \
                            "Rune's non-invariant helper lost its parameter types"
                        assert re.search(r"\(void\*\)&" + casing[0][0] + r", \(void\*\)&inv_", rune[1]), \
                            "Rune's supported namesake lost its reflection invoker"
                records.append({"kind": kind, "level": level, "mode": mode,
                                "command": list(map(str, arguments)), "exit": result.returncode})
    (directory / "intercept-results.json").write_text(json.dumps(records, indent=2) + "\n")
    print("stripping-intercepts=retained-callable-members,cut-body-absent,direct-and-address", flush=True)
    print("stripping-eventsource-retention=refused-signature-only,supported-callable", flush=True)
    print("stripping-rune-retention=non-invariant-signature-only,namesake-callable", flush=True)
