"""Retained ICU imports remain callable refusals without replacing explicit bindings."""
from pathlib import Path
import json
import os
import re
import subprocess

from ildiet_manifest import manifest


def validate_globalization(probe, cli, original, directory):
    def run(arguments, success=True):
        result = subprocess.run(["dotnet", "exec", *map(str, arguments)], capture_output=True, text=True,
                                env={**os.environ, "DN2CPP_STRICT_COMPLETION": "1"})
        assert (result.returncode == 0) == success, (arguments, result.returncode, result.stdout, result.stderr)
        return result

    inputs = {}
    for kind in ("retained", "called", "managed", "application", "unrelated", "alias"):
        source = directory / ("globalization-" + kind)
        result = run([probe, "--create-stripping-globalization-fixture", original, source, kind])
        assert "stripping-fixture=globalization:" + kind in result.stdout
        inputs[kind] = source

    records = []

    def emit(kind, mode, suffix="", level="medium", direct=None, success=True):
        source = inputs[kind]
        output = directory / ("emission-globalization-" + kind + "-" + mode + suffix)
        arguments = [cli, source / "MediumApp.dll", "-r", source / "System.Private.CoreLib.dll",
                     "-r", source / "MediumLib.dll", "--stripping-level", level,
                     "--no-metadata-compression", "--no-shared-generics", "-o", output]
        if mode == "raw":
            arguments.append("--no-ildiet")
        if direct is not None:
            arguments += ["--direct-pinvoke", direct]
        result = run(arguments, success)
        record = {"kind": kind, "mode": mode, "level": level, "direct": direct,
                  "command": list(map(str, arguments)), "exit": result.returncode,
                  "stdout": result.stdout, "stderr": result.stderr}
        records.append(record)
        if not success:
            return output, result.stdout + result.stderr
        if mode == "full":
            root, published = manifest(output / "ildiet")
            assert root.get("fallback") == "none", "Globalization fixture used fallback copying"
            assert (published / "System.Private.CoreLib.dll").read_bytes() == (source / "System.Private.CoreLib.dll").read_bytes()
        text = "\n".join(p.read_text() for p in [output / "generated.h", *output.glob("generated*.cpp")])
        assert text, "Globalization fixture generated no C++"
        return output, text

    def no_native_binding(output):
        for name in ("pinvoke-libs.txt", "pinvoke-symbols.txt"):
            path = output / name
            assert not path.exists() or "globalization" not in path.read_text().lower(), (output, name)

    for kind in ("retained", "called", "alias", "managed"):
        for mode in ("raw", "full"):
            output, text = emit(kind, mode)
            for member in ("CallCalendars", "AddressCalendars", "CallSortKey"):
                assert re.search(r"\bGlobalization_" + member + r"_\w+\([^;]*?\)\s*\{", text), member
                assert re.search(r"\(void\*\)&Globalization_" + member + r"_\w+, \(void\*\)&inv_", text), \
                    "Medium discarded the retained method invoker: " + member
            for access in ("fldget", "fldset"):
                assert "&" + access + "_Interop_Globalization_f_RetainedField" in text, \
                    "Medium discarded the retained field metadata"
            assert "requires the ICU globalization PAL" in text, "No callable ICU refusal was emitted"
            no_native_binding(output)
            if kind == "managed":
                for entry in ("GetCalendars", "AddressOnly"):
                    assert re.search(r"\b\w+_Managed" + entry + r"_\w+\([^;]*?\)\s*\{", text), entry
                    assert "the native import libSystem.Globalization.Native!GlobalizationNative_" + entry not in text, \
                        "NativeImplementation was replaced by an ICU refusal"

    # AddressOnly has no direct call: High must discover its managed implementation
    # from ldftn rather than relying on Medium's retention of the implementation owner.
    for mode in ("raw", "full"):
        output, text = emit("managed", mode, "-high", level="high")
        assert "ManagedAddressOnly" in text and "requires the ICU globalization PAL" not in text
        no_native_binding(output)

    for index, selector in enumerate(("libSystem.Globalization.Native", "libSystem.Globalization.Native!GlobalizationNative_GetCalendars", "*")):
        output, text = emit("retained", "raw", "-direct-" + str(index), direct=selector)
        symbols = (output / "pinvoke-symbols.txt").read_text()
        assert "GlobalizationNative_GetCalendars" in symbols
        assert "globalization.native" in (output / "pinvoke-libs.txt").read_text().lower()
        assert "the native import libSystem.Globalization.Native!GlobalizationNative_GetCalendars" not in text
        managed_output, managed = emit("managed", "raw", "-direct-" + str(index), direct=selector)
        assert "the native import libSystem.Globalization.Native!GlobalizationNative_GetCalendars" not in managed
        assert "ManagedGetCalendars" in managed and "ManagedAddressOnly" in managed
        managed_symbols = managed_output / "pinvoke-symbols.txt"
        symbols = managed_symbols.read_text() if managed_symbols.exists() else ""
        for entry in ("GetCalendars", "AddressOnly"):
            assert "GlobalizationNative_" + entry not in symbols, "Explicit native binding bypassed NativeImplementation"

    output, text = emit("application", "raw")
    assert "requires the ICU globalization PAL" not in text, "An application lookalike was intercepted"
    assert "dn2cpp_pinvoke_resolve" in text, "Application imports lost resolver-first binding"
    no_native_binding(output)
    _, diagnostic = emit("unrelated", "raw", success=False)
    assert "InternalCall/extern method" in diagnostic and "GetCalendars" in diagnostic, diagnostic
    (directory / "globalization-results.json").write_text(json.dumps(records, indent=2) + "\n")
    print("stripping-globalization=retained-members,callable-refusal,pointer-array,address-only,explicit-bindings,application-negative", flush=True)
