#!/usr/bin/env python3
"""Pin definition scans, bounded fallback and atomic immutable publications."""
from pathlib import Path
import os
import subprocess
import sys
import xml.etree.ElementTree as ET

sys.path.insert(0, str(Path(__file__).resolve().parents[2]))
from ildiet_manifest import contract_digest, framed_hash, manifest

companion, probe, corelib, directory = (Path(value).resolve() for value in sys.argv[1:])
directory.mkdir(parents=True, exist_ok=True)


def run(*arguments, success=True, env=None):
    result = subprocess.run(["dotnet", "exec", *map(str, arguments)], capture_output=True,
                            env={**os.environ, **(env or {})})
    if (result.returncode == 0) != success:
        raise AssertionError((result.returncode, result.stdout.decode("utf-8", errors="replace"),
                              result.stderr.decode("utf-8", errors="replace")))
    return result


def request_for(original, output, **attributes):
    request = ET.Element("ildiet", version="2", input=str(original / "GraphApp.dll"),
                         output=str(output), **attributes)
    ET.SubElement(request, "reference", path=str(original / "GraphLib.dll"))
    ET.SubElement(request, "reference", path=str(corelib))
    return request


def execute(request, name, **options):
    path = directory / (name + "-request.xml")
    ET.ElementTree(request).write(path, encoding="utf-8", xml_declaration=True)
    return run(companion, "--request", path, **options)


def validate(output, request=None):
    root, published = manifest(output)
    if request is not None:
        assert root.get("contract") == contract_digest(request), "request contract changed in publication"
    values = []
    for item in root.findall("assembly"):
        filename = item.get("name") + ".dll"
        values.extend((filename, (published / filename).read_bytes()))
    values.extend(("preservation.xml", (published / "preservation.xml").read_bytes()))
    assert root.get("generation") == "g-" + framed_hash(values), "generation hash does not describe its payload"
    return root, published


def payload(published):
    return {path.name: path.read_bytes() for path in published.iterdir() if path.is_file()}


def preservation_contract(published):
    path = published / "preservation.xml"
    root = ET.parse(path).getroot()
    expected = {
        ("GraphLib", "Graph.BoxingPreserveFieldOwner", "field", "Graph.BoxingPreserveFieldPayload Value"),
        ("GraphLib", "Graph.BoxingPreserveReturnOwner", "method", "Graph.BoxingPreserveReturnPayload Read()"),
        ("GraphLib", "Graph.BoxingPreserveRefOwner", "method", "System.Void Read(Graph.BoxingPreserveRefPayload&)"),
    }
    actual = []
    assert root.tag == "linker" and not root.attrib and len(root) == 1
    for assembly in root:
        assert assembly.tag == "assembly" and assembly.attrib == {"fullname": "GraphLib", "preserve": "nothing"} and len(assembly) == 3, "preservation changed an assembly contract"
        for owner in assembly:
            assert owner.tag == "type" and set(owner.attrib) == {"fullname", "preserve"} and owner.get("preserve") == "nothing" and len(owner) == 1, "preservation changed a type contract"
            for member in owner:
                assert set(member.attrib) == {"signature"}, "preservation changed a member selector"
                actual.append((assembly.get("fullname"), owner.get("fullname"), member.tag, member.get("signature")))
    assert len(actual) == len(expected) and set(actual) == expected, "publication changed the original three explicit member rules"
    return path.read_bytes()


originals = {}
original_relations = {}
for callers in (2, 40):
    original = directory / ("input-" + str(callers))
    result = run(probe, "--create-graph-fixtures", original, corelib, callers)
    assert f"fixture-created=definition-graph callers={callers}".encode() in result.stdout
    contexts = run(probe, "--check-graph-contexts", original, callers, "original")
    assert f"graph-contexts=original:closed-callers={callers}".encode() in contexts.stdout
    original_relations[callers] = {filename: set(run(probe, "--relations", original / filename).stdout.splitlines())
                                   for filename in ("GraphApp.dll", "GraphLib.dll")}
    originals[callers] = original
assert original_relations[2] == original_relations[40], "closed caller count changed the original definition/relation set"

