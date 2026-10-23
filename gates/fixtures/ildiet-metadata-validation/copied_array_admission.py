"""Reached copied bodies arm Array.Initialize without application references."""
from pathlib import Path
import hashlib
import json
import os
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

sys.path.insert(0, str(Path(__file__).resolve().parents[2]))
from ildiet_manifest import contract_digest, framed_hash, manifest

ORIGINAL_MODES = ("direct", "copied-to-copied", "cctor", "ldftn-delegate", "unarmed",
         "module-initializer", "framework-module-unarmed", "framework-attribute-named", "framework-attribute-unarmed",
         "framework-attribute-same-name")
ROOT_ATTRIBUTE_MODES = ("copied-type-attribute", "copied-type-attribute-unnamed", "copied-type-attribute-noquery",
                        "copied-type-attribute-data", "copied-type-attribute-setter", "copied-method-attribute",
                        "copied-assembly-attribute", "copied-parameter-attribute",
                        "copied-property-attribute-data", "copied-event-attribute-data")
PRESERVATION_MODES = ("copied-preserve-method", "copied-preserve-conditional", "copied-preserve-inactive", "copied-preserve-signature", "copied-preserve-named")
ROOT_MODES = ROOT_ATTRIBUTE_MODES + PRESERVATION_MODES
ROOT_NEGATIVES = ("copied-type-attribute-unnamed", "copied-type-attribute-noquery", "copied-type-attribute-data", "copied-preserve-inactive",
                  "copied-property-attribute-data", "copied-event-attribute-data")
OBSERVED_OLD_MODES = ("copied-property-attribute-data", "copied-event-attribute-data")
DEMAND_MODES = ("copied-demand-default", "copied-demand-construct", "copied-demand-formed",
                "copied-demand-method", "copied-demand-method-lookup", "copied-demand-property", "copied-demand-property-lookup",
                "copied-demand-event", "copied-demand-event-lookup", "copied-demand-event-attribute",
                "copied-demand-field", "copied-demand-field-lookup")
DEMAND_NEGATIVES = tuple(mode for mode in DEMAND_MODES if mode.endswith("-lookup")
                         or mode in ("copied-demand-formed", "copied-demand-event-attribute"))
LATE_MODES = ("copied-late-layout", "copied-early-layout", "copied-layout-unarmed", "copied-layout-unmodeled", "copied-layout-framework",
              "copied-filter-instance", "copied-filter-setter", "copied-filter-unmatched", "copied-filter-noquery", "copied-filter-framework",
              "copied-filter-data", "copied-filter-data-noquery", "copied-filter-data-unmatched", "copied-filter-data-inactive", "copied-filter-data-upgrade")
LATE_POSITIVES = ("copied-late-layout", "copied-early-layout")
from selected_layout_admission import SELECTED_MODES

MODES = ORIGINAL_MODES + ROOT_MODES + DEMAND_MODES + LATE_MODES + SELECTED_MODES
COPIED = ("System.ArrayFacade.dll", "System.ArrayLeaf.dll", "DnZlib.dll")
INPUTS = ("GraphApp.dll", "GraphLib.dll", *COPIED)
NEGATIVES = ("CopiedArrayOverloadDependency", "CopiedArrayOrdinaryDependency",
             "CopiedArrayReferenceDependency", "CopiedArrayUnusedProvider")


def digest(path):
    with Path(path).open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def run(command, directory, label, expected=0, extra=None):
    result = subprocess.run(list(map(str, command)), capture_output=True,
                            env={**os.environ, **(extra or {})})
    (directory / (label + ".stdout.log")).write_bytes(result.stdout)
    (directory / (label + ".stderr.log")).write_bytes(result.stderr)
    if expected is not None:
        assert result.returncode == expected, (label, result.returncode, result.stdout.decode(errors="replace"),
                                               result.stderr.decode(errors="replace"))
    return result


def input_hashes(original, corelib):
    paths = [*(Path(original) / name for name in INPUTS), Path(corelib)]
    if (Path(original) / "link.xml").is_file():
        paths.append(Path(original) / "link.xml")
    return {str(path.resolve()): digest(path) for path in paths}


