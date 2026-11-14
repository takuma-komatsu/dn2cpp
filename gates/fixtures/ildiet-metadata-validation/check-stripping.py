#!/usr/bin/env python3
"""Type-wide static closure agrees before and after DLL rewriting."""
from pathlib import Path
import os
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

sys.path.insert(0, str(Path(__file__).resolve().parents[2]))
from ildiet_manifest import contract_digest, manifest

companion, probe, transpiler, directory = (Path(value).resolve() for value in sys.argv[1:5])
profile_args = sys.argv[5:]
cli = Path(profile_args.pop(0)).resolve() if profile_args and not profile_args[0].startswith("--") else (
    Path(__file__).resolve().parents[3] / "src" / "Dn2Cpp.Cli" / "bin"
    / transpiler.parent.parent.name / transpiler.parent.name / "dn2cpp.dll")
if profile_args not in ([], ["--representative"]):
    raise SystemExit("expected an optional --representative profile")
modes = ("full",) if profile_args else ("raw", "full")
directory.mkdir(parents=True, exist_ok=True)
original = directory / "original"


def run(*args, success=True):
    result = subprocess.run(["dotnet", "exec", *map(str, args)], capture_output=True, text=True,
                            env={**os.environ, "DN2CPP_STRICT_COMPLETION": "1"})
    if (result.returncode == 0) != success:
        raise AssertionError((result.returncode, result.stdout, result.stderr))
    return result


def check_core(published, level):
    result = run(probe, "--check-stripping-core", transpiler, published, level)
    assert "stripping-core=" + level in result.stdout, "core policy block did not run"


def startup_pass(text):
    """The work main runs before the entry point, in order: each module initializer of
    the startup pass, then the entry type's guard. A guard is named by its symbol stem,
    since an ILDiet rewrite renumbers rows per level."""
    main = text[text.index("DN2CPP_RT_EXPORT int main("):]
    main = main[:main.index("} catch (")]
    runs = re.findall(r'dn2cpp_cctor_run_startup\(&\w+__ensure, "([^"]+)"\);', main)
    entry = re.findall(r"\btry \{\s*(\w+?)_m\d+__ensure\(\);", main)
    return runs + ["entry " + stem for stem in entry]


assert "stripping-fixture=static-closure" in run(probe, "--create-stripping-fixture", original).stdout
publications = {}
contracts = {}
for name in ("default", "high", "medium"):
    output = directory / name
    request = ET.Element("ildiet", version="2", input=str(original / "MediumApp.dll"), output=str(output))
    if name != "default":
        request.set("strippingLevel", name)
    for filename in ("MediumLib.dll", "System.Private.CoreLib.dll"):
        ET.SubElement(request, "reference", path=str(original / filename))
    ET.SubElement(request, "reflectionRoot", type="MediumFixture.MetadataOnly")
    ET.SubElement(request, "linkXml", path=str(original / "link.xml"))
    path = directory / (name + "-request.xml")
    ET.ElementTree(request).write(path, encoding="utf-8", xml_declaration=True)
    run(companion, "--request", path)
    root, published = manifest(output)
    assert root.get("fallback") == "none", "static closure exceeded the finite graph"
    assert root.get("contract") == contract_digest(request), "stripping level missing from request contract"
    publications[name] = published
    contracts[name] = root.get("contract")
    level = "high" if name == "default" else name
    assert "stripping-ildiet=" + level in run(probe, "--check-stripping-fixture", published, level).stdout
    check_core(published, level)

assert contracts["default"] == contracts["high"] != contracts["medium"], "level contract identity is wrong"
for before in publications["default"].iterdir():
    assert before.read_bytes() == (publications["high"] / before.name).read_bytes(), "explicit High changed default output"
check_core(original, "high")
check_core(original, "medium")

for args in (("--stripping-level",), ("--stripping-level", "low"),
             ("--request", directory / "medium-request.xml", "--stripping-level", "high")):
    run(companion, *args, success=False)

