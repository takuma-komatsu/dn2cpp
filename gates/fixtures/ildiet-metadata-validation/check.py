#!/usr/bin/env python3
"""Check rewritten metadata, preserved payloads, and an entirely dead reference."""
import hashlib
import re
import shutil
import xml.etree.ElementTree as ET
from pathlib import Path
import subprocess
import sys
sys.path.insert(0, str(Path(__file__).resolve().parents[2]))
from ildiet_manifest import generation, manifest

if len(sys.argv) == 4 and sys.argv[1] == "--instrument-startup":
    output = Path(sys.argv[2])
    mode = sys.argv[3]
    header = (output / "generated.h").read_text(encoding="utf-8")
    source = output / "generated.cpp"
    text = source.read_text(encoding="utf-8")
    checks = []
    # A type initializer runs only after a type-use trigger or an explicit
    # preserve. Nothing uses OrdinaryCell; SuppressedCell is preserved in the
    # explicit mode and read by Main in the late mode.
    for name in ("OrdinaryCell", "SuppressedCell"):
        done = re.search(r"extern std::atomic<int8_t> (" + name + r"__cctor_\w+__done);", header)
        value = "sf_InitializerFixture_" + name + "_Value"
        if name == "OrdinaryCell" or mode not in ("explicit", "late"):
            assert done is None, "untriggered initializer has a native startup guard: " + name
            assert not re.search(r"dn2cpp_cctor_run_startup\([^;]*" + name, text), \
                "untriggered initializer became a startup root: " + name
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
resolver_before = {path: path.read_bytes() for path in resolver_fixtures.glob("*.dll")}
inputs = [app, library, corelib, signed, dead]
before = {path: hashlib.sha256(path.read_bytes()).digest() for path in inputs}
for name in ("first", "second"):
    run(companion, app, "-r", library, "-r", corelib, "-r", signed,
        "-r", dead, "-o", directory / name)
first = generation(directory / "first")
second = generation(directory / "second")
run(probe, app, first / app.name)
run(probe, library, first / library.name)
run(probe, signed, first / signed.name, "signed")
run(probe, dead, first / dead.name, "empty", "resources")
for path, digest in before.items():
    assert hashlib.sha256(path.read_bytes()).digest() == digest, f"input changed: {path}"
assert sorted(path.name for path in first.iterdir()) == sorted(path.name for path in second.iterdir())
for path in first.iterdir():
    assert path.read_bytes() == (second / path.name).read_bytes(), f"nondeterministic output: {path.name}"
assert (first / corelib.name).read_bytes() == corelib.read_bytes(), "protected CoreLib was rewritten"


def resolver_case(name, references, selected):
    output = directory / name
    resolver_app = resolver_fixtures / "ResolverApp.dll"
    command = [companion, resolver_app]
    for reference in references:
        command.extend(("-r", resolver_fixtures / reference))
    sink = resolver_fixtures / "System.ILDietFixtureSink.dll"
    command.extend(("-r", sink))
    command.extend(("-o", output))
    run(*command)
    protocol, published = manifest(output)
    assert protocol.findall("reference")[-1].get("path") == str((published / sink.name).resolve()), "copied sink moved in the ordered load set"
    assert next(a for a in protocol.findall("assembly") if a.get("name") == sink.stem).get("mode") == "copied", "opaque sink was not copied"
    assert (published / sink.name).read_bytes() == resolver_before[sink], "opaque sink changed"
    checked = subprocess.run(["dotnet", "exec", str(probe), "--check-resolver", str(published), selected],
                             capture_output=True, text=True)
    print(checked.stdout, end="", flush=True)
    print(checked.stderr, end="", file=sys.stderr, flush=True)
    checked.check_returncode()
    assert checked.stdout.startswith("resolver-valid=" + selected + "\n"), "resolver output prefix changed"
    assert "resolver-data-demand=copied-generic-field,copied-generic-method-use,constructor-bodies,accessor-bodies,empty-stripped-generic" in checked.stdout, "resolver data-demand block did not run"


