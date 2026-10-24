"""Compare reflection admission with and without canonical method discovery."""
from preserve_profile import sharing_modes
from pathlib import Path
import os
import re
import subprocess
import hashlib


FAMILIES = ("canonical-array", "canonical-made-array", "canonical-generic", "canonical-definition")
MODES = tuple(family + "-" + variant for family in FAMILIES for variant in ("unknown", "complete"))
OWNER_MODES = ("accessor-unknown", "accessor-complete", "object-unknown", "object-complete",
               "object-generic-unknown", "object-generic-complete")
CONTEXT_FAMILIES = ("contextual-method-array", "contextual-field-array", "contextual-own-array",
                    "template-array", "template-name-array", "template-base", "template-default-members")
CONTEXT_MODES = tuple(family + "-" + variant for family in CONTEXT_FAMILIES for variant in ("unknown", "complete"))
VIEW_FAMILIES = ("tuple-fixed", "tuple-index", "view-field", "view-property", "view-parameter", "view-accessor",
                 "template-view-return", "template-view-parameter", "interface-open-enumeration", "interface-open-named")
VIEW_MODES = tuple(family + "-" + variant for family in VIEW_FAMILIES for variant in ("unknown", "complete")) + \
    ("tuple-owner-complete", "interface-fixed-enumeration-complete", "interface-fixed-named-complete")
BOUND_FAMILIES = ("tuple-object", "tuple-method-closure")
BOUND_MODES = tuple(family + "-" + variant for family in BOUND_FAMILIES for variant in ("unknown", "complete")) + \
    ("tuple-method-owner-complete",)
ORIGIN_MODES = ("boxed-vector-unknown", "boxed-vector-complete", "null-vector-complete")
DEFINITION_MODES = ("definition-element-unknown", "definition-element-complete")
LOADSET_MODES = ("primitive-loadset-unknown", "primitive-loadset-complete")
MODIFIER_LOADSET_MODES = ("modifier-loadset-unknown", "modifier-loadset-complete")
ALL_LOADSET_MODES = LOADSET_MODES + MODIFIER_LOADSET_MODES


def loadset_references(original, corelib, mode):
    assert mode in ALL_LOADSET_MODES, "unknown primitive loadset mode: " + mode
    return [original / "GraphLib.dll"] + ([corelib] if mode.endswith("-complete") else [])


def check_loadset_inventory(root, published, corelib, mode):
    expected = ["GraphLib.dll"] + ([corelib.name] if mode.endswith("-complete") else [])
    assert [Path(item.get("path")).name for item in root.findall("reference")] == expected, "primitive loadset manifest added or omitted a reference: " + mode
    assert Path(root.get("input")).name == "GraphApp.dll", "primitive loadset changed its input assembly"
    inventory = {item.get("name"): item.get("mode") for item in root.findall("assembly")}
    expected_inventory = {"GraphApp": "rewritten", "GraphLib": "rewritten"}
    if mode.endswith("-complete"):
        expected_inventory[corelib.stem] = "copied"
        assert (published / corelib.name).read_bytes() == corelib.read_bytes(), "primitive loadset changed its copied CoreLib"
    assert inventory == expected_inventory, "primitive loadset manifest changed its exact assembly inventory: " + mode


def check_identical_loadset_inputs(first, second):
    for filename in ("GraphApp.dll", "GraphLib.dll"):
        assert hashlib.sha256((first / filename).read_bytes()).digest() == hashlib.sha256((second / filename).read_bytes()).digest(), "primitive loadset pair changed an original input: " + filename


def loadset_closure_arguments(published, corelib, mode):
    assert mode in ALL_LOADSET_MODES, "unknown loadset closure mode: " + mode
    modules = sorted(published.glob("*.dll"))
    return (("--check-token-closure-external-corelib", corelib) if mode.endswith("-unknown") else ("--check-token-closure",)) + tuple(modules)


def check_loadset_closure_marker(output, mode):
    prefix = b"token-closure=valid-external-corelib operands=" if mode.endswith("-unknown") else b"token-closure=valid operands="
    assert output.startswith(prefix), "loadset operand closure inspection did not run: " + mode


