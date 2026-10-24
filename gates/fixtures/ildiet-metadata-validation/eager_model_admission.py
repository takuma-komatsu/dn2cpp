"""Original eager model formations retain their causal metadata without executing it."""
from preserve_profile import FULL, select_cases, sharing_modes, full_witness
from pathlib import Path
import json
import re
import xml.etree.ElementTree as ET

from copied_array_admission import INPUTS, COPIED, digest, run, emitted, manifest, contract_digest, framed_hash

EAGER_MODES = (
    "eager-attribute-stripped", "eager-attribute-copied", "eager-attribute-chain",
    "eager-attribute-no-ctor", "eager-attribute-template", "eager-attribute-field", "eager-attribute-signature",
    "eager-array-direct", "eager-array-nested", "eager-array-chain", "eager-attribute-framework", "eager-array-fanout",
    "eager-array-unarmed", "eager-array-template", "eager-array-field", "eager-array-signature",
    "eager-array-methodimpl", "eager-array-methodimpl-absent", "eager-array-methodimpl-candidate", "eager-array-methodimpl-different-name",
    "eager-array-vtable", "eager-array-vtable-newslot", "eager-array-vtable-different-name",
    "eager-preserve-direct", "eager-preserve-chain", "eager-preserve-template",
    "eager-array-intrinsic-shell", "eager-array-vector-shape",
    "eager-array-async-adopted", "eager-array-async-declined",
    "eager-array-async-shared-builder",
    "eager-array-async-option-sensitive",
    "eager-array-async-activation",
)
POSITIVE_MODES = (
    "eager-attribute-stripped", "eager-attribute-copied", "eager-attribute-chain",
    "eager-array-direct", "eager-array-nested", "eager-array-chain", "eager-attribute-framework", "eager-array-fanout",
    "eager-array-methodimpl", "eager-array-methodimpl-candidate", "eager-array-vtable",
    "eager-preserve-direct", "eager-preserve-chain", "eager-array-vector-shape",
    "eager-array-async-adopted", "eager-array-async-declined",
    "eager-array-async-shared-builder",
    "eager-array-async-option-sensitive",
    "eager-array-async-activation",
)
OPTION_SENSITIVE_MODE = "eager-array-async-option-sensitive"
ACTIVATION_MODE = "eager-array-async-activation"
OPTION_ARMS = (("default", ()), ("no-adopt-a", ("GraphLib",)),
               ("no-adopt-b", ("GraphTaskB",)), ("no-adopt-both", ("GraphLib", "GraphTaskB")))
NEGATIVE_DEPENDENCIES = (
    "EagerLazyFieldDependency", "EagerLazyMethodDependency", "EagerLazyConditionalDependency",
    "EagerAnchorCtorDependency", "EagerAnchorCctorDependency", "EagerAnchorOrdinaryDependency",
)


def policy_mode(mode):
    return mode.startswith("eager-preserve-")


def inputs_for(mode):
    return (*INPUTS, "GraphTaskB.dll") if mode in ("eager-array-async-shared-builder", OPTION_SENSITIVE_MODE) else INPUTS


def references_for(original, corelib, mode):
    refs = [original / name for name in inputs_for(mode)[1:]]
    if mode.startswith("eager-array-async-"):
        return [*refs, original / "System.Private.CoreLib.dll", original / "Dn2Cpp.Runtime.dll"]
    return [*refs, Path(corelib)]


def copied_for(mode):
    return (*COPIED, "System.Private.CoreLib.dll", "Dn2Cpp.Runtime.dll") if mode.startswith("eager-array-async-") else COPIED


