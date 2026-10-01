from pathlib import Path
import hashlib
import json
import re
import sys

path = Path(sys.argv[1])
data = path.read_bytes()
proof_path = path.parent / "task-call-patch-proof.json"
patterns = [
    rb"\x02\x03\x6f...\x0a\x2a",
    rb"\x02\x6f...\x0a\x2a",
    rb"\x02\x03\x6f...\x2b\x2a",
]
positions = []
for pattern in patterns:
    for match in re.finditer(pattern, data, re.DOTALL):
        positions.append(match.start() + (2 if match.group()[1] == 3 else 1))
positions = sorted(set(positions))
current_sha = hashlib.sha256(data).hexdigest()
if not positions:
    if not proof_path.exists():
        raise ValueError("No unpatched wrappers and no previous patch proof")
    proof = json.loads(proof_path.read_text())
    if proof["afterSha256"] != current_sha or proof["matchedWrappers"] != 7:
        raise ValueError("The existing assembly does not match its patch proof")
    print("Task direct wrappers already patched: " + current_sha)
    sys.exit(0)
if len(positions) != 7:
    raise ValueError("Expected seven optimized direct wrappers, got " + str(len(positions)))
patched = bytearray(data)
for position in positions:
    if patched[position] != 0x6f:
        raise ValueError("Expected callvirt opcode")
    patched[position] = 0x28
path.write_bytes(patched)
proof_path.write_text(json.dumps({
    "matchedWrappers": 7,
    "positions": positions,
    "beforeSha256": current_sha,
    "afterSha256": hashlib.sha256(patched).hexdigest(),
}, indent=2) + "\n")
print("Patched seven Task direct wrappers")