outputs = {}
for callers in (2, 40):
    output = directory / ("scan-" + str(callers))
    request = request_for(originals[callers], output)
    result = execute(request, "scan-" + str(callers))
    root, published = validate(output, request)
    preservation_contract(published)
    assert root.get("fallback") == "none", "definition graph unexpectedly fell back"
    counters = root.find("counters").attrib
    assert counters["decode.max-per-method"] == "1", "method body decoded more than once"
    assert int(counters["decode.body"]) > 0 and int(counters["summary.methods"]) > 0, "graph fixture did not run scans and reflection summaries"
    assert int(counters["rules.demand"]) > 0, "generic demand fixture did not propagate a demand"
    fixture = run(probe, "--check-graph-fixtures", published).stdout.decode("utf-8")
    contexts = run(probe, "--check-graph-contexts", published, callers, "published")
    assert f"graph-contexts=published:closed-callers={callers}".encode() in contexts.stdout, "closed generic binding demand block did not run"
    assert "graph-fixtures=" in fixture
    assert "graph-type-arrays=alternative-arities,repeated-types,element-alternatives,generic-type-tuples,generic-method-tuples,positional-demands,generic-argument-vectors" in fixture, "Type[] alternative fixture block did not run"
    closure = run(probe, "--check-token-closure", *sorted(published.glob("*.dll")))
    assert b"token-closure=valid operands=" in closure.stdout, "operand closure block did not run"
    for filename in ("GraphApp.dll", "GraphLib.dll"):
        before = original_relations[callers][filename]
        after = set(run(probe, "--relations", published / filename).stdout.splitlines())
        assert after <= before, "retained relation changed: " + repr(after - before)
        assert after, "relation check examined no definitions"
    assert "graph-reflection=activator-named-app,activator-named-library,assembly-named-app,assembly-named-library,default-field-box,nested-field-box,nullable-field-box,byref-field-box,generic-field-box,getmember-ctor,getmembers-ctor,differently-named-methodimpl" in fixture, "reflection retention fixture block did not run"
    for section in ("plain-field", "plain-getter", "plain-method", "plain-accessor", "base", "argument", "element", "generic-definition", "generic-method", "parameter", "assembly", "nested", "interfaces", "argument-key", "argument-key-array", "invoke-member-concrete", "invoke-member-symbolic", "invoke-member-flags", "type-initializer", "type-initializer-symbolic", "nullable-inexact", "nullable-inexact-symbolic", "member-nested", "member-nested-symbolic", "activator-flags", "default-field", "default-event", "default-nested", "initializer-constructors", "initializer-member", "closure-named", "named-constructor-own", "named-constructor-base", "named-initializer", "named-constructor-symbolic", "named-initializer-symbolic", "type-name-forwarder", "type-name-forwarder-generic", "type-name-case-upper", "type-name-case-lower", "type-name-argument-union", "symbolic-constructor", "symbolic-constructors", "symbolic-constructor-member", "symbolic-constructor-members", "symbolic-constructor-default", "symbolic-constructor-query", "symbolic-constructor-direct-invoke", "attribute-filter-member", "attribute-filter-derived-parameter", "preserve-box-field", "preserve-box-return", "preserve-box-ref", "bind-box-named-return", "bind-box-method-ref", "invoke-member-event", "event-query", "lookup-flags-mixed", "lookup-flags-known", "lookup-arity-mixed", "lookup-arity-known", "bind-name-return", "bind-name-ref", "bind-name-query", "property-set-box", "property-invoke-box", "property-query-box"):
        marker = ":first-and-late-bindings" if section.startswith("symbolic-constructor") or section in ("base", "argument", "element", "generic-definition", "generic-method", "parameter", "nested", "argument-key", "argument-key-array", "named-constructor-symbolic", "named-initializer-symbolic") else ":checked"
        assert "graph-projection=" + section + marker in fixture, "projection fixture block did not run: " + section
    # The existing summary remains a prefix; only the deterministic counter line follows.
    lines = result.stdout.decode("utf-8").splitlines()
    summary = next(i for i, line in enumerate(lines) if line.startswith("ILDiet: removed "))
    assert summary + 1 < len(lines) and lines[summary + 1].startswith("ILDiet: counters "), "counter line displaced the existing summary"
    counter_line = lines[summary + 1]
    assert all(key + "=" + value in counter_line for key, value in counters.items()), "stdout/XML counter mismatch"
    outputs[callers] = (root, published, result.stdout)
assert outputs[2][0].find("counters").get("decode.body") == outputs[40][0].find("counters").get("decode.body"), "caller count increased definition body scans"
assert outputs[2][0].find("counters").get("summary.methods") == outputs[40][0].find("counters").get("summary.methods"), "closed caller count increased method reflection summaries"
print("graph-validation=late-promotion,dead-branches,growing-generics,arrays,type-array-alternatives,generic-tuples,relations,token-closure,generic-demands,single-scan", flush=True)
print("graph-validation=closed-contexts,observed-stubs,unchanged-definition-set,body-and-summary-non-growth", flush=True)

original = originals[2]
output = directory / "deterministic"
request = request_for(original, output)
first_result = execute(request, "deterministic")
first_root, published = validate(output, request)
first_pointer = (output / "ildiet-result.xml").read_bytes()
first_payload = payload(published)
for knobs in ({}, {"DN2CPP_MAX_GENERIC_DEPTH": "1", "DN2CPP_MAX_INSTANTIATIONS": "1"},
              {"DN2CPP_MAX_GENERIC_DEPTH": "64", "DN2CPP_MAX_INSTANTIATIONS": "2000000"}):
    result = execute(request, "deterministic", env=knobs)
    root, repeated = validate(output, request)
    assert (output / "ildiet-result.xml").read_bytes() == first_pointer
    assert payload(repeated) == first_payload and result.stdout == first_result.stdout, "environment knobs changed ILDiet output or counters"
print("graph-validation=deterministic-generation,manifest,counters,environment-independent", flush=True)

for mode in ("standalone", "protocol"):
    output = directory / ("budget-" + mode)
    if mode == "standalone":
        run(companion, original / "GraphApp.dll", "-r", original / "GraphLib.dll", "-r", corelib,
            "--work-budget", "1", "-o", output)
        root, published = validate(output)
    else:
        request = request_for(original, output, workBudget="1")
        execute(request, "budget-protocol")
        root, published = validate(output, request)
    assert root.get("fallback") == "budget" and root.get("constructorRegistriesRewritten") == "false"
    assert all(item.get("mode") == "copied" for item in root.findall("assembly"))
    expected = {"GraphApp.dll": original / "GraphApp.dll", "GraphLib.dll": original / "GraphLib.dll", corelib.name: corelib}
    for filename, source in expected.items():
        assert (published / filename).read_bytes() == source.read_bytes(), "budget fallback changed " + filename
    assert preservation_contract(published) == first_payload["preservation.xml"], "copy fallback changed the original preservation contract"

