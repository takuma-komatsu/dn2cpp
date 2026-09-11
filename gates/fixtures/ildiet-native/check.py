#!/usr/bin/env python3
"""Compare native stripping with managed ILDiet, including its ordered protocol."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET

parser = argparse.ArgumentParser(description=__doc__)
for name in ("managed", "native", "probe", "app", "library", "runtime", "corelib", "directory"):
    parser.add_argument(name, type=lambda value: Path(value).resolve())
parser.add_argument("--managed-only", action="store_true",
                    help="check the managed oracle and harness without claiming native parity")
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
    root = ET.parse(path).getroot()
    assert root.tag == "ildietResult" and set(root.attrib) == {
        "input", "preservation", "cutsValidated", "constructorRegistriesRewritten"}, f"invalid protocol result: {path}"
    assert root.attrib["cutsValidated"] == "true", f"cuts were not validated: {path}"
    assert root.attrib["constructorRegistriesRewritten"] == "false", f"ordinary input reported a registry rewrite: {path}"

    def relative(value):
        return Path(value).resolve().relative_to(output).as_posix()

    result = {"input": relative(root.attrib["input"]),
              "preservation": relative(root.attrib["preservation"]),
              "cutsValidated": root.attrib["cutsValidated"],
              "constructorRegistriesRewritten": root.attrib["constructorRegistriesRewritten"], "references": []}
    for child in root:
        assert child.tag == "reference" and set(child.attrib) == {"path"} and not len(child)
        result["references"].append(relative(child.attrib["path"]))
    assert result["input"] == args.app.name
    assert result["preservation"] == "preservation.xml"
    assert result["references"] == [path.name for path in references], "reference order changed"
    return result


before = {path: digest(path) for path in inputs}
runtimeconfig = args.app.with_suffix(".runtimeconfig.json")
expected = run(["dotnet", "exec", args.app], args.directory / "original-run")
original_metadata = run(["dotnet", "exec", args.probe, args.library],
                        args.directory / "original-metadata").decode().splitlines()
removed = {"method ILDietControlLib.Base::UnusedPrivate", "type ILDietControlLib.UnusedType"}
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
            options += ["-o", output]
        else:
            request_path = work / "request.xml"
            request = ET.Element("ildiet", input=str(args.app), output=str(output), copyAll="false")
            for reference in references:
                ET.SubElement(request, "reference", path=str(reference))
            ET.SubElement(request, "linkXml", path=str(descriptor))
            ET.SubElement(request, "cut", method="ILDietControlLib.UnusedType::UnusedMethod")
            ET.ElementTree(request).write(request_path, encoding="unicode")
            options = ["--request", request_path]
        first = None
        for repeat in ("first", "repeat"):
            run(command + options + ["--result", result_path], work / (repeat + "-strip"))
            protocol = manifest(result_path, output)
            current = (snapshot(output), protocol)
            if first is None:
                first = current
                (output / "stale.pdb").write_text("stale symbols must not survive replacement")
            else:
                assert current == first, f"nondeterministic output or stale files: {work}"
        (work / "normalized-result.json").write_text(json.dumps(protocol, indent=2) + "\n")
        assert {path.name for path in output.glob("*.dll")} == {path.name for path in inputs}
        for reference in references[1:-1]:
            assert digest(output / reference.name) == before[reference], f"protected DLL changed: {reference}"
        assert digest(output / args.library.name) != before[args.library], "library was copied unchanged"
        assert not list(output.rglob("*.pdb")), "rewritten output retained debug symbols"
        metadata = set(run(["dotnet", "exec", args.probe, output / args.library.name],
                           work / "metadata").decode().splitlines())
        assert kept <= metadata, f"runtime or layout dependencies lost: {kept - metadata}"
        assert "type ILDietControlLib.UnusedType" not in metadata, "unused type survived"
        private_method = "method ILDietControlLib.Base::UnusedPrivate"
        assert (private_method in metadata) == (mode == "request"), "descriptor did not control preservation"
        preservation = ET.parse(output / "preservation.xml").getroot()
        assert preservation.tag == "linker" and bool(len(preservation)) == (mode == "request")
        assert digest(output / signed.name) != before[signed], "signed witness was copied unchanged"
        run(["dotnet", "exec", args.metadata_validation, signed, output / signed.name, "signed"],
            work / "signed-metadata")
        actual = run(["dotnet", "exec", "--runtimeconfig", runtimeconfig, output / args.app.name],
                     work / "stripped-run")
        assert actual == expected, f"stripped managed behavior changed: {work}"
        if oracle is None:
            oracle = current
        else:
            assert current == oracle, f"managed/native DLL, preservation or protocol mismatch: {work}"
        assert all(digest(path) == original for path, original in before.items()), "input DLL changed"
        print(f"PASS {mode} {implementation}: deterministic stripping, signed identity, metadata, protocol, execution", flush=True)

if args.managed_only:
    print("OK: managed oracle verified; native stripping parity remains unproven")
else:
    print("OK: native ILDiet matches managed stripping, preservation and ordered protocol")