def descriptor_for(original, mode):
    if mode in SELECTED_MODES:
        from selected_layout_admission import selected_descriptor
        return selected_descriptor(original, mode)
    if mode in LATE_MODES:
        return late_descriptor(original, mode)
    path = Path(original) / "link.xml"
    if mode not in PRESERVATION_MODES:
        assert not path.exists(), "non-preservation copied fixture gained a descriptor"
        return None
    root = ET.parse(path).getroot()
    assert root.tag == "linker" and not root.attrib and len(root) == (2 if mode == "copied-preserve-signature" else 1)
    if mode == "copied-preserve-signature":
        check_signature_policy(root[0], published=False)
    assembly = root[-1]
    assert assembly.tag == "assembly" and assembly.attrib == {"fullname": "System.ArrayFacade", "preserve": "nothing"} and len(assembly) == 1
    owner = assembly[0]
    expected = {"fullname": "Graph.CopiedArrayFacade", "preserve": "nothing"}
    if mode != "copied-preserve-method":
        expected["required"] = "false"
    assert owner.tag == "type" and owner.attrib == expected and len(owner) == 1
    assert owner[0].tag == "method" and owner[0].attrib == {"name": "Initialize"} and len(owner[0]) == 0
    return path


def check_signature_policy(assembly, published):
    assert assembly.tag == "assembly" and assembly.attrib == {"fullname": "GraphApp", "preserve": "nothing"} and len(assembly) == 1
    owner = assembly[0]
    assert owner.tag == "type" and owner.attrib == {"fullname": "Graph.CopiedArrayEntry", "preserve": "nothing"} and len(owner) == 1
    expected = {"signature": "Graph.CopiedArrayFacade PreservedSignature()"} if published else {"name": "PreservedSignature"}
    assert owner[0].tag == "method" and owner[0].attrib == expected and len(owner[0]) == 0


def check_publication_preservation(published, mode):
    if mode in SELECTED_MODES:
        from selected_layout_admission import selected_policy
        selected_policy(published / "preservation.xml", mode, published=True)
        return
    if mode in LATE_MODES:
        check_late_preservation(published / "preservation.xml", mode, published=True)
        return
    preservation = ET.parse(published / "preservation.xml").getroot()
    if mode not in PRESERVATION_MODES:
        assert preservation.tag == "linker" and not preservation.attrib and len(preservation) == 0, "copied fixture introduced preservation roots"
        return
    assert preservation.tag == "linker" and not preservation.attrib and len(preservation) == (2 if mode == "copied-preserve-signature" else 1)
    if mode == "copied-preserve-signature":
        check_signature_policy(preservation[0], published=True)
    assembly = preservation[-1]
    assert assembly.tag == "assembly" and assembly.attrib == {"fullname": "System.ArrayFacade", "preserve": "nothing"} and len(assembly) == 1
    expected = {"fullname": "Graph.CopiedArrayFacade", "preserve": "nothing"}
    if mode != "copied-preserve-method":
        expected["required"] = "0"
    owner = assembly[0]
    assert owner.tag == "type" and owner.attrib == expected and len(owner) == 1
    assert owner[0].tag == "method" and owner[0].attrib == {"signature": "System.Void Initialize(System.Array)"} and len(owner[0]) == 0


def emitted(destination):
    files = sorted(destination.glob("generated*.cpp")) + sorted(destination.glob("generated*.h"))
    assert files, (destination, "missing generated definitions")
    return {path.name: digest(path) for path in files}