bad_link = directory / "invalid-link.xml"
bad_link.write_text("<linker><assembly", encoding="utf-8")
for kind in ("linkXml", "root", "reflectionRoot", "typeRoot", "methodRoot", "cut"):
    request = request_for(original, directory / ("invalid-budget-" + kind), workBudget="1")
    if kind == "linkXml":
        ET.SubElement(request, kind, path=str(bad_link))
    elif kind == "reflectionRoot":
        ET.SubElement(request, kind, type="Graph.DoesNotExist")
    elif kind == "cut":
        ET.SubElement(request, kind, method="Graph.DoesNotExist::Missing")
    else:
        attributes = dict(assembly="GraphLib", type="Graph.DoesNotExist")
        if kind == "methodRoot": attributes["method"] = "Missing"
        ET.SubElement(request, kind, **attributes)
    execute(request, "invalid-budget-" + kind, success=False)
    assert not (Path(request.get("output")) / "ildiet-result.xml").exists(), "invalid request published fallback"

for kind in ("missing-version", "old-version", "unknown-element", "unknown-attribute", "assemblyless-root", "invalid-budget"):
    request = request_for(original, directory / ("invalid-protocol-" + kind))
    if kind == "missing-version": del request.attrib["version"]
    elif kind == "old-version": request.set("version", "1")
    elif kind == "unknown-element": ET.SubElement(request, "future")
    elif kind == "unknown-attribute": request.set("future", "true")
    elif kind == "assemblyless-root": ET.SubElement(request, "root", type="Graph.Late")
    else: request.set("workBudget", "0")
    execute(request, "invalid-protocol-" + kind, success=False)
print("graph-validation=standalone-budget,protocol-budget,copy-identical,validate-before-fallback,strict-v2", flush=True)

output = directory / "atomic"
request = request_for(originals[2], output)
for point in ("plan", "apply", "write", "commit"):
    execute(request, "atomic-before-" + point)
    _, previous = validate(output, request)
    pointer = (output / "ildiet-result.xml").read_bytes()
    files = payload(previous)
    changed = request_for(originals[40], output)
    result = execute(changed, "atomic-fail-" + point, success=False,
                     env={"DN2CPP_ILDIET_FAIL_POINT": point})
    assert point.encode() in result.stderr, "failure injection did not reach " + point
    assert (output / "ildiet-result.xml").read_bytes() == pointer and payload(previous) == files, "failed publication changed the current generation"
    execute(changed, "atomic-after-" + point)
    _, current = validate(output, changed)
    assert not list(output.glob(".stage-*")) and not list(output.glob("*.tmp")), "recovery left staging files"
    assert {path.name for path in output.glob("g-*")} == {current.name}, "recovery retained an obsolete generation"
print("graph-validation=atomic-plan,atomic-apply,atomic-write,atomic-commit,recovery-cleanup", flush=True)


# Keep protocol consumer failures independently rerunnable after graph checks pass.
cli = Path(os.environ.get("DN2CPP_CLI_DLL", str(Path("src/Dn2Cpp.Cli/bin") / probe.parent.parent.name / probe.parent.name / "dn2cpp.dll"))).resolve()
subprocess.run([sys.executable, str(Path(__file__).with_name("check-protocol-consumer.py")),
                str(cli), str(companion), str(probe), str(corelib), str(originals[2]), str(directory)], check=True)


# Unknown reflection receivers widen member metadata, so capability witnesses use
# independent inputs and cannot mask the graph's existing absence controls.
for arm in ("dynamic-return", "dynamic-ref", "reflection-return", "reflection-ref",
            "bind-return", "bind-ref", "none-return", "none-ref"):
    original = directory / ("delegate-input-" + arm)
    created = run(probe, "--create-delegate-box-fixtures", original, corelib, arm)
    assert ("fixture-created=delegate-boxes arm=" + arm).encode() in created.stdout, "delegate capability fixture creation did not run"
    output = directory / ("delegate-box-" + arm)
    request = request_for(original, output)
    execute(request, "delegate-box-" + arm)
    root, published = validate(output, request)
    assert root.get("fallback") == "none", "delegate capability fixture unexpectedly fell back"
    assert root.find("counters").get("decode.max-per-method") == "1", "delegate capability fixture decoded a body twice"
    preservation = ET.parse(published / "preservation.xml").getroot()
    assert preservation.tag == "linker" and not preservation.attrib and len(preservation) == 0, "delegate capability fixture introduced a preservation rule"
    checked = run(probe, "--check-delegate-box-fixtures", published, arm).stdout.splitlines()
    expected = [("graph-delegate-box=" + arm + ":" + order + ":" +
                 ("stub" if arm.startswith("none-") or order == "Unallocated" else "body")).encode()
                for order in ("First", "Late", "Unallocated")]
    assert checked == expected, "delegate capability discovery or unallocated control block did not run"
    closure = run(probe, "--check-token-closure", *sorted(published.glob("*.dll")))
    assert b"token-closure=valid operands=" in closure.stdout, "delegate capability operand closure did not run"
    for filename in ("GraphApp.dll", "GraphLib.dll"):
        before = set(run(probe, "--relations", original / filename).stdout.splitlines())
        after = set(run(probe, "--relations", published / filename).stdout.splitlines())
        assert after and after <= before, "delegate capability changed an original relation"