def expected_kind(mode, kind, no_adopt=()):
    if mode == OPTION_SENSITIVE_MODE:
        assert tuple(no_adopt) in [arm for _, arm in OPTION_ARMS]
        return {"conditional": tuple(no_adopt) == ("GraphLib",), "array": True,
                "array_b": tuple(no_adopt) != ("GraphLib",), "policy": False}[kind]
    assert not no_adopt
    if kind == "policy":
        return policy_mode(mode) and mode in POSITIVE_MODES
    if kind == "array":
        return mode.startswith("eager-array-") and mode in POSITIVE_MODES and mode not in (
            "eager-array-async-adopted", "eager-array-async-shared-builder")
    if kind == "array_b":
        return mode == "eager-array-async-shared-builder"
    return mode.startswith("eager-attribute-") and mode in POSITIVE_MODES or mode in (
        "eager-array-async-adopted", "eager-array-async-shared-builder")


def observation(stdout):
    prefix = "graph-eager-observed="
    result = json.loads(next(line[len(prefix):] for line in stdout.decode().splitlines() if line.startswith(prefix)))
    order_prefix = "graph-eager-option-order="
    order = [line[len(order_prefix):] for line in stdout.decode().splitlines() if line.startswith(order_prefix)]
    assert len(order) <= 1
    if order:
        result["optionPredicateOrder"] = json.loads(order[0])
    return result


def policy(path, mode, published=False):
    root = ET.parse(path).getroot()
    assert root.tag == "linker" and not root.attrib and len(root) <= 1
    if not published:
        assert len(root) == 1
    names = []
    for assembly in root:
        assert assembly.tag == "assembly" and assembly.attrib == {"fullname": "GraphLib", "preserve": "nothing"}
        for owner in assembly:
            name = owner.get("fullname")
            names.append(name)
            conditional = name in ("Graph.EagerConditional", "Graph.EagerLazyConditional")
            assert conditional or name == "Graph.EagerPolicy`1" and policy_mode(mode)
            assert owner.attrib == {"fullname": name, "preserve": "nothing", **({"required": "0"} if conditional else {})}
            assert len(owner) == 1 and owner[0].tag == "method" and len(owner[0]) == 0
            assert owner[0].attrib == ({"signature": "System.Void Activate()"} if published else {"name": "Activate"})
    assert len(names) == len(set(names))
    if not published:
        assert names == ["Graph.EagerConditional", "Graph.EagerLazyConditional"] + (
            ["Graph.EagerPolicy`1"] if policy_mode(mode) else [])
    return names


def hashes_for(original, refs):
    return {str(p): digest(p) for p in [original / "GraphApp.dll", *refs, original / "link.xml"]}


def create(probe, corelib, area, mode):
    assert mode in EAGER_MODES
    area.mkdir(parents=True)
    original = area / "input"
    created = run(["dotnet", "exec", probe, "--create-copied-array-fixtures", original, corelib, mode], area, "create")
    assert created.stdout.splitlines() == [("fixture-created=copied-array mode=" + mode).encode()]
    checked = run(["dotnet", "exec", probe, "--check-copied-array-fixtures", original, mode, "original"], area, "original-check")
    assert checked.stdout.splitlines()[-1].startswith(("graph-copied-array=original:" + mode + ":").encode())
    policy(original / "link.xml", mode)
    (area / "original-evidence.json").write_text(json.dumps(observation(checked.stdout), indent=2) + "\n")
    return original


