"""Retained member-specific cuts keep callable bodies without scanning replaced IL."""
from pathlib import Path
import json
import os
import re
import subprocess


def validate_gc_generations(probe, cli, original, directory):
    def run(arguments):
        result = subprocess.run(["dotnet", "exec", *map(str, arguments)], capture_output=True, text=True,
                                env={**os.environ, "DN2CPP_STRICT_COMPLETION": "1"})
        assert result.returncode == 0, (arguments, result.returncode, result.stdout, result.stderr)
        return result

    records = []
    for kind in ("retained", "called"):
        source = directory / ("gc-generations-" + kind)
        result = run([probe, "--create-stripping-gc-generations-fixture", original, source, kind])
        assert "stripping-fixture=gc-generations:" + kind in result.stdout
        for level in ("high", "medium"):
            for mode in ("raw", "full"):
                output = directory / ("emission-gc-generations-" + kind + "-" + level + "-" + mode)
                arguments = [cli, source / "MediumApp.dll", "-r", source / "System.Private.CoreLib.dll",
                             "-r", source / "MediumLib.dll", "--stripping-level", level,
                             "--no-metadata-compression", "--no-shared-generics", "-o", output]
                if mode == "raw":
                    arguments.append("--no-ildiet")
                result = run(arguments)
                text = "\n".join(p.read_text() for p in [output / "generated.h", *output.glob("generated*.cpp")])
                assert "RawBodyOnly" not in text, "Retention scanned the replaced body"
                for member in ("GetGenerationInternal", "GetMaxGeneration"):
                    bodies = re.findall(r"\bGC_" + member + r"_\w+\([^;]*?\)\s*\{([^}]+)\}", text)
                    assert len(bodies) == (1 if level == "medium" or kind == "called" else 0)
                    assert all(re.search(r"\b(\w+) = 0;\s*return \1;", body) for body in bodies), bodies
                    if level == "medium":
                        assert re.search(r"\(void\*\)&GC_" + member + r"_\w+, \(void\*\)&inv_", text)
                records.append({"kind": kind, "level": level, "mode": mode,
                                "command": list(map(str, arguments)), "exit": result.returncode})
    (directory / "gc-generations-results.json").write_text(json.dumps(records, indent=2) + "\n")
    print("stripping-gc-generations=private-internalcall,retained,direct,address,single-logical-generation", flush=True)
