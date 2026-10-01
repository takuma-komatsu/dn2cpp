from pathlib import Path
import re
import sys

path = Path(sys.argv[1])
data = bytearray(path.read_bytes())
# The optimized wrappers contain only argument loads, callvirt and ret.
for loads, expected in [(b"\x02\x03\x04", 3), (b"\x02\x03", 3)]:
    matches = list(re.finditer(re.escape(loads) + rb"\x6f....\x2a", data, re.DOTALL))
    if len(matches) != expected:
        raise ValueError("Threading direct-call wrappers changed shape")
    for match in matches:
        data[match.start() + len(loads)] = 0x28
path.write_bytes(data)
