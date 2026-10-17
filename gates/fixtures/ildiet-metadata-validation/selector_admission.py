"""Pin closed original-primary parameter-vector admission without sharing."""
from pathlib import Path
import os
import re
import subprocess


MODES = ("symbolic-first", "symbolic-late", "literal-complete", "null-mixed", "null-literal-complete")


def check_selector_admission(cli, original, corelib, output, mode):
    assert mode in MODES, "unknown selector admission mode: " + mode
    destination = Path(output)
    destination.mkdir(parents=True, exist_ok=True)
    # Inline header bodies and split units must both come from this emission.
    for pattern in ("generated*.cpp", "generated*.h"):
        for path in destination.glob(pattern):
            path.unlink()
    command = ["dotnet", "exec", str(cli), str(original / "GraphApp.dll"),
               "-r", str(original / "GraphLib.dll"), "-r", str(corelib),
               "--no-ildiet", "--no-shared-generics", "-o", str(destination)]
    result = subprocess.run(command, capture_output=True, env=os.environ.copy())
    (destination / "primary.stdout.log").write_bytes(result.stdout)
    (destination / "primary.stderr.log").write_bytes(result.stderr)
    assert result.returncode == 0, ("selector primary emission failed", mode, result.returncode,
                                    result.stdout.decode("utf-8", errors="replace"),
                                    result.stderr.decode("utf-8", errors="replace"))
    assert b"dn2cpp: shared-generics:" not in result.stdout, "selector primary unexpectedly used sharing: " + mode
    units = sorted(destination.glob("generated*.cpp"))
    assert units, "selector primary emission produced no translation units: " + mode
    bodies = "\n".join(path.read_text(encoding="utf-8") for path in units + sorted(destination.glob("generated*.h")))

    def definitions(symbol):
        return list(re.finditer(r"(?m)^[A-Za-z_][A-Za-z0-9_* ]* (" + symbol +
                                r"_m[0-9]+)\(([^;\n]*)\)\s*\{", bodies))

    assert definitions("Entry_Main") and definitions("SelectorDriver_Start"), "selector primary lost its actual entry/start bodies: " + mode
    helpers = definitions(r"SelectorDriver_Read[A-Za-z0-9_]*")
    assert len(helpers) == (2 if mode == "symbolic-late" else 1), "selector primary lost an exact closed helper body: " + mode
    assert bool(definitions("SelectorDriver_Schedule")) == (mode == "symbolic-late"), "selector primary lost its delayed-binding schedule: " + mode
    runs = definitions("SelectorOwner_Run")

    def target(arity):
        return [match for match in runs if len(re.findall(r"\bint32_t\b", match.group(2))) == arity
                and (arity != 0 or not match.group(2).strip())]

    arity = 3 if mode.startswith("symbolic-") else 1 if mode == "literal-complete" else 0
    selected = mode != "null-literal-complete"
    assert bool(target(arity)) == selected, "primary selector target body admission differed: " + mode + ":Run/" + str(arity)
    leaf = "Three" if arity == 3 else "One" if arity == 1 else "Zero"
    assert bool(definitions("Selector" + leaf + "Dependency_Touch")) == selected, "primary selector dependency admission differed: " + mode + ":" + leaf
    if mode == "literal-complete":
        assert not target(3) and not definitions("SelectorThreeDependency_Touch"), "primary complete vector opened the three-parameter overload"
    assert not definitions("SelectorOwner_Unselected") and not definitions("SelectorUnselectedDependency_Touch"), "primary selector lookup opened an unrelated name: " + mode
    print("graph-selector-admission=" + mode + ":closed-primary,selected-bodies,unselected-negative", flush=True)