def check_primary_admission(cli, original, corelib, output, mode):
    assert mode in MODES + OWNER_MODES + CONTEXT_MODES + VIEW_MODES + BOUND_MODES + ORIGIN_MODES + DEFINITION_MODES + ALL_LOADSET_MODES, "unknown canonical admission mode: " + mode
    delegator = mode in ("object-unknown", "object-generic-unknown")
    for shared in sharing_modes(mode in ("canonical-array-unknown", "canonical-array-complete")):
        arm = "sharing-on" if shared else "sharing-off"
        destination = Path(output) / arm
        destination.mkdir(parents=True, exist_ok=True)
        # A stale translation unit must never supply the body being asserted.
        for pattern in ("generated*.cpp", "generated*.h"):
            for path in destination.glob(pattern):
                path.unlink()
        references = loadset_references(original, corelib, mode) if mode in ALL_LOADSET_MODES else [original / "GraphLib.dll", corelib]
        command = ["dotnet", "exec", str(cli), str(original / "GraphApp.dll")]
        for reference in references:
            command.extend(("-r", str(reference)))
        command.extend(("--no-ildiet", "--shared-generics" if shared else "--no-shared-generics", "-o", str(destination)))
        if delegator:
            # Bound the unsupported base constructor only for C++ admission, not runtime parity.
            command.extend(("--cut", "System.Reflection.TypeInfo::.ctor"))
        if mode in ALL_LOADSET_MODES:
            assert (corelib in references) == mode.endswith("-complete") and "--auto-ref" not in command, "primitive primary changed its exact reference contract"
            (destination / "primary-command.txt").write_text("\n".join(command) + "\n", encoding="utf-8")
        result = subprocess.run(command, capture_output=True, env=os.environ.copy())
        (destination / "primary.stdout.log").write_bytes(result.stdout)
        (destination / "primary.stderr.log").write_bytes(result.stderr)
        assert result.returncode == 0, ("canonical primary emission failed", mode, arm, result.returncode,
                                        result.stdout.decode("utf-8", errors="replace"),
                                        result.stderr.decode("utf-8", errors="replace"))
        units = sorted(destination.glob("generated*.cpp"))
        assert units, "canonical primary emission produced no translation units: " + mode + ":" + arm
        # Inline-promoted definitions live in generated.h; split bodies live in
        # the generated translation units. Both are admission evidence.
        files = units + sorted(destination.glob("generated*.h"))
        bodies = "\n".join(path.read_text(encoding="utf-8") for path in files)
        assert (b"dn2cpp: shared-generics:" in result.stdout) == shared, "primary sharing receipt differed: " + mode + ":" + arm
        if mode in ALL_LOADSET_MODES:
            assemblies = 2 + int((Path(cli).parent / "Dn2Cpp.Runtime.dll").is_file()) + int(mode.endswith("-complete"))
            assert re.search(rb"(?m)^dn2cpp: " + str(assemblies).encode() + rb" assemblies,", result.stdout), "primitive primary loaded an unexpected assembly count: " + mode + ":" + arm

        def definition(symbol):
            return re.search(r"(?m)^[A-Za-z_][A-Za-z0-9_* ]* " + symbol +
                             r"_m[0-9]+\([^;\n]*\)\s*\{", bodies) is not None

        if delegator:
            assert definition("TypeDelegator__ctor"), "TypeDelegator's actual constructor body was lost: " + mode + ":" + arm
        entry = re.search(r"(?m)^[A-Za-z_][A-Za-z0-9_* ]* Entry_Main_m[0-9]+\([^;\n]*\)\s*\{([^}]+)\}", bodies)
        assert entry, "actual Entry.Main body was not emitted: " + mode + ":" + arm
        calls = re.findall(r"\b(UncertaintyDriver(?:_1)?_Read(?:_Tis(?:String|CnRef))?_m[0-9]+)\(", entry.group(1))
        generic_method = mode.startswith(("canonical-", "contextual-method-", "contextual-field-", "tuple-")) \
            and not mode.startswith(("tuple-object-",))
        assert len(calls) == 1 and (shared or not generic_method or calls[0].startswith("UncertaintyDriver_Read_TisString_m")), \
            "Entry.Main lost its actual generic Read call: " + mode + ":" + arm
        assert re.search(r"(?m)^[A-Za-z_][A-Za-z0-9_* ]* " + re.escape(calls[0]) + r"\([^;\n]*\)\s*\{", bodies), \
            "Entry.Main's actual Read target has no body definition: " + mode + ":" + arm
        for symbol in ("UncertaintyMarkerAttribute__ctor", "UncertaintyMarkerAttribute_set_Label",
                       "UncertaintyCtorDependency_Touch", "UncertaintySetterDependency_Touch") + \
                (("UncertaintyMarkerAttribute_get_Label",) if mode in OWNER_MODES else ()) + \
                (("KnownPropertyOwner_get_Value",) if mode.startswith("accessor-") else ()):
            assert not definition(symbol), "unknown attribute owner admitted an unrelated body: " + mode + ":" + arm + ":" + symbol
        for symbol in ("UncertaintyHost_Annotated", "UncertaintyAnnotatedDependency_Touch") + (("KnownOwner_Available",) if mode in ALL_LOADSET_MODES or mode.startswith("object-") else ()):
            assert not definition(symbol), "canonical attribute selection executed its provider body: " + mode + ":" + arm
        label = "graph-loadset-admission=" if mode in ALL_LOADSET_MODES else "graph-definition-admission=" if mode in DEFINITION_MODES else "graph-origin-admission=" if mode in ORIGIN_MODES else "graph-bound-admission=" if mode in BOUND_MODES else "graph-metadata-admission=" if mode in VIEW_MODES else "graph-canonical-admission="
        print(label + mode + ":" + arm + ":actual-entry-read-body,no-attribute-dependencies,provider-nonexecution", flush=True)
