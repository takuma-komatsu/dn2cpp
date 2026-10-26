"""Private Buffer move declarations preserve their supported copy contracts."""
from pathlib import Path
import json
import os
import re
import subprocess

from ildiet_manifest import manifest


def validate_buffer_moves(probe, cli, original, directory):
    records = []

    def run(arguments, success=True):
        result = subprocess.run(["dotnet", "exec", *map(str, arguments)], capture_output=True, text=True,
                                env={**os.environ, "DN2CPP_STRICT_COMPLETION": "1"})
        records.append({"command": list(map(str, arguments)), "exit": result.returncode,
                        "stdout": result.stdout, "stderr": result.stderr})
        (directory / "buffer-move-results.json").write_text(json.dumps(records, indent=2) + "\n")
        assert (result.returncode == 0) == success, records[-1]
        return result

    oracle = run([probe, "--check-buffer-moves-clr"])
    assert oracle.stdout.strip() == "buffer-moves-clr=barrier,batch,overlap,same,zero,large"
    for kind in ("retained", "called", "bad-shape"):
        source = directory / ("buffer-moves-" + kind)
        created = run([probe, "--create-stripping-buffer-moves-fixture", original, source, kind])
        assert "stripping-fixture=buffer-moves:" + kind in created.stdout
        for level in ("high", "medium"):
            for mode in ("raw", "full"):
                output = directory / ("emission-buffer-moves-" + kind + "-" + level + "-" + mode)
                arguments = [cli, source / "MediumApp.dll", "-r", source / "System.Private.CoreLib.dll",
                             "-r", source / "MediumLib.dll", "--stripping-level", level,
                             "--no-metadata-compression", "--no-shared-generics", "-o", output]
                if mode == "raw":
                    arguments.append("--no-ildiet")
                result = run(arguments, success=kind != "bad-shape")
                if kind == "bad-shape":
                    assert "Buffer" in result.stderr and "BulkMoveWithWriteBarrierInternal" in result.stderr
                    continue
                text = "\n".join(p.read_text() for p in [output / "generated.h", *output.glob("generated*.cpp")])
                assert "RawBodyOnly" not in text, "Retention scanned a replaced move body"
                expected = level == "medium" or kind == "called"
                for name, helper in (("BulkMoveWithWriteBarrierInternal", "dn2cpp_gc_memmove_refs"),
                                     ("BulkMoveWithWriteBarrierBatch", "dn2cpp_gc_memmove_refs")):
                    bodies = re.findall(r"\bBuffer_" + name + r"_\w+\([^;]*?\)\s*\{([^}]+)\}", text)
                    assert len(bodies) == (1 if expected else 0), (kind, level, name)
                    assert all(helper in body for body in bodies), (name, bodies)
                    if level == "medium":
                        assert re.search(r"\(void\*\)&Buffer_" + name + r"_\w+, \(void\*\)&inv_", text), name
                if mode == "full":
                    root, published = manifest(output / "ildiet")
                    assert root.get("fallback") == "none"
                    assert (published / "System.Private.CoreLib.dll").read_bytes() == (source / "System.Private.CoreLib.dll").read_bytes()
    print("stripping-buffer-moves=retained,direct,address,overlap,same,zero,large,shape-rejected", flush=True)
