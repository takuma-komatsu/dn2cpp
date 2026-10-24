"""Array.Initialize admission from original value layouts and discarded formation."""
from preserve_profile import select_cases, sharing_modes, full_witness
from pathlib import Path
import hashlib
import json
import os
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

sys.path.insert(0, str(Path(__file__).resolve().parents[2]))
from ildiet_manifest import manifest

MODES = ("initialize-first", "layout-first", "unarmed")
DEPENDENCIES = ("ArrayLayoutCellDependency", "ArrayLayoutShellDependency",
                "ArrayLayoutNestedDependency", "ArrayLayoutMadeDependency")
NEGATIVES = ("ArrayLayoutOverloadDependency", "ArrayLayoutOrdinaryDependency",
             "ArrayLayoutReferenceDependency", "ArrayLayoutUnobservedDependency", "ArrayLayoutUnusedProvider")


def run(command, directory, label):
    result = subprocess.run(list(map(str, command)), capture_output=True, env=os.environ.copy())
    (directory / (label + ".stdout.log")).write_bytes(result.stdout)
    (directory / (label + ".stderr.log")).write_bytes(result.stderr)
    assert result.returncode == 0, (label, result.returncode, result.stdout.decode(errors="replace"),
                                    result.stderr.decode(errors="replace"))
    return result.stdout


def input_hashes(original, corelib):
    return {str(path.resolve()): hashlib.sha256(path.read_bytes()).hexdigest()
            for path in (original / "GraphApp.dll", original / "GraphLib.dll", Path(corelib))}


def check_array_layout_admission(cli, original, corelib, output, mode):
    assert mode in MODES
    original, output = Path(original), Path(output)
    output.mkdir(parents=True, exist_ok=True)
    hashes = input_hashes(original, corelib)
    receipts = []
    for integrated in (False, True):
        for shared in sharing_modes():
            label = ("integrated" if integrated else "original") + ("-sharing-on" if shared else "-sharing-off")
            destination = output / label
            command = ["dotnet", "exec", str(cli), str(original / "GraphApp.dll"), "-r", str(original / "GraphLib.dll"),
                       "-r", str(corelib), "--shared-generics" if shared else "--no-shared-generics", "-o", str(destination)]
            if not integrated:
                command += ["--no-ildiet"]
            stdout = run(command, output, label)
            if not shared:
                assert b"dn2cpp: shared-generics:" not in stdout, (mode, label)
            files = sorted(destination.glob("generated*.cpp")) + sorted(destination.glob("generated*.h"))
            assert files, (mode, label, "missing generated definitions")
            source = "\n".join(path.read_text(encoding="utf-8") for path in files)

            def defined(symbol):
                return bool(re.search(r"(?m)^[A-Za-z_][A-Za-z0-9_* ]* " + symbol + r"_m[0-9]+\([^;\n]*\)\s*\{", source))

            assert defined("ArrayLayoutEntry_Main") and defined(r"ArrayLayoutEntry_Move[A-Za-z0-9_]*") \
                and defined("ArrayLayoutEntry_Form"), (mode, label)
            observed = {name: defined(name + "_Touch") for name in DEPENDENCIES}
            template_row = r"\{\s*&gendef_Graph_ArrayLayoutMadeCell_1,\s*&ti_Graph_ArrayLayoutMadeCell_[A-Za-z0-9_]+,"
            made_template = bool(re.search(template_row, source))
            for name, present in observed.items():
                # Shared reflection formation can use a runtime template. The layout
                # and nongeneric shell witnesses are concrete in both primary modes.
                if mode == "unarmed":
                    assert not present, (mode, label, "unarmed constructor", name)
                elif name != "ArrayLayoutMadeDependency" or not shared:
                    assert present, (mode, label, "missing constructor dependency", name)
                elif not present:
                    assert made_template, (mode, label, "discarded formation emitted neither a concrete dependency nor its runtime template")
            for name in NEGATIVES:
                method = "Read" if name == "ArrayLayoutUnusedProvider" else "Touch"
                assert not defined(name + "_" + method), (mode, label, "opened metadata/provider body", name)
            if integrated:
                root, _ = manifest(destination / "ildiet")
                assert root.get("fallback") == "none", (mode, label)
                assert root.find("counters").get("decode.max-per-method") == "1", (mode, label)
            else:
                assert not (destination / "ildiet").exists(), (mode, label)
            assert input_hashes(original, corelib) == hashes, "primary modified an original array-layout input"
            receipts.append({"mode": mode, "arm": label, "command": command, "input_sha256": hashes,
                             "constructor_dependencies_emitted": observed, "made_cell_runtime_template": made_template,
                             "metadata_and_provider_negatives": True})
            print("graph-array-layout-admission=" + mode + ":" + label + ":actual-entry,layout,shell,formation,metadata-negatives", flush=True)
    (output / "admission.json").write_text(json.dumps(receipts, indent=2) + "\n")


