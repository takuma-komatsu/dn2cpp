#!/usr/bin/env python3
"""Check conditional script roots and constructor routing before model loading."""
from pathlib import Path
import subprocess
import sys
import xml.etree.ElementTree as ET

companion, probe, corelib, directory = (Path(value).resolve() for value in sys.argv[1:])
directory.mkdir(parents=True, exist_ok=True)
original = directory / "original"


def run(*args):
    subprocess.run(["dotnet", "exec", *map(str, args)], check=True)


run(probe, "--create-engine-fixtures", original)
before = {path.name: path.read_bytes() for path in original.glob("*.dll")}
for name, copy in (("stripped", False), ("copy-all", True), ("repeated", False)):
    output = directory / name
    request = ET.Element("ildiet", input=str(original / "EngineApp.dll"),
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
    run(companion, "--request", request_path, "--result", result_path)
    result = ET.parse(result_path).getroot()
    if copy:
        for filename, content in before.items():
            assert (output / filename).read_bytes() == content, "copy-all rewrote " + filename
        assert result.get("constructorRegistriesRewritten") != "true", "copy-all reported a registry rewrite"
    else:
        assert result.get("constructorRegistriesRewritten") == "true", "registry rewrite result was not reported"
        run(probe, "--check-engine-fixtures", original, output)
        for filename in before:
            run(probe, original / filename, output / filename)
for filename, content in before.items():
    assert (original / filename).read_bytes() == content, "input changed: " + filename
for path in (directory / "stripped").glob("*.dll"):
    assert path.read_bytes() == (directory / "repeated" / path.name).read_bytes(), "nondeterministic rewrite: " + path.name
print("engine-policy-validation=metadata,routing,copy-all,determinism,unchanged-inputs")
