"""Original attribute inventories preserve identity effects without factory execution."""
from pathlib import Path
import json
import re
import xml.etree.ElementTree as ET

from copied_array_admission import INPUTS, COPIED, digest, run, emitted, manifest, contract_digest, framed_hash

INVENTORY_MODES = (
    "inventory-copied-type-early", "inventory-copied-type-late", "inventory-copied-type-no-read", "inventory-copied-type-no-carrier",
    "inventory-copied-assembly", "inventory-copied-module", "inventory-copied-assembly-no-read",
    "inventory-removed-method", "inventory-removed-return", "inventory-removed-parameter", "inventory-removed-property",
    "inventory-untouched-ctor", "inventory-untouched-ctor-no-read", "inventory-untouched-unreached",
    "inventory-class-carried-early", "inventory-class-carried-late", "inventory-class-no-carrier", "inventory-class-carried-no-read",
    "inventory-framework-named", "inventory-framework-unnamed", "inventory-excluded-event", "inventory-excluded-generic-parameter",
    "inventory-copied-field", "inventory-untouched-cctor", "inventory-untouched-cctor-unreached",
)
EFFECTS = ("Ctor", "Setter", "Getter", "Sibling", "Ordinary")


def active(mode):
    assert mode in INVENTORY_MODES
    return not mode.endswith(("-no-carrier", "-unreached", "-unnamed")) and "-excluded-" not in mode


def constructs(mode):
    return mode.startswith("inventory-class-") and not mode.endswith("-no-carrier") or mode.startswith("inventory-untouched-") and not mode.endswith("-unreached")


def policy(path, mode, published=False, present=True):
    root = ET.parse(path).getroot()
    assert root.tag == "linker" and not root.attrib
    if not present:
        assert len(root) == 0
        return
    assert len(root) == 1 and root[0].tag == "assembly" and root[0].attrib == {"fullname": "GraphLib", "preserve": "nothing"}
    owner = root[0]
    assert len(owner) == 1 and owner[0].tag == "type"
    target = "Graph.InventorySiblingAttribute" if mode.startswith("inventory-class-") else "Graph.InventoryConditional"
    assert owner[0].attrib == {"fullname": target, "preserve": "nothing", "required": "0"}
    assert len(owner[0]) == 1 and len(owner[0][0]) == 0 and owner[0][0].tag == "method"
    assert owner[0][0].attrib == ({"signature": "System.Void Activate()"} if published else {"name": "Activate"})


def create(probe, corelib, area, mode):
    area.mkdir(parents=True)
    original = area / "input"
    result = run(["dotnet", "exec", probe, "--create-copied-array-fixtures", original, corelib, mode], area, "create")
    assert result.stdout.splitlines() == [("fixture-created=copied-array mode=" + mode).encode()]
    result = run(["dotnet", "exec", probe, "--check-copied-array-fixtures", original, mode, "original"], area, "original-check")
    assert result.stdout.splitlines()[-1].startswith(("graph-copied-array=original:" + mode + ":").encode())
    policy(original / "link.xml", mode)
    return original