def validate_array_layout(companion, probe, cli, corelib, directory):
    directory = Path(directory)
    directory.mkdir(parents=True, exist_ok=True)
    for mode in select_cases('array_layout', MODES, ('initialize-first', 'unarmed')):
        original = directory / (mode + "-input")
        created = run(["dotnet", "exec", probe, "--create-array-layout-fixtures", original, corelib, mode], directory, mode + "-create")
        assert created.splitlines() == [("fixture-created=array-layout mode=" + mode).encode()]
        checked = run(["dotnet", "exec", probe, "--check-array-layout-fixtures", original, mode, "original"], directory, mode + "-original")
        assert checked.startswith(("graph-array-layout=original:" + mode + ":").encode())
        hashes = input_hashes(original, corelib)
        generations = []
        for repeat in ("first", "repeat"):
            output = directory / (mode + "-" + repeat)
            request = ET.Element("ildiet", version="2", input=str(original / "GraphApp.dll"), output=str(output))
            ET.SubElement(request, "reference", path=str(original / "GraphLib.dll"))
            ET.SubElement(request, "reference", path=str(corelib))
            request_path = directory / (mode + "-" + repeat + "-request.xml")
            ET.ElementTree(request).write(request_path, encoding="utf-8", xml_declaration=True)
            run(["dotnet", "exec", companion, "--request", request_path], directory, mode + "-" + repeat)
            result, published = manifest(output)
            assert result.get("fallback") == "none", (mode, repeat)
            assert result.find("counters").get("decode.max-per-method") == "1", (mode, repeat)
            preservation = ET.parse(published / "preservation.xml").getroot()
            assert preservation.tag == "linker" and not preservation.attrib and len(preservation) == 0, "array layout introduced preservation roots"
            generations.append(({path.name: hashlib.sha256(path.read_bytes()).hexdigest()
                                 for path in published.iterdir() if path.is_file()}, dict(result.find("counters").attrib)))
            checked = run(["dotnet", "exec", probe, "--check-array-layout-fixtures", published, mode, "published"], directory, mode + "-" + repeat + "-check")
            assert checked.startswith(("graph-array-layout=published:" + mode + ":").encode())
            closure = run(["dotnet", "exec", probe, "--check-token-closure", *sorted(published.glob("*.dll"))], directory, mode + "-" + repeat + "-closure")
            assert b"token-closure=valid operands=" in closure
            for filename in ("GraphApp.dll", "GraphLib.dll"):
                before = set(run(["dotnet", "exec", probe, "--relations", original / filename], directory, mode + "-" + repeat + "-before-" + filename).splitlines())
                after = set(run(["dotnet", "exec", probe, "--relations", published / filename], directory, mode + "-" + repeat + "-after-" + filename).splitlines())
                assert after and after <= before, (mode, filename, "changed original relations")
        assert generations[0] == generations[1], "array layout publication/counters are nondeterministic"
        check_array_layout_admission(cli, original, corelib, directory / (mode + "-primary"), mode)
        assert input_hashes(original, corelib) == hashes, "array layout changed original input bytes"
    full_witness("graph-validation=array-initialize-model-layouts,nongeneric-shells,symbolic-outer-fields,nested-values,discarded-generic-formation,late-replay,unarmed-control,unique-external-dependencies,metadata-negatives,actual-primary-sharing-on-and-off,determinism,token-closure", flush=True)


if __name__ == "__main__":
    assert len(sys.argv) == 6, "companion probe cli corelib output"
    validate_array_layout(*map(Path, sys.argv[1:]))
