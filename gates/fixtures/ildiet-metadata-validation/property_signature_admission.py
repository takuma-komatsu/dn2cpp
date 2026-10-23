"""Explicit assembly copying preserves bytes without inventing executable roots."""
from pathlib import Path
import argparse
import json
import re
import xml.etree.ElementTree as ET

from copied_array_admission import digest, emitted, run, manifest, contract_digest

# Label, original signature, descriptor policy, enabled feature.
CASES = (("all", "mismatch", "all", False),
         ("default", "matching", "default", False),
         ("feature-on", "matching", "feature", True),
         ("feature-off", "matching", "feature", False),
         ("unreferenced", "matching", "unreferenced", False),
         ("type-all", "matching", "type", False),
         ("matching", "matching", "none", False),
         ("mismatch", "mismatch", "none", False),
         ("truncated", "truncated", "none", False),
         ("skip", "mismatch", "none", False),
         ("skip-all", "mismatch", "all", False))
FILES = ("PropertyApp.dll", "PropertyCarrier.dll", "PropertyDependency.dll")


def call(command, area, label, expected=0):
    command = list(map(str, command))
    result = run(command, area, label, expected=None)
    (area / (label + ".command.json")).write_text(json.dumps(command, indent=2) + "\n")
    (area / (label + ".status.json")).write_text(json.dumps({"exit_code": result.returncode}) + "\n")
    if expected is not None:
        assert result.returncode == expected, (label, result.returncode, result.stderr.decode(errors="replace"))
    return result


def primary(cli, original, refs, descriptor, feature, preserved, area, integrated=False, failure=False, skips=()):
    area.mkdir()
    observations = {}
    for shared in (True, False):
        label = "sharing-on" if shared else "sharing-off"
        output = area / label
        command = ["dotnet", "exec", cli, original / FILES[0]]
        for ref in refs:
            command += ["-r", ref]
        command += ["--link-xml", descriptor, "--shared-generics" if shared else "--no-shared-generics", "-o", output]
        if feature:
            command += ["--link-feature", "com"]
        if not integrated:
            command += ["--no-ildiet"]
        else:
            for name in skips:
                command += ["--ildiet-skip-assembly", name]
        result = call(command, area, label, expected=None if failure else 0)
        if failure:
            assert result.returncode != 0 and b"Original array signature does not match the retained Cecil signature." in result.stderr
            continue
        files = emitted(output)
        source = "\n".join((output / name).read_text(encoding="utf-8") for name in files)
        def count(symbol):
            return len(re.findall(r"(?m)^[A-Za-z_][A-Za-z0-9_* ]* " + symbol + r"_m[0-9]+\([^;\n]*\)\s*\{", source))
        observed = {symbol: count(symbol) for symbol in (
            "PropertyEntry_Main", "PropertyCarrier_get_Value", "PropertyDependency_Touch",
            "PropertyCarrier_Dormant", "PropertyDependency_Unused")}
        assert observed == {"PropertyEntry_Main": 1, "PropertyCarrier_get_Value": 1,
                            "PropertyDependency_Touch": 1, "PropertyCarrier_Dormant": int(preserved),
                            "PropertyDependency_Unused": int(preserved)}, observed
        observations[label] = observed
        (area / (label + ".generated.json")).write_text(json.dumps(files, indent=2) + "\n")
    return observations