def check_copied_array_admission(cli, original, corelib, output, mode, arms=("original", "integrated"),
                                 references=None, extra=None, expected_demand_admission=None):
    assert mode in MODES
    if mode in SELECTED_MODES:
        from selected_layout_admission import selected_admission
        return selected_admission(cli, original, corelib, output, mode, arms, references, extra, expected_demand_admission)
    if mode in LATE_MODES:
        return check_copied_late_admission(cli, original, corelib, output, mode, arms, references, extra, expected_demand_admission)
    original, output = Path(original), Path(output)
    output.mkdir(parents=True, exist_ok=True)
    references = list(references) if references is not None else [*(original / name for name in INPUTS[1:]), Path(corelib)]
    input_names = ["GraphApp.dll", *(Path(path).name for path in references)]
    if mode == "framework-attribute-same-name":
        facade_module = input_names.index("System.ArrayFacade.dll")
        leaf_module = input_names.index("System.ArrayLeaf.dll")
        assert facade_module < leaf_module, "same-named attribute fixture changed its original module order"
    hashes = {str(path.resolve()): digest(path) for path in [original / INPUTS[0], *map(Path, references)]}
    descriptor = descriptor_for(original, mode)
    if descriptor is not None:
        hashes[str(descriptor.resolve())] = digest(descriptor)
    receipts = []
    for arm in arms:
        assert arm in ("original", "integrated")
        for shared in (True, False):
            label = arm + ("-sharing-on" if shared else "-sharing-off")
            destination = output / label
            command = ["dotnet", "exec", str(cli), str(original / "GraphApp.dll")]
            for reference in references:
                command += ["-r", str(reference)]
            command += ["--shared-generics" if shared else "--no-shared-generics", "-o", str(destination)]
            if descriptor is not None:
                command += ["--link-xml", str(descriptor)]
            if arm == "original":
                command += ["--no-ildiet"]
            result = run(command, output, label, extra=extra)
            if not shared:
                assert b"dn2cpp: shared-generics:" not in result.stdout, (mode, label)
            source = "\n".join((destination / name).read_text(encoding="utf-8") for name in emitted(destination))

            def definitions(symbol):
                return re.findall(r"(?m)^[A-Za-z_][A-Za-z0-9_* ]* " + symbol + r"_m[0-9]+\([^;\n]*\)\s*\{", source)

            assert definitions("CopiedArrayEntry_Main"), (mode, label, "entry not emitted")
            assert bool(definitions("CopiedArrayEntry_PreservedSignature")) == (mode == "copied-preserve-signature"), (mode, label, "preserved signature body did not execute")
            armed = mode not in ("unarmed", "framework-module-unarmed", "framework-attribute-unarmed", *ROOT_NEGATIVES)
            demand_admission = None
            if mode in DEMAND_MODES:
                if arm == "integrated":
                    assert expected_demand_admission is not None and set(expected_demand_admission) == {"True", "False"}, "Observe both original sharing modes before integrated demand checks"
                constructor_count = len(definitions("CopiedArrayValue__ctor"))
                assert constructor_count <= 1, (mode, label, "multiple original default constructors")
                armed = constructor_count == 1
                if mode in DEMAND_NEGATIVES or not shared:
                    assert armed == (mode not in DEMAND_NEGATIVES), (mode, label, "wrong concrete demand capability admission")
                selected = ("CopiedDemandOwner__ctor" if mode in ("copied-demand-default", "copied-demand-construct", "copied-demand-formed")
                    else "CopiedDemandOwner_Run" if "-method" in mode
                    else "CopiedDemandOwner_get_Value" if "-property" in mode
                    else "CopiedDemandOwner_add_Changed" if "-event" in mode
                    else "CopiedDemandFieldValue_ToString")
                selected_body = bool(definitions(selected))
                helper_body = bool(re.findall(r"(?m)^[A-Za-z_][A-Za-z0-9_* ]* CopiedArrayEntry_Demand[^ \n(]*\([^;\n]*\)\s*\{", source))
                assert helper_body == (mode != "copied-demand-formed"), (mode, label, "formed helper executed or called helper missing")
                if mode == "copied-demand-event-attribute":
                    if not shared:
                        assert selected_body, (mode, label, "nonexecuting attribute control did not execute its event accessor")
                else:
                    assert selected_body == armed, (mode, label, "capability lost its selected copied body")
                if mode not in ("copied-demand-default", "copied-demand-construct"):
                    assert not definitions("CopiedDemandOwner__ctor"), (mode, label, "opened unrelated copied constructor")
                assert not definitions("CopiedDemandOwner_remove_Changed"), (mode, label, "primary selected the uncalled event remover")
                for member in ("_ctor", "set_Arm", "get_Arm"):
                    assert not definitions("CopiedDemandAttribute_" + member), (mode, label, "event accessor constructed its attribute", member)
                demand_admission = {"armed": armed, "selected_body": selected_body, "helper_body": helper_body}
                if expected_demand_admission is not None and str(shared) in expected_demand_admission:
                    assert demand_admission == expected_demand_admission[str(shared)], (mode, label, "changed original per-sharing admission")
            assert len(definitions("CopiedArrayValue__ctor")) == int(armed), (mode, label, "wrong original value constructor bodies")
            assert bool(definitions("CopiedArrayDependency_Touch")) == armed, (mode, label, "wrong original unique dependency body")
            assert not definitions("CopiedArrayValue_Ordinary") and not definitions("CopiedArrayReference__ctor"), (mode, label, "opened ordinary/reference constructor")
            for name in NEGATIVES:
                method = "Read" if name == "CopiedArrayUnusedProvider" else "Touch"
                assert not definitions(name + "_" + method), (mode, label, "opened metadata/provider body", name)
            assert not definitions("CopiedArrayFacade_Construct"), (mode, label, "unused copied Construct became a primary root")
            attribute_definitions = definitions("CopiedBoundaryAttribute__ctor")
            attribute_constructed = mode in ("framework-attribute-named", "framework-attribute-same-name") \
                or mode in ROOT_ATTRIBUTE_MODES and mode not in ROOT_NEGATIVES
            assert bool(attribute_definitions) == attribute_constructed, (mode, label, "wrong framework attribute constructor admission")
            assert bool(definitions("CopiedBoundaryAttribute_set_Arm")) == (mode == "copied-type-attribute-setter"), (mode, label, "wrong named setter admission")
            assert not definitions("CopiedBoundaryAttribute_get_Arm"), (mode, label, "attribute construction executed its named property getter")
            assert not definitions("CopiedArrayFacade_AttributedMember"), (mode, label, "attribute query executed its metadata-only owner method")
            for accessor in ("get_AttributedProperty", "add_AttributedEvent", "remove_AttributedEvent"):
                assert not definitions("CopiedArrayFacade_" + accessor), (mode, label, "attribute metadata query executed an owner accessor", accessor)
            if mode in PRESERVATION_MODES:
                assert bool(definitions("CopiedArrayFacade_Initialize")) == armed, (mode, label, "wrong copied preserved method admission")
            if mode == "framework-attribute-same-name":
                modules = {int(index) for index in re.findall(r"\bt_m([0-9]+)_Graph_CopiedBoundaryAttribute\b", source)}
                assert modules == {facade_module, leaf_module}, (mode, label, "same-named copied type identities changed", modules)
                assert len(attribute_definitions) == 1 and re.search(r"\(\s*t_m" + str(facade_module)
                    + r"_Graph_CopiedBoundaryAttribute\s*\*", attribute_definitions[0]), (mode, label, "primary did not admit the original first-module attribute constructor")
            if arm == "integrated":
                root, published = manifest(destination / "ildiet")
                assert root.get("fallback") == "none", (mode, label)
                assert root.find("counters").get("decode.max-per-method") == "1", (mode, label)
                check_publication_preservation(published, mode)
                for name in COPIED:
                    assert (published / name).read_bytes() == (original / name).read_bytes(), (mode, label, "copied bytes changed", name)
            else:
                assert not (destination / "ildiet").exists(), (mode, label)
            assert {path: digest(path) for path in hashes} == hashes, "primary modified an original copied-boundary input"
            if mode in DEMAND_MODES and arm == "original":
                if expected_demand_admission is None:
                    expected_demand_admission = {}
                expected_demand_admission[str(shared)] = demand_admission
            receipts.append({"mode": mode, "arm": label, "command": command, "input_sha256": hashes,
                             "generated_sha256": emitted(destination), "value_constructor_and_dependency": armed,
                             "unused_construct_and_metadata_negatives": True,
                             "demand_admission": demand_admission, "shared": shared})
            print("graph-copied-array-admission=" + mode + ":" + label + ":actual-entry,default-constructor,unique-dependency,unused-construct-negative", flush=True)
    (output / "admission.json").write_text(json.dumps(receipts, indent=2) + "\n")
    return receipts


