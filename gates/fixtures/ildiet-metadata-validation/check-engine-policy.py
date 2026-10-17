#!/usr/bin/env python3
"""Check conditional script roots and constructor routing before model loading."""
from pathlib import Path
import subprocess
import re
import sys
sys.path.insert(0, str(Path(__file__).resolve().parents[2]))
from ildiet_manifest import generation, manifest
import xml.etree.ElementTree as ET

companion, probe, corelib, directory = (Path(value).resolve() for value in sys.argv[1:])
directory.mkdir(parents=True, exist_ok=True)
original = directory / "original"


def run(*args):
    result = subprocess.run(["dotnet", "exec", *map(str, args)], capture_output=True)
    sys.stdout.buffer.write(result.stdout)
    sys.stderr.buffer.write(result.stderr)
    if result.returncode != 0:
        raise AssertionError((result.returncode, result.stdout.decode("utf-8", errors="replace"), result.stderr.decode("utf-8", errors="replace")))
    return result


run(probe, "--check-script-registration", directory / "registrations")
run(probe, "--create-engine-fixtures", original)
before = {path.name: path.read_bytes() for path in original.glob("*.dll")}
executions = {}
for name, copy in (("stripped", False), ("copy-all", True), ("repeated", False)):
    output = directory / name
    request = ET.Element("ildiet", version="2", input=str(original / "EngineApp.dll"),
                         output=str(output), copyAll=str(copy).lower())
    ET.SubElement(request, "reference", path=str(original / "GodotSharp.dll"))
    ET.SubElement(request, "reference", path=str(corelib))
    ET.SubElement(request, "rewriteAssembly", name="GodotSharp")
    ET.SubElement(request, "conditionalMembers", assembly="EngineApp", baseType="Engine.Object")
    ET.SubElement(request, "suppressDefaultSeeds", assembly="EngineApp", type="EngineFixture.UnusedScript+Helper")
    ET.SubElement(request, "typeRoot", assembly="EngineApp", type="EngineFixture.LiveScript")
    ET.SubElement(request, "typeRoot", assembly="GodotSharp", type="Engine.Object")
    ET.SubElement(request, "methodRoot", assembly="GodotSharp", type="Engine.Constructors", method=".cctor")
    ET.SubElement(request, "registrationAttribute", assembly="GodotSharp", type="Engine.AssemblyHasScriptsAttribute")
    ET.SubElement(request, "constructorRegistry", assembly="GodotSharp", type="Engine.Constructors", method=".cctor")
    request_path = directory / (name + "-request.xml")
    result_path = directory / (name + "-result.xml")
    ET.ElementTree(request).write(request_path, encoding="utf-8", xml_declaration=True)
    execution = run(companion, "--request", request_path, "--result", result_path)
    result, published = manifest(output, result_path)
    executions[name] = (execution.stdout, result, published)
    if copy:
        for filename, content in before.items():
            assert (published / filename).read_bytes() == content, "copy-all rewrote " + filename
        assert result.get("constructorRegistriesRewritten") != "true", "copy-all reported a registry rewrite"
    else:
        assert result.get("constructorRegistriesRewritten") == "true", "registry rewrite result was not reported"
        run(probe, "--check-engine-fixtures", original, published)
        for filename in before:
            run(probe, original / filename, published / filename)
for filename, content in before.items():
    assert (original / filename).read_bytes() == content, "input changed: " + filename
for path in generation(directory / "stripped").glob("*.dll"):
    assert path.read_bytes() == (generation(directory / "repeated") / path.name).read_bytes(), "nondeterministic rewrite: " + path.name
print("engine-policy-validation=metadata,routing,copy-all,determinism,unchanged-inputs")


def state(output):
    result = subprocess.run(["dotnet", "exec", str(probe), "--engine-state", str(output)],
                            capture_output=True, check=True)
    return set(result.stdout.decode("utf-8").splitlines())


def diagnostics(stdout, root, observed):
    rows = [(item.get("scope"), item.get("reason")) for item in root.findall("diagnostic") if item.get("kind") == "registry"]
    assert rows == sorted(rows), "registry XML diagnostics are not ordinally sorted"
    lines = stdout.decode("utf-8").splitlines()
    actual = [line for line in lines if line.startswith("ILDiet: constructor registry ")]
    assert actual == ["ILDiet: constructor registry " + scope + " " + reason for scope, reason in rows], "stdout/XML registry diagnostics disagree"
    if root.get("fallback") != "none":
        assert not rows, "copy fallback reported a rewritten-registry diagnostic"
        return rows
    assert rows, "live constructor registry emitted no diagnostic"
    summary = next(i for i, line in enumerate(lines) if line.startswith("ILDiet: removed "))
    assert all(lines.index(line) < summary for line in actual), "registry diagnostics displaced summary/counter ordering"
    factories = {row.split(" ", 1)[0].removeprefix("factory=") for row in observed if row.startswith("factory=")}
    for scope, reason in rows:
        assert scope == "GodotSharp:Engine.Constructors::.cctor", "registry diagnostic scope changed"
        match = re.fullmatch(r"keeps (\d+) original factories \((.+); examples (.+)\)", reason)
        assert match, "registry diagnostic omitted count, reason, or examples: " + reason
        assert int(match[1]) == len(factories), "diagnostic count disagrees with retained original factory operands"
        assert "unrecognized factory shape" in match[2], "unknown factory retention reason was omitted"
        reasons = match[2].split(", ")
        assert reasons == sorted(set(reasons)), "registry retention reasons are not sorted and distinct"
        examples = match[3].split(", ")
        assert 0 < len(examples) <= 3 and set(examples) <= factories, "registry examples do not name retained original factories"
    return rows


