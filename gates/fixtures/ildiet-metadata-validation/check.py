#!/usr/bin/env python3
"""Check rewritten metadata, preserved payloads, and an entirely dead reference."""
import hashlib
import re
import shutil
import xml.etree.ElementTree as ET
from pathlib import Path
import subprocess
import sys

if len(sys.argv) == 4 and sys.argv[1] == "--instrument-startup":
    output = Path(sys.argv[2])
    mode = sys.argv[3]
    header = (output / "generated.h").read_text(encoding="utf-8")
    source = output / "generated.cpp"
    text = source.read_text(encoding="utf-8")
    checks = []
    for name in ("OrdinaryCell", "SuppressedCell"):
        done = re.search(r"extern std::atomic<int8_t> (" + name + r"__cctor_\w+__done);", header)
        value = "sf_InitializerFixture_" + name + "_Value"
        if name == "SuppressedCell" and mode == "signature":
            assert done is None, "suppressed initializer has a native startup guard"
            assert not re.search(r"dn2cpp_cctor_run_startup\([^;]*SuppressedCell", text), "suppressed initializer became a startup root"
            checks.append(f"{value} == 0")
        else:
            assert done is not None, "rooted initializer has no completion guard: " + name
            checks.extend((f"{done[1]}.load(std::memory_order_acquire) == 1", f"{value} == 7"))
    entry = text.index("    try {", text.index("int main("))
    witness = "    if (!(" + " && ".join(checks) + ")) {\n"
    witness += '        std::fprintf(stderr, "initializer startup state failed\\n");\n'
    witness += "        dn2cpp_main_exit(66);\n        return 66;\n    }\n"
    source.write_text(text[:entry] + witness + text[entry:], encoding="utf-8")
    sys.exit(0)

companion, probe, app, library, corelib, directory = map(Path, sys.argv[1:])
directory.mkdir(parents=True, exist_ok=True)
dead = directory / "ILDietDeadReference.dll"
signed = companion.parent / "Mono.Cecil.dll"


def run(*arguments):
    subprocess.run(["dotnet", "exec", *map(str, arguments)], check=True)


run(probe, "--create-dead", dead)
resolver_fixtures = directory / "resolver-fixtures"
run(probe, "--create-resolver-fixtures", resolver_fixtures)
inputs = [app, library, corelib, signed, dead]
before = {path: hashlib.sha256(path.read_bytes()).digest() for path in inputs}
for name in ("first", "second"):
    run(companion, app, "-r", library, "-r", corelib, "-r", signed,
        "-r", dead, "-o", directory / name)
first = directory / "first"
run(probe, app, first / app.name)
run(probe, library, first / library.name)
run(probe, signed, first / signed.name, "signed")
run(probe, dead, first / dead.name, "empty", "resources")
for path, digest in before.items():
    assert hashlib.sha256(path.read_bytes()).digest() == digest, f"input changed: {path}"
assert sorted(path.name for path in first.iterdir()) == sorted(path.name for path in (directory / "second").iterdir())
for path in first.iterdir():
    assert path.read_bytes() == (directory / "second" / path.name).read_bytes(), f"nondeterministic output: {path.name}"
assert (first / corelib.name).read_bytes() == corelib.read_bytes(), "protected CoreLib was rewritten"


def resolver_case(name, references, selected):
    output = directory / name
    resolver_app = resolver_fixtures / "ResolverApp.dll"
    command = [companion, resolver_app]
    for reference in references:
        command.extend(("-r", resolver_fixtures / reference))
    command.extend(("-o", output))
    run(*command)
    run(probe, "--check-resolver", output, selected)


resolver_case("resolver-first", ("ResolverFirst.dll", "ResolverSecond.dll"), "ResolverFirst")
resolver_case("resolver-second", ("ResolverSecond.dll", "ResolverFirst.dll"), "ResolverSecond")
print("metadata-validation=resources,identity,empty-reference,token-integrity,determinism,resolver-order,nested-resolver,missing-type")
print("metadata-validation=generic-constructors")
print("metadata-validation=generic-data-properties")
sys.stdout.flush()

