"""Pin original-primary admission for same-name parameter types in distinct assemblies."""
from pathlib import Path
import os
import re
import subprocess


def check_scope_admission(cli, original, corelib, output):
    for shared in (True, False):
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
        assert result.returncode == 0, ("scope primary emission failed", arm, result.returncode,
                                        result.stdout.decode("utf-8", errors="replace"),
                                        result.stderr.decode("utf-8", errors="replace"))
        assert (b"dn2cpp: shared-generics:" in result.stdout) == shared, "scope primary sharing receipt differed"
        units = sorted(destination.glob("generated*.cpp"))
        assert units, "scope primary produced no translation units"
        bodies = "\n".join(path.read_text(encoding="utf-8") for path in units + sorted(destination.glob("generated*.h")))

        def definitions(symbol):
            return list(re.finditer(r"(?m)^[A-Za-z_][A-Za-z0-9_* ]* (" + symbol +
                                    r"_m[0-9]+)\(([^;\n]*)\)\s*\{", bodies))

        entry = definitions("Entry_Main")
        assert len(entry) == 1, "scope primary lost its actual entry body"
        for name in ("Read", "Generic"):
            methods = definitions(r"ScopeMethods_[A-Za-z0-9_]*" + name + r"[A-Za-z0-9_]*")
            assert len(methods) == 2, "scope primary lost a distinct overload body: " + arm + ":" + name
            for module in (0, 1):
                assert sum("t_m" + str(module) + "_Graph_ScopePayload*" in method.group(2)
                           for method in methods) == 1, "scope primary merged parameter identities: " + arm + ":" + name
            for origin in ("Application", "Library"):
                assert len(definitions("Scope" + name + origin + "Dependency_Touch")) == 1, \
                    "scope primary lost an admitted unique dependency: " + arm + ":" + name + ":" + origin
        for symbol in ("ScopeUnusedProvider_Read", "ScopeReadUnusedDependency_Touch", "ScopeGenericUnusedDependency_Touch"):
            assert not definitions(symbol), "scope primary opened an unused provider body: " + arm + ":" + symbol
        assert "t_Graph_ScopeUnusedPayload*" not in bodies, "scope primary opened an unselected overload"
        print("graph-scope-admission=" + arm + ":actual-entry,four-overload-bodies,unique-external-dependencies,unselected-controls", flush=True)
