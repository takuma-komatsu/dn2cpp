"""Model formations do not satisfy normal demands; admitted effects keep their arguments."""
from pathlib import Path
import json
import os
import re
import subprocess
import xml.etree.ElementTree as ET

from copied_array_admission import INPUTS, digest, emitted, manifest, contract_digest, framed_hash

MODES = ("authority-model-only", "authority-ordinary-late", "authority-array", "authority-policy",
         "authority-policy-unformed", "authority-attribute", "authority-attribute-passive")
POSITIVE = frozenset(("authority-ordinary-late", "authority-array", "authority-policy"))
CONSEQUENCE = frozenset(("authority-array", "authority-policy", "authority-attribute"))
NEGATIVES = ("AuthorityDormantDependency", "AuthoritySafeUnusedDependency", "AuthorityPayloadUnusedDependency",
             "AuthorityDormantUnusedDependency", "AuthorityAnchorDependency", "AuthorityLazyDependency", "AuthorityAttributePayloadUnusedDependency")


def command_run(command, area, label, extra=None, expected=0):
    area.mkdir(parents=True, exist_ok=True)
    command = list(map(str, command))
    (area / (label + ".command.json")).write_text(json.dumps(command, indent=2) + "\n")
    result = subprocess.run(command, capture_output=True, env={**os.environ, **(extra or {})})
    (area / (label + ".stdout.log")).write_bytes(result.stdout)
    (area / (label + ".stderr.log")).write_bytes(result.stderr)
    (area / (label + ".process.json")).write_text(json.dumps({"returncode": result.returncode}, indent=2) + "\n")
    if expected is not None:
        assert result.returncode == expected, (label, result.returncode, result.stderr.decode(errors="replace"))
    return result


def inputs(original, refs):
    return {str(p.resolve()): digest(p) for p in [original / "GraphApp.dll", *refs, original / "link.xml"]}


def references(original, corelib, runtime):
    return [*(original / name for name in INPUTS[1:]), Path(corelib), Path(runtime)]


def evidence(stdout):
    rows = [line for line in stdout.decode().splitlines() if line.startswith("model-authority-observed=")]
    assert len(rows) == 1
    return json.loads(rows[0].split("=", 1)[1])


def policy(path, mode, published=False):
    root = ET.parse(path).getroot()
    assert root.tag == "linker" and not root.attrib
    expected = "Graph.AuthorityPolicy`1" if mode.startswith("authority-policy") else (
        "Graph.AuthorityEffect`1" if mode.startswith("authority-attribute") else None)
    if not expected:
        assert len(root) == 0
        return
    assert len(root) <= 1 if published else len(root) == 1
    for assembly in root:
        assert assembly.tag == "assembly" and assembly.attrib == {"fullname": "GraphLib", "preserve": "nothing"}
        assert len(assembly) == 1
        owner = assembly[0]
        assert owner.tag == "type" and owner.attrib == {"fullname": expected, "preserve": "nothing",
            **({"required": "0"} if mode.startswith("authority-attribute") else {})}
        assert len(owner) == 1 and owner[0].tag == "method" and len(owner[0]) == 0
        assert owner[0].attrib == ({"signature": "System.Void Activate()"} if published else {"name": "Activate"})


def create(probe, corelib, area, mode):
    assert mode in MODES and not area.exists()
    area.mkdir(parents=True)
    original = area / "input"
    result = command_run(["dotnet", "exec", probe, "--create-copied-array-fixtures", original, corelib, mode], area, "create")
    assert result.stdout.splitlines() == [("fixture-created=copied-array mode=" + mode).encode()]
    assert {p.name for p in original.iterdir()} == {*INPUTS, "link.xml"}
    checked = command_run(["dotnet", "exec", probe, "--check-copied-array-fixtures", original, mode, "original"], area, "original-check")
    assert checked.stdout.splitlines()[-1].startswith(("graph-model-authority=original:" + mode + ":").encode())
    (area / "original-evidence.json").write_text(json.dumps(evidence(checked.stdout), indent=2) + "\n")
    policy(original / "link.xml", mode)
    return original