initializer_fixtures = directory / "initializer-fixtures"
for name in ("signature", "explicit", "late", "original"):
    original = initializer_fixtures / ("input-" + name) / "InitializerFixture.dll"
    run(probe, "--create-initializer-fixture", original, name)
    output = initializer_fixtures / name
    if name == "original":
        output.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(original, output / original.name)
    else:
        request = ET.Element("ildiet", input=str(original.resolve()), output=str(output.resolve()))
        ET.SubElement(request, "reference", path=str(corelib.resolve()))
        ET.SubElement(request, "reference", path=str((corelib.parent / "System.Console.dll").resolve()))
        ET.SubElement(request, "suppressDefaultSeeds", assembly="InitializerFixture", type="InitializerFixture.SuppressedCell")
        if name == "explicit":
            ET.SubElement(request, "methodRoot", assembly="InitializerFixture", type="InitializerFixture.SuppressedCell", method=".cctor")
        request_path = initializer_fixtures / (name + "-request.xml")
        ET.ElementTree(request).write(request_path, encoding="utf-8", xml_declaration=True)
        run(companion, "--request", request_path, "--result", initializer_fixtures / (name + "-result.xml"))
        run(probe, "--check-initializer-fixture", original, output / original.name, name)
        run(probe, original, output / original.name)
    native = output / "native" / "bin" / probe.parent.parent.name / probe.parent.name
    native.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(output / original.name, native / original.name)
    for extension in (".runtimeconfig.json", ".deps.json"):
        shutil.copyfile(original.with_suffix(extension), native / original.with_suffix(extension).name)
print("metadata-validation=suppressed-initializer,explicit-root,late-code-root,ordinary-generic-initializer")

backend_fixtures = directory / "backend-policy-fixtures"
backend_original = backend_fixtures / "original"
run(probe, "--create-backend-policy-fixture", backend_original)
backend_before = {path.name: path.read_bytes() for path in backend_original.glob("*.dll")}
for name, copy_all in (("stripped", False), ("repeated", False), ("copy-all", True)):
    output = backend_fixtures / name
    request = ET.Element("ildiet", input=str((backend_original / "PolicyApp.dll").resolve()),
                         output=str(output.resolve()), copyAll=str(copy_all).lower())
    ET.SubElement(request, "reference", path=str((backend_original / "PolicyLibrary.dll").resolve()))
    ET.SubElement(request, "reference", path=str(corelib.resolve()))
    ET.SubElement(request, "rewriteAssembly", name="PolicyLibrary")
    ET.SubElement(request, "conditionalMembers", assembly="PolicyApp", baseType="Policy.Root")
    ET.SubElement(request, "typeRoot", assembly="PolicyApp", type="PolicyFixture.ExplicitScript")
    ET.SubElement(request, "methodRoot", assembly="PolicyLibrary", type="Policy.Registry", method=".cctor")
    ET.SubElement(request, "registrationAttribute", assembly="PolicyLibrary", type="Policy.RegistrationsAttribute")
    ET.SubElement(request, "constructorRegistry", assembly="PolicyLibrary", type="Policy.Registry", method=".cctor")
    request_path = backend_fixtures / (name + "-request.xml")
    result_path = backend_fixtures / (name + "-result.xml")
    ET.ElementTree(request).write(request_path, encoding="utf-8", xml_declaration=True)
    run(companion, "--request", request_path, "--result", result_path)
    result = ET.parse(result_path).getroot()
    if copy_all:
        assert result.get("constructorRegistriesRewritten") != "true", "copy-all reported a policy rewrite"
        for filename, content in backend_before.items():
            assert (output / filename).read_bytes() == content, "copy-all changed " + filename
    else:
        assert result.get("constructorRegistriesRewritten") == "true", "ancestor routing was not reported"
        run(probe, "--check-backend-policy-fixture", backend_original, output)
        for filename in backend_before:
            run(probe, backend_original / filename, output / filename)
for filename, content in backend_before.items():
    assert (backend_original / filename).read_bytes() == content, "policy input changed: " + filename
    assert (backend_fixtures / "stripped" / filename).read_bytes() == (backend_fixtures / "repeated" / filename).read_bytes(), \
        "nondeterministic policy rewrite: " + filename
print("metadata-validation=backend-runtime-policy,signature-only-declarations,copy-all,determinism,unchanged-inputs")
