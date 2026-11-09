"""Named byref-like tokens preserve metadata without inventing virtual execution."""
from preserve_profile import FULL, select_cases, sharing_modes, full_witness
from pathlib import Path
import json
import re
import xml.etree.ElementTree as ET

from copied_array_admission import INPUTS, digest, emitted, run, manifest, contract_digest, framed_hash

MODES = (
    "byref-token-core", "byref-token-copied", "byref-token-rewritten",
    "byref-token-direct", "byref-token-constrained", "byref-token-interface",
    "byref-token-boxable", "byref-token-preserved",
)
ARMED = frozenset(MODES[3:])


def command_run(command, area, label, extra=None, expected=0):
    area.mkdir(parents=True, exist_ok=True)
    command = list(map(str, command))
    (area / (label + ".command.json")).write_text(json.dumps(command, indent=2) + "\n")
    return run(command, area, label, extra=extra, expected=expected)


def references(original, corelib, runtime):
    return [*(original / name for name in INPUTS[1:]), Path(corelib), Path(runtime)]


def inputs(original, refs):
    return {str(p.resolve()): digest(p) for p in [original / "GraphApp.dll", *refs, original / "link.xml"]}


def evidence(stdout):
    lines = [line for line in stdout.decode().splitlines() if line.startswith("byref-token-evidence=")]
    assert len(lines) == 1
    return json.loads(lines[0].split("=", 1)[1])


def policy(path, mode, published=False):
    root = ET.parse(path).getroot()
    assert root.tag == "linker" and not root.attrib
    if mode != "byref-token-preserved":
        assert len(root) == 0, "Token fixture gained preservation roots"
        return
    assert len(root) == 1 and root[0].tag == "assembly"
    assert root[0].attrib == {"fullname": "DnZlib", "preserve": "nothing"}
    assert len(root[0]) == 1 and root[0][0].tag == "type"
    owner = root[0][0]
    assert owner.attrib == {"fullname": "Graph.ByRefTokenPayload", "preserve": "nothing", "required": "0"}
    assert len(owner) == 1 and owner[0].tag == "method" and len(owner[0]) == 0
    assert owner[0].attrib == ({"signature": "System.Void Activate()"} if published else {"name": "Activate"})


def create(probe, corelib, area, mode):
    assert mode in MODES and not area.exists()
    area.mkdir(parents=True)
    original = area / "input"
    command_run(["dotnet", "exec", probe, "--create-copied-array-fixtures", original, corelib, mode], area, "create")
    assert {p.name for p in original.iterdir()} == {*INPUTS, "link.xml"}
    checked = command_run(["dotnet", "exec", probe, "--check-copied-array-fixtures", original, mode, "original"], area, "original-check")
    assert checked.stdout.splitlines()[-1].startswith(("graph-copied-array=original:" + mode + ":").encode())
    policy(original / "link.xml", mode)
    (area / "original-evidence.json").write_text(json.dumps(evidence(checked.stdout), indent=2) + "\n")
    return original


def primary(cli, original, refs, area, mode, integrated=False, expected=None, extra=None):
    assert not area.exists()
    area.mkdir(parents=True)
    before = inputs(original, refs)
    observations, records = {}, []
    for shared in sharing_modes():
        label = "sharing-on" if shared else "sharing-off"
        destination = area / label
        command = ["dotnet", "exec", cli, original / "GraphApp.dll"]
        for ref in refs:
            command += ["-r", ref]
        command += ["--link-xml", original / "link.xml", "--shared-generics" if shared else "--no-shared-generics", "-o", destination]
        if not integrated:
            command.append("--no-ildiet")
        execution = command_run(command, area, label, extra=extra)
        if not shared:
            assert b"dn2cpp: shared-generics:" not in execution.stdout
        files = emitted(destination)
        source = "\n".join((destination / name).read_text(encoding="utf-8") for name in files)

        def definitions(symbol):
            return re.findall(r"(?m)^[A-Za-z_][A-Za-z0-9_* ]* " + symbol + r"_m[0-9]+\([^;\n]*\)\s*\{", source)

        assert len(definitions("ByRefTokenEntry_Main")) == 1
        counts = {"array": len(definitions("ByRefTokenValue__ctor")),
                  "dependency": len(definitions("ByRefTokenDependency_Touch")),
                  "override": len(definitions("ByRefTokenPayload_ToString")),
                  "interface": len(definitions("ByRefTokenPayload_ExplicitRender")),
                  "policy": len(definitions("ByRefTokenPayload_Activate"))}
        wanted = {"array": int(mode in ARMED), "dependency": int(mode in ARMED),
                  "override": int(mode in ("byref-token-direct", "byref-token-constrained", "byref-token-boxable")),
                  "interface": int(mode == "byref-token-interface"), "policy": int(mode == "byref-token-preserved")}
        assert counts == wanted, (mode, label, "Actual original-primary hypothesis differs", counts, wanted)
        for symbol in ("ByRefTokenOverloadDependency_Touch", "ByRefTokenOrdinaryDependency_Touch",
                       "ByRefTokenValue_Ordinary", "ByRefTokenPayload_Unused", "TypedReference_GetHashCode"):
            assert not definitions(symbol), (mode, label, "Unexpected body", symbol)
        observations[str(shared)] = counts
        if expected is not None:
            assert counts == expected[str(shared)], (mode, label, "Primary admission changed")
        if integrated:
            assert expected is not None and set(expected) == set(map(str, sharing_modes())), "Originals must be measured first"
            root, published = manifest(destination / "ildiet")
            assert root.get("fallback") == "none" and root.find("counters").get("decode.max-per-method") == "1"
            policy(published / "preservation.xml", mode, published=True)
            assert [Path(item.get("path")).name for item in root.findall("reference")] == [p.name for p in refs]
            assert {p.name for p in published.glob("*.dll")} == {"GraphApp.dll", *(p.name for p in refs)}
            for ref in refs:
                if ref.name != "GraphLib.dll":
                    assert (published / ref.name).read_bytes() == ref.read_bytes(), (mode, ref)
        else:
            assert not (destination / "ildiet").exists()
        assert inputs(original, refs) == before
        records.append({"shared": shared, "command": list(map(str, command)), "admission": counts,
                        "input_sha256": before, "generated_sha256": files})
    (area / "receipt.json").write_text(json.dumps(records, indent=2) + "\n")
    return {"observations": observations, "generated": {str(r["shared"]): r["generated_sha256"] for r in records}}