print("graph-validation=delegate-invoke-boxes,dynamic,reflection,user-bind,return,ref,first-and-late,unallocated,unarmed", flush=True)

original = directory / "nominal-input"
created = run(probe, "--create-nominal-fixtures", original, corelib)
assert b"fixture-created=nominal-module-identity" in created.stdout, "nominal identity fixture creation did not run"
identity = run(probe, "--check-nominal-fixtures", original, "original")
assert b"graph-nominal=original:shared-module,distinct-assemblies,distinct-mvids,top-and-nested,first-and-late-call-structure" in identity.stdout, "nominal input identity or delayed binding block did not run"
output = directory / "nominal-output"
request = request_for(original, output)
result = execute(request, "nominal")
root, published = validate(output, request)
assert root.get("fallback") == "none", "nominal identity fixture unexpectedly fell back"
assert root.find("counters").get("decode.max-per-method") == "1", "nominal identity fixture decoded a body twice"
preservation = ET.parse(published / "preservation.xml").getroot()
assert preservation.tag == "linker" and not preservation.attrib and len(preservation) == 0, "nominal identity fixture introduced a preservation rule"
checked = run(probe, "--check-nominal-fixtures", published, "published").stdout.splitlines()
assert checked == [b"graph-nominal-binding=top:first:GraphApp:body", b"graph-nominal-binding=top:late:GraphLib:body",
                   b"graph-nominal-binding=nested:first:GraphLib:body", b"graph-nominal-binding=nested:late:GraphApp:body",
                   b"graph-nominal=published:all"], "nominal identity or first/late constructor body block did not run"
closure = run(probe, "--check-token-closure", *sorted(published.glob("*.dll")))
assert b"token-closure=valid operands=" in closure.stdout, "nominal identity operand closure did not run"
for filename in ("GraphApp.dll", "GraphLib.dll"):
    before = set(run(probe, "--relations", original / filename).stdout.splitlines())
    after = set(run(probe, "--relations", published / filename).stdout.splitlines())
    assert after and after <= before, "nominal identity changed an original relation"
pointer = (output / "ildiet-result.xml").read_bytes()
files = payload(published)
repeated = execute(request, "nominal")
_, generation = validate(output, request)
assert pointer == (output / "ildiet-result.xml").read_bytes() and files == payload(generation) and result.stdout == repeated.stdout, "nominal module identities changed deterministic publication"
print("graph-validation=nominal-module-identity,distinct-assemblies,shared-module-name,nested-types,both-binding-orders,late-demand-replay,constructor-bodies,unselected-controls", flush=True)

from canonical_admission import OWNER_MODES as owner_modes, check_primary_admission

# Each incomplete owner and its complete control have independent load sets: an
# unknown lookup's global member metadata cannot arm another filter's bodies.
for family in ("lookup-name", "lookup-flags", "base", "generic-definition", "array",
               "element", "accessor", "array-rank", "object", "type-name", "interface"):
    for variant in ("unknown", "complete"):
        mode = family + "-" + variant
        print("graph-uncertainty-mode=" + mode, flush=True)
        original = directory / ("uncertainty-input-" + mode)
        created = run(probe, "--create-uncertainty-fixtures", original, corelib, mode)
        assert ("fixture-created=reflection-uncertainty mode=" + mode).encode() in created.stdout, "uncertainty fixture creation did not run: " + mode
        checked = run(probe, "--check-uncertainty-fixtures", original, mode, "original")
        assert checked.stdout.splitlines() == [("graph-uncertainty=original:" + mode + ":known-candidate-present").encode()], "uncertainty fixture has no nonempty known candidate: " + mode
        if mode in owner_modes:
            check_primary_admission(cli, original, corelib, directory / ("owner-original-primary-" + mode), mode)
        output = directory / ("uncertainty-output-" + mode)
        request = request_for(original, output)
        execute(request, "uncertainty-" + mode)
        root, published = validate(output, request)
        assert root.get("fallback") == "none", "uncertainty fixture unexpectedly fell back: " + mode
        assert root.find("counters").get("decode.max-per-method") == "1", "uncertainty fixture decoded a body twice: " + mode
        preservation = ET.parse(published / "preservation.xml").getroot()
        assert preservation.tag == "linker" and not preservation.attrib and len(preservation) == 0, "uncertainty fixture introduced a preservation rule: " + mode
        checked = run(probe, "--check-uncertainty-fixtures", published, mode, "published")
        effect = ":metadata-member-closure" if mode in ("accessor-unknown", "object-unknown") else ":unknown-no-body" if variant == "unknown" else ":metadata-only"
        assert checked.stdout.splitlines() == [("graph-uncertainty=published:" + mode + effect).encode()], "uncertainty filter or complete control did not run: " + mode
        closure = run(probe, "--check-token-closure", *sorted(published.glob("*.dll")))
        assert b"token-closure=valid operands=" in closure.stdout, "uncertainty operand closure did not run: " + mode
        for filename in ("GraphApp.dll", "GraphLib.dll"):
            before = set(run(probe, "--relations", original / filename).stdout.splitlines())
            after = set(run(probe, "--relations", published / filename).stdout.splitlines())
            assert after and after <= before, "uncertainty fixture changed an original relation: " + mode
        if mode in owner_modes:
            check_primary_admission(cli, published, corelib, directory / ("owner-published-primary-" + mode), mode)
