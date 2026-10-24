"""Selected metadata rows and field-only copied layouts retain distinct demands."""
from preserve_profile import sharing_modes
from pathlib import Path
import json
import re
import xml.etree.ElementTree as ET

SELECTED_MODES = (
    "selected-ss-data", "selected-named-direct", "selected-named-inherited",
    "selected-layout-search-early", "selected-layout-search-late", "selected-layout-box-early", "selected-layout-box-late",
    "selected-sc-data", "selected-cs-data", "selected-cc-data",
    "selected-ss-instance", "selected-sc-instance", "selected-cs-instance", "selected-cc-instance",
    "selected-ss-upgrade", "selected-sc-upgrade", "selected-cs-upgrade", "selected-cc-upgrade",
    "selected-ss-noquery", "selected-ss-unmatched", "selected-ss-inactive",
    "selected-sc-noquery", "selected-sc-unmatched", "selected-sc-inactive",
    "selected-named-field", "selected-named-direct-upgrade", "selected-named-inherited-upgrade",
    "selected-named-direct-unmatched", "selected-named-direct-nested", "selected-named-inherited-nested",
    "selected-layout-search-nocap", "selected-layout-search-nolayout", "selected-layout-search-nofactory",
    "selected-layout-box-nocap", "selected-layout-box-nolayout",
    "selected-named-identity-carried", "selected-named-identity-passive",
    "selected-ss-known-instance", "selected-named-inherited-known-upgrade", "selected-ss-preserved-instance",
)
EFFECTS = ("Dispatch", "Ctor", "Setter", "SecondSetter", "Getter", "Sibling", "Ordinary", "Conditional")


def traits(mode):
    assert mode in SELECTED_MODES
    layout = mode.startswith("selected-layout-")
    named = mode.startswith("selected-named-")
    match = not mode.endswith(("-noquery", "-unmatched"))
    construct = "-preserved-" in mode or "-known-" in mode and mode.endswith(("-instance", "-upgrade")) and match
    dispatch = layout and not mode.endswith(("-nocap", "-nolayout", "-nofactory"))
    return layout, named, match, construct, dispatch


def expected_effects(mode):
    layout, named, match, construct, dispatch = traits(mode)
    return {name: value for name, value in zip(EFFECTS, (
        dispatch, not layout and (named and not mode.endswith("-identity-passive") or construct), not layout and construct and "-field" not in mode,
        named and construct, False, False, False, mode.endswith("-identity-carried") or not layout and not named and ("-known-" in mode or "-preserved-" in mode) and match and not mode.endswith("-inactive")))}


def selected_policy(path, mode, published=False):
    root = ET.parse(path).getroot()
    assert root.tag == "linker" and not root.attrib
    layout, named, *_ = traits(mode)
    if layout or named and "-identity-" not in mode:
        assert len(root) == 0, "selected fixture gained preservation roots"
        return
    if "-preserved-" in mode:
        assert len(root) == 2
        preserved = next(a for a in root if a.get("fullname") == "GraphLib")
        assert preserved.attrib == {"fullname": "GraphLib", "preserve": "nothing"} and len(preserved) == 1
        owner = preserved[0]
        assert owner.tag == "type" and owner.attrib == {"fullname": "Graph.SelectedSpecialAttribute", "preserve": "nothing"}
        expected = ({"signature": "System.Void .ctor(System.Type)"}, {"signature": "System.Void set_Value(System.Object)"}) if published else ({"name": ".ctor"}, {"name": "set_Value"})
        assert len(owner) == 2 and all(m.tag == "method" and len(m) == 0 for m in owner)
        assert sorted(tuple(m.attrib.items()) for m in owner) == sorted(tuple(m.items()) for m in expected)
        root.remove(preserved)
    assert len(root) == 1 and root[0].tag == "assembly" and root[0].attrib == {"fullname": "DnZlib", "preserve": "nothing"}
    assembly = root[0]
    assert len(assembly) == 1 and assembly[0].tag == "type" and assembly[0].attrib == {"fullname": "Graph.SelectedConditional", "preserve": "nothing", "required": "0"}
    owner = assembly[0]
    assert len(owner) == 1 and owner[0].tag == "method" and len(owner[0]) == 0
    assert owner[0].attrib == ({"signature": "System.Void Activate()"} if published else {"name": "Activate"})


def selected_descriptor(original, mode):
    path = Path(original) / "link.xml"
    layout, named, *_ = traits(mode)
    if layout or named and "-identity-" not in mode:
        assert not path.exists()
        return None
    selected_policy(path, mode)
    return path