# Direct CLI selection and captured request selection must retain the same DLLs.
direct = directory / "direct"
run(companion, original / "MediumApp.dll", "-r", original / "MediumLib.dll", "-r",
    original / "System.Private.CoreLib.dll", "--link-xml", original / "link.xml",
    "--stripping-level", "medium", "-o", direct)
_, published = manifest(direct)
assert "stripping-ildiet=medium-direct" in run(probe, "--check-stripping-fixture", published, "medium-direct").stdout
assert "stripping-core=medium-direct" in run(probe, "--check-stripping-core", transpiler, published, "medium-direct").stdout


def emitted_files(output):
    return {path.name: path.read_bytes() for path in output.glob("generated*") if path.is_file()}


for mode in modes:
    defaults = None
    for level in ("default", "high", "medium"):
        output = directory / ("emission-" + level + "-" + mode)
        arguments = [original / "MediumApp.dll", "-r", original / "MediumLib.dll", "-r",
                     original / "System.Private.CoreLib.dll", "--link-xml", original / "link.xml",
                     "--reflection-root", "MediumFixture.MetadataOnly", "--trim-reflection",
                     "--no-metadata-compression", "--no-shared-generics", "-o", output]
        if mode == "raw":
            arguments.append("--no-ildiet")
        if level != "default":
            arguments.extend(("--stripping-level", level))
        run(cli, *arguments)
        files = emitted_files(output)
        assert files, "CLI emitted no C++"
        if level == "default":
            defaults = files
        if level == "high":
            assert files == defaults, "explicit High changed generated C++"
        text = "\n".join(content.decode("utf-8") for content in files.values())
        # Medium retention schedules no initializer at startup: both levels run the same
        # initializers before Main, and every other initializer waits for its first trigger.
        if level == "high":
            high_startup = startup_pass(text)
            assert high_startup == ["<Module>, MediumLib", "<Module>, MediumApp", "entry Program__cctor"], \
                "unexpected startup pass: " + repr(high_startup)
        elif level == "medium":
            assert startup_pass(text) == high_startup, \
                "Medium retention changed the startup pass: " + repr(startup_pass(text))
        ctor = re.search(r"\bUsed__ctor_\w+\([^;]*?\)\s*\{", text)
        assert bool(ctor) == (level == "medium"), "wrong emitted private constructor policy"
        for owner in ("AllocationTrigger", "VirtualTrigger", "GenericVirtualTrigger", "EqualityTrigger",
                      "HashTrigger", "StaticReadTrigger"):
            assert bool(re.search(r"\b" + owner + r"_Dormant_\w+\([^;]*?\)\s*\{", text)) == (level == "medium"), \
                "wrong emitted type-wide retention of an initializer trace owner: " + owner
        assert not re.search(r"\b(?:Unreferenced|MetadataOnly)_MustStayAbsent_\w+\([^;]*?\)\s*\{", text), \
            "metadata discovery emitted an unrelated body"
        if level != "medium":
            continue
        for member in ("Used__ctor", "Used_get_Value", "Used_set_Value", "Used_add_Changed",
                       "Used_remove_Changed", "MediumWitness_FrameworkPrivate", "MediumWitness__ctor",
                       "MediumWitness_get_FrameworkValue", "MediumWitness_set_FrameworkValue",
                       "MediumWitness_add_FrameworkChanged", "MediumWitness_remove_FrameworkChanged"):
            assert re.search(r"\(void\*\)&" + member + r"_\w+, \(void\*\)&inv_", text), \
                "retained member lacks a reflection invoker: " + member
        for table in ("fldtab", "proptab", "eventtab", "ctortab"):
            assert table + "_MediumFixture_Used" in text, "missing retained metadata table: " + table
            assert table + "_System_MediumWitness" in text, "missing retained framework metadata table: " + table
        assert "&fldget_MediumFixture_Used_f_PrivateField" in text and "&fldset_MediumFixture_Used_f_PrivateField" in text, \
            "private field accessors were removed"
        assert "&fldget_System_MediumWitness_f_FrameworkField" in text and "&fldset_System_MediumWitness_f_FrameworkField" in text, \
            "private framework field accessors were removed"
        assert '"Generic\\0"' in text, "uninstantiated generic method definition row was removed"
        assert re.search(r"md_native_refl_ti_System_MediumWitness\[\] = \{\s*\{[^\n]*attrtab_", text), \
            "framework type attributes were removed"
        assert re.search(r"\(void\*\)&MediumWitness_FrameworkPrivate_\w+, \(void\*\)&inv_[^,]+, attrtab_", text), \
            "framework method attributes were removed"
        assert re.search(r"Dn2CppObject\* attrcreate_\w+\(\)\s*\{\s*MediumMarkerAttribute__cctor_\w+__ensure\(\);", text), \
            "attribute construction lost its first-use initializer guard"
        for owner in ("SignatureTarget", "BodyTarget", "DeferredBox", "MediumMarkerAttribute"):
            assert re.search(r"inline void " + owner + r"__cctor_\w+__ensure\(\)", text), \
                "retained initializer lacks a first-use guard"
            assert not re.search(r"dn2cpp_cctor_run_startup\(&" + owner + r"__cctor_", text), \
                "retention scheduled an initializer at startup"