print("graph-validation=reflection-owner-uncertainty,lookup-names,lookup-flags,base,generic-definition,array,element,accessor,unknown-rank,object-owner,type-name,non-ascii-interface,isolated-complete-controls,annotated-body-negative", flush=True)


def check_composed_uncertainty(mode):
    print("graph-uncertainty-mode=" + mode, flush=True)
    original = directory / ("uncertainty-input-" + mode)
    created = run(probe, "--create-uncertainty-fixtures", original, corelib, mode)
    assert ("fixture-created=reflection-uncertainty mode=" + mode).encode() in created.stdout, "composed uncertainty fixture creation did not run: " + mode
    checked = run(probe, "--check-uncertainty-fixtures", original, mode, "original")
    assert checked.stdout.splitlines() == [("graph-uncertainty=original:" + mode + ":known-candidate-present").encode()], "composed uncertainty fixture lost its signature or owner precondition: " + mode
    if mode in owner_modes:
        check_primary_admission(cli, original, corelib, directory / ("owner-original-primary-" + mode), mode)
    output = directory / ("uncertainty-output-" + mode)
    request = request_for(original, output)
    execute(request, "uncertainty-" + mode)
    root, published = validate(output, request)
    assert root.get("fallback") == "none", "composed uncertainty fixture unexpectedly fell back: " + mode
    assert root.find("counters").get("decode.max-per-method") == "1", "composed uncertainty fixture decoded a body twice: " + mode
    preservation = ET.parse(published / "preservation.xml").getroot()
    assert preservation.tag == "linker" and not preservation.attrib and len(preservation) == 0, "composed uncertainty fixture introduced a preservation rule: " + mode
    checked = run(probe, "--check-uncertainty-fixtures", published, mode, "published")
    effect = ":metadata-member-closure" if mode == "object-generic-unknown" else ":unknown-no-body" if mode.endswith("-unknown") else ":metadata-only"
    assert checked.stdout.splitlines() == [("graph-uncertainty=published:" + mode + effect).encode()], "composed uncertainty filter or exact-owner control did not run: " + mode
    closure = run(probe, "--check-token-closure", *sorted(published.glob("*.dll")))
    assert b"token-closure=valid operands=" in closure.stdout, "composed uncertainty operand closure did not run: " + mode
    for filename in ("GraphApp.dll", "GraphLib.dll"):
        before = set(run(probe, "--relations", original / filename).stdout.splitlines())
        after = set(run(probe, "--relations", published / filename).stdout.splitlines())
        assert after and after <= before, "composed uncertainty fixture changed an original relation: " + mode
    if mode in owner_modes:
        check_primary_admission(cli, published, corelib, directory / ("owner-published-primary-" + mode), mode)

for mode in ("object-generic-unknown", "object-generic-complete", "box-class-unknown",
             "box-class-complete", "box-value-complete", "field-array-unknown",
             "field-array-complete", "field-nested-unknown", "field-nested-complete"):
    check_composed_uncertainty(mode)

print("graph-validation=composed-owner-uncertainty,object-generic-definition,inexact-reference-box,sealed-and-value-controls,open-field-arrays,nested-wrapper-signatures,closed-field-controls,annotated-body-negative", flush=True)

for mode in ("field-bound-array-complete", "field-bound-nested-complete"):
    check_composed_uncertainty(mode)
print("graph-validation=closed-generic-field-origins,formal-array-signatures,formal-nested-wrapper-signatures,metadata-only-controls", flush=True)

for mode in ("method-array-unknown", "method-array-complete"):
    check_composed_uncertainty(mode)
print("graph-validation=method-token-origins,open-method-array-signature,closed-methodspec-control,unknown-no-body-and-provider-nonexecution", flush=True)

from canonical_admission import MODES as canonical_modes

for mode in canonical_modes:
    check_composed_uncertainty(mode)
    check_primary_admission(cli, directory / ("uncertainty-input-" + mode), corelib,
                            directory / ("canonical-primary-" + mode), mode)
print("graph-validation=canonical-body-owner-uncertainty,array-tokens,made-arrays,generic-tokens,generic-definitions,actual-primary-sharing-admission,isolated-complete-controls,provider-nonexecution", flush=True)

from canonical_admission import CONTEXT_MODES as contextual_modes

for mode in contextual_modes:
    check_composed_uncertainty(mode)
    check_primary_admission(cli, directory / ("uncertainty-input-" + mode), corelib,
                            directory / ("contextual-primary-" + mode), mode)
print("graph-validation=contextual-member-signatures,caller-methodspec,field-memberref,own-methoddef,runtime-template-arrays,named-templates,projected-template-base,template-default-members,direct-closed-controls,actual-primary-sharing-admission,provider-nonexecution", flush=True)

from canonical_admission import VIEW_MODES as view_modes

for mode in view_modes:
    check_composed_uncertainty(mode)
    check_primary_admission(cli, directory / ("uncertainty-input-" + mode), corelib,
                            directory / ("metadata-primary-" + mode), mode)
print("graph-validation=template-tuple-arguments,fixed-and-unknown-indices,direct-template-owner-control,detached-field-property-parameter-accessor-signatures,nested-template-signatures,compiled-tuple-plain-array-substitution,open-dependent-interfaces,named-and-enumerated,closed-and-fixed-controls,actual-primary-admission,provider-nonexecution", flush=True)

from canonical_admission import BOUND_MODES as bound_modes

for mode in bound_modes:
    check_composed_uncertainty(mode)
    check_primary_admission(cli, directory / ("uncertainty-input-" + mode), corelib,
                            directory / ("bounded-primary-" + mode), mode)
