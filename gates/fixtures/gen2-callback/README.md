# Retained GC notification boundary

The rewriter imports the installed CoreLib's private Gen2GcCallback members.
Its output is a **native-only policy fixture**: never run it on the CLR.
The policy refuses the retained constructor/finalizer bodies and blocks target
allocation before finalizer registration. Existing Register calls remain no-ops;
this does not implement GC generation notifications.

The supplied native header is not a legitimate managed Gen2 object. It tests
callable-body and clone refusal without constructing or queuing such an object.
Closed-null instance delegates retain their existing binding error. Reflection
uses normal Type lookup; the isolated reflection and interpreter fixtures use
explicit link.xml roots because their Type flows through an opaque helper.
These roots do not demonstrate automatic reflection target discovery.

The interpreter patch executes the single-callback constructor through both
existing code formats. The object-argument overload remains a converter admission
refusal, not an executed interpreter case. Its direct, address and reflective
routes are tested separately. Exception text is compared by an imported AOT
helper because String.op_Equality is not an admitted interpreter import here.
An ordinary imported finalizable constructor remains executable, and deriving
from the sealed CoreLib type is rejected before patch execution.

Optional DN2CPP_GEN2_ALLOC_COUNTERS=1 uses GNU/wasm linker wrapping to count actual
target allocations and registrations separately from exception allocations.
Positive ordinary allocations/registrations validate the instrumentation.
Cross-platform runs without that option still validate decoded cold metadata,
the actual retained finalizer slot, and callable/clone refusal.

NoAlloc admits the exact direct exception-only bodies. Indirect calli remains a
conservative dynamic-dispatch rejection; the fixture tests that boundary separately.
