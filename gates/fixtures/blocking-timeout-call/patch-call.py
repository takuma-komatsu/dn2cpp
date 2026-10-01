from pathlib import Path
import re, sys, json, hashlib
path=Path(sys.argv[1]); data=path.read_bytes()
matches=list(re.finditer(rb"\x02\x03(?:\x04)?\x6f....\x2a",data,re.DOTALL))
if len(matches)!=30: raise ValueError(f"Expected thirty optimized direct wrappers, got {len(matches)}")
patched=bytearray(data)
for m in matches: patched[m.start()+(3 if data[m.start()+2]==4 else 2)]=0x28
path.write_bytes(patched)
(path.parent/'patch-proof.json').write_text(json.dumps({'matchedWrappers':30,'beforeSha256':hashlib.sha256(data).hexdigest(),'afterSha256':hashlib.sha256(patched).hexdigest()},indent=2)+'\n')