print("graph-validation=object-tuple-knownness,uncertain-template-generic-method-closure,direct-member-control,metadata-only-formations,actual-primary-admission,provider-nonexecution", flush=True)

from canonical_admission import ORIGIN_MODES as origin_modes

for mode in origin_modes:
    check_composed_uncertainty(mode)
    check_primary_admission(cli, directory / ("uncertainty-input-" + mode), corelib,
                            directory / ("origin-primary-" + mode), mode)
print("graph-validation=whole-array-object-knownness,direct-vector-control,null-argument-array-control,actual-primary-admission,provider-nonexecution", flush=True)

from selector_admission import MODES as selector_modes, check_selector_admission

for mode in selector_modes:
    print("graph-selector-mode=" + mode, flush=True)
    original = directory / ("selector-input-" + mode)
    created = run(probe, "--create-selector-fixtures", original, corelib, mode)
    assert created.stdout.splitlines() == [("fixture-created=selector-vectors mode=" + mode).encode()], "selector fixture creation did not run: " + mode
    checked = run(probe, "--check-selector-fixtures", original, mode, "original")
    assert checked.stdout.splitlines() == [("graph-selector=original:" + mode + ":sequence-shapes,closed-bindings,no-body-roots").encode()], "selector original sequence or binding preconditions did not run: " + mode
    output = directory / ("selector-output-" + mode)
    request = request_for(original, output)
    execute(request, "selector-" + mode)
    root, published = validate(output, request)
    assert root.get("fallback") == "none", "selector fixture unexpectedly fell back: " + mode
    assert root.find("counters").get("decode.max-per-method") == "1", "selector fixture decoded a body twice: " + mode
    preservation = ET.parse(published / "preservation.xml").getroot()
    assert preservation.tag == "linker" and not preservation.attrib and len(preservation) == 0, "selector fixture introduced preservation: " + mode
    checked = run(probe, "--check-selector-fixtures", published, mode, "published")
    assert checked.stdout.splitlines() == [("graph-selector=published:" + mode + ":selected-bodies,complete-controls,unselected-negative").encode()], "selector body or negative control block did not run: " + mode
    closure = run(probe, "--check-token-closure", *sorted(published.glob("*.dll")))
    assert b"token-closure=valid operands=" in closure.stdout, "selector operand closure did not run: " + mode
    for filename in ("GraphApp.dll", "GraphLib.dll"):
        before = set(run(probe, "--relations", original / filename).stdout.splitlines())
        after = set(run(probe, "--relations", published / filename).stdout.splitlines())
        assert after and after <= before, "selector fixture changed an original relation: " + mode
    check_selector_admission(cli, original, corelib, directory / ("selector-primary-" + mode), mode)
print("graph-validation=selector-sequence-alternatives,first-and-later-bindings,literal-vector-narrowing,null-vector-single-candidate,closed-primary-admission,selected-bodies,unselected-negative", flush=True)

from canonical_admission import DEFINITION_MODES as definition_modes

for mode in definition_modes:
    check_composed_uncertainty(mode)
    check_primary_admission(cli, directory / ("uncertainty-input-" + mode), corelib,
                            directory / ("definition-primary-" + mode), mode)
print("graph-validation=nominal-definition-generic-argument,closed-argument-control,direct-argument-extraction,incomplete-tuple-provenance,actual-primary-admission,unknown-no-body,provider-nonexecution", flush=True)

from canonical_admission import LOADSET_MODES as loadset_modes, loadset_references, check_loadset_inventory, check_identical_loadset_inputs, loadset_closure_arguments, check_loadset_closure_marker

loadset_inputs = {}
for mode in loadset_modes:
    original = directory / ("uncertainty-input-" + mode)
    created = run(probe, "--create-uncertainty-fixtures", original, corelib, mode)
    assert created.stdout.splitlines() == [("fixture-created=reflection-uncertainty mode=" + mode).encode()], "primitive loadset fixture creation did not run: " + mode
    checked = run(probe, "--check-uncertainty-fixtures", original, mode, "original")
    assert checked.stdout.splitlines() == [("graph-uncertainty=original:" + mode + ":known-candidate-present").encode()], "primitive loadset signature preconditions did not run: " + mode
    assert {path.name for path in original.glob("*.dll")} == {"GraphApp.dll", "GraphLib.dll"}, "primitive input directory contains an implicit reference"
    loadset_inputs[mode] = original
