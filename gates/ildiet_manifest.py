"""Read and independently validate an ILDiet publication used by gate fixtures."""
from pathlib import Path
import re
import xml.etree.ElementTree as ET


def fnv1a(content):
    value = 14695981039346656037
    for byte in content:
        value = ((value ^ byte) * 1099511628211) & 0xffffffffffffffff
    return f"{value:016x}"


def manifest(output, result=None):
    output = Path(output).resolve()
    root = ET.parse(result or output / "ildiet-result.xml").getroot()
    assert root.tag == "ildietResult" and root.get("version") == "2", "invalid ILDiet protocol version"
    name = root.get("generation", "")
    assert re.fullmatch(r"g-[0-9a-f]{16}", name), "invalid ILDiet generation"
    generation = output / name
    assert generation.is_dir(), "published generation is missing"
    assert root.get("fallback") in ("none", "hot-update", "generic-cut", "budget"), "invalid fallback"
    assert re.fullmatch(r"[0-9a-f]{16}", root.get("contract", "")), "invalid contract digest"
    assert root.get("cutsValidated") in ("true", "false"), "invalid cut-validation state"
    for value in (root.get("input"), root.get("preservation"), *(r.get("path") for r in root.findall("reference"))):
        path = Path(value).resolve()
        assert path.parent == generation and path.is_file(), "manifest points outside generation: " + str(path)
    assemblies = root.findall("assembly")
    assert assemblies and len({a.get("name") for a in assemblies}) == len(assemblies), "invalid assembly inventory"
    assert {a.get("name") + ".dll" for a in assemblies} == {p.name for p in generation.glob("*.dll")}, "incomplete assembly inventory"
    for assembly in assemblies:
        assert assembly.get("mode") in ("rewritten", "copied"), "invalid assembly mode"
        path = generation / (assembly.get("name") + ".dll")
        assert assembly.get("hash") == fnv1a(path.read_bytes()), "assembly hash mismatch: " + path.name
        assert assembly.get("registryRewritten") in ("true", "false"), "missing registry mode"
        assert assembly.get("registryRewritten") != "true" or assembly.get("mode") == "rewritten", "copied registry changed"
    assert (root.get("constructorRegistriesRewritten") == "true") == any(a.get("registryRewritten") == "true" for a in assemblies), "registry aggregate mismatch"
    return root, generation


def generation(output):
    return manifest(output)[1]


def framed_hash(values):
    framed = bytearray()
    for value in values:
        value = value.encode("utf-8") if isinstance(value, str) else value
        framed.extend(len(value).to_bytes(8, "little"))
        framed.extend(value)
    return fnv1a(framed)


def contract_digest(request):
    kinds = {"root", "reflectionRoot", "rewriteAssembly", "skipAssembly", "typeRoot", "methodRoot", "conditionalMembers",
             "suppressDefaultSeeds", "registrationAttribute", "constructorRegistry", "cut", "feature"}
    clauses = []
    for child in request:
        if child.tag in kinds:
            values = [child.tag]
            for key, value in sorted(child.attrib.items()):
                values.extend((key, value))
            clauses.append(framed_hash(values))
    return framed_hash(["ildiet-contract-v2", request.get("copyAll", "false"), *sorted(clauses)])