# Exercise both sides of deferred used-slot × boxed-type discovery. This graph
# boxes normally, while only a retained body's Object.Equals creates the demand.
retained_equality = directory / "retained-equality"
run(probe, "--create-stripping-fixture", retained_equality, "retained-equality")
check_core(retained_equality, "medium")
for mode in modes:
    output = directory / ("emission-retained-equality-" + mode)
    arguments = [retained_equality / "MediumApp.dll", "-r", retained_equality / "MediumLib.dll", "-r",
                 retained_equality / "System.Private.CoreLib.dll", "--link-xml", retained_equality / "link.xml",
                 "--reflection-root", "MediumFixture.MetadataOnly", "--trim-reflection",
                 "--no-metadata-compression", "--no-shared-generics", "--stripping-level", "medium", "-o", output]
    if mode == "raw":
        arguments.append("--no-ildiet")
    run(cli, *arguments)
    if mode == "full":
        _, published = manifest(output / "ildiet")
        check_core(published, "medium")
    text = "\n".join(content.decode("utf-8") for content in emitted_files(output).values())
    assert re.search(r"inline void DormantDeferredTarget__cctor_\w+__ensure\(\)", text), \
        "retained equality demand lost the initializer guard"
    assert not re.search(r"dn2cpp_cctor_run_startup\(&DormantDeferredTarget__cctor_", text), \
        "retained equality demand scheduled an initializer at startup"
    high = directory / ("emission-retained-equality-high-" + mode)
    arguments[arguments.index("medium")] = "high"
    arguments[arguments.index(output)] = high
    run(cli, *arguments)
    high_text = "\n".join(content.decode("utf-8") for content in emitted_files(high).values())
    assert startup_pass(text) == startup_pass(high_text), \
        "retained equality demand changed the startup pass: " + repr(startup_pass(text))