def validate_property_signatures(companion, probe, cli, corelib, directory):
    directory = Path(directory).resolve()
    assert not directory.exists()
    directory.mkdir(parents=True)
    prepared = []
    for label, mode, policy, feature in CASES:
        area = directory / label
        area.mkdir()
        original = area / "input"
        call(["dotnet", "exec", probe, "--create-property-signature-fixtures", original, corelib, mode], area, "create")
        assert {p.name for p in original.iterdir()} == set(FILES)
        call(["dotnet", "exec", probe, "--check-property-signature-fixtures", original, mode, "original"], area, "original-check")
        refs = [original / name for name in FILES[1:]] + [Path(corelib).resolve(), Path(cli).resolve().parent / "Dn2Cpp.Runtime.dll"]
        linker = ET.Element("linker")
        if policy != "none":
            attributes = {"fullname": "PropertyCarrier"}
            if policy == "type":
                attributes["preserve"] = "nothing"
            elif policy != "default":
                attributes["preserve"] = "all"
            if policy == "feature":
                attributes.update(feature="com", ignoreIfUnreferenced="true")
            if policy == "unreferenced":
                dead = original / "ILDietDeadReference.dll"
                call(["dotnet", "exec", probe, "--create-dead", dead], area, "create-unreferenced")
                refs.append(dead)
                attributes.update(fullname="ILDietDeadReference", ignoreIfUnreferenced="true")
            assembly = ET.SubElement(linker, "assembly", attributes)
            if policy == "type":
                ET.SubElement(assembly, "type", fullname="Graph.PropertyCarrier", preserve="all")
        descriptor = area / "link.xml"
        ET.ElementTree(linker).write(descriptor, encoding="utf-8", xml_declaration=True)
        copied = policy in ("all", "default") or policy == "feature" and feature
        preserved = copied or policy == "type"
        skips = ("PropertyCarrier", Path(corelib).stem) if label.startswith("skip") else ()
        copied = copied or bool(skips)
        hashes = {str(p): digest(p) for p in [original / FILES[0], *refs, descriptor]}
        raw = None if mode == "truncated" else primary(cli, original, refs, descriptor, feature, preserved, area / "original-primary")
        prepared.append((label, mode, feature, copied, preserved, skips, area, original, refs, descriptor, hashes, raw))
    # Every valid original-primary witness precedes companion publication.
    for label, mode, feature, copied, preserved, skips, area, original, refs, descriptor, hashes, raw in prepared:
        output = area / "publication"
        request = ET.Element("ildiet", version="2", input=str(original / FILES[0]), output=str(output))
        for ref in refs:
            ET.SubElement(request, "reference", path=str(ref))
        ET.SubElement(request, "linkXml", path=str(descriptor))
        if feature:
            ET.SubElement(request, "feature", name="com")
        for name in skips:
            ET.SubElement(request, "skipAssembly", name=name)
        if skips:
            # Explicit copying also dominates backend permission to rewrite.
            ET.SubElement(request, "rewriteAssembly", name=Path(corelib).stem)
        request_path = area / "request.xml"
        ET.ElementTree(request).write(request_path, encoding="utf-8", xml_declaration=True)
        result = call(["dotnet", "exec", companion, "--request", request_path], area, "companion", expected=None)
        failure = mode == "truncated" or mode == "mismatch" and not copied
        if failure:
            assert result.returncode == 1 and b"ILDiet: error:" in result.stderr
            assert not (output / "ildiet-result.xml").exists()
            if mode == "mismatch":
                assert b"Original array signature does not match the retained Cecil signature." in result.stderr
        else:
            assert result.returncode == 0, result.stderr.decode(errors="replace")
            root, published = manifest(output)
            assert root.get("fallback") == "none" and root.get("contract") == contract_digest(request)
            assert [Path(item.get("path")).name for item in root.findall("reference")] == [p.name for p in refs]
            modes = {item.get("name"): item.get("mode") for item in root.findall("assembly")}
            assert modes["PropertyApp"] == modes["PropertyDependency"] == "rewritten"
            assert modes["PropertyCarrier"] == ("copied" if copied else "rewritten")
            if copied:
                assert (published / FILES[1]).read_bytes() == (original / FILES[1]).read_bytes()
            elif not preserved:
                assert digest(published / FILES[1]) != digest(original / FILES[1])
            for ref in refs[2:4]:
                assert (published / ref.name).read_bytes() == ref.read_bytes()
            if label == "unreferenced":
                assert modes["ILDietDeadReference"] == "rewritten"
            exported = ET.parse(published / "preservation.xml").getroot()
            all_rows = exported.findall("./assembly[@preserve='all']")
            assert len(all_rows) == int(copied and preserved)
            if copied and preserved:
                assert all_rows[0].attrib == {"fullname": "PropertyCarrier", "preserve": "all"} and len(all_rows[0]) == 0
            state = "preserved" if copied or preserved else "published"
            call(["dotnet", "exec", probe, "--check-property-signature-fixtures", published, mode, state], area, "retained-check")
            call(["dotnet", "exec", probe, "--check-token-closure", *sorted(published.glob("*.dll"))], area, "closure")
            replay = ET.Element("ildiet", version="2", input=root.get("input"), output=str(area / "replay"))
            for ref in root.findall("reference"):
                ET.SubElement(replay, "reference", path=ref.get("path"))
            ET.SubElement(replay, "linkXml", path=root.get("preservation"))
            for name in skips:
                ET.SubElement(replay, "skipAssembly", name=name)
            replay_path = area / "replay.xml"
            ET.ElementTree(replay).write(replay_path, encoding="utf-8", xml_declaration=True)
            call(["dotnet", "exec", companion, "--request", replay_path], area, "replay")
            replay_root, replayed = manifest(area / "replay")
            assert {item.get("name"): item.get("mode") for item in replay_root.findall("assembly")} == modes
            if copied:
                assert (replayed / FILES[1]).read_bytes() == (published / FILES[1]).read_bytes()
            call(["dotnet", "exec", probe, "--check-property-signature-fixtures", replayed, mode, state], area, "replay-check")
        if mode != "truncated":
            actual = primary(cli, original, refs, descriptor, feature, preserved, area / "integrated-primary", integrated=True, failure=failure, skips=skips)
            if not failure:
                assert actual == raw
        if label == "skip":
            check_skip_options(companion, cli, original, refs, descriptor, request, area)
        assert {path: digest(path) for path in hashes} == hashes
        (area / "receipt.json").write_text(json.dumps({"input_sha256": hashes, "original_admission": raw,
                                                      "companion_exit": result.returncode}, indent=2) + "\n")
        print("graph-property-signature=" + label + ":original-first,explicit-assembly-copy,signature,closure,replay,admission", flush=True)


