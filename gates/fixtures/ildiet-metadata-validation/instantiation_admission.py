"""Finite generic formations do not authorize an unknown argument vector."""
from preserve_profile import sharing_modes
from pathlib import Path
import hashlib
import os
import re
import subprocess


MODES = ("method-first", "method-only", "type-first", "type-late", "type-only", "method-known", "method-mixed", "method-static")


def check_instantiation_admission(cli, original, corelib, output, mode):
    assert mode in MODES, "unknown instantiation admission mode: " + mode
    original = Path(original)
    expected_hashes = {name: hashlib.sha256((original / name).read_bytes()).hexdigest()
                       for name in ("GraphApp.dll", "GraphLib.dll")}
    executes = not mode.endswith("-only")
    type_case = mode.startswith("type-")
    for shared in sharing_modes():
        arm = "sharing-on" if shared else "sharing-off"
        destination = Path(output) / arm
        destination.mkdir(parents=True, exist_ok=True)
        for pattern in ("generated*.cpp", "generated*.h"):
            for path in destination.glob(pattern):
                path.unlink()
        command = ["dotnet", "exec", str(cli), str(original / "GraphApp.dll"),
                   "-r", str(original / "GraphLib.dll"), "-r", str(corelib), "--no-ildiet",
                   "--shared-generics" if shared else "--no-shared-generics", "-o", str(destination)]
        result = subprocess.run(command, capture_output=True, env=os.environ.copy())
        (destination / "primary.stdout.log").write_bytes(result.stdout)
        (destination / "primary.stderr.log").write_bytes(result.stderr)
        assert result.returncode == 0, ("instantiation primary emission failed", mode, arm, result.returncode,
                                        result.stdout.decode("utf-8", errors="replace"),
                                        result.stderr.decode("utf-8", errors="replace"))
        if not shared:
            assert b"dn2cpp: shared-generics:" not in result.stdout, "instantiation primary unexpectedly used sharing: " + mode
        units = sorted(destination.glob("generated*.cpp"))
        assert units, "instantiation primary produced no translation units: " + mode + ":" + arm
        bodies = "\n".join(path.read_text(encoding="utf-8") for path in units + sorted(destination.glob("generated*.h")))

        def definitions(symbol):
            return list(re.finditer(r"(?m)^[A-Za-z_][A-Za-z0-9_* ]* (" + symbol +
                                    r"_m[0-9]+)\(([^;\n]*)\)\s*\{", bodies))

        assert len(definitions("Entry_Main")) == 1 and len(definitions("InstantiationDriver_FormKnown")) == 1, \
            "instantiation primary lost the actual entry or discarded formation body: " + mode + ":" + arm
        assert bool(definitions("InstantiationDriver_InvokeUnknown")) == executes, \
            "instantiation primary helper admission differed: " + mode + ":" + arm
        assert bool(definitions("InstantiationDriver_Schedule")) == mode.endswith("-late"), \
            "instantiation primary lost its delayed binding schedule: " + mode + ":" + arm
        owner = "InstantiationTypeFactory" if type_case else "InstantiationMethodFactory"
        concrete_body = mode in ("method-known", "method-mixed", "method-static")
        for symbol in (owner + r"[A-Za-z0-9_]*_Create[A-Za-z0-9_]*", "InstantiationPayload__ctor", "InstantiationCtorDependency_Touch"):
            assert bool(definitions(symbol)) == concrete_body, "instantiation primary selected body admission differed: " + mode + ":" + arm + ":" + symbol
        if executes and mode != "method-known":
            diagnostics = destination / "reflection-unrecognized.tsv"
            assert diagnostics.is_file() and b"Graph.InstantiationDriver::InvokeUnknown" in diagnostics.read_bytes(), (mode, arm, "unknown generic arguments need a diagnostic")
            assert b"ClosedGeneric\t" in diagnostics.read_bytes() or b"no method body retained" in diagnostics.read_bytes(), (mode, arm, "unknown execution needs a refusal remedy")
        if type_case:
            template_row = r"\{\s*&gendef_Graph_InstantiationTypeFactory_1,\s*&ti_Graph_InstantiationTypeFactory_[A-Za-z0-9_]+,"
            assert bool(re.search(template_row, bodies)) == shared, "instantiation primary runtime template selection differed: " + mode + ":" + arm
        for symbol in (r"InstantiationDormantFactory_Create[A-Za-z0-9_]*", "InstantiationDormantPayload__ctor",
                       "InstantiationDormantCtorDependency_Touch", "InstantiationDormantBodyDependency_Touch",
                       owner + r"[A-Za-z0-9_]*_Unselected[A-Za-z0-9_]*", "InstantiationUnselectedDependency_Touch",
                       "InstantiationUnusedProvider_Read", "InstantiationProviderDependency_Touch"):
            assert not definitions(symbol), "instantiation primary formation opened an unrelated body: " + mode + ":" + arm + ":" + symbol
        assert {name: hashlib.sha256((original / name).read_bytes()).hexdigest()
                for name in expected_hashes} == expected_hashes, "instantiation primary changed an original DLL: " + mode
        effect = "selected-constructor-dependency" if concrete_body else "template-without-concrete-bodies" if type_case and shared else "formation-only"
        print("graph-instantiation-admission=" + mode + ":" + arm +
              ":actual-entry,discarded-formation," + effect + ",dormant-and-provider-negative", flush=True)
