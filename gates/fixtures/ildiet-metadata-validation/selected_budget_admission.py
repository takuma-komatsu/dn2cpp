"""Work-budget boundaries publish copy fallbacks or complete rewritten outputs."""
from preserve_profile import FULL, budget_cuts, full_witness
from pathlib import Path
import json
import xml.etree.ElementTree as ET

from copied_array_admission import (INPUTS, check_publication_preservation, contract_digest,
                                    descriptor_for, digest, framed_hash, manifest, publish_copied, run)

BUDGET_MODES = ("selected-ss-data", "selected-named-direct-nested")
# Keep the exhaustive interval small; fixture growth requires an explicit review.
MAX_FIXTURE_WORK = 1024


def prepare_budget_fixture(probe, corelib, area, mode, references=None):
    area = Path(area)
    area.mkdir(parents=True, exist_ok=True)
    original = area / "input"
    created = run(["dotnet", "exec", probe, "--create-copied-array-fixtures", original, corelib, mode], area, "create")
    assert created.stdout.splitlines() == [("fixture-created=copied-array mode=" + mode).encode()]
    checked = run(["dotnet", "exec", probe, "--check-copied-array-fixtures", original, mode, "original"], area, "original")
    assert checked.stdout.startswith(("graph-copied-array=original:" + mode + ":").encode())
    refs = [*(original / name for name in INPUTS[1:]), Path(corelib), *(references or [])]
    paths = [original / "GraphApp.dll", *refs]
    descriptor = descriptor_for(original, mode)
    hashes = {str(path): digest(path) for path in paths + ([] if descriptor is None else [descriptor])}
    closure = run(["dotnet", "exec", probe, "--check-token-closure", *paths], area, "original-closure")
    assert b"token-closure=valid operands=" in closure.stdout
    return original, refs, hashes


def budget_attempt(companion, original, references, mode, area, budget, protocol=True, extra=None, publication=None):
    area = Path(area)
    area.mkdir(parents=True, exist_ok=True)
    publication = Path(publication) if publication is not None else area / "publication"
    request = ET.Element("ildiet", version="2", input=str(original / "GraphApp.dll"), output=str(publication), workBudget=str(budget))
    for reference in references:
        ET.SubElement(request, "reference", path=str(reference))
    descriptor = descriptor_for(original, mode)
    if descriptor is not None:
        ET.SubElement(request, "linkXml", path=str(descriptor))
    request_path = area / "request.xml"
    ET.ElementTree(request).write(request_path, encoding="utf-8", xml_declaration=True)
    command = ["dotnet", "exec", companion]
    if protocol:
        command += ["--request", request_path]
    else:
        command += [original / "GraphApp.dll"]
        for reference in references:
            command += ["-r", reference]
        if descriptor is not None:
            command += ["--link-xml", descriptor]
        command += ["--work-budget", str(budget), "-o", publication]
    result = run(command, area, "companion", expected=None, extra=extra)
    (area / "execution.json").write_text(json.dumps({"command": list(map(str, command)), "budget": budget,
        "protocol": protocol, "returncode": result.returncode}, indent=2) + "\n")
    return result, request, publication


def validate_budget_copy(result, request, publication, original, references, mode, hashes, probe=None, area=None, extra=None, cache=None):
    assert result.returncode == 0, (result.returncode, result.stderr)
    if cache:
        root = ET.parse(Path(publication) / "ildiet-result.xml").getroot()
        assert root.tag == "ildietResult" and root.get("version") == "2"
        assert root.get("generation") == cache["generation"]
        published = Path(publication).resolve() / root.get("generation")
        assert root.get("preservation") == str(published / "preservation.xml")
        assert [dict(row.attrib) for row in root.findall("assembly")] == cache["assemblies"]
    else:
        root, published = manifest(publication)
    assert root.get("fallback") == "budget" and root.get("constructorRegistriesRewritten") == "false"
    assert root.get("cutsValidated") == "true" and root.get("contract") == contract_digest(request)
    paths = [original / "GraphApp.dll", *references]
    assemblies = root.findall("assembly")
    assert [row.get("name") + ".dll" for row in assemblies] == [path.name for path in paths]
    assert Path(root.get("input")) == published / paths[0].name
    assert [row.get("path") for row in root.findall("reference")] == [str(published / path.name) for path in references]
    assert {p.name for p in published.iterdir()} == {p.name for p in paths} | {"preservation.xml"}
    values = []
    for row, source in zip(assemblies, paths):
        assert row.get("mode") == "copied" and row.get("registryRewritten") == "false"
        assert digest(published / source.name) == hashes[str(source)], (source, "fallback changed bytes")
        if not cache:
            values.extend((source.name, (published / source.name).read_bytes()))
    check_publication_preservation(published, mode)
    values.extend(("preservation.xml", (published / "preservation.xml").read_bytes()))
    if cache:
        assert values[-1] == cache["preservation"]
    else:
        assert root.get("generation") == "g-" + framed_hash(values)
        if cache is not None:
            cache.update(generation=root.get("generation"), assemblies=[dict(row.attrib) for row in assemblies], preservation=values[-1])
    counters = root.find("counters").attrib
    assert counters["work.budget"] == request.get("workBudget")
    assert int(counters["work"]) == int(request.get("workBudget"))
    assert counters["fallback"] == "1" and int(counters["fallback.assemblies"]) == len(paths)
    assert int(counters["decode.max-per-method"]) <= 1
    assert {path: digest(path) for path in hashes} == hashes, "Budget fallback mutated original inputs"
    if probe is not None:
        checked = run(["dotnet", "exec", probe, "--check-token-closure", *sorted(published.glob("*.dll"))], area, "closure", extra=extra)
        assert b"token-closure=valid operands=" in checked.stdout
    normalized = (Path(publication) / "ildiet-result.xml").read_text(encoding="utf-8").replace(str(publication), "<publication>")
    return {"manifest": normalized, "stdout": result.stdout.decode().replace(str(publication), "<publication>"),
            "stderr": result.stderr.decode().replace(str(publication), "<publication>"),
            "generation": root.get("generation"), "counters": dict(counters)}