def hashes_for(original, refs):
    return {str(p): digest(p) for p in [original / "GraphApp.dll", *refs, original / "link.xml"]}


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
    for name in COPIED:
        row = next(row for row in root.findall("assembly") if row.get("name") + ".dll" == name)
        assert row.get("mode") == "copied" and row.get("registryRewritten") == "false"
        assert (published / name).read_bytes() == (original / name).read_bytes()
    state = "observed-before" if observed else "published"
    check = run(["dotnet", "exec", probe, "--check-copied-array-fixtures", published, mode, state], area, "retained-check", extra=extra)
    assert check.stdout.splitlines()[-1].startswith(("graph-copied-array=" + state + ":" + mode + ":").encode())
    prefix = "graph-inventory-observed="
    observation = json.loads(next(line[len(prefix):] for line in check.stdout.decode().splitlines() if line.startswith(prefix)))
    # A conditional rule may disappear with its owner on an inactive/old path.
    present = len(ET.parse(published / "preservation.xml").getroot()) != 0
    policy(published / "preservation.xml", mode, published=True, present=present)
    if not observed and active(mode):
        assert present and observation["metadata"] and observation["attributesPresent"], "Lost original conditional activation evidence"
    closure = run(["dotnet", "exec", probe, "--check-token-closure", *sorted(published.glob("*.dll"))], area, "closure", extra=extra)
    assert b"token-closure=valid operands=" in closure.stdout
    for name in INPUTS:
        first = set(run(["dotnet", "exec", probe, "--relations", original / name], area, "before-" + name, extra=extra).stdout.splitlines())
        after = set(run(["dotnet", "exec", probe, "--relations", published / name], area, "after-" + name, extra=extra).stdout.splitlines())
        assert after and after <= first
    assert hashes_for(original, refs) == hashes
    receipt = {"observation": observation, "files": {p.name: digest(p) for p in published.iterdir() if p.is_file()},
               "manifest": (area / "publication/ildiet-result.xml").read_text().replace(str(area / "publication"), "<publication>"),
               "stdout": execution.stdout.decode().replace(str(area / "publication"), "<publication>"), "stderr": execution.stderr.decode(),
               "retained_stdout": check.stdout.decode(), "retained_stderr": check.stderr.decode()}
    (area / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    return receipt


def admission(cli, original, refs, area, mode, integrated=False, extra=None, expected=None):
    area.mkdir(parents=True)
    hashes = hashes_for(original, refs)
    receipts = []
    for shared in (True, False):
        label = "sharing-on" if shared else "sharing-off"
        destination = area / label
        command = ["dotnet", "exec", cli, original / "GraphApp.dll"]
        for ref in refs:
            command += ["-r", ref]
        command += ["--link-xml", original / "link.xml", "--shared-generics" if shared else "--no-shared-generics", "-o", destination]
        if not integrated:
            command += ["--no-ildiet"]
        execution = run(command, area, label, extra=extra)
        if not shared:
            assert b"dn2cpp: shared-generics:" not in execution.stdout
        files = emitted(destination)
        source = "\n".join((destination / name).read_text() for name in files)
        def definition(symbol):
            return re.findall(r"(?m)^[A-Za-z_][A-Za-z0-9_* ]* " + symbol + r"_m[0-9]+\([^;\n]*\)\s*\{", source)
        assert definition("InventoryEntry_Main") and definition("InventoryEntry_Carry") and definition("InventoryEntry_Read")
        effects = {}
        for name in EFFECTS:
            body = definition("Inventory" + name + "Target__ctor")
            assert len(body) <= 1
            effects[name] = bool(body)
            assert bool(definition("Inventory" + name + "Dependency_Touch")) == effects[name]
            assert effects[name] == (name == "Ctor" and constructs(mode)), (mode, label, "unexpected constructor/accessor effect", name)
        target = "InventorySiblingAttribute" if mode.startswith("inventory-class-") else "InventoryConditional"
        actual = bool(definition(target + "_Activate"))
        assert actual == bool(definition("InventoryConditionalDependency_Touch")) == active(mode), (mode, label, "original conditional admission", actual)
        attr = "InventoryOuterAttribute" if mode.startswith("inventory-class-") else "InventoryDataAttribute"
        assert bool(definition(attr + "__ctor")) == constructs(mode)
        for symbol in (attr + "_get_Value", attr + "_set_Value", "InventorySiblingAttribute__ctor", "InventoryCarrier_Unused", "InventoryCarrier_UnusedGeneric", "InventoryCarrier_get_UnusedProperty",
                       "InventoryCarrier_set_UnusedProperty", "InventoryCarrier_add_UnusedEvent", "InventoryCarrier_remove_UnusedEvent", "InventoryCarrier__cctor"):
            assert not definition(symbol), (mode, label, symbol)
        observation = {"active": actual, "effects": effects}
        if expected is not None:
            assert observation == expected[str(shared)], "Integrated primary lost original attribute identity admission"
        if integrated:
            root, published = manifest(destination / "ildiet")
            assert root.get("fallback") == "none" and int(root.find("counters").get("decode.max-per-method")) <= 1
            if active(mode): policy(published / "preservation.xml", mode, published=True)
            for name in COPIED: assert (published / name).read_bytes() == (original / name).read_bytes()
        else:
            assert not (destination / "ildiet").exists()
        assert hashes_for(original, refs) == hashes
        receipts.append({"shared": shared, "integrated": integrated, "generated_sha256": files, "admission": observation,
                         "command": list(map(str, command)), "input_sha256": hashes})
        print("graph-inventory-admission=" + mode + ":" + label + (":integrated" if integrated else ":original"), flush=True)
    (area / "admission.json").write_text(json.dumps(receipts, indent=2) + "\n")
    return receipts


def validate_attribute_inventory(companion, probe, cli, corelib, directory):
    directory = Path(directory)
    directory.mkdir(parents=True)
    for mode in INVENTORY_MODES:
        area = directory / mode
        original = create(probe, corelib, area, mode)
        refs = [*(original / name for name in INPUTS[1:]), Path(corelib)]
        raw = admission(cli, original, refs, area / "original-primary", mode)
        first = publish(companion, probe, original, refs, area / "first", mode)
        repeat = publish(companion, probe, original, refs, area / "repeat", mode)
        assert first == repeat
        admission(cli, original, refs, area / "integrated-primary", mode, integrated=True,
                  expected={str(row["shared"]): row["admission"] for row in raw})
    print("graph-validation=original-attribute-inventory,carried-and-untouched-owners,assembly-module-rows,removed-member-evidence,argument-and-zero-argument-class-identities,no-read-identity,conditional-contract,nonconstructing-provider-closure", flush=True)