original_state = state(original)
baseline_diagnostics = {}
for name, (stdout, root, published) in executions.items():
    baseline_diagnostics[name] = diagnostics(stdout, root, state(published))
assert baseline_diagnostics["stripped"] == baseline_diagnostics["repeated"], "registry diagnostics changed across identical rewrites"

for name in ("type-root", "wrapper-root", "reflection-wrapper", "reflection-script", "cut-factory", "budget", "full-script"):
    output = directory / name
    request = ET.parse(directory / "stripped-request.xml").getroot()
    request.set("output", str(output))
    if name == "type-root":
        ET.SubElement(request, "typeRoot", assembly="EngineApp", type="EngineFixture.UnusedScript")
    elif name == "wrapper-root":
        ET.SubElement(request, "root", assembly="GodotSharp", type="Engine.Unused")
    elif name == "reflection-wrapper":
        ET.SubElement(request, "reflectionRoot", type="Engine.Unused")
    elif name == "reflection-script":
        ET.SubElement(request, "reflectionRoot", type="EngineFixture.UnusedScript")
    elif name == "cut-factory":
        ET.SubElement(request, "cut", method="Engine.Constructors::FactoryUnused")
    elif name == "budget":
        request.set("workBudget", "1")
    elif name == "full-script":
        ET.SubElement(request, "root", assembly="EngineApp", type="EngineFixture.UnusedScript")
    request_path = directory / (name + "-request.xml")
    ET.ElementTree(request).write(request_path, encoding="utf-8", xml_declaration=True)
    execution = run(companion, "--request", request_path)
    result, published = manifest(output)
    observed = state(published)
    diagnostics(execution.stdout, result, observed)
    if name == "budget":
        assert result.get("fallback") == "budget" and result.get("constructorRegistriesRewritten") == "false"
        assert observed == original_state, "budget fallback changed registry or registration semantics"
        for filename, content in before.items():
            assert (published / filename).read_bytes() == content, "budget fallback rewrote " + filename
        continue
    assert "factory=FactoryUnknownFactoryType constructs=Engine.UnknownFactoryType" in observed, "unknown factory shape lost its original body"
    assert int(result.find("counters").get("registry.factories.unknown")) > 0, "unknown factory fixture did not run"
    registrations = {row for row in observed if row.startswith("script=")}
    assert ("script=EngineFixture.UnusedScript" in registrations) == (name in ("type-root", "full-script")), "metadata-only root changed script registration"
    if name == "reflection-script":
        assert "type=EngineFixture.UnusedScript" in observed
        assert "method=EngineFixture.UnusedScript::Callback stub=True" in observed, "reflection root admitted callback body"
        assert "method=EngineFixture.UnusedScript::.ctor stub=True" in observed, "reflection root admitted constructor body"
    if name == "reflection-wrapper":
        assert "factory=FactoryUnused constructs=Engine.Unused" in observed, "reflection wrapper lost its factory"
        assert "method=Engine.Unused::Unselected stub=True" in observed, "reflection wrapper admitted ordinary body"
        assert "type=Engine.FactoryBodyOnlyDependency" not in observed, "reflection wrapper opened unselected body closure"
    if name in ("wrapper-root", "cut-factory"):
        assert "factory=FactoryUnused constructs=Engine.Unused" in observed, "explicit factory retention was redirected"
    if name == "wrapper-root":
        assert "method=Engine.Unused::Unselected stub=False" in observed
        assert "type=Engine.FactoryBodyOnlyDependency" in observed, "full root lost ordinary body closure"
    if name in ("type-root", "full-script"):
        assert "method=EngineFixture.UnusedScript::Callback stub=False" in observed
    for assembly in result.findall("assembly"):
        if assembly.get("name") == "GodotSharp":
            changed = {row for row in observed if row.startswith("factory=")} != {row for row in original_state if row.startswith("factory=")}
        elif assembly.get("name") == "EngineApp":
            changed = registrations != {row for row in original_state if row.startswith("script=")}
        else:
            changed = False
        assert (assembly.get("registryRewritten") == "true") == changed, "registry flag disagrees with applied registry/attribute plan: " + assembly.get("name")
print("engine-policy-validation=known-factory,unknown-factory,type-root,full-wrapper,reflection-wrapper,reflection-script,cut-factory,budget,full-script,applied-registry-flags,sorted-retention-diagnostics")
