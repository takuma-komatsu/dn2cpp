from pathlib import Path
import re
import sys

path = Path(sys.argv[1])
data = path.read_bytes()
# DirectEquals keeps only its argument loads, callvirt and ret.
pattern = rb"\x02\x03\x04\x6f....\x2a"
matches = list(re.finditer(pattern, data, re.DOTALL))
if len(matches) != 1:
    raise ValueError("DirectEquals IL body is not unique")
offset = matches[0].start() + 3
path.write_bytes(data[:offset] + b"\x28" + data[offset + 1:])
