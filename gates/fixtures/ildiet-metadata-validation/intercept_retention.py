"""Retained member-specific cuts keep callable bodies without scanning replaced IL."""
from pathlib import Path
import json
import os
import re
import subprocess


def validate_intercept_retention(probe, cli, original, directory):
    def run(arguments):
        result = subprocess.run(["dotnet", "exec", *map(str, arguments)], capture_output=True, text=True,
                                env={**os.environ, "DN2CPP_STRICT_COMPLETION": "1"})
        assert result.returncode == 0, (arguments, result.returncode, result.stdout, result.stderr)
        return result

    records = []
    for kind in ("retained", "called"):
        source = directory / ("intercept-" + kind)
        result = run([probe, "--create-stripping-intercept-fixture", original, source, kind])
        assert "stripping-fixture=intercept:" + kind in result.stdout
        for level in ("high", "medium"):
            for mode in ("raw", "full"):
                output = directory / ("emission-intercept-" + kind + "-" + level + "-" + mode)
                arguments = [cli, source / "MediumApp.dll", "-r", source / "System.Private.CoreLib.dll",
                             "-r", source / "MediumLib.dll", "--stripping-level", level,
                             "--no-metadata-compression", "--no-shared-generics", "-o", output]
                if mode == "raw":
                    arguments.append("--no-ildiet")
                result = run(arguments)
                text = "\n".join(p.read_text() for p in [output / "generated.h", *output.glob("generated*.cpp")])
                assert "RawBodyOnly" not in text, "Retention scanned the replaced body"
                for member in ("BlockCopy", "ByteLength", "GetByte", "SetByte"):
                    body = re.search(r"\bBuffer_" + member + r"_\w+\([^;]*?\)\s*\{", text)
                    assert bool(body) == (level == "medium" or kind == "called"), (kind, level, member)
                    if level == "medium":
                        assert re.search(r"\(void\*\)&Buffer_" + member + r"_\w+, \(void\*\)&inv_", text), \
                            "Retained cut member lacks a reflection invoker: " + member
                records.append({"kind": kind, "level": level, "mode": mode,
                                "command": list(map(str, arguments)), "exit": result.returncode})
    (directory / "intercept-results.json").write_text(json.dumps(records, indent=2) + "\n")
    print("stripping-intercepts=retained-callable-members,cut-body-absent,direct-and-address", flush=True)