def normal_budget_control(companion, probe, original, corelib, references, mode, area):
    publish_copied(companion, probe, original, corelib, area, mode, references=references)
    root, published = manifest(Path(area) / "publication")
    work = int(root.find("counters").get("work"))
    assert 1 < work <= MAX_FIXTURE_WORK, "Review fixture growth before enlarging the exhaustive budget interval"
    return work, (published / "preservation.xml").read_bytes()


def validate_selected_budgets(companion, probe, corelib, directory):
    directory = Path(directory)
    directory.mkdir(parents=True, exist_ok=True)
    receipts = []
    for mode in BUDGET_MODES:
        area = directory / mode
        original, refs, hashes = prepare_budget_fixture(probe, corelib, area, mode)
        work, preservation = normal_budget_control(companion, probe, original, corelib, refs, mode, area / "normal")
        normal_root, normal_output = manifest(area / "normal" / "publication")
        normal_files = {p.name: digest(p) for p in normal_output.iterdir() if p.is_file()}
        for budget in (work, work + 1):
            result, request, publication = budget_attempt(companion, original, refs, mode,
                                                          area / ("success-" + str(budget)), budget)
            assert result.returncode == 0, (result.returncode, result.stderr)
            root, published = manifest(publication)
            assert root.get("fallback") == "none" and root.get("contract") == contract_digest(request)
            assert root.get("generation") == normal_root.get("generation")
            assert int(root.find("counters").get("work")) == work
            assert int(root.find("counters").get("work.budget")) == budget
            assert {p.name: digest(p) for p in published.iterdir() if p.is_file()} == normal_files
            assert {path: digest(path) for path in hashes} == hashes
        generation = None
        cache = {}
        cuts = budget_cuts(work)
        for budget in cuts:
            attempt = area / ("cut-" + str(budget))
            # Generations depend on payload, not counters. Reusing this directory
            # avoids a full corelib copy for every cut while saving each receipt.
            result, request, publication = budget_attempt(companion, original, refs, mode, attempt, budget,
                                                          publication=area / "copies")
            receipt = validate_budget_copy(result, request, publication, original, refs, mode, hashes, cache=cache)
            published = publication / receipt["generation"]
            assert (published / "preservation.xml").read_bytes() == preservation
            assert generation is None or generation == receipt["generation"]
            generation = receipt["generation"]
            (attempt / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
        # Also pin the standalone public option and fresh-output determinism.
        repeated = []
        for label in ("standalone-first", "standalone-repeat"):
            attempt = area / label
            result, request, publication = budget_attempt(companion, original, refs, mode, attempt, work - 1, protocol=False)
            repeated.append(validate_budget_copy(result, request, publication, original, refs, mode, hashes, probe, attempt))
        assert repeated[0] == repeated[1]
        assert repeated[0]["generation"] == generation
        receipts.append({"mode": mode, "first_budget": cuts[0], "last_budget": cuts[-1],
                         "budgets_checked": list(cuts), "success_budgets_checked": [work, work + 1],
                         "every_integer_budget_checked": FULL, "copy_generation": generation})
        print("graph-selected-budget=" + mode + (":dense-cuts=1.." + str(work - 1) if FULL else ":sampled-cuts=" + ",".join(map(str, cuts))) + ":success-cuts=" + str(work) + "," + str(work + 1) + ":copy-identical,contract,generation,closure,repeat", flush=True)
    (directory / "success.json").write_text(json.dumps(receipts, indent=2) + "\n")
    full_witness("graph-validation=selected-row-budget-interruption,passive-row-budget-interruption,exhaustive-public-cuts,byte-identical-copy-fallback,normal-validator-controls", flush=True)