resolver_case("resolver-first", ("ResolverFirst.dll", "ResolverSecond.dll"), "ResolverFirst")
resolver_case("resolver-second", ("ResolverSecond.dll", "ResolverFirst.dll"), "ResolverSecond")
assert all(path.read_bytes() == content for path, content in resolver_before.items()), "resolver input DLL changed"
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
        request = ET.Element("ildiet", version="2", input=str(original.resolve()), output=str(output.resolve()))
        ET.SubElement(request, "reference", path=str(corelib.resolve()))
        ET.SubElement(request, "reference", path=str((corelib.parent / "System.Console.dll").resolve()))
        ET.SubElement(request, "suppressDefaultSeeds", assembly="InitializerFixture", type="InitializerFixture.SuppressedCell")
        if name == "explicit":
            ET.SubElement(request, "methodRoot", assembly="InitializerFixture", type="InitializerFixture.SuppressedCell", method=".cctor")
        request_path = initializer_fixtures / (name + "-request.xml")
        ET.ElementTree(request).write(request_path, encoding="utf-8", xml_declaration=True)
        run(companion, "--request", request_path, "--result", initializer_fixtures / (name + "-result.xml"))
        published = generation(output)
        run(probe, "--check-initializer-fixture", original, published / original.name, name)
        run(probe, original, published / original.name)
    native = output / "native" / "bin" / probe.parent.parent.name / probe.parent.name
    native.mkdir(parents=True, exist_ok=True)
    shutil.copyfile((output if name == "original" else published) / original.name, native / original.name)
    for extension in (".runtimeconfig.json", ".deps.json"):
        shutil.copyfile(original.with_suffix(extension), native / original.with_suffix(extension).name)
    if name == "explicit":
        # The transpiler runs without ILDiet, so it takes the same explicit root.
        keep = ET.Element("linker")
        keep_type = ET.SubElement(ET.SubElement(keep, "assembly", fullname="InitializerFixture"),
                                  "type", fullname="InitializerFixture.SuppressedCell")
        ET.SubElement(keep_type, "method", name=".cctor")
        ET.ElementTree(keep).write(output / "native" / "link.xml", encoding="utf-8", xml_declaration=True)
print("metadata-validation=suppressed-initializer,explicit-root,late-code-root,ordinary-generic-initializer")

backend_fixtures = directory / "backend-policy-fixtures"
backend_original = backend_fixtures / "original"
run(probe, "--create-backend-policy-fixture", backend_original)
backend_before = {path.name: path.read_bytes() for path in backend_original.glob("*.dll")}
for name, copy_all in (("stripped", False), ("repeated", False), ("copy-all", True)):
    output = backend_fixtures / name
    request = ET.Element("ildiet", version="2", input=str((backend_original / "PolicyApp.dll").resolve()),
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
    result, published = manifest(output, result_path)
    if copy_all:
        assert result.get("constructorRegistriesRewritten") != "true", "copy-all reported a policy rewrite"
        for filename, content in backend_before.items():
            assert (published / filename).read_bytes() == content, "copy-all changed " + filename
    else:
        assert result.get("constructorRegistriesRewritten") == "true", "ancestor routing was not reported"
        run(probe, "--check-backend-policy-fixture", backend_original, published)
        for filename in backend_before:
            run(probe, backend_original / filename, published / filename)
for filename, content in backend_before.items():
    assert (backend_original / filename).read_bytes() == content, "policy input changed: " + filename
    assert (generation(backend_fixtures / "stripped") / filename).read_bytes() == (generation(backend_fixtures / "repeated") / filename).read_bytes(), \
        "nondeterministic policy rewrite: " + filename
print("metadata-validation=backend-runtime-policy,signature-only-declarations,copy-all,determinism,unchanged-inputs")
