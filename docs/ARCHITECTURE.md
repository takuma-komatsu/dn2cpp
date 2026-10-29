# ARCHITECTURE — dn2cpp internals and extension points

A contributor-facing document showing **where to add what**. For the big picture
see `README.md`, for working conventions `AGENTS.md`, for the open backlog
[GitHub Issues](https://github.com/takuma-komatsu/dn2cpp/issues?q=is%3Aissue%20is%3Aopen).

---

## 1. Pipeline

```
input .NET assembly (+ reference assemblies via -r)
  └─ AssemblyLoadSet     … ordered reference closure and default shim resolution
  └─ ILDiet             … managed DLL stripping and preservation remapping
  │                        (--no-ildiet bypasses this stage)
  └─ Compilation        … metadata loading, type/method model construction,
  │                        generic specialization, reachability tree-shake
  └─ MethodCompiler      … IL → C++ function body for each reachable method
  └─ CppEmitter          … structs/vtables/type infos/string literals + all bodies
  │     └─ IEmitBackend  … emits the tail (main or the GDExtension registration table)
  └─ generated.h  (type layouts + external-linkage declarations)
     + generated.cpp  (data definitions + entry point)
     [ + generated_N.cpp ]  (bodies split past ~1MB; only cross-TU symbols get
       external linkage, TUs compile in parallel)
        └─ clang++ + runtime/core (+ runtime/godot) → native binary / .dylib
```

`TranspileOptions.StrippingLevel` selects the retention policy independently of
`UseILDiet`. `High` preserves the existing member-level reachability. `Medium`
adds a queue of intentionally used types in both the definition graph and the
transpiler; decoding a type or enumerating the loaded class model never adds a
queue entry on its own. There is no assembly classification for this policy.
BCL and GodotSharp follow the same rules as application and external-library
types, even when their DLL bytes are copied unchanged by ILDiet.

Medium starts at executable method owners and executable/runtime type roots. It
follows reachable body operands (including calls, construction and type tokens),
local-variable and exception-handler types, and retained type structure:
base types, interfaces, fields, property/event types, method return/parameter
types, generic arguments and constraints, attribute types and their type-valued
arguments. Each admitted type keeps its declared public and non-public fields,
constructors, methods and property/event accessors and their reflection metadata.
Retained bodies and member structure add further demands until the queue drains.
A nested type's declaring owner and custom-modifier marker types keep their
metadata identity without acquiring type-wide authority. The wrapped signature
type still participates. Existing intrinsic and explicit-cut boundaries govern
body analysis.
Member-specific cuts retain callable wrappers from the same lowering used by
address-taken calls; retention never restores their replaced IL bodies.

Closed generic types carry their known arguments through this closure. Open
definitions are traversed symbolically; they do not imply every possible type or
method specialization. A recursively expanding closed-generic demand fails
through the existing expansion guard rather than silently narrowing retention.
Reflection metadata discovery alone does not promote unrelated types to the
Medium queue. `--reflection-root` remains a metadata-only request under both
levels; it needs an independent static or executable preservation edge to admit
bodies. Runtime-only targets still require explicit preservation.

The transpiler scans Medium-only bodies and their initializer dependencies as
retention requests. They do not enroll static initializers in the startup pass;
ordinary executable uses retain their normal initialization triggers. `--no-ildiet`
bypasses rewriting only. The request contract, its digest and replayed CLI
arguments carry the stripping level, and gate-cache contexts include the flag.

ILDiet runs in a companion process over the original metadata, before
`Compilation` exists. Its definition graph distinguishes metadata, runtime,
member and explicit-root type states from metadata and body method states.
Signatures and original bodies decode once per definition. A metadata-to-body
promotion applies only body dependencies and executable state-machine attribute
arguments; every IL operand participates, including dead branches.
Generic-parameter demands flow through bindings without
constructing closed caller-depth contexts. Binding identities preserve declaring
scopes through nested generic and array shapes. Explicit MethodImpl edges connect
slot and implementation parameters independently of method names. Runtime and
boxing demands follow those bindings as well. Structural reflection defers
finite projections over symbolic types until existing or later bindings supply
targets. Generic-argument vectors preserve positions and repeated arguments;
interface enumeration follows the base and inherited-interface closure.
Symbolic assembly enumeration stays within each bound owner's module. Deferred
nested and interface lookups conservatively cover the owner's original nested
or inherited-interface definitions without narrowing by the requested name.

The shared `ReflectionApis` and `ReflectionSummary` select each site's metadata
rows and bodies. Unknown type selection keeps all stripped types' metadata and
member rows. Unknown member selection keeps rows on all metadata-reached owners,
including owners discovered later through signatures. Neither scope adds bodies
or runtime state. ILDiet's retained bodies must contain every body `Compilation`
admits. A metadata-only method becomes a marked throwing stub; admitting it is
an `InvalidOperationException` naming the method. Property rows retain their
independent accessor selection, including an intentionally accessorless row;
replayed preservation cannot invent an executable accessor. Explicit static
initializer selections, including `TypeInitializer` and constructor or member
queries, retain the owner's original initializer body rather than a stub.
Ordinary member-row retention leaves unused initializers removed.

Reflection-produced boxes from fields, invocation results and by-reference
arguments normalize nullable and by-reference payloads before requesting runtime
dispatch. Summary object types also unwrap nullable boxes before member lookup,
including generic payloads forwarded through symbolic bindings. Non-generic
array searches request dispatch for recorded element types.
Runtime array factories combined with those searches, or runtime-handle boxing,
also cover metadata-retained value layouts. The conservative closure promotes
value types without widening reference-script registration.

Analysis is monotone and never mutates Cecil IL. After closure, a frozen plan
validates retention and registry decisions before applying changes. Writes go to
an immutable generation; atomic replacement of `ildiet-result.xml` publishes it.
The versioned protocol carries ordered assembly paths, modes, content hashes,
contract digest, fallback and deterministic counters. The driver validates
identities, order, hashes and publication state before loading only that output
set. Reference discovery cannot reintroduce original DLLs afterward.

ILDiet shares `PreservationReader` with the transpiler. Its effective descriptor
exports original attribute and linker rules as signatures; backend and reflection
roots remain contract entries rather than synthetic preservation. Cut selectors
requiring generic specialization retain original bytes and defer validation to
`Compilation`. A deterministic graph-budget exhaustion copies the complete
resolved set byte for byte. Invalid input and broken invariants fail instead of
publishing a partial analysis. Cecil is confined to the companion.

The backend receives original paths and project options through
`ConfigureILDiet`. Its policy declares rewritable assemblies, type and method
roots, conditional member roots, suppressed seeds, registration attributes and
constructor registries. The protocol carries these declarations without engine
names or resource parsing in the transpiler or ILDiet. Verified per-assembly
registry modes and fallback enter `TranspileOptions` and backend diagnostics;
these states never control emission decisions.

For the .NET-module backend, `GodotProjectScripts` matches `ScriptPathAttribute`
metadata to project resources before stripping. It scans all project scenes and
resources, resolves autoloads and UIDs, recognizes static GDScript C# references,
and reads uncompressed binary dependency tables without decoding payloads.
Static path literals also contribute roots through GDScript constants, including
raw and triple-quoted strings. Ordinary and triple-quoted strings share decoding
of control escapes, Unicode escapes and backslash line continuations before path
and UID resolution. Unicode decoding validates hexadecimal digits, code-point
range and surrogate pairing; raw strings leave Unicode escapes literal. Unknown
resource formats, unresolved dependencies (including UID literals), invalid
escapes or unterminated strings retain scripts associated with the affected
project with a diagnostic. Missing project information retains all user Godot
types. Ignored and generated directories cannot contribute roots.

User Godot descendants do not inherit ILDiet's default public-member or
static-constructor seeds. The backend nominates compiler-generated nested helpers
and the SDK's `MethodName`, `PropertyName` and `SignalName` helpers by their
metadata and helper-base ancestry. Handwritten nested .NET types retain the usual
seeds; keeping such a type also keeps its enclosing script. A surviving descendant
retains its own
engine-callable members. Script-registration type arrays are filtered after
reachability to runtime-used types. Metadata and reflection roots alone do not
add script registrations. The GodotSharp constructor registry defers recognized
factory edges until runtime or explicit roots establish retained wrappers. It
keeps factories without compatible retained ancestors and drains their new
dependencies to a fixpoint. A frozen plan redirects eligible entries while
preserving engine class-name keys. Unknown factory shapes remain conservative.
The backend shares its native bootstrap root declarations between ILDiet and
emission, including synchronization, exception reporting and interop layouts.
When `--trim-godot-classes` is enabled, emission-stage reachability evaluates the
actual type constructed by each factory, including factories already redirected
by ILDiet. A further redirect preserves the registration key and selects a
retained ancestor factory. Explicit reflection roots and preservation rules
retain concrete wrapper factories as well as their requested metadata. ILDiet
does not enable or suppress this pass;
hot-update keeps its independent suppression rule. Reflection trimming and shared
generics likewise depend on their own options, not the ILDiet result.

With High, `Compilation` admits bodies from explicit roots: the entry point,
preservation rules, non-framework `<Module>` initializers, `[UnmanagedCallersOnly]` methods,
a hot-update base's export contract, and the backend's `AdditionalRootMethods`,
`ExternallyAllocatedClasses` and attribute-read owners. From there a body enters
only through a static edge, the used-slot and allocated-type dispatch closure,
the bounded runtime-support closures below, or a recognized reflection site. A
public member is not a root, and any other `.cctor` enters only through
preservation or through `ReachCctor` on a use of its type. For a precise-init
(non-`beforefieldinit`) type the uses include every call trigger
`ResolveInitializerTriggerClass` sorts out of a call token (a static method, an
instance constructor, a value type's instance method), a constrained call that
runs a value type's own method, an `Object` override included, or a static
virtual implementation, a pointer to a static method, and a root or preserved
member declared on the type (`ReachRootInitializer`). Reached
non-generic `.cctor`s run in the startup pass, except one only a reflectively
bound body reaches; that one and closed generic ones run behind the first-use
`__ensure` guards that call, `ldftn` and static-field sites emit, and a shared
body that would need a placeholder owner's guard taints. Reflection
admission lives in `Compilation.ReflectionTargets.cs`: `ScanBodyForGenerics`
resolves each reached method's `ReflectionSummary` in that method's own
instantiation, and each live site selects what its known operands name, per
alternative of a joined operand. A resolved value keeps its alternatives
distinct, so a chain of lookups grows only with the distinct members it names,
and a value or an application whose alternatives pass their bound becomes
unknown with a reason, as any operand admission cannot follow does. Each finite
argument-array alternative contributes its tuples beside an unknown remainder;
null arrays and elements contribute no tuple. An unknown binding-flags
alternative selects under every visibility, storage, `FlattenHierarchy` and
`IgnoreCase`; a method
or constructor lookup with a `Type[]` operand keeps, for each known tuple, the
overload `DefaultBinder.SelectMethod` returns, or every candidate when the
binder finds the tuple ambiguous or the model cannot rank it; a property lookup
narrows by its index types and return type to what `GetProperty` returns for
some alternative (`KeepPropertyChoices`). Those filters also constrain template
eligibility, executable selection and static fallback. `GetMember`,
`GetDefaultMembers` and `InvokeMember` read a trailing `*` in a name as a
prefix, as .NET does, and `GetMember` keeps only the member kinds its
`MemberTypes` operand names. An accessor read without `nonPublic`, such as
`GetSetMethod()` or `GetAccessors(false)`, selects only public accessors. Lookups
and enumerations keep the rows they list, generic method definitions included,
however their result is consumed, and they, attribute-data reads and type
identity select metadata and record their owner for the `--trim-reflection`
keep-set. A property or event found on a base of the reflected type associates
none of its private accessors, as `RuntimeType` does. A resolved constant
`Type.GetType` or `Assembly.GetType` name emits the type's identity and
metadata, never its bodies or initializer, and a closed generic name forms as
`MakeGenericType` does. A nested generic argument name served by a runtime
template closes to its exact identity only after its constraints are proved.
A qualified name searches its assembly by simple name
and then where that assembly forwards the name, a framework facade the load set
omits standing for CoreLib, and array modifiers of any rank apply. An
assembly-scoped `ignoreCase` lookup of an ASCII name without generic arguments
folds ASCII case; any other `ignoreCase` name, a name matching several types and
a by-ref or pointer modifier are unknown. Each known assembly receiver is a
separate search scope, with unknown remainders kept beside known targets.
Unscoped lookup keeps the ordered caller/CoreLib fallback. An array type
projects its element type's assembly. An assembly listing hands out each generic
definition as `typeof(G<>)` does, and a runtime-instantiated type's
generic arguments, definition, base type and assembly project from its template
and argument tuple. An attribute instance read reaches constructors and named
setters only for rows selected on known owners. An unknown owner with a known
filter reports an unrecognized site and adds no executable targets. Metadata
and identity retained through ordinary inventory, copied-IL closure or explicit
preservation remain available. A data read reaches no constructor or setter,
even when another route reached the attribute's constructor.
Named delegate binding and `Delegate.Method` share selection for known
receivers, including objects and runtime templates. They search the full base
chain at every visibility, bounded only by name, case and delegate `Invoke`
arity; attribute reads use the selected member identity.
Invocation, field and property access, event subscription, delegate binding,
construction and attribute instantiation reach the selected bodies, boxes and
type initializers; a constant field's access reaches no initializer, and a
construction whose flags set no lookup bit (`0xFF`) selects as
`Public | Instance`, as .NET's `Activator` does.
`RuntimeHelpers.GetUninitializedObject` allocates a known type and reaches its
initializer but no constructor, and an unknown operand reports with a
constructor remedy. Selections over generic
definitions re-apply as new instantiations appear, inside the discovery fixpoint
and each compile round. `ReflectionSummary.cs` interprets one method body over
SRM and `ILDecoder`, and `ReflectionApis.cs` classifies the API and its operand
slots; both are free of model types and linked into ILDiet, so ILDiet and
emission classify the same overloads. A `box` whose token may name a reference
type passes the object through, so its `GetType` is the token's type only when
no type derives from it, and a box of `Nullable<T>` holds a `T`. An `isinst`
retains its operand alternatives, including unknown remainders, alongside a
possible null; `castclass` preserves its operand on normal continuation. An
invocation, construction or delegate binding lets its instance escape and may
write any element of its argument array back. `Object.GetType` on a new SZ array
keeps the allocation's element type independently of stores and escapes.
`AsEnumerable` and `Cast` return their source; `Delegate.Method` over an
`ldvirtftn` target a derived type may override is unknown; and folding `int32`
operands to a result outside the `int32` range is unknown. A summary renders
each shared term once, so its canonical text grows with its distinct terms.
Implicit `FirstOrDefault`, `LastOrDefault`, `SingleOrDefault` and
`ElementAtOrDefault` retain the generic element type's default alongside possible
elements. Raw signatures prove modeled CLI zero or null defaults; other defaults
remain unknown. Explicit defaults retain their supplied operand.
Captured fields retain their raw signature's modeled zero or null alongside
finite writes, including null array references. A later store does not prove
the field was initialized before an earlier read; unmodeled defaults remain
unknown.
A fresh array's unproven element default remains
explicitly unknown; an integral or reference element load proves that default
is zero or null.
Defaults published through a captured array retain their element tokens so a
later typed load can make the same proof without changing other alternatives.
Integral array and captured-field stores truncate to their storage width;
loads apply the instruction's or field type's sign or zero extension.
Captured arrays retain their semantic element type, and sequence reads and
copies observe normalized stored values. Unproved conversions remain unknown.
An allocation site that may repeat can represent several live arrays. Stores
at such a site retain possible defaults, including those published through
fields; a write to one instance cannot initialize another. Sites proved not
to repeat keep exact values when every path writes the slot before use.

A `MakeGenericType` over a known tuple takes an existing instantiation, else the
definition's runtime template while `TemplateMayServe` finds that a clone can run
what the sites select on it, else a static instantiation when the constraints
admit the tuple, else a ClosedGeneric diagnostic. A body that runs, binds,
constructs or instantiates through an operand depending on the class type
parameters cannot serve a clone, which applies the body's sites only in the
placeholder context, so such a request takes the static instantiation, whose
exact context resolves them; a template body's sites report from the placeholder
context while planning retains the body. A clone may be built from any
placeholder level row of an eligible template, so a base definition with no
template of its own is served too. Lookups and member enumerations on a clone
select on every placeholder level, with the selected fields' types and members'
attribute rows; field type effects obey the metadata selection's visibility,
`DeclaredOnly` and hiding rules. Template metadata, eligibility, executable
selection and static fallback share singular lookup hiding rules, including
after `MakeGenericMethod` projection. Parameter listings follow the selected
method or constructor.
Its interfaces, its nested types and a known tuple's arguments list too, and
any other listing of a clone or of its members is unknown.
Member type projections retain fixed types through the selected metadata views.
`GetInterface` and `GetInterfaces` on an open definition project fixed interfaces
without forming an executable owner; interfaces depending on the definition's
type parameters remain unknown. Each owner, name and `ignoreCase` alternative
is a separate interface lookup, and a definitely ambiguous lookup selects no
interface. An undecidable match remains unknown.
`MakeGenericMethod` on an open declaring type retains detached method metadata
for attribute and signature reads without forming an executable owner or body.
Any projected signature containing a detached generic view remains unknown to
executable admission.
Finite known argument alternatives keep their ordinary selection even when another
alternative is unknown. Unknown arguments add no executable candidates from
existing instantiations or runtime-template members; bodyless template identity
planning remains available. Independently reached or explicitly preserved bodies
remain usable. A receiver the summary cannot name as a definition, or one only
the runtime models, reports ClosedGeneric at the site.
Every eligible template level enters the emitter's identity and layout closure,
including a metadata-only library level used as a clone source. Registering that
metadata, including types named only by member signatures, does not allocate a
receiver or reach its used virtual slots. A selected or preserved instance
constructor admits that level's allocation and dispatch.
Forming a type admits no body: a template's constructors are reached only for a
construction a site selects, and its other members only for a selected
invocation or binding, on every placeholder level of the definition, a virtual
row reaching each deriving template's override (`ReachAskedTemplateMembers`). A
preservation rule on a generic definition's member joins the same reach, one
shared body per level and no closed instantiation, in a library definition as
in an application one; a bodiless template row a clone could run sets its
method row's `preservable` field, so its refusal names that rule, and an
override row a clone cannot run refuses with the closed-generic remedy.

A live site with an unknown operand selects only what is known, an unknown flags
value standing for the flags superset above, and records one diagnostic, as does
a generic instantiation that no template serves and whose constraints cannot be
decided ahead of time; the driver prints each with its remedy and writes it to
`reflection-unrecognized.tsv`. A site several instantiation contexts report is
one row merged from what each asks, with the least kind, the reasons in ordinal
order and a fix that preserves every owner, and rows sort on every printed
column, so neither reach order nor the first context decides the report. A
diagnostic describes the site, so a preservation
rule that covers its target leaves it in place. `ReflectionFixText` names the
member when the lookup names it exactly and the whole type only when the member
is unknown, XML-escaping every name it splices; a closed-generic remedy names
the instantiation statically, since a definition's preservation rule never mints
a closed instantiation, and one over a definition the runtime models as one type
for every instantiation constructs the closed type directly instead. The
reflection capability bits switch on emission machinery (invoker thunks,
constructor signature rows, delegate-invoke rows, late-bound dispatchers and
rgctx prologues) and never admit a member body by themselves.

Core invariants:

- **Explicit vtables** (no C++ `virtual`): dispatch goes through the
  `Dn2CppTypeInfo` at the head of every object.
- **The evaluation stack is single-assignment temporaries**, spilled to per-depth
  normalized variables at basic-block boundaries so control-flow joins agree.
  Promoted narrow-enum arguments normalize to their CLI storage width on real-IL
  body entry, and local/argument stores use the same narrowing rule; the native
  ABI remains unchanged.
- **Generation is deterministic**: same input, same output. Nothing in the
  environment may change the C++ a successful transpile emits — the self-host
  fixpoint depends on it.
- **Non-generic array equality and runtime-handle boxing retain conservative
  type sets**, without following call contexts or aliases. Array elements,
  boxed allocations and type tokens contribute candidates; runtime type factories
  also include closed value types discovered by emitted layout and metadata
  closure, while unemitted definitions remain deferred.
  This can retain bodies for types a search never selects. An unsupported value
  equality keeps its slot empty, and the runtime array-search guard reports
  `NotSupportedException` if that equality is actually needed.

## 2. Modules and the core⇄target contract

| C# project | Role |
|------------|------|
| `src/Dn2Cpp.Transpiler` | Pure-.NET core: `Compilation` / `MethodCompiler` / `CppEmitter` / `Model` / `ILDecoder` / `SignatureProvider` / `CppTypes`. Ships `ConsoleBackend` |
| `src/Dn2Cpp.Godot` | GDExtension backend: `GodotBackend : IEmitBackend`, `GodotCallIntrinsics : ICallIntrinsics` |
| `src/Dn2Cpp.DotnetModule` | Godot mono-module backend: transpiles a real Godot.NET.Sdk game against the **real** `GodotSharp.dll` and emits the `godotsharp_game_main_init` export the engine loads as a NativeAOT-style drop-in |
| `src/Dn2Cpp.Cli` | Composition root. `--gdextension` → `GodotBackend`, `--dotnet-module` → `DotnetModuleBackend`, else `ConsoleBackend` |
| `src/GodotSharpShim` | The "fake GodotSharp": a **reference assembly that gets transpiled**, generated from `extension_api.json` by `BindingGenerator`, API-compatible with Godot.NET.Sdk. (`System.Linq` needs no stand-in; the real BCL assembly transpiles as-is) |

The core knows nothing about Godot: no `Godot.*` name appears in
`src/Dn2Cpp.Transpiler`. Target-specific behavior enters through **two hooks**.

### `IEmitBackend` (`src/Dn2Cpp.Transpiler/IEmitBackend.cs`)

Four required members; the rest default to "none", so a new backend overrides
only what it needs.

- `RuntimeHeader` — the header generated TUs include.
- `CallIntrinsics` — the `ICallIntrinsics` instance, or null.
- `EmitEpilogue(CppEmitter, StringBuilder, IReadOnlyList<MethodInfo> cctors)` —
  the tail (`main`, the registration tables, the mono-module entry export);
  `cctors` are the static constructors to run at startup.
- `ShouldSkipMethodBody(ClassInfo, MethodInfo)` — emit no body, for an engine
  shim whose calls the intrinsics replace inline.

Optional, with the invariant each exists for:

- `ConfigureILDiet` — declares managed stripping and external entry-point policy
  before model construction. GDExtension retains script descendants and public
  application exports; the .NET-module backend uses project resource roots and
  conditional script members while allowing GodotSharp wrapper removal.

- `WantsSyntheticBody` — hot-update base only: give a skipped-body method a real,
  invoker-compatible body rendered from the intrinsic's own call lowering.
- `HasPlaceholderBody` — body emitted but dead (every call site lowered inline);
  a hot-update base excludes such methods from the reflection fnPtr/invoker
  wiring, so a patch import fails loudly instead of binding the placeholder.
- `ExternallyAllocatedClasses` — types the target instantiates outside the C#
  call graph (Godot's ClassDB create-instance path runs no `newobj`), so their
  dispatch and ToString/Equals/Finalize wiring is not silently unwired.
- `AdditionalRootMethods` — methods the epilogue calls directly or the target
  enters by its own contract (GDExtension exports, script constructors), seeded
  as reachability roots; nothing else in an application is a root by being
  public. Called once, before the first reachability drain and so before
  `EmitEpilogue`: a backend may resolve and cache what its epilogue names, and
  the epilogue must not export a body this set omits.
- `AttributeReadClasses` / `AttributeReadAssemblies` — owners whose attribute
  rows the target's runtime reads in a registration walk no site names; their
  rows are constructed as a recognized attribute read would construct them.
- `AllocatedTypeRoots` — methods and attribute owners the target's runtime reads
  when an allocated receiver enters its bridge. An allocation cursor drains these
  demands after the initial seeds. The .NET-module backend uses this for
  code-created Godot objects and their user base classes, including nested types
  outside assembly script registration; unallocated siblings gain no bodies.
  Its bridge roots include the generated static-method dispatcher on each user
  level, which `ScriptManagerBridge.CallStatic` binds through `CreateDelegate`.
- `CoversReflectionSite` — true when the backend's contract already retains what
  a reflection site in that caller can reach. It only reports an unrecognized
  site there as backend-covered; it changes no admission.
- `AdditionalBoundedMethods` — merged with the core BCL set by
  `Compilation.IsBoundedMethod`: subtree cut, call/`ldftn`/`ldvirtftn` sites
  neutralized. The address-of stub's shape is its **consumer's**, never the
  callee's parameter list — a delegate calls it `(target, <Invoke's params>)`, a
  vtable/interface slot `(receiver, <params>)`. Wrong is invisible on a flat
  native ABI and traps on wasm, where `call_indirect` carries a type immediate.
  See `MethodCompiler.FtnStubShape`.
- `CalliAbiType` — overrides a `calli` return's C++ ABI type when a target's
  native declaration is narrower than its managed signature. The default keeps
  the declared type; DotnetModule uses it for GodotSharp's core-enum returns.

### `ICallIntrinsics` (`src/Dn2Cpp.Transpiler/ICallIntrinsics.cs`)

- `TryEmitCall(MethodCompiler, MethodInfo callee, bool isCallvirt)` — expand a
  call directly to C++. True means handled (arguments consumed, result pushed);
  false falls through to a normal managed call.
- `TryEmitNewobj(MethodCompiler, MethodInfo ctor)` — the same for `newobj`.

`TryEmitCall` gets **first refusal**, ahead of every core lowering including the
bounded-method neutralization, so the hooks compose: a backend can cut a body
with `AdditionalBoundedMethods` and still lower its call sites to something real.

> **Changing these two interfaces affects all three lanes.** Say so explicitly in
> the commit message.

The `MethodCompiler` API an intrinsic may use: `Pop` / `Push` / `Emit` /
`NewLocal` / `NewLocalArray` / `NoteReferencedType` / `NoteArrayElementType` /
`static Cast` / `static Signature`, plus the shared GCHandle lowering
(`EmitGCHandleAlloc` / `GCHVal`) so a backend forwarding its own handle wrapper
cannot drift from the core's `GCHandle` intrinsic. The backend-facing
`CppEmitter` surface is `EntryPoint`, `Classes`, `IsAppModuleClass`, `IsEmitted`,
`TypeInfoRef`, `SigProvider`, `ArrayElementTypes`, `HotUpdateBase`,
`EmitInitCalls` — checkable with `grep -o 'emitter\.[A-Za-z]*'` over the two
backend projects and `ConsoleBackend.cs`; run it when you widen the surface.

## 3. Key data structures (`src/Dn2Cpp.Transpiler/Model.cs`)

- `TypeDesc` — a type (Primitive / Class / External / SZArray / MDArray / ByRef /
  Pointer / GenericVar / Template). `Substitute(GenericContext)` substitutes type
  arguments, including function-signature children. Function pointers retain their
  convention, return and parameter types for structural generic identity while
  layout, native spelling and hot-update method ABI remain one unmanaged pointer.
- `ClassInfo` — a type definition: `Module`, `BaseClass`, `Interfaces`, `Fields`,
  `Methods`, `Vtable`, `GenericArity` / `Context` / `Completed`.
- `MethodInfo` — a method: `Module`, `Signature`, `Context`, `VtableSlot`, `Rva`.
  `MethodInfo.Emittable` is the single funnel from a method to the C++ symbol
  representing it.
- `Module` — one assembly, owning its `Reader` and handle maps (handles are
  unique only within a reader).

Members, signatures and field types are decoded **on demand**; read
*Transpiler invariants* in `AGENTS.md` before writing any walk over
`Compilation.Classes` that reads them.

## 4. Extension recipes (common changes)

### A. Adding an IL opcode — core lane

1. `ILDecoder` — register the opcode's operand kind (None/Token/Branch/…).
2. `MethodCompiler.Translate` — add a `case ILOpCode.Xxx:`, lower with
   `Pop`/`Push`/`Emit`.
3. If a runtime helper is needed, add a `dn2cpp_*` function under `runtime/core`
   and declare it in `runtime/core/dn2cpp.h`.
4. Add a minimal case to `samples/dotnet/HelloWorld`; green
   `./gates/build-and-run-sample.sh`.

The retained internal `Activator.CreateInstance(Type, bool, bool)` factory shares
parameterless constructor selection with the public native factory. Its booleans
remain `nonPublic` and `wrapExceptions`; constructor discovery does not depend on
the wrapping choice. Null Type arguments keep their named argument exception;
custom Type objects are refused before interpreting the native Type payload.
Default value creation uses normal Nullable boxing, while raw internal Nullable
reboxing retains its separate object representation.

The retained `RuntimeType.CreateInstanceDefaultCtor(bool, bool)` adapter checks
the instance for null before using that same factory with inverted `publicOnly`.
It never reads a custom Type payload or enters CoreCLR activation-cache machinery.
Receiver-based planning selects only parameterless constructors; an unknown
visibility flag conservatively retains both accessibilities, without changing
the runtime visibility check or rooting other constructor arities.
The nongeneric `RuntimeType.CreateInstanceOfT()` body uses the same instance
adapter with public-only selection and constructor exception wrapping fixed on.
Its receiver-based planning retains only public parameterless constructors;
value/Nullable boxed return policies remain separate from the public generic
caller's struct-construction path.
The retained `RuntimeType.CallDefaultStructConstructor(ref byte)` instead invokes
a public parameterless value constructor on caller-owned storage, without boxing,
zeroing or copying. An implicit constructor leaves that storage unchanged, including
ref-like values. Private default-constructor metadata survives stripping so absence
is never confused with inaccessibility. Receiver planning reaches execution and
initialization, not a box; unresolved value-type templates retain existing refusal.
The caller must provide sufficient storage. Null storage for an explicit constructor
is refused before dispatch as a native safety boundary; implicit no-op initialization
does not inspect it. This dynamic constructor dispatch is not an allocation primitive
and remains an opaque-dispatch boundary for NoAlloc.

The managed `RuntimeTypeHandle.GetActivationInfo` bridge requires CoreCLR-owned
allocator and constructor function-pointer ABIs. Its exact raw nested signature
routes to a catchable `PlatformNotSupportedException`, without checking arguments
or writing output references. The separate raw QCall overload keeps native import
binding authority. ActivatorCache and generic cache bodies remain real IL; this
boundary does not provide a working activation cache or fabricated VM pointers.
Supported public construction uses the native adapters above. Reflection's existing
byref-pointer invocation restrictions and exception-only NoAlloc policy remain.

The managed `RuntimeTypeHandle.ConstructName(TypeNameFormatFlags)` adapter first
validates the handle, then composes existing Name/ToString/FullName/AssemblyQualifiedName
helpers for modes 0/1/3/7. Unsupported flags and a null FullName/AQN result are explicit
native capability refusals, not claims about private CLR flag behavior. Public getters
keep their separate open/formal/function-pointer null contract. Existing metadata and
name-formatting limits remain; compound open generic definitions are not newly promised
for modes 3/7. The raw QCall overload retains its import policy. String construction
remains allocating for NoAlloc; boxed handles use the existing unbox/invoker ABI.


The retained `RuntimeTypeHandle.GetDeclaringType(RuntimeType)` bridge requires a
canonical Type object before reading its payload. Recorded formal parameters return
their typical declaring type; outer arrays and signature types have no enclosing type.
Ordinary nested types explicitly refuse because general enclosing-owner identity is
not represented. The hot nested flag, not absence of a cold nested table, distinguishes
this boundary from top-level null. Missing formal provenance also refuses. The public
MemberInfo declaring-type helper is unchanged; this is not nested-type parity. Returning
an interned Type follows the existing NoAlloc policy for cached reflection handles,
which does not promise zero cold initialization allocation.

The exact managed `RuntimeTypeHandle.GetIntroducedMethods(RuntimeType)` factory keeps
its real IL and nested value-type return. Its real constructor still enters the existing
GetFirstIntroducedMethod unsupported-VM boundary before any iterator is returned;
GetNextIntroducedMethod retains the same separate refusal. The copied-body traversal
preserves constructor and field dependencies. No null guard or empty iterator is
substituted for the factory, and public reflection rows are not CLR MethodDesc handles.
This enables transpilation through the factory, not private VM enumeration at runtime.

The retained RuntimeTypeCache generic-definition local helper reads its actual managed
owner field and requires a canonical Type header before payload access. Definitions
return the same owner; constructed types use the existing native definition identity.
Only successful lookup updates the real cache field, using the ordinary managed
reference-store barrier. The outer getter retains its existing-value shortcut, while
a direct local-helper call recomputes and overwrites even a populated cache. Null cache
and owner throw before writes; unsupported custom owners refuse. Invalid nongeneric
owners follow the native helper's safety policy, not claimed private CLR VM parity.
This does not implement CoreCLR cache construction or admit raw QCall handles. Returning
interned identities follows the existing NoAlloc policy, not a zero-cold-allocation claim.

The retained `CustomAttributeData.GetCustomAttributes(MemberInfo)` wrapper accepts
canonical native Type, method, constructor, field, property and event handles. Null
throws ArgumentNullException for `target`; custom MemberInfo headers explicitly refuse
before payload access rather than silently losing a virtual override. Existing row-data
materialization supplies AttributeType without running attribute constructors. A managed
ReadOnlyCollection wraps nonempty rows so IList mutation remains unsupported; empty
results retain the array representation and its index-bound checks. Encoded argument views and sibling overloads are not expanded. The new
wrapper does not change existing instance helpers or custom virtual dispatch policy.

The retained CustomAttributeData.Constructor base body remains ordinary managed IL:
nonvirtual calls and addresses return null without reading the receiver. Virtual calls
and virtual addresses use a bounded slot selector: ldvirtftn fixes the selected
implementation at acquisition, not on a later calli receiver. Canonical row handles
refuse with PlatformNotSupportedException because exact constructor identity is not
stored; their selected address is an unconditional refusal thunk. Managed subclasses
retain their actual registered virtual slot. Unknown or stripped
implementations refuse before payload access. Reflection invokes the same virtual
policy, while permitted closed-null delegate bindings enter the actual base body.
A virtual delegate on a canonical row refuses Delegate.Method rather than report the
base method as a fabricated runtime override identity. Taking the exact base getter
address also retains this virtual slot on allocated subclasses, because the resulting
MethodInfo can be invoked virtually; a direct base call alone does not add that demand.
This does not infer constructors from attribute types or factories, extend metadata
layout, or parse attribute blobs. NoAlloc accepts the base body but conservatively
refuses the virtual helper, which can call arbitrary managed overrides.

### B. A BCL intrinsic (a `System.*` call) — core lane

1. Add a case to the `switch (declType, name)` in
   `MethodCompiler.TranslateIntrinsic`.
2. Implement any `dn2cpp_*` helper in `runtime/core`.
3. For value-type receivers see `DerefReceiver` (dereferences the `ldloca`
   pointer).

**When does a method earn an intrinsic?** The default is to transpile the real
BCL IL — Regex/Guid/Crypto coverage landed by removing transpile blockers, not by
adding intrinsics. Add one only when:

1. the real body is untranspilable — InternalCall / QCall / unmodeled runtime
   state (object headers, string interning, reflection caches);
2. it is a hot path where a C++ helper clearly beats transpiled IL
   (`System.Math` → `<cmath>`, primitive `ToString`, the main `String` API);
3. the runtime itself calls it (trap helpers, exception plumbing).

**Sibling-overload rule.** When intrinsifying one overload of a family
(`IndexOf`/`LastIndexOf`/`StartsWith`/`EndsWith`, the `StringComparison`-taking
variants), cover the siblings in the same change — a lone overload leaves its
siblings falling through to real IL, or to a loud `NotSupportedException`, with
divergent behavior.

#### Intercepts: the sibling rule made structural

An intercept has two askers — the emit route (`MethodCompiler.TranslateCall`) and
the reachability cut (`Compilation.ResolveCallTarget`) — which must agree on
*which overloads* are lowered. The invariant is **`cut ⟹ route`**: if
reachability deleted the edge to a body, no emitted body may name that body's
symbol. Cutting by NAME while the lowering table matches the SIGNATURE puts every
unmodeled overload in the gap; the failure is a green transpile and a C++ link
that dies on a mangled name.

So add **one descriptor row** in
`src/Dn2Cpp.Transpiler/CoreIntrinsics.Intercepts.cs` and reference it from every
asker — **(pure predicate, cut kind, emit arm, scan effect)**, one list, nothing
to drift.

CoreLib's `Interop.Globalization` imports have no ICU PAL in the native runtime.
Their native-import descriptor keeps managed wrappers and retained signatures,
but routes execution to a catchable `PlatformNotSupportedException` through the
same P/Invoke mouth used by direct calls and address-taken wrappers. A registered
`NativeImplementation` or explicit `--direct-pinvoke` binding supplies the import
instead; application imports are unaffected.

CoreLib imports of the private `QCall` module follow the same refusal rule:
the standalone runtime has no CoreCLR entry points. Existing member-specific
lowerings retain ownership of direct calls and address-taken wrappers. This is
selected from the input declaration, never the transpiler host. In particular,
a CoreLib import of `kernel32.dll` keeps its normal native binding even when a corresponding
CoreLib flavour declares a `QCall` import with the same entry-point name.
The exact `RuntimeTypeHandle.IsCollectible(QCallTypeHandle)` import keeps this
binding authority even though its declaring type is intrinsic-mapped. Its raw
handle query is not the public reflection API's noncollectible-image policy.

The exact managed `RuntimeTypeHandle.IsSZArray(RuntimeType)` helper uses the
existing native SZ-array predicate with a null Type guard. Its retained getter,
direct calls and address-taken wrappers share that lowering; neither CoreCLR
element codes nor rank alone define native array identity.

The exact managed `RuntimeTypeHandle.IsByRef(RuntimeType)` helper uses the
existing outer signature-kind predicate with a null Type guard. Arrays, pointers,
function pointers and byref-like types are not themselves by-reference types;
a by-reference wrapper is, independently of element metadata precision. The
protected override on sealed RuntimeType keeps its IL and exact call/address
bindings, without adding CoreCLR element codes or custom Type dispatch.

The exact managed `RuntimeTypeHandle.IsPrimitive(RuntimeType)` helper uses the
existing native primitive flag with a null Type guard. It does not inherit an
element or enum underlying type's flags; native integers are primitive, while
void, decimal and compound handles are not. The protected sealed RuntimeType
override retains its IL and exact call/address bindings, without exposing
CoreCLR handles or widening custom Type dispatch. The pure element-code helper
`RuntimeHelpers.IsPrimitiveType` remains independent of this Type query.

The exact managed `RuntimeTypeHandle.IsPointer(RuntimeType)` helper uses the
existing outer signature-kind predicate with a null Type guard. Native integers,
arrays, byrefs and function pointers themselves are not pointers; an outer
pointer wrapper is, regardless of its element. The real protected RuntimeType
override retains its IL and exact call/address bindings. This neither exposes
CoreCLR handles nor widens custom Type dispatch.

The exact `RuntimeTypeHandle.IsFunctionPointer(RuntimeType)` managed predicate
and `IsUnmanagedFunctionPointer(RuntimeType)` InternalCall share the existing
native signature predicates. Only the queried type counts: pointer, by-reference
and array wrappers do not inherit their element's function-pointer flags.
Retained public getters keep their managed bodies. Their exact sealed bindings
avoid CoreCLR virtual slots across calls and address loads. Direct calls, address
bindings and retained reflection rows use the same lowering. The native null Type
guard also applies to the raw unmanaged predicate, whose CoreCLR contract requires a
nonnull RuntimeType. This does not extend custom Type dispatch or open
function-pointer construction.

The exact `RuntimeTypeHandle.IsGenericVariable(RuntimeType)` InternalCall reads
only the native formal-parameter flag. Definitions, constructed types and
compound wrappers do not inherit generic-parameter identity. Its retained public
RuntimeType getter keeps its managed body; exact sealed calls and address bindings
avoid CoreCLR-only virtual slots. Direct calls, delegates and explicitly retained
helper reflection rows share that lowering. Raw null arguments use the native
Type safety guard, not a claimed CoreCLR private-call exception contract. This
does not extend custom Type dispatch or the separate raw index/contains helpers.

The exact managed `RuntimeTypeHandle.GetAssembly(RuntimeType)` helper returns
an interned native RuntimeAssembly object; public Assembly boundaries retain their
headerless identity conversion. The retained sealed public getter keeps its IL;
its raw address keeps that ABI, while delegate construction erases it through the
managed-wrapper adapter. Its standalone delegate metadata row does not add named
lookup to the runtime-owned RuntimeType catalog. Helper null arguments use
CoreLib's invalid-handle ArgumentNullException; public virtual receiver checks
still raise NullReferenceException. Private assembly-cache
InternalCalls and QCall imports keep their separate binding authority. Under the
native image's noncollectible policy, function-pointer assembly identity follows
its return signature recursively without changing its element-type predicates.

The exact managed `RuntimeTypeHandle.GetInstantiationInternal` and
`GetInstantiationPublic` wrappers read native formal or constructed type arguments.
They validate default handles and return null at zero arity. Nonempty results are
fresh arrays carrying the declared RuntimeType[] or Type[] identity; public getter
normalization stays in its managed body. Raw handles are not rooted through array
or pointer elements, and the raw QCall keeps native-import binding authority.

The exact managed `RuntimeTypeHandle.GetGenericVariableIndex()` wrapper and public
`RuntimeType.GenericParameterPosition` getter read native formal contexts. A
position is the parameter's identity index in its defining type or method vector,
not its index in a constructed use. The lookup verifies an interned formal under
the existing context lock without completing constraints. Missing context refuses
explicitly. Default handles, null public receivers and non-parameters preserve
their distinct managed validation. Retained calls, function pointers, delegates
and reflection share the same lowering; the raw private InternalCall is not an
alias for the managed wrapper.

The exact managed `RuntimeTypeHandle.ContainsGenericVariables()` wrapper validates
its handle and shares the native `ContainsGenericParameters` predicate. Formal
parameters and open definitions are terminal positives; constructed arguments and
element types recurse without inspecting constraints or declaring contexts. The
public getter keeps its managed body. Retained calls, function pointers, delegates
and reflection share this wrapper. The exact static InternalCall accepts a
canonical native Type object and reuses the same predicate after header validation;
null fails with native NullReferenceException, while custom Type headers refuse
before payload access. This native safety policy is not a private CLR null-call
contract. The instance wrapper retains its checked default-handle validation,
and open function-pointer construction keeps its existing refusal.

The retained `RuntimeTypeHandle.GetArgumentTypesFromFunctionPointer(RuntimeType)`
uses existing immutable native signature children to return a fresh precise Type[]:
return type first, followed by parameters in order, with interned Type identities.
The zero-parameter case still owns an array containing its return type. Public
parameter/return helpers and managed cache storage keep their existing behavior.
Null, custom headers and non-function-pointer input have explicit native safety
policies (NullReferenceException, PlatformNotSupportedException and
InvalidOperationException), not private CLR invalid-input parity. Canonical header
validation precedes payload access; no function pointer is invoked and no new
signature substitution or raw QCall capability is introduced.

The retained `RuntimeTypeHandle.GetUtf8Name(RuntimeType)` validates the canonical
Type header before payload access. Null throws NullReferenceException; formal,
signature and array types preserve the managed Arg_InvalidHandle ArgumentException
with no parameter name. Ordinary types explicitly refuse unavailable CoreCLR
metadata-name pointers. Native display names are not raw TypeDef simple-name
provenance, so no borrowed pointer or successful MdUtf8String is fabricated. The
real managed value-return ABI remains intact for calls, addresses and reflection;
public naming and the raw GetUtf8NameInternal leaf keep their existing policies.

The exact managed `RuntimeTypeHandle.GetConstraints()` wrapper reads the existing
native formal-constraint metadata. Default handles and non-parameter handles keep
their distinct validation. Zero constraints return null; nonempty results are
fresh, precisely typed Type[] arrays over interned constraint identities. The
retained public RuntimeType getter keeps its managed non-parameter validation and
Type.EmptyTypes normalization. Direct calls, address bindings and reflection rows
share the wrapper, while its raw QCall keeps native-import binding authority.
This does not change the separate Type intrinsic's existing unsupported-receiver
policy or the formal context, completion and missing-metadata rules.

The exact managed `RuntimeTypeHandle.MakePointer()` and `MakeByRef()` wrappers
validate their handles, then share the existing native compound-type factory.
Pointer and by-reference identities are interned with static signature types;
a by-reference element remains invalid for either construction. Their public
RuntimeType overrides keep their managed bodies, including the distinction
between a null virtual receiver and a nonvirtual body's default-handle check.
Sealed public calls and virtual-address bindings enter those same real bodies
without indexing CoreCLR virtual slots. Direct calls, address bindings and
reflection-only rows share the handle wrapper; the separate raw QCalls retain
native-import binding authority. This does not
extend the public Type policy or supported function-pointer construction.

The exact managed `RuntimeTypeHandle.MakeSZArray()` and `MakeArray(int)` wrappers
validate their handles before using the existing native array-type policy.
Ordinary concrete SZ arrays require an emitted registry identity; concrete ranked
arrays remain unsupported, including rank-one non-SZ arrays. Open and signature
array types use their existing interning and rank limits. These wrappers do not
expand the native array factory or implement CoreCLR's private loader ABI.
A direct private MakeArray call with a nonpositive rank throws
PlatformNotSupportedException after handle validation. Its public RuntimeType
overload keeps the real managed rank check before handle construction, and a
virtual null receiver still faults before entry. Both public overloads retain
real IL across calls and address bindings; the raw QCalls remain independent
native-import boundaries.

The exact retained `RuntimeType.IsAssignableFrom(Type)` override reuses the
existing native relation engine, including its metadata and nullable handling.
Raw calls return false for a null source before observing the receiver;
`callvirt` validates the receiver first. Non-null sources must carry the native
RuntimeType object header: custom Type objects are rejected with
`PlatformNotSupportedException` before accessing a native Type payload, even
when the raw receiver is null. This boundary does not implement custom
`UnderlyingSystemType` or TypeBuilder dispatch, change existing relation limits,
or intercept the TypeInfo overload or arbitrary Type overrides.
`selfhost-prim-subset` verifies the retained routes, Boolean invocation ABI,
header-only rejection boundary, and existing class/interface/array relations.

The exact `RuntimeType.IsEnumDefined(object)` override reuses the native enum
lookup with strict boxed enum/underlying-type compatibility and literal name
aliases. A raw retained call checks null value before dereferencing its receiver;
callvirt and virtual address binding check the receiver before entry. This
adapter does not widen custom Type dispatch or CoreCLR enum table support.

The exact public `RuntimeType.MakeGenericType(Type[])` override shares the existing
native `Type.MakeGenericType` AOT, open-type and template policy. Its retained body,
direct calls and address bindings never enter CoreCLR's instantiating loader.
The reflection planner recognizes that exact supplied CoreLib declaration and
signature through the same assembly-identity resolver as normal type references,
including supplied forwarders and the signed-facade fallback. Known call arguments
retain the same closed types as the public Type route without an independent
closed-type root.
A virtual call checks its receiver before entry; a nonvirtual body validates the
argument array first. The two managed `RuntimeTypeHandle.Instantiate` wrappers
instead preserve their pre-QCall null-element and default-handle checks, then
refuse CoreCLR-owned type construction and constraint validation. Their raw QCall
keeps native-import binding authority. This does not extend generic constraint
validation or the public API's supported runtime-instantiation subset.

The exact public `RuntimeType.DeclaringMethod` getter uses native formal-owner
metadata and preserves its non-parameter validation text. RuntimeType is sealed;
its address and virtual-address bindings use that same wrapper, never a CoreCLR
vtable slot. The separate managed `GetDeclaringMethodForGenericParameter` bridge
returns CoreCLR's `IRuntimeMethodInfo` and refuses execution rather than inventing
a MethodDesc. Its raw QCall sibling retains native-import binding authority.

The exact managed `RuntimeHelpers.IsPrimitiveType(CorElementType)` and
`CanPrimitiveWiden(CorElementType, CorElementType)` helpers retain their real IL.
They classify element codes, not Type identities. Undefined codes keep the input
body's masked shifts and widening-table identity fallback; the widening table
comes from the input RVA field. Direct calls, address bindings, retained
reflection rows and copied-body traversal share this same boundary. Other
RuntimeHelpers members remain mapped, and public primitive/conversion policies
are unchanged.

The managed `ThrowHelper.GetAmbiguousMatchException` factories retain their real
IL for both `MemberInfo` and `Attribute` inputs. They return an exception instead
of throwing it, preserving the declaring-type getter and object-based `SR.Format`
evaluation before construction. Direct calls, address-taken calls and retained
reflection entries use that same body; other ThrowHelper members remain mapped.
This does not extend the shared formatter's custom `IFormattable` or
`ISpanFormattable` support.

The private-protected `Type.CreateGetMemberWithSameMetadataDefinitionAsNotFoundException`
factory also retains its real IL. It reads the input member's name, formats the
input CoreLib resource and returns a fresh `ArgumentException` with parameter
name `member`; the caller owns the throw. The factory's null dereference is
separate from the public lookup's null-argument validation. Its call, address
and retained reflection routes share the same managed body. A named-only entry
registers its ordinary emitted row without mutating the runtime-owned Type
handle or adding a partial member enumeration. Delegate metadata and named lookup
reuse that row identity.

`MemberInfo.HasSameMetadataDefinitionAs` keeps its managed base body for explicit
nonvirtual calls and `ldftn`: it throws `NotImplementedException`, including for
a null argument. Virtual calls and `ldvirtftn` use the native reflection handle's
runtime kind, validate `other`, and compare definition identity rather than
object equality. Ordinary managed receivers retain their existing virtual slot.
Recorded tokens use the image's defining-assembly identity; closed generic owners
and methods normalize to their definitions. Canonical builtin rows, synthetic
method descriptors with their defining owner, registered formal parameters, and
element-bearing types have separate proven identities. Module-dependent comparisons
require a recorded assembly or an exact known CoreLib primitive/Object/String handle;
canonical registry membership alone does not prove a defining module. Unknown modules
and unrecorded tokenless rows refuse with `PlatformNotSupportedException`, even when the same handle is supplied.
The module model remains one defining module per assembly simple name, not a new
AssemblyLoadContext or netmodule loader. This does not add a named reflection
catalog for the base method; address-retained metadata must refuse invocation
rather than run its nonvirtual base body on a native reflection receiver.

The private CoreLib `GC.RunFinalizers(): UInt32` callback belongs to CoreCLR's
native finalizer worker, including its thread reset and queue ownership. dn2cpp
owns finalization through its dedicated consumer or manual-drain mode. The exact
private, static, nongeneric, zero-argument callback retains a callable signature
but throws `PlatformNotSupportedException` when invoked, including by reflection
or an address-taken wrapper. It does not return a fabricated count, reset caller
threads, or introduce a second queue consumer. Public `GC.Collect`,
`GC.WaitForPendingFinalizers`, suppression and re-registration keep their runtime
implementations. This is a private runtime boundary, not CLR callback parity or a
general fallback for retained methods.

- **Predicate**: the member set only — names, and behind a name gate a thunked
  signature. Never Compilation state.
- **Table**, by what the (type, member) pair *means*: `MethodDefIntercepts`
  (in-module MethodDefinition), `MemberRefIntercepts` (cross-assembly
  MemberReference), `MethodSpecIntercepts` (a MethodSpecification token, before
  resolution), `ConstrainedVirtualIntercepts` (the resolved impl a dispatch bound
  to), `ScanIntercepts` (the raw token `ScanBodyForGenerics` sees),
  `BoundedIntercepts` (the resolved method the reachability drain sees).
- **Cut kind**: `Cut` (edge deleted), `BodyReplace` (still reachable, vtable slot
  a real pointer, IL never scanned, emitter synthesizes the body), `Bounded`
  (subtree cut, call site neutralized), `None` (route without cut — bloat, never
  a link error).
- **Emit arm** (`InterceptEmitArm`): the emit funnels switch on it with a
  throwing default, so an armless row fails the first transpile that matches it
  instead of naming a deleted body. A mouth outside the funnels — a synthesized
  body, an arm inside `EmitManagedCall`, a position that is itself semantics —
  takes a **documentary** arm and no case.
- **Scan effect**, if `ScanBodyForGenerics` acts on the token: `MarkThenResolve`
  or `EffectThenSkipResolve`.
- **Position**: reference the row at each asker's own chain position. The tables
  are registries, not chains, and the chains' differing test order decides which
  line answers a token, hence the emitted bytes.

**What cannot be a row** stays at its call site with a comment naming its
counterpart, sharing the *predicate call* at both sites: a non-pure predicate
(needs the `ClassInfo`, module or Compilation); a route-without-cut tested on the
receiving side rather than the callee (`TextWriter.Write`, `SafeHandle`, the
`[HotPath]` span indexer); one that rewrites arguments instead of replacing the
call; a per-instance set (the bounded set unions a backend's
`AdditionalBoundedMethods` and the CLI's `--cut` specs, so the row carries the
core half and `Compilation.IsBoundedMethod` / `IsDynamicCodegenMember` stay the
merge point — *not* the same-named `CoreIntrinsics` predicates); an asymmetric
set, where a row would assert an agreement that does not hold; and the
`RuntimeHelpers.RunClassConstructor` MemberRef mouth, which neither cuts nor
routes but reaches the named type's `.cctor` — it rides in
`CoreIntrinsics.ScanNeedsParentTypeName` as a hand-written residue beside the
scan rows' names, and deriving it away leaves a green transpile and a shipped
game that silently skips a class initializer .NET runs. Reflection APIs are not
intercepts: `ReflectionApis.Classify` names their operand slots, and the
per-method summary decides what each site selects.

**The backstop.** `CppEmitter.AssertCalledBodiesEmitted` diffs, after the emit
fixpoint, the symbols emitted bodies **name** against those they **define**
(`{m.CppName : m ∈ compiledMethods}`), failing with callee, caller and reach
chain. It is unconditional and decode-free, and records at
`MethodInfo.Emittable` — the funnel every mouth passes — so it does not care how
a symbol was named. It is a net, not a proof. A backend **epilogue** is emitted
text rather than a compiled body, so ask `CppEmitter.RequireDefinedBodySymbol`
there: an engine entry point has no C# caller by construction, so degrading it to
a null slot ships a game whose callback never runs.

**Which remedy is right turns on whether the real body is transpilable**, and the
arm decides it:

- **MemberReference arm — public BCL surface** (`System.IO.Path` / `File`,
  `Environment`, `MemoryExtensions`, the sub-word integers), where an unmodeled
  overload usually has a transpilable real body. → **Fall through**, and make the
  cut see the shape too.
- **MethodDefinition arm — private runtime plumbing, without exception**:
  bodyless InternalCall/QCall, machinery dn2cpp replaces, or a body whose
  emission the optimizer would erase (`GC.KeepAlive`). An overload upstream adds
  is more of the same, so its fall-through *is* the cascade the cut deletes. →
  **An unmodeled shape throws, always.** Cut by NAME, lower by SHAPE, end the
  table in a throw.

**The debugger-logging surface is NativeAOT's**: `Debugger.IsLogging` const-folds
false and `Debugger.LogInternal` is `Bounded` `Silent`, so `DebugProvider` keeps
only its `Interop.Sys.SysLog` arm and a `Trace`/`Debug` write goes to syslog
(stderr on wasm), never to stdout — asserted by the misc-intrinsics section of
`gates/build-and-run-selfhost-prim-subset.sh`.

The sub-word integers sit in both arms and take the loud side in both: their
`ToString`/`Parse`/`TryParse`/`TryFormat` reach the number-formatting cascade, so
falling through trades a loud failure for silent bloat. .NET gives all eight
integer widths the identical overload shape — verify against the ref assembly.

**A name-gated route may decline a shape; it may never pop one.** Popping a fixed
operand count after a name match corrupts the evaluation stack on a
differently-shaped overload and emits C++ that compiles and misbehaves —
invisible to the backstop, since it is not a disagreement between askers.

Predicate traps: take the signature as a **thunk**, so a foreign token pays for
no decode; where the remedy is "always throw", key on the **name alone** and
leave the shape test to the emitter, which has already paid for the signature;
**decline generic members outright**, which makes the askers' differing
`GenericContext` irrelevant. When a cut cannot see what the emit guard sees (the
receiver's static type, say), make the cut agree or **delete the cut** and let
the real body be reachable — never approximate it.

#### Headerless managed references

Culture, number-format, text, assembly and module typed values use raw runtime
payloads. Every conversion to an object or interface acquires an interned managed
wrapper with the source CLR identity. `IFormatProvider` uses the managed reference
ABI; numeric intrinsics extract a known provider's number-format payload only when
formatting or parsing consumes it. Composite formatting defers extraction until
a numeric hole needs provider data, including empty and default formats.
Culture identity and missing numeric data are separate payload state. Writable
constructor results own a lazy number-format slot independently of culture-data
and readonly lookup caches. Assignments preserve the supplied NFI reference;
clones copy initialized NFI data into a writable instance without forcing a lazy
getter. Readonly wrapping freezes an initialized NFI only for specific cultures;
neutral cultures retain its reference. Culture and text wrappers compare immutable
names, including alternate sorts; number-format wrappers retain reference equality.
Current-culture assignment and wrapper
caching preserve missing-data state without forcing the number-format getter.
Numeric consumers enforce it when provider data are needed; positive integer decimal and hexadecimal fast paths do not need them.
Consuming a custom numeric provider reaches
its `GetFormat(Type)` implementation and accepts only a `NumberFormatInfo` result;
other results fall back to the current culture. Allocation, identity conversion,
and unrelated known-provider formatting do not retain unused provider bodies.

Numeric culture data are frozen CLR/ICU snapshots in `runtime/core/intrinsics/`.
Native binaries never discover or load the deployment host's ICU. Reference
versions are explicit and checked by `gates/check-culture-reference.sh` in CI.
The Linux numbering-extension snapshot is selected separately from Windows and
Apple, without changing the shared named-culture and fallback tables. Android
and Web retain the existing standard numbering snapshot; the Linux selection
excludes Android even though its compiler defines `__linux__`.
A separate PAL locale name selects undefined-language fallback profiles: managed
current cultures do not change ICU resource fallback. The selector keeps only a
frozen host index and never constructs a culture, preventing recursive default
culture initialization. Fallback profiles preserve the requested region's
localized currency and resolve numbering aliases against the host. Their host
and region inventory follows the modeled culture candidates; known resource
languages outside that inventory retain the existing unmodeled numeric fallback.
Apple and standard ICU resource-language inventories remain separate.
Internal format-spec callbacks retain their raw number-format payload ABI.
Named and managed-default culture identities sort accepted BCP47 variants before
discarding an invalid tail. Accepted ICU keywords enable the original managed
u/t suffix; collation and private-use legacy variants retain their identity.
Undefined-language spelling follows the fixed target reference; an empty custom name
retains its LCID, neutral state and missing numeric data independently of the
invariant culture and participates in the resolved-name wrapper cache.
An empty BCP47 locale conversion retries the original legacy name before
extlang promotion can replace its country identity. Nonempty converted root
locales omit a promoted undefined-language prefix.
The legacy locale path preserves variant order; raw host numeric-resource
selection remains separate from managed identity.

Multidimensional array slots retain the typed raw payload ABI exposed by
`Address`. Erased loads/stores and reflection convert using the actual element
type, including covariant access and copies between MD and SZ representations.
SZ reference slots contain managed wrappers. Type and bounds checks precede
conversion and mutation in each entry's CLR order.
Writable MD `Address` checks null and exact element identity before bounds;
SZ `ldelema` checks null and bounds before exact element identity, while its
`Address` method checks type before bounds. Bounds opt-outs retain the element
contract, and `readonly.` access permits covariance. Shared MD allocations and writable
addresses resolve their precise array identity through the existing type-info
context slots. A raw headerless MD element requires a typed body rather than a
canonical managed-reference byref ABI.
Readonly covariance keeps the original cell address. Erased indirect reference
loads recover raw MD owners from a dedicated scanned GC allocation kind before
reading their headers, then intern the current payload's managed wrapper. The
calloc fallback indexes its never-freed raw MD allocations instead. Neither path
copies the cell or retains an extra GC root, so saved and returned aliases remain
live across writes and collections. Exact typed headerless reads keep their raw ABI.
An erased indirect read can materialize an identity wrapper; pending objects and managed byrefs
on the IL evaluation stack therefore stay live across `ldind.ref` and `ldobj` on Web.
Runtime argument-exception helpers likewise keep earlier managed operands live
while constructing later message, parameter-name and boxed-value operands.

#### Platform ISA contract

The `System.Runtime.Intrinsics.X86` / `.Arm` / `.Wasm` families are one
capability contract, not a set of independent constants:

- **One token macro per family.** `runtime/core/isa/dn2cpp_isa_tokens.g.h`
  defines `DN2CPP_ISA_<Arch>_<Type>` from the target macro
  (`DN2CPP_TARGET_X64` / `ARM64` / `WASM32`, `runtime/core/dn2cpp_cpu_features.h`)
  and the run-time feature word. The transpiler emits the **token**, never a
  host-derived constant: the self-host fixpoint requires the emitted text to be
  a function of the inputs alone, and the machine running the transpiler is
  not one of them.
- **Non-lowered getters const-fold to 0.** `BranchLiveness` then prunes the
  guarded arms, so their icalls never enter the reachability tree, and a call
  to one of the family's instructions routes to a `PlatformNotSupportedException`
  throw — what .NET does when `IsSupported` is false. Lowered getters read the
  token at run time, and every lowered helper re-reads it before executing
  (`dn2cpp_isa_require`): a call made while the token is false — the CPU lacks
  the ISA, or `DN2CPP_CPU_FEATURES` masked it — throws
  `PlatformNotSupportedException` as .NET does, never the instruction.
  `X86Base.CpuId` is the target-capability exception: .NET implements it as
  an x86 QCall, so disabling hardware intrinsics makes `X86Base.IsSupported`
  false without disabling `CpuId`. Its generated helper omits the token check
  on x86 and retains the foreign-target throwing stub.
- **Cut ⟹ route via the `MdPlatformIsa` row.** Its predicate is the
  `ClassInfo.PlatformIsa` stamp rather than a name table: the nested types
  (`X64`, `Arm64`, `VL`, `V256`, `V512`) have bare names, so only a stamp made
  when the class is classified can tell `Sse2.X64` from any other `X64`.
- **Lowered is a proof, not a flag.** `tools/gen-isa-map` writes a family as
  Lowered into `src/Dn2Cpp.Transpiler/CoreIntrinsics.PlatformIsa.g.cs` only
  when every public static method of it has a helper
  (`dn2cpp_isa_<arch>_<type>_<method>_<argsig>`) under `runtime/core/isa/`, and
  every family it implies is Lowered too — the families whose feature bits lie
  in the implication closure of its own (`DN2CPP_CPU_FEATURE_TABLE` parents),
  which covers the enclosing family and .NET's instruction-set implications
  (`Dp` ⟹ `AdvSimd`, `Popcnt` ⟹ `Sse42`). A child true beside a parent folded
  to 0 is a state .NET never has, and BCL code guarded by the child would reach
  the parent's throwing calls. `gates/build-and-run-platform-isa-surface.sh`
  re-derives both rules from the csv and the header.
- **Every Lowered helper is exercised.** `Exercises.g.cs` in the probe, written
  by the same generator, calls each mapped method with fixed inputs and prints
  the bytes. The native gate compiles the probe in bounded reachability shards:
  each managed variant omits the other families' table and exercise roots, so
  their helper headers and immediate dispatches cannot enter that native image.
  The gate proves the shards assign every row, top-level exercise and invalid
  immediate witness exactly once, and that each generated image contains only
  its assigned exercise bodies. It then concatenates their output and diffs it
  against the identically sharded real-.NET runs. A wrong lowering therefore
  fails on the machine that has the instruction rather than in a user's
  program, without one compiler or process owning the full ISA surface.
  `Wasm.PackedSimd` has no such oracle (no host .NET answers true for it), so
  its run is frozen in `gates/expected/platform-isa-wasm-simd.txt` and every
  row with a portable equivalent carries a `@ref` cross-check against the
  `Vector128` layer, an independent implementation; the gate refuses a mismatch.
- **wasm SIMD is a build axis.** A module either carries SIMD instructions or
  loads on an engine without them, so the wasm detector answers from
  `__wasm_simd128__` and the CMake option `DN2CPP_WASM_SIMD` (`-msimd128`,
  PUBLIC on the runtime so every TU agrees with the detector) is what makes
  `PackedSimd.IsSupported` true. The helpers' real bodies exist only under that
  macro; a default wasm build compiles their throwing stubs.
- **AVX10.2 needs a positive policy decision.** Raw CPUID detection retains the
  hardware fact, but `Avx10v2` and the V512 `AvxVnniInt8` / `AvxVnniInt16`
  surface enter the effective set only under `DN2CPP_ENABLE_AVX10V2=1`, matching
  .NET 10's default-off policy. The compiler-capability guard is an independent
  requirement, so the opt-in cannot expose helpers the compiler cannot build.
- **The mask intersects policy.** `DN2CPP_CPU_FEATURES` narrows the post-policy
  detected set (the effective set is the implication closure of policy ∩ allowed)
  and can never opt into AVX10.2 or make a getter true that detection left false.

#### Adopting a third-party async task type

`Compilation.AdoptCustomAsyncTaskTypes` is the one place a non-BCL type is
intrinsic-mapped, and it is automatic — nothing to add per library. A type
carrying `[AsyncMethodBuilder(typeof(B))]` is registered with its builder and the
awaiter its `GetAwaiter()` returns, and the three lower to the same runtime
structs the BCL Task family does; the member names are the BCL's by mandate, so
every existing Task/ValueTask intrinsic case fires unchanged. A struct task is
modeled on ValueTask and a reference one on Task, and the kind drives the
task/builder/awaiter keys together. Discovery never throws: `-r`-ing an assembly
must not fail on a task type the program never touches.

Adoption applies only while every cross-assembly reference stays inside the
mapped member-name and signature-shape contract: a metadata-only MemberRef pre-scan
(`RawAsyncAdoption.DeclineOutOfContractAdoptions`) declines the adoption of any
assembly whose task/builder/awaiter is referenced outside it (`UniTask.Yield()`'s
shape), so that library's real IL transpiles through the general pipeline — no
flag, one stderr notice naming the member.
`gates/build-and-run-custom-async-task.sh` asserts both directions. The scan
cannot see a same-assembly call (a MethodDef has no MemberRef row), so that one
still fails loud at emit, naming the adoption and the manual override.

`--no-adopt-async <assembly-simple-name>` (repeatable) is that override: it
declines adoption by hand, for exactly the same real-IL route. Per assembly,
not per type — sibling task types' promises interlock — and an unmatched name is
a hard error. `gates/build-and-run-gdtask.sh` proves the automatic decline on
the real GDTask end to end; `gates/build-and-run-unitask.sh` proves it on the
real NuGet UniTask, console lane.
`--cut "DeclType::Method"` (same hard-error rule and bounded semantics as
`AdditionalBoundedMethods`) is the carve-out lever for a genuinely untranspilable
corner, with its regression test in
`gates/build-and-run-transpiler-limits.sh`; nothing in the tree uses it, because
a per-library cut does not scale — ask first whether the wall is general.

#### Degrade or fail loud? (deciding an AOT boundary)

> **A DIAGNOSTIC API degrades. A LOAD-BEARING one fails loud.**

Ask what the *caller* does with the result.

- **Load-bearing → fail loud.** `Expression.Compile()`, `DynamicMethod`,
  `Reflection.Emit`: the caller wants a delegate and cannot proceed without one,
  so any value handed back is a lie it then executes. Cut at reachability; call
  sites throw a catchable `PlatformNotSupportedException` naming the member
  (`Compilation.IsDynamicCodegenMember`).
- **Diagnostic → degrade, and say so.** `System.Diagnostics.StackTrace` /
  `StackFrame`: every caller proceeds regardless of what it finds. Under
  `--shadow-stack` the current-stack ctors materialize the live shadow stack's
  frame names; `StackTrace(Exception)` reuses the trace stamped at throw in every
  build; flag-off, a capture reports zero frames.

A degradation must **name itself** — a zero-frame trace's `ToString()` returns
`"   at <stack trace unavailable in AOT>"`, never `""`, since an empty string is
indistinguishable from an empty stack. Three corollaries: degrade only what is
actually unavailable (a caller-supplied `new StackFrame(file, line)` is not a
capture, and `StackTrace(IEnumerable<StackFrame>)` asks for a specific trace, so
it throws); where the degradation is a value the caller must handle, be sure they
can (a zero-frame trace's `GetFrame(i)` is null for every `i`, so those accessors
are null-tolerant); and a materialized frame is a rendered STRING, not metadata,
so `GetMethod()` stays null by design (argument at the intrinsic site,
`src/Dn2Cpp.Transpiler/MethodCompiler.EmitIntrinsic.Reflection.cs`).

**Before either verdict, ask whether the thing is unavailable at all.**
`System.Diagnostics.Tracing.EventSource`'s *delivery* surface no-ops, which is
not a degrade and needs no self-naming: every .NET write path opens with
`if (!IsEnabled()) return;` and no listener can attach in a native build. Its
*identity* surface (`Name`, `Guid`, `Settings`, `CurrentThreadActivityId`) is
answered for real from the type and the base ctor's arguments — "cannot be faked"
is a claim about the surface, not about the feature. Genuinely absent is
*observation*: `EventListener`'s member set is a loud transpile abort with a
named refusal, because a listener that constructs happily and receives nothing
forever cannot detect its own failure.

**And ask whether the runtime METADATA already holds the answer.** A member whose
result is a *function of the runtime type metadata* needs neither a compiled body
nor a statically reached instantiation, so reflection can answer it exactly for
type arguments no call site named (`Unsafe.SizeOf<T>` static,
`Object.MemberwiseClone` instance). Add a row to `g_meta_members` in
`runtime/core/intrinsics/dn2cpp_system_reflection.cpp`, which the *named* lookup
consults after the type's own rows miss. Four rules: partial intrinsic catalogs stay out of
`GetMethods()`, while Object's complete family and its ValueType/Exception
replacements share the named-lookup handles and override rules; the answer
comes from layout reasoning, never from `instanceSize`
raw (that field is the box-payload width); a row carries its own `attrs`, since
the lookup's `BindingFlags` filter and the row must not disagree; the lookup
walks the **base chain**, with the row still naming the declaring type.

Two limits. A type reached by a **type token alone** has no emitted field layout,
so where the layout model cannot compute an extent the emitter stamps
`DN2CPP_TF_LAYOUT_UNKNOWN` and the size/stride readers throw naming the type. And
an **intrinsic-represented** value type stamps its hand-written C++ struct's
`sizeof`; should one diverge from .NET, fix the *representation*, never
`dn2cpp_layout_size` — a static `Unsafe.SizeOf<T>`, an IL `sizeof` and the
array/`Unsafe.Add` stride are emit-time `sizeof` expressions no runtime override
reaches, so an override splits one question into two answers.

Gate placement follows the verdict: a degraded API can never sit in a
`corelib_diff_gate` (real .NET does the real thing) and goes in a
`corelib_freeze_gate`; a metadata-answered member matches real .NET, so its
section belongs in a `corelib_diff_gate` (`samples/dotnet/ReflectInvoke`).

#### Reflection metadata representation

The reflection and generic virtual dispatch contracts are in
[REFLECTION-DISPATCH-DESIGN.md](REFLECTION-DISPATCH-DESIGN.md).

`Dn2CppTypeInfo` keeps type identity, layout, inheritance, dispatch, generic
arguments and array classification directly addressable. Reflection-only fields
are read through `reflection()`. Member handles and tables use the common
`Dn2CppMetadataHandle` / `Dn2CppMetadataTable` API in
`runtime/core/dn2cpp_metadata.h`; consumers must not assume a native row array.

`TranspileOptions.CompressMetadata` defaults to `true`. Setting it to `false`
through `--no-metadata-compression` forces native rows across all generated
metadata, overriding automatic and explicit packed selections. Selector
validation still runs. This changes representation without retaining stripped
types or members.

The producer is `CppEmitter.MetadataEncoding.cs` with row construction in
`CppEmitter.MetadataRecords.cs`. Its field order and integer signedness must
match the schemas in `runtime/core/dn2cpp_metadata.cpp`. Records carry a block
index, their padded byte extent and a presence mask as unsigned variable-length
integers; signed values use zigzag encoding. Pointer indices refer to constant
symbol pools. The codec's reserved local-block marker and backward offset
support records with an adjacent pool. Runtime-owned metadata uses native
constant rows, so retaining one builtin does not retain unrelated builtins
through a global pointer array. An omitted derived member flag uses
the recorded ECMA attributes; an explicitly recorded zero stays zero. Parameter
modifier knowledge remains separate from the modifier vectors and counts.
Parameter defaults use nullable factory pointers: a factory returning null records
a null default, while an absent factory leaves Invoke to the optional/no-default
rule. Factories retain constant bits and UTF-16 strings, including Decimal and
DateTime attribute constants. Invoke substitutes the canonical Missing singleton
before conversion and applies default/by-ref copy-back only after target success.

Packed static records are aligned and identified by the low address bit in a
pointer-sized handle. Native static and dynamic rows retain untagged real
pointers; dynamic rows keep their existing GC roots. Native views read those
rows directly. Packed views decode onto the stack, and a temporary view's
address must not escape its expression. Member interning uses the original row
identity together with the reflected type, never the temporary decoded address.
A synthesized generic type's constructor and method rows, and a clone level's
interned method rows, store their original handle and replacement declaring type
in a delta descriptor.

Event rows retain original add/remove/raise method handles and an ordered vector
of other method handles. Event handles intern the row and reflected owner;
handler recipes substitute the actual declaring context. Inherited lookup drops
private declarations, then hides exact names before applying binding flags.
Accessor queries share method handles with ordinary lookup, including runtime
generic clones. Subscription uses the existing invocation guards; a recognized
subscription reaches the selected accessors, and metadata lookup alone does not
retain accessor bodies. Preservation retains all
accessor roles through both attributes and link descriptors.

Generic parameter constraints use immutable cold recipes on method descriptors
and type reflection rows. Formal method parameters are interned by the typical
MethodDef, and formal type parameters by the generic definition. `DeclaringMethod`
returns that typical definition through the shared method-handle interner,
independent of closed or inherited lookup contexts and constraint availability.
Type formals return null; nonparameters refuse the query. Recursive
initialization publishes identity and immutable parameter contexts under a cold
lock before relation queries resolve dependent parameters and F-bounds.
Reflection assignability completes nested formal relations before the shared
cast predicate caches an answer. Unloaded named recipes cannot resolve against
another assembly's matching name.
Stripped definitions expose formal identity, but constraint and relation queries
guard the declaring definition through `dn2cpp_require_metadata`.

Constraint recipes reuse existing array and compound interning. Constructed
constraints reuse an AOT or runtime-template identity when available; otherwise
they receive an interned metadata-only descriptor with no layout or dispatch.
Open compositions and closed descriptors without a runtime layout cannot enter
allocation or invocation. `ContainsGenericParameters` follows elements and
generic arguments, never constraints or ancestry. This cold representation does
not change generic-context slots, hot flag widths or the baked patch wire format.

Generic MethodDef signature recipes resolve type parameters against the actual
declaring context and method parameters against the typical definition.
Public signature queries and open `Make*` compositions share the constraint
interners without eagerly resolving parameter constraints. Constructed method
views retain their original definition, argument vector and reflected owner in
immutable cold rows; they never reuse a callable row's invoker. Public composition
validates declared constraints before publishing a handle. Open method arguments
participate in `ContainsGenericParameters` and the invocation and delegate guards.
Runtime-template delegate binding resolves full location recipes after clone
publication, against the actual declaring level. Both sides share structural
interners; unknown names cannot establish equality. Raw address forwarding does
not permit canonical value or reference-slot access, and Invoke retains its
independent boxing and copy-cell checks.

Formal array covariance and generic variance require reference-type evidence;
an unconstrained or dependent parameter alone cannot establish a reference layout.

Generic definitions retain declared member rows decoded from their TypeDefs,
without a class specialization, object layout or executable thunk. The same
symbolic recipes describe methods, constructors, fields and property/event accessors.
Inherited lookup substitutes the actual declaring level while handles preserve
their reflected owner; method formals still belong to the typical MethodDef.
Open base compositions share these immutable tables and interned type identities.
Cold assignability completes a symbolic base before following its relations.
Member operations guard the actual declaring type before accessing storage or
calling an invoker. Literal values remain readable, and a closed declaring base
remains callable when reflected through an open derived type. Metadata retention
and packed/native storage use the same policy across definition emission paths.

`Compilation.ReflectionMetadata.cs` selects native storage for exact types
named by surviving `ldtoken`/`GetTypeFromHandle` pairs in reached IL. It uses
each closed scan context and excludes canonical placeholders, dead code and
folded-away type comparisons. Branch entries inside the pair prevent the
inference. The selection freezes before metadata emission; adding a choice
afterward is an invariant failure. Serialized attribute arguments and handle
values flowing through locals are outside this inference. Explicit
`ReflectionMetadataFormats` selectors override it without changing reachability
or reflection preservation. The CLI syntax is documented in the README.

The same policy recognizes the exact metadata name
`Dn2Cpp.Runtime.NoCompressMetadataAttribute` and custom attributes derived from
it. It walks attribute ancestry and the target's base-class chain through raw
metadata, resolving assembly scopes, nested definitions and generic base types
without materializing classes or adding reachability roots. Cycle guards and
definition-level caches bound those walks. A generic definition's marker applies
to every emitted constructed type. Containment, array elements, generic arguments
and implemented interfaces do not propagate the choice.

Storage precedence is global compression disable, explicit per-type selector,
attribute selection (including inherited selection), then automatic inference.
The CLI's exact-type selectors do not inherit. Runtime-owned storage restrictions
and selector validation remain in force. Attributes change representation only;
`[Preserve]` and the existing retention mechanisms still control which rows exist.

Open generic definitions retain the existing CLR-name-keyed runtime identity.
The emitter collects their concrete and token-site module owners before writing
rows. Native selection is the union of those owners' attribute policies and
surviving `typeof` sites. A qualified explicit format overrides that shared row;
conflicting explicit formats for the same physical row are rejected. This
aggregation does not merge closed-type metadata identities.

The selected format applies to the type's cold row and its own fields,
methods, constructors, properties, events, parameters, custom attributes and enum
members. Automatic and explicit choices do not propagate along inheritance;
only the attribute policy inherits. Generic arguments and array components
never inherit a format choice.
Empty cold metadata stays absent. Parameter and attribute interning includes
the format in its key; both formats retain stable original row identities and
constant initialization. Emission retains only the method- and constructor-table
symbols and extents of each completed pointer block, so completed block contents
can be released.

Method invocation reads an immutable dispatch plan. Each native row, each packed
record inside a generated block's method or constructor table and each
synthesized delta row publishes one plan, which every thread reads in place; a
local codec record is planned per call. Plans contain metadata and code
pointers, never receivers, arguments, results or resolved interface
implementations. Virtual dispatch remains receiver-dependent. Method and
parameter descriptors carry generic definition identity, generic virtual chain
roots and ABI pass modes.
Invocation validates arguments before entering a generated thunk, copies back
by-reference cells, boxes an unmanaged pointer result as
`System.Reflection.Pointer` and wraps callee exceptions. Reflective delegates retain
the selected row and closed signature; plans never retain invocation state.
`gates/build-and-run-reflect-invoke.sh` checks each operation's first and
repeated allocation budget. A published plan is keyed by its row's address
and never freed, which relies on generated images staying loaded, interned
native rows and synthesized delta rows never being freed or reused, and all of
them staying immutable; unloading, collecting or mutating them requires a new
plan lifetime contract.

Names remain directly comparable UTF-8 strings with exact and suffix sharing.
Packed signature displays can carry a token stream marked by an invalid UTF-8
leading byte. The display reader computes the final UTF-16 length and writes
directly into the returned managed string. The emitter accounts for dictionary
cost when choosing tokenization. Native rows use directly shared UTF-8 displays.
Displays containing NUL or unpaired surrogates use a length-prefixed UTF-16
stream in either row format; they never enter the UTF-8 name or token pool.
Attribute records retain order and factories;
sharing their metadata does not share the attribute instances returned to callers.

Object and ValueType fallback rows answer named lookups through the common
handle API; member enumeration does not append those fallback rows. Reflection
discovery closes boxes produced by field reads, constructors, invocation results
and by-reference write-back before freezing layouts. Delegate invoker
declarations remain bounded by the finite signature closure.

Delegate method identities use a private catalog for runtime-owned and opaque
owners, registered before managed code runs. These rows do not enter named
lookup or member enumeration. Interface dispatch rows retain the selected
declaring method independently of its callable adapter; array identities retain
the CLR helper's requested generic arguments even when variance shares an
execution row. Reflection trimming preserves known selected declaring rows.
An unknown stripped level still refuses through `dn2cpp_require_metadata`.

A recognized site that selects a virtual member marks its slot used, so every
allocated receiver's override is reached as for a call site. Through any other
reflective entry, a receiver's override runs only when a call site, a recognized
site or a preservation rule retained it; once reflection can enter rows
late-bound, each kept closed generic virtual row gets a dispatcher whose missing
cases refuse. An unretained override or body is a catchable
`PlatformNotSupportedException` naming the member, its assembly and a
preservation remedy, or the closed-generic remedy for a runtime instantiation's
override that no preservation rule can admit. Refusals for class vtable slots,
interface slots and generic virtual dispatch use the exact selected declaring
owner and metadata name, even when member metadata is stripped. Recording these
identities forms no closed methods and admits no member metadata or executable
bodies. Known preservation capability selects the remedy; unknown capability on
a runtime template gives both preservation and static-instantiation guidance.
Without a confirmed implementation identity, refusals give general guidance to
preserve the receiver's implementation. Reflection makes no instance of a user
type the image never allocates, whose overrides and interface slots are not
wired, and refuses the same way. `Invoke` of a member that returns such a value
type, directly or through a by-ref return, or writes
one back through a by-ref parameter refuses before the target runs, since it
would hand out the box; a
`CreateDelegate` binding of the same member passes the value unboxed and runs.
Ordinary compiled dispatch keeps its trap behavior.

`Marshal.PtrToStructure(IntPtr, Type)` admits boxed dispatch for finite known
value types. A runtime Type may use an already prepared box; creating an
unprepared box refuses before reading native memory into it.

This is an internal generated-code/runtime ABI: regenerate the C++ and rebuild
the runtime together. Changes must preserve streaming output, deterministic
pool indices, trim refusals and the direct type-test/dispatch paths.
`gates/build-and-run-reflect-invoke.sh` combines .NET parity with a native
codec boundary fixture and always runs the program's
`DN2CPP_REFLECTION_MEASURE` mode, which records per-operation first and
repeated allocations and clock ticks.
`gates/measure-reflection-metadata.py` compares those captures and measures
linked Mach-O sections and fixup payloads; retain symbols when attributing bytes
to individual metadata pools.

### C. A Godot engine call — godot lane

Engine-class bindings are generated: `BindingGenerator` emits the C# shim surface
(`src/GodotSharpShim/GodotShims.g.cs`) from `extension_api.json`, and
`GodotCallIntrinsics` builds the call map at runtime from the same JSON. A method
already within the supported marshalling shapes needs no work. To enable one that
is filtered out:

1. Widen the shape in `src/Dn2Cpp.Godot/GodotApi.cs` (`IsSupportedArgType` /
   `IsSupportedRetType` / `MapTypeToGd`).
2. The generic method-bind ptrcall (`dn2cpp_godot_call_ptrcall` and its `_ret_*`
   companions) covers the primitive shapes; add to `runtime/godot` only for a new
   shape.
3. Regenerate the shim:
   `dotnet run --project src/Dn2Cpp.Cli -- --generate-bindings extension_api.json`.
4. Use it in `samples/godot/GodotSample/MyNode.cs`; green
   `./gates/build-and-run-godot-sample.sh` and
   `./gates/build-and-run-godot-bindgen.sh` (the bindgen fixpoint).

Exception: builtin value-type instance methods (Vector3, Color, …) are curated
one `(type, method)` tuple at a time in `GodotApi.BridgedBuiltinMethods`.

### D. A new target backend — cross-lane

1. Implement `IEmitBackend` (+ `ICallIntrinsics` if needed).
2. Put the runtime under `runtime/<target>/`.
3. Add the option to the composition root in `src/Dn2Cpp.Cli`.

### E. A `[HotPath]` optimization knob — core lane

`[Dn2Cpp.Runtime.HotPathAttribute]` opts a method into stronger code generation.
The **bare attribute changes no semantics**: it routes the body into a dedicated
TU (`generated_hot.cpp`) compiled with `-O3` + `DN2CPP_HOT_ARCH`, forces
per-instantiation monomorphic bodies, and keeps the body out of inline header
promotion. Everything that changes behavior is an opt-in named argument —
`SkipBoundsChecks`, `NoAlloc`, `FastMath`, `NoAlias`. A fifth touches four seams:

1. **The attribute** (`src/Dn2Cpp.Runtime/HotPath.cs`): one `public bool`
   auto-property whose doc comment is the contract — what the knob permits the
   compiler to assume, what becomes UB if the caller breaks it, and that the
   attribute is inert under a normal .NET runtime.
2. **The bit** (`Model.cs`, `MethodInfo`): a `private const byte HotPathXxx`, a
   `public bool Xxx` accessor, a decode arm in `ComputeHotPathBits`. An
   undecodable blob keeps `HotPathPresent` and drops every knob — the safe
   direction.
3. **The consumer.** An *emission* knob reads `_method.Xxx` where it emits
   (`SkipBoundsChecks` in `src/Dn2Cpp.Transpiler/MethodCompiler.Arithmetic.cs`,
   `NoAlias` in `MethodCompiler.Signature()`); a *routing* knob forks
   `CppEmitter.Emit`'s `EmitBody` local function (`FastMath` → a second TU).
4. **The sample section + gate** (`samples/dotnet/HotPath`,
   `gates/build-and-run-hotpath.sh`): a new `*.cs` bucket with its own namespace
   and `__GateEntry()`, wired into `Program.cs`. The gate exact-diffs the program
   against real .NET — where the attribute is inert — then grep-asserts the
   knob's effect on the emitted C++.

Four rules the existing knobs were shaped by:

- **A knob-off transpile must be byte-identical.** Route the on/off choice
  through *one* expression family rather than branching at each emission site, so
  the off path is literally the pre-knob code; this keeps the self-host fixpoint.
- **A knob is attribute-driven, never environment-driven**, since it changes the
  C++ a successful transpile emits. A *cap* may be env-driven; a knob may not.
- **Every gate assert needs its complement.** An absence assert ("no
  `dn2cpp_bounds_check` in this body") is also satisfied by a vanished mechanism;
  pair it with a positive control.
- **A verifying knob decides from the emitted text, not from IL shape.**
  `NoAlloc` scans emitted bodies for runtime-call tokens and walks the call
  closure over edges collected at `Compilation.NoteNamedBodySymbol`; inferring
  from IL would miss the intrinsic emission sites that allocate or throw with no
  corresponding IL instruction. Hotupdate images record from the first emitted
  body so late-retained defaults can verify earlier callees; other builds arm
  recording for known NoAlloc roots. Verification runs after the emit fixpoint,
  failing with a `NotSupportedException`.

Auto-`noexcept` is a closed non-goal, not a missing knob.

### F. A managed-fault guard on the emitted body — core lane

Two IL shapes fault on a value rather than on a bug: an instance access on a
**null receiver** and an integer **division by zero**. .NET names and catches both
(`NullReferenceException`, `DivideByZeroException`). Such a guard touches nearly
every line of the program, so its cost is measured rather than assumed —
`gates/measure-faultchecks.sh` re-derives the numbers.

**The guard is an inline function, not a spliced compare, and the C++ optimizer is
the static analysis.** `dn2cpp_null_check(p)` returns `p`;
`dn2cpp_div_signed(a, b)` returns `a / b`. Both are `inline` over
single-assignment temps, so where the receiver is provably non-null or the divisor
constant clang deletes the branch and a loop-invariant check hoists out — hence no
transpiler-side non-null or constant-divisor analysis. Trapping on the hardware
fault instead is not available: on arm64 integer division does not trap, a null
deref the optimizer can *see* is undefined behaviour rather than a fault, and wasm
has no signals.

Two rules for adding a guard of this kind:

1. **Guard at the mouth ECMA-335 names, not in the callee.** The null test belongs
   to `callvirt` and to `ldfld`/`stfld`/`ldflda`, not to instance calls in
   general — a plain `call` on a null receiver is defined *not* to throw at the
   call, and its body then faults on the first field read, which the field guard
   turns into the same NRE at the same instruction. A callee-prologue check would
   throw for both and would fire on bodies that never touch `this`. The splices
   are `MethodCompiler.EmitManagedCall` (one splice covers the direct, virtual,
   interface and GVM arms, all of which read `args[0]`) and
   `MethodCompiler.FieldAccess` (the sole builder of the `->` lvalue).
2. **The guard returns its operand**, so it wraps the cast rather than sitting
   beside it. `((T*)dn2cpp_null_check(p))->f` is still an lvalue — one splice
   serves the read, the write and `ldflda` — and the call is *sequenced* before
   the member offset is formed, which a bare adjacent compare would not be.

`--hotupdate-base` applies the same receiver rule to interpreted method imports.
`Compilation.ReachPatchCallableInterfaceBodies` keeps the named default bodies
of retained user-interface rows and reaches their allocated receivers' dispatch
targets. Default bodies retained after sharing verdicts finalize compile as
closed instantiations without adding canonical candidates. A reference-class
instance body that derives RGCTX from `this` uses a
per-method monomorphic fallback, preserving the exact closed context on null
calls and their nested calls. The shared call-edge closure applies that fallback
when context use propagates from a callee's hidden argument. `CppEmitter.NullReceiver`
guards each CLR dictionary
lookup at its IL instruction, after earlier effects. Methods with explicit
contexts and runtime templates outside the patch ABI keep their sharing path.
For a class type parameter, direct `typeof(T)` uses of `IsValueType`,
`IsPrimitive`, `IsEnum` and `IsByRefLike` follow the CLR's storage-kind folds
without a dictionary lookup. The same predicates fold for the body's own
constructed declaring type, including intervening `nop` instructions. Arrays,
other constructed types and other Type getters keep their lookup checks.
Each affected closed instantiation gets its own body, trading code size for an
exact context when `this` is null.

**The opt-out is a native build option, never a transpiler flag.**
`-DDN2CPP_NULL_CHECKS=OFF` / `-DDN2CPP_ARITH_CHECKS=OFF` degrade each family to
the unguarded operation. Because the guards are inline functions the emitted code
names unconditionally, the generated C++ is byte-identical either way, so nothing
here perturbs the self-host fixpoint. Turning either off restores undefined
behaviour, which is not the same as restoring a crash.

**A new guard must reach both execution modes through ONE helper.** The
interpreter (`interp_binary`) and `decimal`'s div/rem call the shared helper;
hand-written copies had drifted on `int.MinValue % -1`, and two execution modes
that answer a fault differently are directly observable by a hot-update program.

## 5. Cross-cutting machinery (a map of where things live)

| Mechanism | Main location |
|-----------|---------------|
| Generics (instantiation discovery / specialization) | `Compilation` (discovery + specialization at the IL-scan fixpoint), `SignatureProvider` (`GenericContext` threading) |
| Canonical shared generics (default on; `--no-shared-generics` monomorphizes everything) | `CanonicalGenerics` (layout-group canonicalization: enum / sub-8-byte int args → width-preserving placeholders, reference args → `CnRef`), `Compilation.FinalizeSharedGenerics` (owner/user group linkage for classes and generic-method instantiations, instantiation-dependence taint oracle with per-method monomorphic fallback, rgctx slot registries), `CppEmitter` (owner struct/body emission, canonical interface alias rows, per-real forwarders, rgctx tables) |
| Runtime-instantiation templates (`MakeGenericType` over a typeof-only definition, any argument) | `Compilation.BuildRuntimeInstantiationTemplates` (trigger + `$CnAny` metadata registration + shape bound), `Compilation.RuntimeTemplateBaseProjection` (injective bare-parameter recipes in each level's local argument order), `Compilation.JudgeRuntimeTemplates` (per-body/per-slot eligibility; a level's generic-method instantiation qualifies only over closed method arguments, `Compilation.IsTemplateLevelMethodInstance`), `Compilation.ReachTemplateLevelGvmImpl` (the generic-virtual dispatcher case of a template whose clones, but not the template, derive from the dispatcher's declaring type), `CppEmitter.EmitRuntimeTemplates` (the `dn2cpp_runtime_templates` rows, rooted definitions preferred over intermediate sources), `runtime/core/intrinsics/dn2cpp_system_reflection.cpp` (`dn2cpp_try_synthesize_generic`: clone, intern by definition and arguments, project bases, source-specific rgctx tables, clone-owned member rows) |
| Reachability tree-shake | `Compilation` (walks call/newobj/ldftn from roots). Unreached bodies are never decoded |
| Const-folded capability getters + dead-arm pruning | `CoreIntrinsics.ConstFoldedGetter` (the oracle: add a `(type, method) → bool` entry), `BranchLiveness` (per-body live-offset walk shared by the reachability scanner and `MethodCompiler`, so a dead arm's icalls/P-Invokes never enter the tree), `Compilation.ResolveCallTarget` (the getter's own body edge is cut) |
| Platform ISA contract (`X86.*` / `Arm.*` / `Wasm.*` `IsSupported` and instructions) | `src/Dn2Cpp.Transpiler/CoreIntrinsics.PlatformIsa.g.cs` (the generated family table: token, enclosing family, Lowered), the `MdPlatformIsa` intercept row keyed on the `ClassInfo.PlatformIsa` stamp, `runtime/core/dn2cpp_cpu_features.h` + `runtime/core/intrinsics/dn2cpp_cpu_features.cpp` (detection, AVX10.2 opt-in policy, the `DN2CPP_CPU_FEATURES` mask), `runtime/core/isa/` (tokens and per-family helpers, written by `tools/gen-isa-map`) |
| Multi-assembly | the `Module` abstraction + `ResolveTypeRef` (cross-assembly TypeRef/MemberRef resolution) |
| Exception handling | `MethodCompiler` (structural EH-region reconstruction → nested C++ try/catch, `isinst` dispatch) |
| vtables / interfaces | `Compilation` (slot assignment), `CppEmitter` (vtable/interface table emission) |
| Arrays | `runtime/core` (`Dn2CppArrayI4/Ref/N`), `MethodCompiler` (representation chosen by element type) |
| Godot bridge | `runtime/godot/dn2cpp_godot.cpp` (ClassDB registration, ptrcall, method binds) |

## 6. Verification gates

A PR that changes no Godot-specific files may merge after the strict non-Godot
pre-merge check passes in default Debug. Coding agents may run that check
autonomously for any change. Godot-specific changes require a human-run
Godot-inclusive pre-merge check; `AGENTS.md` defines the file scope. The
parallel runner provides smoke checks separately:

```bash
./gates/run-all-gates.sh               # all gates (pre-build once → parallel; Godot chained)
SKIP_GODOT=1 ./gates/run-all-gates.sh  # skip Godot for a faster smoke check
./gates/pre-merge.sh                   # the merge gate: Debug, no cache, Xcode gates host-selected
./gates/pre-merge.sh --skip-godot      # sufficient when no Godot-specific files change
./gates/pre-merge.sh --skip-xcode      # explicitly exclude Xcode-only gates
./gates/pre-merge.sh --require-xcode   # include Xcode gates on any host
CONFIG=Release ./gates/pre-merge.sh    # Release only
./gates/pre-merge.sh --both-configs    # Release then Debug, no cache

# the representative trio per lane (manual smoke check)
./gates/build-and-run-sample.sh        # console
./gates/build-and-run-multiassembly.sh # multi-assembly
./gates/build-and-run-godot-sample.sh  # Godot GDExtension (real engine)
```

Gate structure, the skip/partial protocol and the result-cache rules are in
`AGENTS.md`.

`build-and-run-preserve-control.sh` defaults to representative preservation
witnesses: independent positive and negative fixtures, selected native parity
checks, and sampled work-budget boundaries. Its graph receipts name the selected
cases and sharing modes. Set `DN2CPP_PRESERVE_CONTROL_FULL=1` to restore the
extended native, admission, publication-repeat and exhaustive budget matrices
when investigating preservation or reachability changes. A representative pass
does not claim those extended combinations; both profiles retain the managed
metadata and publication-contract checks they execute.

The retained managed `RuntimeTypeHandle.GetInterfaces(RuntimeType)` wrapper uses
native interface rows in their existing order. Formal parameters and compound
pointer/byref/function-pointer types return the precise `Array.Empty<Type>()`
singleton without constraint enumeration. Ordinary empty results share that
singleton; nonempty results own a fresh `RuntimeType[]`. The public enumeration
helper and raw QCall binding policy are unchanged. Opaque shells still expose
only retained interface rows; this bridge does not provide a new interface solver.
The native row order is not necessarily CLR interface-map order, and an open
generic shell can omit a substituted generic interface that a closed instance
retains. The wrapper preserves these existing limits rather than sorting rows
or inferring missing interfaces.

The retained managed `RuntimeTypeHandle.GetFields(RuntimeType, Span<IntPtr>, ref int)`
wrapper preserves its TypeDesc branch: a formal, pointer, byref or function-pointer
type writes zero to count and succeeds without reading the span. Ordinary types
explicitly refuse CoreCLR FieldDesc enumeration without changing the caller's
count or buffer; native FieldInfo rows are not VM descriptor pointers. Null type
and custom-header checks precede payload access. A null count address is a native
NRE safety boundary only on the successful TypeDesc branch. Span's existing
byref-like reflection Invoke refusal and raw marshaller/import policy remain
unchanged. NoAlloc accepts this exception-only helper under the existing policy,
not a promise that constructing an exception performs no allocation.

### Retained private process-exit orchestration

The exact managed CoreLib `AppContext.OnProcessExit()` hook throws
`PlatformNotSupportedException` when invoked: CoreCLR load-context, listener and
AppDomain shutdown orchestration is not a native runtime capability. Retaining
this private body does not register it with native process exit. No partial
callbacks run before refusal; existing native exit behavior and EventSource
observation refusals remain unchanged. This boundary does not provide public
`ProcessExit` or `AssemblyLoadContext.Unloading` delivery. Throw-only NoAlloc
admission follows the existing exception policy, not a physical allocation-free
exception guarantee.