def formation_evidence(before, after, mode):
    if mode not in POSITIVE_MODES:
        return
    keys = [] if mode == ACTIVATION_MODE else [
        ("System.ArrayFacade" if mode == "eager-attribute-framework" else "GraphLib") + ":Graph.EagerAnchor"]
    if mode.endswith("-chain"):
        keys.append("GraphLib:Graph.EagerMid`1")
    if mode == "eager-array-fanout":
        keys.append("GraphLib:Graph.EagerSecondAnchor")
    if "-methodimpl" in mode:
        keys.append("GraphLib:Graph.EagerContract")
    if "-vtable" in mode:
        keys.append("GraphLib:Graph.EagerVirtualBase`1")
    if mode == "eager-array-vector-shape":
        keys.append("GraphLib:System.Numerics.Vector`1")
    if mode.startswith("eager-array-async-"):
        keys.append("GraphLib:Graph.EagerTask" if mode == ACTIVATION_MODE else "GraphLib:Graph.EagerTask`1")
    if mode in ("eager-array-async-adopted", "eager-array-async-shared-builder", OPTION_SENSITIVE_MODE):
        keys.append("GraphLib:System.Threading.Tasks.Task`1")
    if mode in ("eager-array-async-shared-builder", OPTION_SENSITIVE_MODE):
        keys.append("GraphTaskB:Graph.EagerDeclinedTask`1")
    if mode in ("eager-array-async-shared-builder", OPTION_SENSITIVE_MODE, ACTIVATION_MODE):
        assert after["asyncContracts"] == before["asyncContracts"]
        assert before["declineCarrier"] is not None and after["declineCarrier"] == before["declineCarrier"], (
            mode, "Original dead private decline carrier IL/MemberRef changed")
        first = bytes.fromhex(before["declineCarrierIl"])
        second = bytes.fromhex(after["declineCarrierIl"])
        # The sole call's metadata token may be remapped; every other IL byte is original.
        assert len(first) == len(second) == (11 if mode == OPTION_SENSITIVE_MODE else 10)
        assert first[:5] == second[:5] and first[9:] == second[9:]
        if mode == OPTION_SENSITIVE_MODE:
            assert len(before["optionPredicateOrder"]) == 3
            assert after["optionPredicateOrder"] == before["optionPredicateOrder"], (
                mode, "Original async candidate or contract method row order changed")
    if mode.startswith("eager-attribute-"):
        keys.append(("DnZlib" if mode in ("eager-attribute-copied", "eager-attribute-framework") else "GraphLib") + ":Graph.EagerProvider`1")
    for key in keys:
        assert key in before["formations"] and key in after["formations"], (mode, "missing original formation source", key)
        assert after["formations"][key] == before["formations"][key], (mode, "changed causal relation/member signature", key)


