"""Validate exported DeClang evidence and native incremental rebuilds."""
import json
from pathlib import Path
import re
import sys

mode, directory, state_file = sys.argv[1:]
build = Path(directory)
state_path = Path(state_file)
config = json.loads((build / "declang-config.json").read_text(encoding="utf-8"))
assert config["version"] == 1 and config["targets"], "missing DeClang targets"
selected = [target for target in config["targets"] if "DeClangSelected" in target["managedMethod"]]
assert len(selected) == 1, "the selected game method must map to one implementation"
for target in config["targets"]:
    result = json.loads((build / "declang/results" / (target["cppFile"] + ".json")).read_text(encoding="utf-8"))
    assert result["cppFile"] == target["cppFile"]
    assert any(re.fullmatch(target["symbolPattern"], symbol) for symbol in result["flattenedSymbols"]), target
source = selected[0]["cppFile"]
objects = list((build / "CMakeFiles").rglob(source + ".o"))
assert len(objects) == 1, "selected generated object must exist"
runtime = sorted((build / "CMakeFiles/dn2cpp_runtime.dir").rglob("*.o"))
assert runtime, "DeClang must compile its own runtime objects"
state = {
    "seed": selected[0]["seed"],
    "objectTime": objects[0].stat().st_mtime_ns,
    "runtime": {str(path): path.stat().st_mtime_ns for path in runtime},
}
if mode == "snapshot":
    state_path.write_text(json.dumps(state), encoding="utf-8")
elif mode == "delete-result":
    state_path.write_text(json.dumps(state), encoding="utf-8")
    (build / "declang/results" / (source + ".json")).unlink()
elif mode in ("check-seed", "check-rebuild"):
    previous = json.loads(state_path.read_text(encoding="utf-8"))
    assert state["runtime"] == previous["runtime"], "DeClang rebuilt unchanged runtime objects"
    assert state["objectTime"] > previous["objectTime"], "selected object was not recompiled"
    if mode == "check-seed":
        assert state["seed"] != previous["seed"], "changing the preset seed did not change native configuration"
    else:
        assert state["seed"] == previous["seed"], "result deletion changed the function seed"
else:
    raise AssertionError("unknown check mode")
