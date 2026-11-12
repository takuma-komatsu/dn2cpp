"""Collector leaves stay behind managed GC policy and reference barriers."""
from pathlib import Path
import re
import sys

output = Path(sys.argv[1])
text = "\n".join(path.read_text() for path in sorted(output.glob("generated*.cpp")))
for suffix in ("Internal", "Batch"):
    pattern = r"\b(Program_Move" + suffix + r"_m\d+)\([^;\n]*\)\s*\{(.*?)^\}"
    bodies = re.findall(pattern, text, re.MULTILINE | re.DOTALL)
    if len(bodies) != 1 or "dn2cpp_gc_memmove_refs(" not in bodies[0][1]:
        raise AssertionError(f"Buffer {suffix} route did not use the collector-aware move")
    if "std::memmove(" in bodies[0][1]:
        raise AssertionError(f"Buffer {suffix} route bypassed the reference barrier")
print("buffer collector-aware bodies verified")

queries = re.findall(r"\b(GC_GetTotalMemory_m\d+)\(([^;\n]*)\)\s*\{(.*?)^\}",
                     text, re.MULTILINE | re.DOTALL)
private = [body for _, args, body in queries if not args.strip()]
public = [body for _, args, body in queries if args == "int32_t a0"]
if len(private) != 1 or "dn2cpp_gc_get_total_memory(0)" not in private[0]:
    raise AssertionError("private memory query lost its collector accounting leaf")
if len(public) != 1:
    raise AssertionError("expected one managed public memory-query body")
body = public[0]
labels = {match.group(1): match.start() for match in re.finditer(r"^(IL_[0-9A-F]+):", body, re.MULTILINE)}
backedge = any(labels.get(match.group(1), len(body)) < match.start()
               for match in re.finditer(r"goto (IL_[0-9A-F]+);", body))
if (body.count("dn2cpp_gc_get_total_memory(0)") < 2
        or "dn2cpp_gc_wait_for_pending_finalizers();" not in body
        or not re.search(r"GC_Collect_m\d+\(\);", body) or not backedge):
    raise AssertionError("public memory query lost CoreLib's collect/wait/stabilization loop")
print("gc managed memory-query policy and native leaf verified")