def publish_copied(companion, probe, original, corelib, output, mode, state="published", references=None):
    output = Path(output)
    output.mkdir(parents=True, exist_ok=True)
    references = list(references) if references is not None else [*(original / name for name in INPUTS[1:]), Path(corelib)]
    request = ET.Element("ildiet", version="2", input=str(original / "GraphApp.dll"), output=str(output / "publication"))
    for reference in references:
        ET.SubElement(request, "reference", path=str(reference))
    descriptor = descriptor_for(original, mode)
    descriptor_hash = digest(descriptor) if descriptor is not None else None
    if descriptor is not None:
        ET.SubElement(request, "linkXml", path=str(descriptor))
    path = output / "request.xml"
    ET.ElementTree(request).write(path, encoding="utf-8", xml_declaration=True)
    run(["dotnet", "exec", companion, "--request", path], output, "companion")
    result, published = manifest(output / "publication")
    assert result.get("contract") == contract_digest(request), (mode, state, "request contract changed")
    values = []
    for assembly in result.findall("assembly"):
        name = assembly.get("name") + ".dll"
        values.extend((name, (published / name).read_bytes()))
    values.extend(("preservation.xml", (published / "preservation.xml").read_bytes()))
    assert result.get("generation") == "g-" + framed_hash(values), (mode, state, "generation does not describe its payload")
    assert Path(result.get("input")).name == "GraphApp.dll"
    assert [Path(row.get("path")).name for row in result.findall("reference")] == [Path(path).name for path in references], (mode, state, "publication changed reference order")
    assert result.get("fallback") == "none", (mode, state)
    assert result.find("counters").get("decode.max-per-method") == "1", (mode, state)
    check_publication_preservation(published, mode)
    if descriptor is not None:
        assert digest(descriptor) == descriptor_hash, "companion modified the original descriptor"
    for name in COPIED:
        item = next(row for row in result.findall("assembly") if row.get("name") + ".dll" == name)
        assert item.get("mode") == "copied" and item.get("registryRewritten") == "false", (mode, name)
        assert (published / name).read_bytes() == (original / name).read_bytes(), (mode, "copied bytes changed", name)
    checked = run(["dotnet", "exec", probe, "--check-copied-array-fixtures", published, mode, state], output, "retained-check")
    marker = ("graph-copied-array=" + state + ":" + mode + ":").encode()
    if state == "observed-before" and mode in SELECTED_MODES:
        assert checked.stdout.splitlines()[-1].startswith(marker)
        assert b"graph-selected-observed=" in checked.stdout
    elif state == "observed-before" and mode in LATE_MODES:
        assert checked.stdout.splitlines()[-1].startswith(marker)
        assert ("graph-copied-late-observed-before=" + mode + ":constructor-body=").encode() in checked.stdout
    elif state == "observed-before":
        assert mode in (*OBSERVED_OLD_MODES, *DEMAND_MODES) and checked.stdout.splitlines()[-1].startswith(marker)
        assert ("graph-copied-array-observed-before=" + mode + ":constructor-body=").encode() in checked.stdout
    else:
        assert checked.stdout.startswith(marker)
    closure = run(["dotnet", "exec", probe, "--check-token-closure", *sorted(published.glob("*.dll"))], output, "closure")
    assert b"token-closure=valid operands=" in closure.stdout
    for name in INPUTS:
        before = set(run(["dotnet", "exec", probe, "--relations", original / name], output, "before-" + name).stdout.splitlines())
        after = set(run(["dotnet", "exec", probe, "--relations", published / name], output, "after-" + name).stdout.splitlines())
        assert after and after <= before, (mode, name, "changed original relations")
    return ({path.name: digest(path) for path in published.iterdir() if path.is_file()}, dict(result.find("counters").attrib))