forwarded = directory / "forwarded-attribute"
run(probe, "--create-stripping-forwarder-fixture", original, forwarded)
for mode in modes:
    output = directory / ("emission-forwarded-attribute-" + mode)
    arguments = [forwarded / "MediumApp.dll", "-r", forwarded / "MediumLib.dll", "-r",
                 forwarded / "System.Private.CoreLib.dll", "-r", forwarded / "MediumFacade.dll",
                 "--link-xml", forwarded / "link.xml", "--reflection-root", "MediumFixture.MetadataOnly",
                 "--trim-reflection", "--no-metadata-compression", "--no-shared-generics",
                 "--stripping-level", "medium", "-o", output]
    if mode == "raw":
        arguments.append("--no-ildiet")
    run(cli, *arguments)
    text = "\n".join(content.decode("utf-8") for content in emitted_files(output).values())
    assert re.search(r"\bForwardedArgumentTarget_Marker_\w+\([^;]*?\)\s*\{", text), \
        "an enum width error lost the following Type-valued attribute dependency"
    assert re.search(r"\(void\*\)&Used_Follow_\w+, \(void\*\)&inv_[^,]+, attrtab_", text), \
        "forwarded enum decoding dropped the emitted attribute row"
    assert re.search(r"o->f_Flags\s*=\s*[^;]*4294967298", text), "forwarded Int64 enum value was truncated"
    assert re.search(r"o->f_Sentinel\s*=\s*[^;]*73", text), "the named value following the enum was misdecoded"

missing = run(cli, forwarded / "MediumApp.dll", "-r", forwarded / "MediumLib.dll", "-r",
              forwarded / "System.Private.CoreLib.dll", "--no-ildiet", "--stripping-level", "medium",
              "-o", directory / "missing-attribute-facade", success=False)
diagnostic = missing.stdout + missing.stderr
assert missing.returncode == 2 and "Unhandled exception" not in diagnostic, "unresolved enum escaped the CLI diagnostic"
for fact in ("MediumFixture.ForwardedAttribute", "MediumLib", "MediumFixture.Used.Follow",
             "MediumFixture.WideFlags, MediumFacade", "encoded width is unknown", "Medium stripping while retaining"):
    assert fact in diagnostic, "missing attribute failure provenance: " + fact
print("stripping-levels=high-compatible,medium-static-closure,metadata-inert,known-generics,all-assemblies,lazy-initializer,emitted-metadata;modes=" + ",".join(modes))

from intercept_retention import validate_intercept_retention
validate_intercept_retention(probe, cli, original, directory, modes=modes)


def medium_emission(source, output, success=True):
    return run(cli, source / "MediumApp.dll", "-r", source / "System.Private.CoreLib.dll", "-r", source / "MediumLib.dll",
               "--stripping-level", "medium", "--no-metadata-compression", "--no-shared-generics", "-o", output,
               success=success)


# The collector-aware move lowering accepts only the CoreLib byte-count shape.
shape = directory / "buffer-move-shape"
assert "stripping-fixture=buffer-move-shape" in run(probe, "--create-stripping-buffer-move-shape-fixture", original, shape).stdout
refused = medium_emission(shape, directory / "emission-buffer-move-shape", success=False)
assert refused.returncode == 2 and "Unhandled exception" not in refused.stderr, refused.stderr
assert "System.Buffer::BulkMoveWithWriteBarrierInternal(Byte&, Byte&, Int32)" in refused.stderr, refused.stderr

# Retaining an unavailable PAL or QCall import emits a callable refusal and never binds the import.
imports = directory / "unavailable-imports"
assert "stripping-fixture=unavailable-imports" in run(probe, "--create-stripping-unavailable-imports-fixture", original, imports).stdout
output = directory / "emission-unavailable-imports"
medium_emission(imports, output)
text = "\n".join(p.read_text() for p in [output / "generated.h", *output.glob("generated*.cpp")])
for owner, refusal in (("Globalization", "requires the ICU globalization PAL"), ("Kernel32", "requires the CoreCLR runtime")):
    for member in ("CallImport", "AddressImport"):
        assert re.search(r"\b" + owner + "_" + member + r"_\w+\([^;]*?\)\s*\{", text), "Medium dropped " + owner + "." + member
    assert refusal in text, "no callable refusal for " + owner
for name in ("pinvoke-libs.txt", "pinvoke-symbols.txt"):
    listed = (output / name).read_text().lower() if (output / name).exists() else ""
    assert "globalization" not in listed and "qcall" not in listed and "closehandle" not in listed, (name, listed)
print("stripping-medium-imports=buffer-move-shape-refused,unavailable-imports-unbound")