check_identical_loadset_inputs(*loadset_inputs.values())
for mode, original in loadset_inputs.items():
    print("graph-loadset-mode=" + mode, flush=True)
    output = directory / ("uncertainty-output-" + mode)
    request = ET.Element("ildiet", version="2", input=str(original / "GraphApp.dll"), output=str(output))
    for reference in loadset_references(original, corelib, mode):
        ET.SubElement(request, "reference", path=str(reference))
    execute(request, "uncertainty-" + mode)
    root, published = validate(output, request)
    check_loadset_inventory(root, published, corelib, mode)
    assert root.get("fallback") == "none", "primitive loadset fixture unexpectedly fell back: " + mode
    assert root.find("counters").get("decode.max-per-method") == "1", "primitive loadset fixture decoded a body twice: " + mode
    preservation = ET.parse(published / "preservation.xml").getroot()
    assert preservation.tag == "linker" and not preservation.attrib and len(preservation) == 0, "primitive loadset introduced preservation: " + mode
    checked = run(probe, "--check-uncertainty-fixtures", published, mode, "published")
    effect = ":unknown-no-body" if mode.endswith("-unknown") else ":metadata-only"
    assert checked.stdout.splitlines() == [("graph-uncertainty=published:" + mode + effect).encode()], "primitive loadset body/control block did not run: " + mode
    closure = run(probe, *loadset_closure_arguments(published, corelib, mode))
    check_loadset_closure_marker(closure.stdout, mode)
    if mode.endswith("-unknown"):
        omitted = run(probe, "--check-token-closure-external-corelib", corelib, published / "GraphApp.dll", success=False)
        assert b"operand requested an unprovided assembly: GraphLib" in omitted.stderr, "external closure inspector resolved a user reference outside the supplied publication"
    for filename in ("GraphApp.dll", "GraphLib.dll"):
        before = set(run(probe, "--relations", original / filename).stdout.splitlines())
        after = set(run(probe, "--relations", published / filename).stdout.splitlines())
        assert after and after <= before, "primitive loadset changed an original relation: " + mode
    check_primary_admission(cli, original, corelib, directory / ("loadset-primary-" + mode), mode)
print("graph-validation=primitive-field-owner,missing-corelib-uncertainty,explicit-corelib-control,identical-inputs,exact-reference-and-assembly-inventory,actual-primary-loadset-admission,unknown-no-body,provider-nonexecution", flush=True)

from canonical_admission import MODIFIER_LOADSET_MODES as modifier_loadset_modes

modifier_inputs = {}
for mode in modifier_loadset_modes:
    original = directory / ("uncertainty-input-" + mode)
    created = run(probe, "--create-uncertainty-fixtures", original, corelib, mode)
    assert created.stdout.splitlines() == [("fixture-created=reflection-uncertainty mode=" + mode).encode()], "modified primitive fixture creation did not run: " + mode
    checked = run(probe, "--check-uncertainty-fixtures", original, mode, "original")
    assert checked.stdout.splitlines() == [("graph-uncertainty=original:" + mode + ":known-candidate-present").encode()], "volatile field signature preconditions did not run: " + mode
    assert {path.name for path in original.glob("*.dll")} == {"GraphApp.dll", "GraphLib.dll"}, "modified primitive input contains an implicit reference"
    modifier_inputs[mode] = original
check_identical_loadset_inputs(*modifier_inputs.values())
for mode, original in modifier_inputs.items():
    print("graph-loadset-mode=" + mode, flush=True)
    output = directory / ("uncertainty-output-" + mode)
    request = ET.Element("ildiet", version="2", input=str(original / "GraphApp.dll"), output=str(output))
    for reference in loadset_references(original, corelib, mode):
        ET.SubElement(request, "reference", path=str(reference))
    execute(request, "uncertainty-" + mode)
    root, published = validate(output, request)
    check_loadset_inventory(root, published, corelib, mode)
    assert root.get("fallback") == "none", "modified primitive fixture unexpectedly fell back: " + mode
    assert root.find("counters").get("decode.max-per-method") == "1", "modified primitive fixture decoded a body twice: " + mode
    preservation = ET.parse(published / "preservation.xml").getroot()
    assert preservation.tag == "linker" and not preservation.attrib and len(preservation) == 0, "modified primitive introduced preservation: " + mode
    checked = run(probe, "--check-uncertainty-fixtures", published, mode, "published")
    effect = ":unknown-no-body" if mode.endswith("-unknown") else ":metadata-only"
    assert checked.stdout.splitlines() == [("graph-uncertainty=published:" + mode + effect).encode()], "modified primitive body/control block did not run: " + mode
    closure = run(probe, *loadset_closure_arguments(published, corelib, mode))
    check_loadset_closure_marker(closure.stdout, mode)
    if mode.endswith("-unknown"):
        omitted = run(probe, "--check-token-closure-external-corelib", corelib, published / "GraphApp.dll", success=False)
        assert b"operand requested an unprovided assembly: GraphLib" in omitted.stderr, "modified closure inspector resolved an unprovided user assembly"
    for filename in ("GraphApp.dll", "GraphLib.dll"):
        before = set(run(probe, "--relations", original / filename).stdout.splitlines())
        after = set(run(probe, "--relations", published / filename).stdout.splitlines())
        assert after and after <= before, "modified primitive changed an original relation: " + mode
    check_primary_admission(cli, original, corelib, directory / ("loadset-primary-" + mode), mode)
print("graph-validation=volatile-modreq-primitive-owner,modifier-normalization,missing-corelib-uncertainty,explicit-corelib-control,identical-inputs,exact-reference-and-assembly-inventory,scoped-external-corelib-token-inspection,actual-primary-loadset-admission,unknown-no-body,provider-nonexecution", flush=True)

from scope_admission import check_scope_admission