def publish(companion, probe, original, refs, area, mode, observed=False, extra=None):
    area.mkdir(parents=True)
    hashes = hashes_for(original, refs)
    request = ET.Element("ildiet", version="2", input=str(original / "GraphApp.dll"), output=str(area / "publication"))
    for ref in refs:
        ET.SubElement(request, "reference", path=str(ref))
    ET.SubElement(request, "linkXml", path=str(original / "link.xml"))
    request_path = area / "request.xml"
    ET.ElementTree(request).write(request_path, encoding="utf-8", xml_declaration=True)
    execution = run(["dotnet", "exec", companion, "--request", request_path], area, "companion", extra=extra)
    root, published = manifest(area / "publication")
    assert root.get("fallback") == "none" and int(root.find("counters").get("decode.max-per-method")) <= 1
    assert root.get("contract") == contract_digest(request)
    assert [Path(row.get("path")).name for row in root.findall("reference")] == [p.name for p in refs]
    assert Path(root.get("input")).name == "GraphApp.dll"
    values = []
    for row in root.findall("assembly"):
        name = row.get("name") + ".dll"
        values.extend((name, (published / name).read_bytes()))
    values.extend(("preservation.xml", (published / "preservation.xml").read_bytes()))
    assert root.get("generation") == "g-" + framed_hash(values)
    for name in copied_for(mode):
        row = next(row for row in root.findall("assembly") if row.get("name") + ".dll" == name)
        assert row.get("mode") == "copied" and row.get("registryRewritten") == "false"
        assert (published / name).read_bytes() == (original / name).read_bytes()
    if mode in ("eager-array-async-shared-builder", OPTION_SENSITIVE_MODE):
        assert next(row for row in root.findall("assembly") if row.get("name") == "GraphTaskB").get("mode") == "rewritten"
    state = "observed-before" if observed else "published"
    checked = run(["dotnet", "exec", probe, "--check-copied-array-fixtures", published, mode, state], area, "retained-check", extra=extra)
    assert checked.stdout.splitlines()[-1].startswith(("graph-copied-array=" + state + ":" + mode + ":").encode())
    observed_state = observation(checked.stdout)
    names = policy(published / "preservation.xml", mode, published=True)
    if not observed:
        before = json.loads((original.parent / "original-evidence.json").read_text())
        formation_evidence(before, observed_state, mode)
        if expected_kind(mode, "conditional") or mode == OPTION_SENSITIVE_MODE:
            assert "Graph.EagerConditional" in names and observed_state["attributeEvidence"]
        if policy_mode(mode):
            assert "Graph.EagerPolicy`1" in names
        if mode in ("eager-array-async-shared-builder", OPTION_SENSITIVE_MODE, ACTIVATION_MODE):
            assert int(root.find("counters").get("methods.evidence-body", "0")) >= 1
    closure = run(["dotnet", "exec", probe, "--check-token-closure", *sorted(published.glob("*.dll"))], area, "closure", extra=extra)
    assert b"token-closure=valid operands=" in closure.stdout
    names = (*inputs_for(mode), "System.Private.CoreLib.dll", "Dn2Cpp.Runtime.dll") if mode.startswith("eager-array-async-") else inputs_for(mode)
    for name in names:
        first = set(run(["dotnet", "exec", probe, "--relations", original / name], area, "before-" + name, extra=extra).stdout.splitlines())
        after = set(run(["dotnet", "exec", probe, "--relations", published / name], area, "after-" + name, extra=extra).stdout.splitlines())
        assert after and after <= first
    assert hashes_for(original, refs) == hashes
    receipt = {"observation": observed_state, "files": {p.name: digest(p) for p in published.iterdir() if p.is_file()},
               "manifest": (area / "publication/ildiet-result.xml").read_text(encoding="utf-8").replace(str(area / "publication"), "<publication>"),
               "stdout": execution.stdout.decode().replace(str(area / "publication"), "<publication>"), "stderr": execution.stderr.decode(),
               "retained_stdout": checked.stdout.decode(), "retained_stderr": checked.stderr.decode()}
    (area / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    return receipt


def admission(cli, original, refs, area, mode, integrated=False, extra=None, expected=None, observe=False, no_adopt=()):
    area.mkdir(parents=True)
    hashes = hashes_for(original, refs)
    receipts = []
    for shared in sharing_modes():
        label = "sharing-on" if shared else "sharing-off"
        destination = area / label
        command = ["dotnet", "exec", cli, original / "GraphApp.dll"]
        for ref in refs:
            command += ["-r", ref]
        for assembly in no_adopt:
            command += ["--no-adopt-async", assembly]
        command += ["--link-xml", original / "link.xml", "--shared-generics" if shared else "--no-shared-generics", "-o", destination]
        if not integrated:
            command += ["--no-ildiet"]
        execution = run(command, area, label, extra=extra, expected=None if observe else 0)
        if execution.returncode != 0:
            assert observe and integrated and b"ILDiet's retained set must contain every body reachability admits" in execution.stderr, (
                mode, label, execution.returncode, execution.stderr.decode(errors="replace"))
            errors = re.findall(r"([^\r\n]*?) is admitted, but ILDiet kept only its signature:", execution.stderr.decode(errors="replace"))
            assert len(errors) == 1
            targets = []
            if expected_kind(mode, "conditional", no_adopt):
                targets += [r"Graph\.EagerConditional::Activate", r"Graph\.EagerConditionalDependency::Touch"]
            if expected_kind(mode, "array", no_adopt):
                targets += [r"Graph\.EagerCell_[^:\r\n]+::\.ctor", r"Graph\.EagerArrayDependency::Touch"]
            if expected_kind(mode, "array_b", no_adopt):
                targets += [r"Graph\.EagerBCell_[^:\r\n]+::\.ctor", r"Graph\.EagerBArrayDependency::Touch"]
            if expected_kind(mode, "policy", no_adopt):
                targets += [r"Graph\.EagerPolicy_[^:\r\n]+::Activate", r"Graph\.EagerPolicyDependency::Touch"]
            assert targets and any(re.search(target + r"$", errors[0]) for target in targets), (
                mode, label, "Unrelated frozen39 invariant failure cannot establish this witness", errors)
            assert hashes_for(original, refs) == hashes
            receipts.append({"shared": shared, "integrated": True, "admission": None, "retained_body_invariant_failure": True,
                             "exact_admitted_stub": errors[0],
                             "command": list(map(str, command)), "input_sha256": hashes})
            continue
        if not shared:
            assert b"dn2cpp: shared-generics:" not in execution.stdout
        files = emitted(destination)
        source = "\n".join((destination / name).read_text(encoding="utf-8") for name in files)
        def definitions(symbol):
            return re.findall(r"(?m)^[A-Za-z_][A-Za-z0-9_* ]* " + symbol + r"_m[0-9]+\([^;\n]*\)\s*\{", source)
        assert all(definitions("EagerEntry_" + name) for name in ("Main", "Carry", "Arm"))
        counts = {"conditional": len(definitions("EagerConditional_Activate")),
                  "array": len(definitions("EagerCell_1__ctor")),
                  "array_b": len(definitions("EagerBCell_1__ctor")),
                  "policy": len(definitions("EagerPolicy_1_Activate"))}
        for name, dependency in (("conditional", "EagerConditionalDependency"), ("array", "EagerArrayDependency"),
                                 ("array_b", "EagerBArrayDependency"), ("policy", "EagerPolicyDependency")):
            assert bool(counts[name]) == bool(definitions(dependency + "_Touch")), (mode, label, "body/dependency mismatch", name)
        negatives = {name: bool(definitions(name + "_Touch")) for name in NEGATIVE_DEPENDENCIES}
        attr_ctor = bool(definitions("EagerDataAttribute__ctor"))
        assert not any(negatives.values()), (mode, label, "lazy field/signature or passive anchor executed", negatives)
        assert attr_ctor == (mode != "eager-attribute-no-ctor")
        for symbol in ("EagerLazyFieldCell_1__ctor", "EagerLazyMethodCell_1__ctor", "EagerLazyConditional_Activate",
                       "EagerAnchor__ctor", "EagerAnchor__cctor", "EagerAnchor_Ordinary", "EagerAnchor_Implement",
                       "EagerAnchor_Resolve", "EagerAnchor_Other", "EagerVirtualBase_1_Resolve", "EagerDataAttribute_Unused",
                       "EagerDeadCross_Decline", "EagerTask_1_OutsideContract", "EagerDeclinedTask_1_OutsideContract"):
            assert not definitions(symbol), (mode, label, "nonexecuting evidence gained a body", symbol)
        if mode == OPTION_SENSITIVE_MODE:
            assert not definitions("EagerBuilder_1_get_Task"), (mode, label, "dead builder getter executed")
        if mode == ACTIVATION_MODE:
            for symbol in ("EagerTask_OutsideContract", "EagerBuilder_Create", "EagerBuilder_Start",
                           "EagerBuilder_SetStateMachine", "EagerBuilder_SetResult", "EagerBuilder_SetException", "EagerBuilder_get_Task"):
                assert not definitions(symbol), (mode, label, "arity-zero evidence gained an emitted body", symbol)
        if observe:
            assert all(not counts[kind] for kind in counts if not expected_kind(mode, kind, no_adopt)), (
                mode, label, "Frozen39 negative admission changed; requires diagnosis", counts)
        if not observe:
            for name in ("conditional", "array", "array_b", "policy"):
                assert bool(counts[name]) == expected_kind(mode, name, no_adopt), (mode, label, "original admission differs from source witness", counts, no_adopt)
            if mode == "eager-array-fanout":
                assert counts["array"] >= 2, "Both original value specializations must be observed"
            if mode.startswith("eager-array-async-") and (not integrated or mode in ("eager-array-async-shared-builder", OPTION_SENSITIVE_MODE, ACTIVATION_MODE)):
                notices = [line for line in execution.stderr.decode().splitlines() if line.startswith("dn2cpp: adoption declined:")]
                wanted = []
                if mode in ("eager-array-async-declined", ACTIVATION_MODE):
                    wanted = ["dn2cpp: adoption declined: Graph.EagerTask::OutsideContract is outside the mapped contract; transpiling GraphLib's real IL"]
                elif mode == "eager-array-async-shared-builder":
                    wanted = ["dn2cpp: adoption declined: Graph.EagerDeclinedTask::OutsideContract is outside the mapped contract; transpiling GraphTaskB's real IL"]
                elif mode == OPTION_SENSITIVE_MODE:
                    declined = ("GraphLib", "GraphTaskB") if not no_adopt else ("GraphLib",) if tuple(no_adopt) == ("GraphTaskB",) else ()
                    wanted = [f"dn2cpp: adoption declined: Graph.EagerBuilder::get_Task is outside the mapped contract; transpiling {name}'s real IL"
                              for name in declined]
                assert notices == wanted, ("Original raw async decline classification changed", notices, wanted)
        observed_state = {"counts": counts, "negatives": negatives, "attribute_ctor": attr_ctor}
        if expected is not None:
            assert observed_state == expected[str(shared)], (mode, label, "integrated primary lost original formation admission")
        if integrated:
            root, published = manifest(destination / "ildiet")
            assert root.get("fallback") == "none" and int(root.find("counters").get("decode.max-per-method")) <= 1
            policy(published / "preservation.xml", mode, published=True)
            for name in copied_for(mode):
                assert (published / name).read_bytes() == (original / name).read_bytes()
        else:
            assert not (destination / "ildiet").exists()
        assert hashes_for(original, refs) == hashes
        receipts.append({"shared": shared, "integrated": integrated, "generated_sha256": files, "admission": observed_state,
                         "command": list(map(str, command)), "input_sha256": hashes})
        print("graph-eager-admission=" + mode + ":" + label + (":integrated" if integrated else ":original"), flush=True)
    (area / "admission.json").write_text(json.dumps(receipts, indent=2) + "\n")
    return receipts


def validate_eager_model(companion, probe, cli, corelib, directory):
    directory = Path(directory)
    directory.mkdir(parents=True)
    for mode in select_cases("eager-model", EAGER_MODES, (
            "eager-attribute-stripped", "eager-attribute-no-ctor",
            "eager-array-direct", "eager-array-unarmed",
            "eager-array-methodimpl", "eager-array-methodimpl-absent")):
        area = directory / mode
        original = create(probe, corelib, area, mode)
        refs = references_for(original, corelib, mode)
        arms = OPTION_ARMS if mode == OPTION_SENSITIVE_MODE else (("default", ()),)
        raw = {label: admission(cli, original, refs, area / ("original-primary-" + label), mode, no_adopt=options)
               for label, options in arms} if mode == OPTION_SENSITIVE_MODE else {
                   "default": admission(cli, original, refs, area / "original-primary", mode)}
        first = publish(companion, probe, original, refs, area / "first", mode)
        if FULL:
            repeat = publish(companion, probe, original, refs, area / "repeat", mode)
            assert first == repeat
        for label, options in arms:
            admission(cli, original, refs, area / ("integrated-primary-" + label if mode == OPTION_SENSITIVE_MODE else "integrated-primary"),
                      mode, integrated=True, no_adopt=options,
                      expected={str(row["shared"]): row["admission"] for row in raw[label]})
    full_witness("graph-validation=eager-original-model,recursive-relations,nested-arguments,methodimpl-memberref,virtual-slot-signatures,passive-formation-sources,lazy-retained-anchor-controls,conditional-and-generic-policy,sharing-on-off", flush=True)