def selected_admission(cli, original, corelib, output, mode, arms, references, extra, expected_admission):
    from copied_array_admission import INPUTS, COPIED, digest, run, emitted, manifest
    original, output = Path(original), Path(output)
    output.mkdir(parents=True, exist_ok=True)
    references = list(references) if references is not None else [*(original / name for name in INPUTS[1:]), Path(corelib)]
    descriptor = selected_descriptor(original, mode)
    paths = [original / "GraphApp.dll", *references, *([] if descriptor is None else [descriptor])]
    hashes = {str(Path(path).resolve()): digest(path) for path in paths}
    observations = {} if expected_admission is None else dict(expected_admission)
    receipts = []
    for arm in arms:
        assert arm in ("original", "integrated")
        for shared in sharing_modes():
            label = arm + ("-sharing-on" if shared else "-sharing-off")
            destination = output / label
            command = ["dotnet", "exec", cli, original / "GraphApp.dll"]
            for path in references:
                command += ["-r", path]
            command += ["--shared-generics" if shared else "--no-shared-generics", "-o", destination]
            if descriptor is not None:
                command += ["--link-xml", descriptor]
            if arm == "original":
                command += ["--no-ildiet"]
            result = run(command, output, label, extra=extra)
            if not shared:
                assert b"dn2cpp: shared-generics:" not in result.stdout
            source = "\n".join((destination / name).read_text(encoding="utf-8") for name in emitted(destination))

            def definitions(symbol):
                return re.findall(r"(?m)^[A-Za-z_][A-Za-z0-9_* ]* " + symbol + r"_m[0-9]+\([^;\n]*\)\s*\{", source)

            assert definitions("SelectedEntry_Main"), (mode, label)
            if not mode.startswith("selected-layout-") and "-known-" not in mode and not mode.endswith("-noquery"):
                diagnostics = destination / "reflection-unrecognized.tsv"
                assert diagnostics.is_file() and b"Graph.SelectedEntry::Read" in diagnostics.read_bytes(), (mode, label, "unknown attribute owner needs a diagnostic")
                assert b"attribute owner comes from parameter 1" in diagnostics.read_bytes(), (mode, label, "unknown owner was inferred from its caller")
            effects = {}
            for name in EFFECTS:
                constructors = definitions("Selected" + name + "Target__ctor")
                assert len(constructors) <= 1
                body = bool(constructors)
                assert bool(definitions("Selected" + name + "Dependency_Touch")) == body, (mode, label, "dependency", name)
                effects[name] = body
            assert effects == expected_effects(mode), (mode, label, "actual primary effect admission", effects)
            layout, named, match, construct, dispatch = traits(mode)
            assert bool(definitions("SelectedPayload_Equals")) == (dispatch and "-search-" in mode), (mode, label, "Equals admission")
            assert bool(definitions("SelectedPayload_ToString")) == (dispatch and "-box-" in mode), (mode, label, "ToString admission")
            assert not definitions("SelectedPayload_Ordinary")
            assert bool(definitions("SelectedSpecialAttribute__ctor")) == (not layout and (named and not mode.endswith("-identity-passive") or construct)), (mode, label, "attribute constructor")
            setter_owner = "SelectedBaseAttribute" if "-inherited" in mode else "SelectedSpecialAttribute"
            assert bool(definitions(setter_owner + "_set_Value")) == (not layout and construct and "-field" not in mode), (mode, label, "named setter")
            assert bool(definitions("SelectedSpecialAttribute_set_Second")) == (named and construct), (mode, label, "second setter")
            for symbol in (setter_owner + "_get_Value", "SelectedSpecialAttribute_get_Second", "SelectedSiblingAttribute__ctor", "SelectedUnmatchedAttribute__ctor"):
                assert not definitions(symbol), (mode, label, "forbidden attribute body", symbol)
            assert bool(definitions("SelectedConditional_Activate")) == effects["Conditional"]
            if layout:
                assert definitions("SelectedEntry_Capability") and definitions("SelectedEntry_Observe") and definitions("SelectedHolder_Touch")
            else:
                assert definitions("SelectedEntry_Read")
            # Emitted provider identity is observed, not presumed. The metadata-only
            # DLL contract remains mandatory even if this provider has no native row.
            provider_emitted = bool(re.search(r"(?m)^const Dn2CppTypeInfo ti_Graph_SelectedProvider =", source))
            observation = {"effects": effects, "provider_typeinfo_emitted": provider_emitted}
            if arm == "integrated":
                assert set(observations) == set(map(str, sharing_modes())), "Observe the selected original sharing modes first"
            if str(shared) in observations:
                assert observation == observations[str(shared)], (mode, label, "changed original primary admission")
            if arm == "original":
                assert not (destination / "ildiet").exists()
                observations[str(shared)] = observation
            else:
                root, published = manifest(destination / "ildiet")
                assert root.get("fallback") == "none" and root.find("counters").get("decode.max-per-method") == "1"
                selected_policy(published / "preservation.xml", mode, published=True)
                for name in COPIED:
                    assert (published / name).read_bytes() == (original / name).read_bytes(), (mode, name)
            assert {path: digest(path) for path in hashes} == hashes
            receipts.append({"mode": mode, "shared": shared, "arm": label, "command": list(map(str, command)),
                             "input_sha256": hashes, "generated_sha256": emitted(destination), "selected_admission": observation})
            print("graph-selected-admission=" + mode + ":" + label + ":effects,metadata-provider-observation,nonexecution", flush=True)
    (output / "admission.json").write_text(json.dumps(receipts, indent=2) + "\n")
    return receipts