original = directory / "scope-method-input"
created = run(probe, "--create-scope-method-fixtures", original, corelib)
assert b"fixture-created=scope-method-overloads" in created.stdout, "scope method fixture creation did not run"
identity = run(probe, "--check-scope-method-fixtures", original, "original")
assert identity.stdout.startswith(b"graph-scope-methods=original:"), "scope method input identity check did not run"
output = directory / "scope-method-output"
request = request_for(original, output)
result = execute(request, "scope-method")
root, published = validate(output, request)
assert root.get("fallback") == "none", "scope method fixture unexpectedly fell back"
assert root.find("counters").get("decode.max-per-method") == "1", "scope method fixture decoded a body twice"
preservation = ET.parse(published / "preservation.xml").getroot()
assert preservation.tag == "linker" and not preservation.attrib and len(preservation) == 0, "scope method fixture introduced preservation roots"
checked = run(probe, "--check-scope-method-fixtures", published, "published")
assert checked.stdout.startswith(b"graph-scope-methods=published:"), "scope method retained-body and negative checks did not run"
closure = run(probe, "--check-token-closure", *sorted(published.glob("*.dll")))
assert b"token-closure=valid operands=" in closure.stdout, "scope method operand closure did not run"
for filename in ("GraphApp.dll", "GraphLib.dll"):
    before = set(run(probe, "--relations", original / filename).stdout.splitlines())
    after = set(run(probe, "--relations", published / filename).stdout.splitlines())
    assert after and after <= before, "scope method resolution changed an original relation"
check_scope_admission(cli, original, corelib, directory / "scope-method-primary")
print("graph-validation=scope-method-overloads,memberref,methodspec,closed-owner,defining-assemblies,unique-external-dependencies,actual-primary-sharing-on-and-off,unselected-controls,token-closure", flush=True)

from array_layout_admission import validate_array_layout

validate_array_layout(companion, probe, cli, corelib, directory / "array-layout")

from instantiation_admission import MODES as instantiation_modes, check_instantiation_admission

for mode in instantiation_modes:
    original = directory / ("instantiation-input-" + mode)
    created = run(probe, "--create-instantiation-fixtures", original, corelib, mode)
    assert created.stdout.splitlines() == [("fixture-created=discarded-instantiation mode=" + mode).encode()], "instantiation fixture creation did not run: " + mode
    checked = run(probe, "--check-instantiation-fixtures", original, mode, "original")
    assert checked.stdout.splitlines() == [("graph-instantiation=original:" + mode + (":discarded-known,finite-or-static-control" if mode in ("method-known", "method-mixed", "method-static") else ":discarded-known,unknown-vector,no-closed-body-roots")).encode()], "instantiation input preconditions did not run: " + mode
    original_hashes = {name: (original / name).read_bytes() for name in ("GraphApp.dll", "GraphLib.dll")}
    output = directory / ("instantiation-output-" + mode)
    request = request_for(original, output)
    execute(request, "instantiation-" + mode)
    root, published = validate(output, request)
    assert root.get("fallback") == "none", "discarded instantiation unexpectedly fell back: " + mode
    assert root.find("counters").get("decode.max-per-method") == "1", "instantiation fixture decoded a body twice: " + mode
    preservation = ET.parse(published / "preservation.xml").getroot()
    assert preservation.tag == "linker" and not preservation.attrib and len(preservation) == 0, "instantiation fixture introduced preservation roots: " + mode
    checked = run(probe, "--check-instantiation-fixtures", published, mode, "published")
    assert checked.stdout.splitlines() == [("graph-instantiation=published:" + mode + (":selected-constructor-dependency,formation-only,dormant-and-provider-negative" if mode.endswith("-only") else ":finite-or-static-execution,unknown-no-body,dormant-and-provider-negative")).encode()], "instantiation selected-body or negative controls did not run: " + mode
    closure = run(probe, "--check-token-closure", *sorted(published.glob("*.dll")))
    assert b"token-closure=valid operands=" in closure.stdout, "instantiation token closure did not run: " + mode
    for filename in ("GraphApp.dll", "GraphLib.dll"):
        before = set(run(probe, "--relations", original / filename).stdout.splitlines())
        after = set(run(probe, "--relations", published / filename).stdout.splitlines())
        assert after and after <= before, "instantiation changed an original relation: " + mode
    check_instantiation_admission(cli, original, corelib, directory / ("instantiation-primary-" + mode), mode)
    assert all((original / name).read_bytes() == content for name, content in original_hashes.items()), "instantiation proof changed an original input: " + mode
print("graph-validation=discarded-method-and-type-instantiation,late-type-binding,unknown-vector-no-body,finite-known-and-mixed-vectors,independent-static-execution,runtime-template-sharing-control,formation-only,dormant-body-and-constructor-negative,unique-external-dependencies,actual-primary-sharing-on-and-off,token-closure", flush=True)

from copied_array_admission import validate_copied_array

validate_copied_array(companion, probe, cli, corelib, directory / "copied-array")

from selected_budget_admission import validate_selected_budgets
validate_selected_budgets(companion, probe, corelib, directory / "selected-budget")

from attribute_inventory_admission import validate_attribute_inventory
validate_attribute_inventory(companion, probe, cli, corelib, directory / "attribute-inventory")

from eager_model_admission import validate_eager_model
validate_eager_model(companion, probe, cli, corelib, directory / "eager-model")

from byref_token_admission import validate_byref_tokens
validate_byref_tokens(companion, probe, cli, corelib, directory / "byref-token")

from model_authority_admission import validate_model_authority
validate_model_authority(companion, probe, cli, corelib, directory / "model-authority")

from passive_evidence_admission import validate_passive_evidence
validate_passive_evidence(companion, probe, cli, corelib, directory / "passive-evidence")

from cut_authority_admission import validate_cut_authority
validate_cut_authority(companion, probe, cli, corelib, directory / "cut-authority")

from selected_cut_admission import validate_selected_cuts
validate_selected_cuts(companion, probe, cli, corelib, directory / "selected-cut")

from property_signature_admission import validate_property_signatures
validate_property_signatures(companion, probe, cli, corelib, directory / "property-signature")
