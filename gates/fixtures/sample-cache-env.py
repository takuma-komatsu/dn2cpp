#!/usr/bin/env python3
"""List environment inputs of a gate's sample projects."""

import glob
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET


def sample_knobs(gate, extra_projects):
    text = gate.read_text(encoding="utf-8")
    projects = set(extra_projects)
    projects.update(Path(p) for p in re.findall(r"samples/[\w./-]+\.csproj", text))
    projects.update(Path(f"samples/dotnet/{name}/{name}.csproj")
                    for name in re.findall(r"\bcorelib_(?:diff|freeze)_gate\s+(\w+)", text))
    projects.update(Path(f"samples/dotnet/{bucket}/{name}.csproj")
                    for bucket, name in re.findall(r"\bordinary_fixture_diff_gate\s+(\w+)\s+(\w+)", text))
    samples = Path("samples").resolve()
    seen = set()
    sources = set()
    while projects:
        project = projects.pop().resolve()
        if project in seen:
            continue
        seen.add(project)
        tree = ET.parse(project)
        directory = project.parent

        def expand(patterns):
            paths = set()
            for pattern in patterns.split(";"):
                if not pattern:
                    continue
                if "$" in pattern:
                    raise ValueError(f"{project}: unevaluated source path {pattern}")
                paths.update(Path(p).resolve() for p in glob.glob(str(directory / pattern), recursive=True))
            return paths

        default_items = tree.findtext(".//EnableDefaultCompileItems", "true") != "false"
        compiled = {p.resolve() for p in directory.rglob("*.cs")
                    if not any(part in ("bin", "obj") or part.startswith(".")
                               for part in p.relative_to(directory).parts)} if default_items else set()
        for item in tree.findall(".//Compile"):
            if "Include" in item.attrib:
                compiled.update(expand(item.attrib["Include"]) - expand(item.get("Exclude", "")))
            if "Remove" in item.attrib:
                compiled.difference_update(expand(item.attrib["Remove"]))
        sources.update(compiled)
        for item in tree.findall(".//ProjectReference"):
            if item.get("ReferenceOutputAssembly", "true") == "false":
                continue
            reference = (directory / item.attrib["Include"]).resolve()
            if reference.is_relative_to(samples):
                projects.add(reference)

    names = set()
    for source in sources:
        text = source.read_text(encoding="utf-8")
        # Retain strings while removing comments that could quote example reads.
        text = re.sub(r'//[^\n]*|/\*.*?\*/|"(?:\\.|[^"\\])*"',
                      lambda m: m[0] if m[0].startswith('"') else "", text, flags=re.S)
        names.update(re.findall(r'\bEnvironment\s*\.\s*GetEnvironmentVariable\s*\(\s*"(DN2CPP_[A-Z0-9_]+)"', text))
    return sorted(names)


if __name__ == "__main__":
    print("\n".join(sample_knobs(Path(sys.argv[1]), {Path(p) for p in sys.argv[2:]})))