def check_skip_options(companion, cli, original, refs, descriptor, request, area):
    direct = ["dotnet", "exec", companion, original / FILES[0], "--link-xml", descriptor]
    for ref in refs:
        direct += ["-r", ref]
    names = [item.get("name") for item in request.findall("skipAssembly")]
    command = direct + ["-o", area / "direct"]
    for name in names:
        command += ["--ildiet-skip-assembly", name]
    call(command, area, "direct")
    root, published = manifest(area / "direct")
    expected = ET.Element("ildiet")
    for name in names:
        ET.SubElement(expected, "skipAssembly", name=name)
    assert root.get("contract") == contract_digest(expected)
    assert (published / FILES[1]).read_bytes() == (original / FILES[1]).read_bytes()
    assert not ET.parse(published / "preservation.xml").getroot().findall("./assembly[@preserve='all']")
    reverse = ET.Element("ildiet")
    reverse.extend(reversed(list(expected)))
    assert contract_digest(expected) == contract_digest(reverse) != contract_digest(ET.Element("ildiet"))
    # No exported skip policy: a fresh request must explicitly select copying again.
    reinput = ["dotnet", "exec", companion, root.get("input"), "--link-xml", root.get("preservation")]
    for ref in root.findall("reference"):
        reinput += ["-r", ref.get("path")]
    missing = call(reinput + ["-o", area / "missing-skip"], area, "missing-skip", expected=1)
    assert b"Original array signature does not match the retained Cecil signature." in missing.stderr
    for index, name in enumerate(("MissingAssembly", "propertycarrier", "PropertyCarrier.dll", "")):
        output = area / ("invalid-" + str(index))
        result = call(direct + ["-o", output, "--ildiet-skip-assembly", name], area,
                      "invalid-" + str(index), expected=1)
        assert b"ILDiet skip assembly is not loaded:" in result.stderr
        assert not (output / "ildiet-result.xml").exists()
    invalid = ET.fromstring(ET.tostring(request))
    invalid.set("output", str(area / "invalid-request"))
    invalid.set("copyAll", "true")
    ET.SubElement(invalid, "skipAssembly", name="MissingAssembly")
    invalid_path = area / "invalid-request.xml"
    ET.ElementTree(invalid).write(invalid_path, encoding="utf-8", xml_declaration=True)
    result = call(["dotnet", "exec", companion, "--request", invalid_path], area, "invalid-request", expected=1)
    assert b"ILDiet skip assembly is not loaded:" in result.stderr
    assert not (area / "invalid-request" / "ildiet-result.xml").exists()
    integrated = ["dotnet", "exec", cli, original / FILES[0], "--link-xml", descriptor,
                  "-o", area / "invalid-integrated", "--ildiet-skip-assembly", "MissingAssembly"]
    for ref in refs:
        integrated += ["-r", ref]
    result = call(integrated, area, "invalid-integrated", expected=2)
    assert b"ILDiet skip assembly is not loaded:" in result.stderr
    assert not (area / "invalid-integrated" / "ildiet" / "ildiet-result.xml").exists()


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("companion", "probe", "cli", "corelib", "directory"):
        parser.add_argument(name)
    validate_property_signatures(**vars(parser.parse_args()))