def primary(cli, original, refs, area, mode, integrated=False, expected=None, extra=None):
    assert not area.exists()
    area.mkdir(parents=True)
    before = inputs(original, refs)
    observations, records = {}, []
    for shared in (True, False):
        label = "sharing-on" if shared else "sharing-off"
        destination = area / label
        command = ["dotnet", "exec", cli, original / "GraphApp.dll"]
        for ref in refs:
            command += ["-r", ref]
        command += ["--link-xml", original / "link.xml", "--shared-generics" if shared else "--no-shared-generics", "-o", destination]
        if not integrated:
            command.append("--no-ildiet")
        execution = command_run(command, area, label, extra)
        files = emitted(destination)
        source = "\n".join((destination / name).read_text() for name in files)

        def definitions(symbol):
            return re.findall(r"(?m)^[A-Za-z_][A-Za-z0-9_* ]* " + symbol + r"_m[0-9]+\([^;\n]*\)\s*\{", source)

        assert all(definitions("AuthorityEntry_" + name) for name in ("Main", "Schedule", "Late", "Trigger"))
        counts = {"safe": len(definitions("AuthoritySafe__ctor")), "payload": len(definitions("AuthorityPayload__ctor")),
                  "cell": len(definitions("AuthorityCell_1__ctor")), "wrapper": len(definitions("AuthorityWrapper_1__ctor")),
                  "policy": len(definitions("AuthorityPolicy_1_Activate")), "effect": len(definitions("AuthorityEffect_1_Activate")),
                  "attribute": len(definitions("AuthorityDataAttribute__ctor"))}
        wanted = {"safe": True, "payload": mode in POSITIVE, "cell": mode == "authority-array",
                  "wrapper": mode in CONSEQUENCE, "policy": mode == "authority-policy",
                  "effect": mode == "authority-attribute", "attribute": mode == "authority-attribute"}
        if mode.startswith("authority-attribute"):
            counts["attributePayload"] = len(definitions("AuthorityAttributePayload__ctor"))
            wanted["attributePayload"] = mode == "authority-attribute"
        assert {k: bool(v) for k, v in counts.items()} == wanted, (mode, label, counts, wanted)
        for name in ("AuthoritySafe", "AuthorityPayload", "AuthorityAttributePayload"):
            assert bool(definitions(name + "__ctor")) == bool(definitions(name + "Dependency_Touch"))
        assert all(not definitions(name + "_Touch") for name in NEGATIVES), (mode, label, "passive body executed")
        for name in ("AuthorityAnchor__ctor", "AuthorityAnchor_Unused", "AuthorityDormant__ctor",
                     "AuthoritySafe_Unused", "AuthorityPayload_Unused", "AuthorityDormant_Unused", "AuthorityAttributePayload_Unused"):
            assert not definitions(name), (mode, label, name)
        if not shared:
            assert b"dn2cpp: shared-generics:" not in execution.stdout
        if integrated:
            root, published = manifest(destination / "ildiet")
            assert root.get("fallback") == "none" and root.find("counters").get("decode.max-per-method") == "1"
            for ref in refs:
                if ref.name != "GraphLib.dll":
                    assert (published / ref.name).read_bytes() == ref.read_bytes()
            policy(published / "preservation.xml", mode, published=True)
        else:
            assert not (destination / "ildiet").exists()
        observed = {"counts": counts, "all_passive_negatives_absent": True}
        if expected is not None:
            assert observed == expected[str(shared)], (mode, label, "primary body set changed")
        observations[str(shared)] = observed
        records.append({"shared": shared, "generated_sha256": files, "command": list(map(str, command)), "observation": observed})
    assert inputs(original, refs) == before
    receipt = {"observations": observations, "records": records, "input_sha256": before}
    (area / "admission.json").write_text(json.dumps(receipt, indent=2) + "\n")
    return receipt


def publish(companion, probe, original, refs, area, mode, extra=None, observed=False):
    assert not area.exists()
    area.mkdir(parents=True)
    before = inputs(original, refs)
    request = ET.Element("ildiet", version="2", input=str(original / "GraphApp.dll"), output=str(area / "publication"))
    for ref in refs:
        ET.SubElement(request, "reference", path=str(ref))
    ET.SubElement(request, "linkXml", path=str(original / "link.xml"))
    request_path = area / "request.xml"
    ET.ElementTree(request).write(request_path, encoding="utf-8", xml_declaration=True)
    command_run(["dotnet", "exec", companion, "--request", request_path], area, "companion", extra)
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
            assert (published / ref.name).read_bytes() == ref.read_bytes()
    policy(published / "preservation.xml", mode, published=True)
    state = "observed-before" if observed else "published"
    checked = command_run(["dotnet", "exec", probe, "--check-copied-array-fixtures", published, mode, state], area, "retained-check", extra)
    actual = evidence(checked.stdout)
    first = json.loads((original.parent / "original-evidence.json").read_text())
    assert actual["ordinaryOwner"] == first["ordinaryOwner"] and actual["lateOwner"] == first["lateOwner"]
    assert actual["distinctMemberRefAlias"] is True
    if mode == "authority-attribute":
        assert actual["attributeTarget"] == first["attributeTarget"]
    closure = command_run(["dotnet", "exec", probe, "--check-token-closure", *sorted(published.glob("*.dll"))], area, "closure", extra)
    assert b"token-closure=valid operands=" in closure.stdout
    for name in INPUTS:
        earlier = set(command_run(["dotnet", "exec", probe, "--relations", original / name], area, "before-" + name, extra).stdout.splitlines())
        later = set(command_run(["dotnet", "exec", probe, "--relations", published / name], area, "after-" + name, extra).stdout.splitlines())
        assert later and later <= earlier, (mode, name, later - earlier)
    assert inputs(original, refs) == before
    receipt = {"observation": actual, "files": {p.name: digest(p) for p in published.iterdir() if p.is_file()},
               "generation": root.get("generation"), "input_sha256": before, "counters": dict(root.find("counters").attrib)}
    (area / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    return receipt


def validate_model_authority(companion, probe, cli, corelib, directory):
    directory = Path(directory)
    assert not directory.exists()
    directory.mkdir(parents=True)
    prepared = []
    for mode in MODES:
        area = directory / mode
        original = create(probe, corelib, area, mode)
        refs = references(original, corelib, Path(cli).parent / "Dn2Cpp.Runtime.dll")
        raw = primary(cli, original, refs, area / "original-primary", mode)
        prepared.append((mode, area, original, refs, raw))
    for mode, area, original, refs, raw in prepared:
        first = publish(companion, probe, original, refs, area / "standalone-first", mode)
        repeat = publish(companion, probe, original, refs, area / "standalone-repeat", mode)
        assert first == repeat, (mode, "nondeterministic authority publication")
        primary(cli, original, refs, area / "integrated-primary", mode, integrated=True, expected=raw["observations"])
        print("graph-model-authority=" + mode + ":original-sharing-on-off,normal-binding-authority,argument-consequences,copy-token-closure", flush=True)
    print("graph-validation=model-binding-authority,model-only-negative,late-ordinary-alias-replay,array-policy-generic-dependencies,formed-attribute-effects,passive-negatives", flush=True)