def validate_copied_array(companion, probe, cli, corelib, directory):
    directory = Path(directory)
    directory.mkdir(parents=True, exist_ok=True)
    for mode in MODES:
        original = directory / (mode + "-input")
        created = run(["dotnet", "exec", probe, "--create-copied-array-fixtures", original, corelib, mode], directory, mode + "-create")
        assert created.stdout.splitlines() == [("fixture-created=copied-array mode=" + mode).encode()]
        checked = run(["dotnet", "exec", probe, "--check-copied-array-fixtures", original, mode, "original"], directory, mode + "-original")
        assert checked.stdout.startswith(("graph-copied-array=original:" + mode + ":").encode())
        hashes = input_hashes(original, corelib)
        first = publish_copied(companion, probe, original, corelib, directory / (mode + "-first"), mode)
        repeat = publish_copied(companion, probe, original, corelib, directory / (mode + "-repeat"), mode)
        assert first == repeat, "copied array publication/counters are nondeterministic"
        check_copied_array_admission(cli, original, corelib, directory / (mode + "-primary"), mode)
        assert input_hashes(original, corelib) == hashes, "copied fixture changed original inputs"
    print("graph-validation=copied-array-direct,copied-to-copied,copied-cctor,copied-ldftn-delegate,unarmed-control,unique-external-dependency,metadata-negatives,unused-construct-negative,copied-bytes-identical,actual-primary-sharing-on-and-off,determinism,token-closure", flush=True)
    print("graph-validation=copied-nonframework-module-initializer,framework-module-unarmed,copied-bytes-identical,actual-primary-sharing-on-and-off", flush=True)
    print("graph-validation=copied-framework-attribute-named,copied-framework-attribute-unarmed,exact-type-token,actual-primary-sharing-on-and-off", flush=True)
    print("graph-validation=copied-framework-attribute-same-name,attribute-and-token-distinct-scopes,first-module-constructor,actual-primary-sharing-on-and-off", flush=True)
    print("graph-validation=copied-type-member-assembly-attributes,known-uncalled-member,setter-only,unnamed-noquery-data-controls,copied-preserved-method,conditional-active-inactive,original-descriptor-contract,actual-primary-sharing-on-and-off", flush=True)
    print("graph-validation=copied-conditional-preserved-signature,uncalled-null-returning-helper,no-facade-call-or-token,actual-primary-sharing-on-and-off", flush=True)
    print("graph-validation=copied-conditional-named-type,discarded-literal-name-lookup,no-facade-call-or-token,actual-primary-sharing-on-and-off", flush=True)
    print("graph-validation=copied-parameter-attribute-row,copied-property-event-attribute-data,uncalled-owner-and-accessors,actual-primary-sharing-on-and-off", flush=True)
    print("graph-validation=copied-symbolic-constructor-method-accessor-field-demands,formed-uncalled-binding,lookup-controls,event-attribute-nonexecution,actual-primary-per-sharing-admission", flush=True)
    print("graph-validation=copied-late-user-layout-replay,unknown-owner-user-attribute-filters,metadata-filter-conditional-identity,nonexecuting-attribute-bodies,actual-primary-per-sharing-admission", flush=True)
    print("graph-validation=selected-stripped-attribute-metadata,named-member-identity,passive-accessor-attributes,field-only-copied-dispatch,actual-primary-per-sharing-admission", flush=True)


