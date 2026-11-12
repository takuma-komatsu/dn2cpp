"""Descriptor-only delegate roots keep callable bodies and reflection invokers."""
from pathlib import Path
import re
import sys

output = Path(sys.argv[1])
text = "\n".join(path.read_text() for path in [output / "generated.h", *sorted(output.glob("generated*.cpp"))])
if "Routes_Call" in text or "Routes_Calli" in text:
    raise AssertionError("retained-only image contains a direct/address fixture root")
for field in ("f__invocationCount", "f__invocationList"):
    if field in text:
        raise AssertionError("retained-only image contains a CoreCLR-layout field walk: " + field)

members = {
    "Equals": (1, "dn2cpp_delegate_base_equal"),
    "GetHashCode": (1, "dn2cpp_delegate_base_hash"),
    "_ctor": (2, "dn2cpp_delegate_initialize_named"),
}
multicast = {
    **members,
    "Equals": (1, "dn2cpp_delegate_equal"),
    "GetHashCode": (1, "dn2cpp_delegate_hash"),
    "NewMulticastDelegate": (2, "dn2cpp_delegate_from_invocation_prefix"),
    "CombineImpl": (1, "dn2cpp_delegate_combine"),
    "RemoveImpl": (1, "dn2cpp_delegate_remove_impl"),
    "GetTarget": (1, "dn2cpp_delegate_get_target"),
    "GetMethodImpl": (1, "dn2cpp_delegate_get_method"),
    "GetInvocationList": (1, "dn2cpp_delegate_invocation_list"),
    "get_HasSingleTarget": (1, "->prev == nullptr"),
    "TryGetAt": (1, "dn2cpp_delegate_try_get_at"),
}
for name in ("CtorClosed", "CtorClosedStatic", "CtorRTClosed", "CtorOpened", "CtorVirtualDispatch",
             "CtorCollectibleClosedStatic", "CtorCollectibleOpened", "CtorCollectibleVirtualDispatch"):
    multicast[name] = (1, "dn2cpp_throw_platform_not_supported")

for owner, expected in (("Delegate", members), ("MulticastDelegate", multicast)):
    for member, (count, helper) in expected.items():
        pattern = r"\b(" + owner + "_" + member + r"_m\d+)\([^;\n]*\)\s*\{(.*?)^\}"
        bodies = re.findall(pattern, text, re.MULTILINE | re.DOTALL)
        if len(bodies) != count:
            raise AssertionError(f"{owner}.{member}: expected {count} bodies, got {len(bodies)}")
        for symbol, body in bodies:
            if helper not in body:
                raise AssertionError(f"{symbol}: missing {helper}")
            if not re.search(r"\(void\*\)&" + re.escape(symbol) + r"\b", text):
                raise AssertionError(f"{symbol}: retained member has no reflection callable address")
print("delegate retained-only bodies and reflection invokers verified")
