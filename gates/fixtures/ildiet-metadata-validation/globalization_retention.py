"""Absent native PAL imports retain members and preserve explicit bindings."""
from pathlib import Path
import json
import os
import re
import subprocess

from ildiet_manifest import manifest


def validate_globalization(probe, cli, original, directory):
    validate_unavailable_pal(probe, cli, original, directory, kernel32=False)


def validate_kernel32(probe, cli, original, directory):
    validate_unavailable_pal(probe, cli, original, directory, kernel32=True)


def validate_unavailable_pal(probe, cli, original, directory, *, kernel32):
    subject = "kernel32" if kernel32 else "globalization"
    owner = "Kernel32" if kernel32 else "Globalization"
    module = "QCall" if kernel32 else "libSystem.Globalization.Native"
    binding_module = module[3:] if module.startswith("lib") else module
    prefix = "" if kernel32 else "GlobalizationNative_"
    first = "CloseHandle" if kernel32 else "GetCalendars"
    address = "SetEvent" if kernel32 else "AddressOnly"
    refusal = "requires the CoreCLR runtime" if kernel32 else "requires the ICU globalization PAL"
    kinds = ("retained", "called", "managed", "application", "unrelated", "native" if kernel32 else "alias")
    if kernel32:
        kinds += ("other-owner", "primitive")

    def run(arguments, success=True):
        result = subprocess.run(["dotnet", "exec", *map(str, arguments)], capture_output=True, text=True,
                                env={**os.environ, "DN2CPP_STRICT_COMPLETION": "1"})
        assert (result.returncode == 0) == success, (arguments, result.returncode, result.stdout, result.stderr)
        return result

    inputs = {}
    for kind in kinds:
        source = directory / (subject + "-" + kind)
        result = run([probe, "--create-stripping-" + subject + "-fixture", original, source, kind])
        assert "stripping-fixture=" + subject + ":" + kind in result.stdout
        inputs[kind] = source

    records = []

    def emit(kind, mode, suffix="", level="medium", direct=None, success=True):
        source = inputs[kind]
        output = directory / ("emission-" + subject + "-" + kind + "-" + mode + suffix)
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
            assert root.get("fallback") == "none", "Absent-PAL fixture used fallback copying"
            assert (published / "System.Private.CoreLib.dll").read_bytes() == (source / "System.Private.CoreLib.dll").read_bytes()
        text = "\n".join(p.read_text() for p in [output / "generated.h", *output.glob("generated*.cpp")])
        assert text, "Absent-PAL fixture generated no C++"
        return output, text

    def no_native_binding(output):
        for name in ("pinvoke-libs.txt", "pinvoke-symbols.txt"):
            path = output / name
            assert not path.exists() or binding_module.lower() not in path.read_text().lower(), (output, name)

    for kind in (("retained", "called", "managed", "other-owner") if kernel32 else ("retained", "called", "alias", "managed")):
        for mode in ("raw", "full"):
            output, text = emit(kind, mode)
            emitted_owner = "QCallCarrier" if kind == "other-owner" else owner
            for member in ("CallImport", "AddressImport", "CallBuffer"):
                assert re.search(r"\b" + emitted_owner + "_" + member + r"_\w+\([^;]*?\)\s*\{", text), member
                assert re.search(r"\(void\*\)&" + emitted_owner + "_" + member + r"_\w+, \(void\*\)&inv_", text), \
                    "Medium discarded the retained method invoker: " + member
            for access in ("fldget", "fldset"):
                assert re.search("&" + access + "_\\w*" + emitted_owner + "_f_RetainedField", text), \
                    "Medium discarded the retained field metadata"
            assert refusal in text, "No callable absent-PAL refusal was emitted"
            no_native_binding(output)
            if kind == "managed":
                for entry in (first, address):
                    assert re.search(r"\b\w+_Managed" + entry + r"_\w+\([^;]*?\)\s*\{", text), entry
                    assert "the native import " + module + "!" + prefix + entry not in text, \
                        "NativeImplementation was replaced by a PAL refusal"

    # The address-only import has no direct call: High must discover its managed implementation
    # from ldftn rather than relying on Medium's retention of the implementation owner.
    for mode in ("raw", "full"):
        output, text = emit("managed", mode, "-high", level="high")
        assert "Managed" + address in text and refusal not in text
        no_native_binding(output)

    for index, selector in enumerate((module, module + "!" + prefix + first, "*")):
        output, text = emit("retained", "raw", "-direct-" + str(index), direct=selector)
        symbols = (output / "pinvoke-symbols.txt").read_text()
        assert prefix + first in symbols
        assert binding_module.lower() in (output / "pinvoke-libs.txt").read_text().lower()
        assert "the native import " + module + "!" + prefix + first not in text
        managed_output, managed = emit("managed", "raw", "-direct-" + str(index), direct=selector)
        assert "the native import " + module + "!" + prefix + first not in managed
        assert "Managed" + first in managed and "Managed" + address in managed
        managed_symbols = managed_output / "pinvoke-symbols.txt"
        symbols = managed_symbols.read_text() if managed_symbols.exists() else ""
        for entry in (first, address):
            assert prefix + entry not in symbols, "Explicit native binding bypassed NativeImplementation"

    output, text = emit("application", "raw")
    assert refusal not in text, "An application lookalike was intercepted"
    assert "dn2cpp_pinvoke_resolve" in text, "Application imports lost resolver-first binding"
    no_native_binding(output)
    _, diagnostic = emit("unrelated", "raw", success=False)
    assert "InternalCall/extern method" in diagnostic and first in diagnostic, diagnostic
    if kernel32:
        for level in ("high", "medium"):
            for mode in ("raw", "full"):
                output, text = emit("primitive", mode, "-" + level, level=level)
                assert re.search(r"void Buffer_ZeroMemoryInternal_\w+\([^;]*?\)\s*\{[^}]*memset", text), \
                    "Address-taken QCall primitive lost its existing zeroing implementation"
                assert "the native import QCall!Buffer_ZeroMemory" not in text
                no_native_binding(output)
        output, text = emit("native", "raw")
        assert refusal not in text, "Real kernel32.dll imports were intercepted"
        assert "CloseHandle" in (output / "pinvoke-symbols.txt").read_text()
        assert "kernel32" in (output / "pinvoke-libs.txt").read_text().lower()
    (directory / (subject + "-results.json")).write_text(json.dumps(records, indent=2) + "\n")
    print("stripping-" + subject + "=retained-members,callable-refusal,pointer-array,address-only,explicit-bindings,application-negative", flush=True)