def publish(companion, probe, original, refs, area, mode, extra=None):
    assert not area.exists()
    area.mkdir(parents=True)
    before = inputs(original, refs)
    request = ET.Element("ildiet", version="2", input=str(original / "GraphApp.dll"), output=str(area / "publication"))
    for ref in refs:
        ET.SubElement(request, "reference", path=str(ref))
    ET.SubElement(request, "linkXml", path=str(original / "link.xml"))
    request_path = area / "request.xml"
    ET.ElementTree(request).write(request_path, encoding="utf-8", xml_declaration=True)
    command_run(["dotnet", "exec", companion, "--request", request_path], area, "companion", extra=extra)
    root, published = manifest(area / "publication")
    assert root.get("fallback") == "none" and root.find("counters").get("decode.max-per-method") == "1"
    assert root.get("contract") == contract_digest(request)
    assert [Path(item.get("path")).name for item in root.findall("reference")] == [p.name for p in refs]
    assert {p.name for p in published.glob("*.dll")} == {"GraphApp.dll", *(p.name for p in refs)}
    values = []
    for item in root.findall("assembly"):
        name = item.get("name") + ".dll"
        values.extend((name, (published / name).read_bytes()))
        assert item.get("registryRewritten") == "false"
        assert item.get("mode") == ("rewritten" if name in ("GraphApp.dll", "GraphLib.dll") else "copied")
    values.extend(("preservation.xml", (published / "preservation.xml").read_bytes()))
    assert root.get("generation") == "g-" + framed_hash(values)
    for ref in refs:
        if ref.name != "GraphLib.dll":
            assert (published / ref.name).read_bytes() == ref.read_bytes(), ref
    policy(published / "preservation.xml", mode, published=True)
    checked = command_run(["dotnet", "exec", probe, "--check-copied-array-fixtures", published, mode, "published"], area, "retained-check", extra=extra)
    actual = evidence(checked.stdout)
    original_evidence = json.loads((original.parent / "original-evidence.json").read_text())
    assert actual["tokens"] == original_evidence["tokens"] and actual["code"] == original_evidence["code"]
    assert actual["array"] == (mode in ARMED)
    closure = command_run(["dotnet", "exec", probe, "--check-token-closure", *sorted(published.glob("*.dll"))], area, "closure", extra=extra)
    assert b"token-closure=valid operands=" in closure.stdout
    for name in INPUTS:
        earlier = set(command_run(["dotnet", "exec", probe, "--relations", original / name], area, "before-" + name, extra=extra).stdout.splitlines())
        later = set(command_run(["dotnet", "exec", probe, "--relations", published / name], area, "after-" + name, extra=extra).stdout.splitlines())
        assert later and later <= earlier, (mode, name, later - earlier)
    assert inputs(original, refs) == before
    receipt = {"classification": "retention",
               "observation": actual, "files": {p.name: digest(p) for p in published.iterdir() if p.is_file()},
               "generation": root.get("generation"), "input_sha256": before,
               "counters": dict(root.find("counters").attrib)}
    (area / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    return receipt


def validate_byref_tokens(companion, probe, cli, corelib, directory):
    directory = Path(directory)
    assert not directory.exists()
    directory.mkdir(parents=True)
    prepared = []
    for mode in select_cases('byref_token', MODES, ('byref-token-copied', 'byref-token-boxable')):
        area = directory / mode
        original = create(probe, corelib, area, mode)
        refs = references(original, corelib, Path(cli).parent / "Dn2Cpp.Runtime.dll")
        raw = primary(cli, original, refs, area / "original-primary", mode)
        prepared.append((mode, area, original, refs, raw))
    for mode, area, original, refs, raw in prepared:
        first = publish(companion, probe, original, refs, area / "standalone-first", mode)
        if FULL:
            repeat = publish(companion, probe, original, refs, area / "standalone-repeat", mode)
            assert first == repeat, (mode, "Nondeterministic standalone publication")
        primary(cli, original, refs, area / "integrated-primary", mode, integrated=True, expected=raw["observations"])
        print("graph-byref-token=" + mode + ":original-selected-sharing,metadata-and-execution,copy-identity,token-closure", flush=True)
    full_witness("graph-validation=named-byref-tokens,core-typedreference-void,copied-and-rewritten,direct-constrained-interface,boxable-control,conditional-preservation-use", flush=True)
