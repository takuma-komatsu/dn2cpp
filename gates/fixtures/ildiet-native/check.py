#!/usr/bin/env python3
"""Compare native stripping with managed ILDiet, including its ordered protocol."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET
import sys
sys.path.insert(0, str(Path(__file__).resolve().parents[2]))
from ildiet_manifest import manifest as read_manifest

parser = argparse.ArgumentParser(description=__doc__)
for name in ("managed", "native", "probe", "app", "library", "runtime", "corelib", "directory"):
    parser.add_argument(name, type=lambda value: Path(value).resolve())
parser.add_argument("--managed-only", action="store_true",
                    help="check the managed oracle and harness without claiming native parity")
parser.add_argument("--execution-link-xml", required=True, type=lambda value: Path(value).resolve(),
                    help="original ILDietControl constructor preservation descriptor")
parser.add_argument("--metadata-validation", required=True, type=lambda value: Path(value).resolve(),
                    help="existing MetadataValidation.dll for signed assembly identity checks")
args = parser.parse_args()
args.directory.mkdir(parents=True, exist_ok=True)
references = [args.library, args.runtime, args.corelib]
references += [args.corelib.parent / (name + ".dll") for name in (
    "System.Runtime", "System.Console", "System.Collections", "System.Linq", "System.Threading")]
signed = args.managed.parent / "Mono.Cecil.dll"
references.append(signed)
inputs = [args.app, *references]


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def snapshot(directory):
    return {str(path.relative_to(directory)): digest(path)
            for path in directory.rglob("*") if path.is_file()}


def run(command, log):
    command = list(map(str, command))
    log.parent.mkdir(parents=True, exist_ok=True)
    log.with_suffix(".command.json").write_text(json.dumps(command, indent=2) + "\n")
    result = subprocess.run(command, capture_output=True)
    log.with_suffix(".stdout.log").write_bytes(result.stdout)
    log.with_suffix(".stderr.log").write_bytes(result.stderr)
    assert result.returncode == 0, f"exit {result.returncode}; see {log}.stderr.log"
    return result.stdout


def manifest(path, output):
    root, published = read_manifest(output, path)
    assert root.get("constructorRegistriesRewritten") == "false", "ordinary input reported a registry rewrite"
    assert root.get("fallback") == "none", "ordinary native stripping fell back"
    assert root.get("cutsValidated") == "true", "ordinary cuts were not validated"
    def relative(value):
        return Path(value).resolve().relative_to(published).as_posix()
    result = {key: root.get(key) for key in ("version", "generation", "cutsValidated", "constructorRegistriesRewritten", "fallback", "contract")}
    result.update(input=relative(root.get("input")), preservation=relative(root.get("preservation")),
                  references=[relative(child.get("path")) for child in root.findall("reference")],
                  assemblies=[dict(child.attrib) for child in root.findall("assembly")],
                  counters=dict(root.find("counters").attrib))
    assert result["input"] == args.app.name
    assert result["preservation"] == "preservation.xml"
    assert result["references"] == [path.name for path in references], "reference order changed"
    return result


before = {path: digest(path) for path in inputs}
runtimeconfig = args.app.with_suffix(".runtimeconfig.json")
expected = run(["dotnet", "exec", args.app], args.directory / "original-run")
original_metadata = run(["dotnet", "exec", args.probe, args.library],
                        args.directory / "original-metadata").decode().splitlines()
removed = {"method ILDietControlLib.Base::UnusedPrivate", "type ILDietControlLib.UnusedType",
           "type ILDietControlLib.ConstructorOnlyDependency"}
assert removed <= set(original_metadata), "stripping fixture lacks its unused metadata"

descriptor = args.directory / "link.xml"
descriptor.write_text('<linker><assembly fullname="ILDietControlLib">'
                      '<type fullname="ILDietControlLib.Base" preserve="nothing">'
                      '<method name="UnusedPrivate" />'
                      '</type></assembly></linker>\n')
kept = {"method ILDietControlLib.Base::Foo",
        "method ILDietControlLib.Callbacks::NativeCallback",
        "method ILDietControlLib.Callbacks::CallbackLeaf",
        "method ILDietControlLib.Callbacks::NativeEntry",
        "method ILDietControlLib.Callbacks::NativeLeaf",
        "field ILDietControlLib.Layout::_unused", "method <Module>::.cctor"}
constructor_rules = {("ILDietControl", "ILDietControl." + name, "System.Void .ctor()") for name in (
    "ByArgument", "ByField", "ByProperty", "ByArrayFirst", "ByArraySecond", "ByObject")}
policy_before = {path: digest(path) for path in (args.execution_link_xml, descriptor)}


def preservation_rules(path):
    root = ET.parse(path).getroot()
    assert root.tag == "linker"
    rules = []
    for assembly in root:
        assert assembly.tag == "assembly" and assembly.get("preserve") == "nothing"
        for owner in assembly:
            assert owner.tag == "type" and owner.attrib == {"fullname": owner.get("fullname"), "preserve": "nothing"}
            assert len(owner) == 1, "preservation invented or lost a selected member"
            member = owner[0]
            assert member.tag == "method" and set(member.attrib) == {"signature"}
            rules.append((assembly.get("fullname"), owner.get("fullname"), member.get("signature")))
    return rules


commands = [("managed", ["dotnet", "exec", args.managed])]
if not args.managed_only:
    commands.append(("native", [args.native]))

for mode in ("direct", "request"):
    oracle = None
    for implementation, command in commands:
        work = args.directory / mode / implementation
        work.mkdir(parents=True, exist_ok=True)
        output = work / "out"
        result_path = work / "result.xml"
        if mode == "direct":
            options = [args.app]
            for reference in references:
                options += ["-r", reference]
            options += ["--link-xml", args.execution_link_xml, "-o", output]
        else:
            request_path = work / "request.xml"
            request = ET.Element("ildiet", version="2", input=str(args.app), output=str(output), copyAll="false")
            for reference in references:
                ET.SubElement(request, "reference", path=str(reference))
            ET.SubElement(request, "linkXml", path=str(args.execution_link_xml))
            ET.SubElement(request, "linkXml", path=str(descriptor))
            ET.SubElement(request, "cut", method="ILDietControlLib.UnusedType::UnusedMethod")
            ET.ElementTree(request).write(request_path, encoding="unicode")
            options = ["--request", request_path]
        first = None
        for repeat in ("first", "repeat"):
            run(command + options + ["--result", result_path], work / (repeat + "-strip"))
            protocol = manifest(result_path, output)
            published = read_manifest(output)[1]
            current = (snapshot(published), protocol)
            if first is None:
                first = current
                (output / "stale.pdb").write_text("stale symbols must not survive replacement")
            else:
                assert current == first, f"nondeterministic output: {work}"
                assert not (output / "stale.pdb").exists(), f"stale flat output remains: {work}"
        (work / "normalized-result.json").write_text(json.dumps(protocol, indent=2) + "\n")
        assert {path.name for path in published.glob("*.dll")} == {path.name for path in inputs}
        for reference in references[1:-1]:
            assert digest(published / reference.name) == before[reference], f"protected DLL changed: {reference}"
        assert digest(published / args.library.name) != before[args.library], "library was copied unchanged"
        assert not list(output.rglob("*.pdb")), "rewritten output retained debug symbols"
        metadata = set(run(["dotnet", "exec", args.probe, published / args.library.name],
                           work / "metadata").decode().splitlines())
        assert kept <= metadata, f"runtime or layout dependencies lost: {kept - metadata}"
        unused_type = "type ILDietControlLib.UnusedType"
        unused_method = "method ILDietControlLib.UnusedType::UnusedMethod"
        assert (unused_type in metadata) == (mode == "request"), "cut target type retention changed"
        assert (unused_method in metadata) == (mode == "request"), "cut target method retention changed"
        assert "type ILDietControlLib.ConstructorOnlyDependency" not in metadata, "unrelated uncut dependency survived"
        cut_selector = "ILDietControlLib.UnusedType::UnusedMethod"
        body_states = run(["dotnet", "exec", args.probe, "--method-body", published / args.library.name,
                           cut_selector], work / "cut-method-body").decode().splitlines()
        assert body_states == [cut_selector + (" stub=False" if mode == "request" else " absent")], \
            "ordinary cut target did not retain its original body"
        private_method = "method ILDietControlLib.Base::UnusedPrivate"
        assert (private_method in metadata) == (mode == "request"), "descriptor did not control preservation"
        expected_rules = set(constructor_rules)
        if mode == "request":
            expected_rules.add(("ILDietControlLib", "ILDietControlLib.Base", "System.Int32 UnusedPrivate()"))
        actual_rules = preservation_rules(published / "preservation.xml")
        assert len(actual_rules) == len(expected_rules) and set(actual_rules) == expected_rules, \
            f"effective preservation changed: {actual_rules}"
        assert digest(published / signed.name) != before[signed], "signed witness was copied unchanged"
        run(["dotnet", "exec", args.metadata_validation, signed, published / signed.name, "signed"],
            work / "signed-metadata")
        actual = run(["dotnet", "exec", "--runtimeconfig", runtimeconfig, published / args.app.name],
                     work / "stripped-run")
        assert actual == expected, f"stripped managed behavior changed: {work}"
        if oracle is None:
            oracle = current
        else:
            assert current == oracle, f"managed/native DLL, preservation or protocol mismatch: {work}"
        assert all(digest(path) == original for path, original in before.items()), "input DLL changed"
        assert all(digest(path) == original for path, original in policy_before.items()), "input descriptor changed"
        print(f"PASS {mode} {implementation}: deterministic stripping, signed identity, metadata, protocol, execution", flush=True)

if args.managed_only:
    print("OK: managed oracle verified; native stripping parity remains unproven")
else:
    print("OK: native ILDiet matches managed stripping, preservation and ordered protocol")