def check_late_preservation(path, mode, published=False):
    root = ET.parse(path).getroot()
    assert root.tag == "linker" and not root.attrib
    if not mode.startswith("copied-filter-data"):
        assert len(root) == 0, "late fixture gained preservation roots"
        return
    assert len(root) == 1
    assembly = root[0]
    assert assembly.tag == "assembly" and assembly.attrib == {"fullname": "DnZlib", "preserve": "nothing"} and len(assembly) == 1
    owner = assembly[0]
    assert owner.tag == "type" and owner.attrib == {"fullname": "Graph.LateConditionalFactory", "preserve": "nothing", "required": "0"} and len(owner) == 1
    expected = {"signature": "System.Void Initialize()"} if published else {"name": "Initialize"}
    assert owner[0].tag == "method" and owner[0].attrib == expected and len(owner[0]) == 0


def late_descriptor(original, mode):
    path = Path(original) / "link.xml"
    if mode.startswith("copied-filter-data"):
        check_late_preservation(path, mode)
        return path
    assert not path.exists(), "late fixture gained a descriptor"
    return None


def check_copied_late_admission(cli, original, corelib, output, mode, arms, references, extra, expected_admission):
    original, output = Path(original), Path(output)
    output.mkdir(parents=True, exist_ok=True)
    references = list(references) if references is not None else [*(original / name for name in INPUTS[1:]), Path(corelib)]
    hashes = {str(Path(path).resolve()): digest(path) for path in [original / "GraphApp.dll", *references]}
    descriptor = descriptor_for(original, mode)
    if descriptor is not None:
        hashes[str(descriptor.resolve())] = digest(descriptor)
    receipts = []
    for arm in arms:
        assert arm in ("original", "integrated")
        for shared in (True, False):
            label = arm + ("-sharing-on" if shared else "-sharing-off")
            destination = output / label
            command = ["dotnet", "exec", str(cli), str(original / "GraphApp.dll")]
            for reference in references:
                command += ["-r", str(reference)]
            command += ["--shared-generics" if shared else "--no-shared-generics", "-o", str(destination)]
            if descriptor is not None:
                command += ["--link-xml", str(descriptor)]
            if arm == "original":
                command += ["--no-ildiet"]
            result = run(command, output, label, extra=extra)
            if not shared:
                assert b"dn2cpp: shared-generics:" not in result.stdout, (mode, label)
            source = "\n".join((destination / name).read_text(encoding="utf-8") for name in emitted(destination))

            def definitions(symbol):
                return re.findall(r"(?m)^[A-Za-z_][A-Za-z0-9_* ]* " + symbol + r"_m[0-9]+\([^;\n]*\)\s*\{", source)

            layout, data = "layout" in mode, mode.startswith("copied-filter-data")
            target = "LateTarget" if layout else "LateValue"
            assert definitions("LateEntry_Main"), (mode, label, "entry missing")
            count = len(definitions(target + "__ctor"))
            assert count <= 1, (mode, label, "opened target overload")
            armed = count == 1
            if not shared or mode not in LATE_POSITIVES:
                assert armed == (mode in LATE_POSITIVES), (mode, label, "wrong concrete late capability admission")
            assert bool(definitions("LateDependency_Touch")) == armed, (mode, label, "unique dependency does not match target admission")
            for symbol in (target + "_Ordinary", "LateOverloadDependency_Touch", "LateOrdinaryDependency_Touch", "LateReference__ctor", "LateReferenceDependency_Touch"):
                assert not definitions(symbol), (mode, label, "unrelated body admitted", symbol)
            helpers = {name: bool(definitions("LateEntry_" + name)) for name in ("Arm", "Model", "Read")}
            assert helpers == ({"Arm": mode != "copied-layout-unarmed", "Model": mode != "copied-layout-unmodeled", "Read": False}
                               if layout else {"Arm": False, "Model": False, "Read": True}), (mode, label, "intended helper did not execute")
            cell_definitions = definitions("LateCell_1__ctor")
            assert len(cell_definitions) <= 1, (mode, label, "copied initializer emitted a constructor overload")
            assert not definitions("LateCellOverloadTarget__ctor"), (mode, label, "copied initializer executed its overload target")
            cell = bool(cell_definitions)
            attr_ctor = bool(definitions("LateSpecialAttribute__ctor"))
            attr_setter = bool(definitions("LateSpecialAttribute_set_Arm"))
            assert not definitions("LateSpecialAttribute_get_Arm") and not definitions("LateOtherAttribute__ctor"), (mode, label, "attribute getter or unmatched constructor executed")
            constructed = False
            assert attr_ctor == constructed and attr_setter == constructed, (mode, label, "wrong attribute constructor/setter admission")
            assert cell == (armed if layout else False), (mode, label, "copied generic ctor does not explain target admission")
            policy_body = bool(definitions("LateConditionalFactory_Initialize"))
            assert policy_body == (armed if data else False), (mode, label, "conditional method does not explain target admission")
            forbidden = {}
            for name in ("LateAttributeCtorTarget", "LateAttributeSetterTarget", "LateAttributeGetterTarget"):
                executes = False
                actual = bool(definitions(name + "__ctor"))
                assert actual == executes and bool(definitions(name + "Dependency_Touch")) == executes, (mode, label, "attribute nonexecution control failed", name)
                forbidden[name] = actual
            observation = {"armed": armed, "helpers": helpers, "cell_ctor": cell, "attribute_ctor": attr_ctor,
                           "attribute_setter": attr_setter, "conditional_method": policy_body, "attribute_targets": forbidden}
            if arm == "integrated":
                assert expected_admission is not None and set(expected_admission) == {"True", "False"}, "Observe both original sharing modes first"
            if expected_admission is not None and str(shared) in expected_admission:
                assert observation == expected_admission[str(shared)], (mode, label, "changed original admission")
            if arm == "integrated":
                root, published = manifest(destination / "ildiet")
                assert root.get("fallback") == "none" and root.find("counters").get("decode.max-per-method") == "1", (mode, label)
                check_publication_preservation(published, mode)
                for name in COPIED:
                    assert (published / name).read_bytes() == (original / name).read_bytes(), (mode, label, "copied bytes changed", name)
            else:
                assert not (destination / "ildiet").exists()
                if expected_admission is None:
                    expected_admission = {}
                expected_admission[str(shared)] = observation
            assert {path: digest(path) for path in hashes} == hashes, "Primary changed original late input bytes"
            receipts.append({"mode": mode, "arm": label, "shared": shared, "command": command, "input_sha256": hashes,
                             "generated_sha256": emitted(destination), "late_admission": observation,
                             "value_constructor_and_dependency": armed})
            print("graph-copied-late-admission=" + mode + ":" + label + ":target,dependency,original-boundary,negative-controls", flush=True)
    (output / "admission.json").write_text(json.dumps(receipts, indent=2) + "\n")
    return receipts


if __name__ == "__main__":
    assert len(sys.argv) == 6, "companion probe cli corelib output"
    validate_copied_array(*map(Path, sys.argv[1:]))
