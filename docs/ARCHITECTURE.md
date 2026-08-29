# ARCHITECTURE — dn2cpp internals and extension points

A contributor-facing document showing **where to add what**. For the big picture
see `README.md`, for working conventions `AGENTS.md`, for the open backlog
`docs/STATUS.md`.

---

## 1. Pipeline

```
input .NET assembly (+ reference assemblies via -r)
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

Core invariants:

- **Explicit vtables** (no C++ `virtual`): dispatch goes through the
  `Dn2CppTypeInfo` at the head of every object.
- **The evaluation stack is single-assignment temporaries**, spilled to per-depth
  normalized variables at basic-block boundaries so control-flow joins agree.
- **Generation is deterministic**: same input, same output. Nothing in the
  environment may change the C++ a successful transpile emits — the self-host
  fixpoint depends on it.

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

- `WantsSyntheticBody` — hot-update base only: give a skipped-body method a real,
  invoker-compatible body rendered from the intrinsic's own call lowering.
- `HasPlaceholderBody` — body emitted but dead (every call site lowered inline);
  a hot-update base excludes such methods from the reflection fnPtr/invoker
  wiring, so a patch import fails loudly instead of binding the placeholder.
- `ExternallyAllocatedClasses` — types the target instantiates outside the C#
  call graph (Godot's ClassDB create-instance path runs no `newobj`), so their
  dispatch and ToString/Equals/Finalize wiring is not silently unwired.
- `AdditionalRootMethods` — methods the epilogue calls directly, seeded as
  reachability roots. Called once, before the first reachability drain and so
  before `EmitEpilogue`: a backend may resolve and cache what its epilogue names.
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

- `TypeDesc` — a type (Primitive / Class / External / SZArray / ByRef /
  GenericVar / Template). `Substitute(GenericContext)` substitutes type arguments.
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
reflection-usage marks (`Invoke`, `GetValue`, `SetValue`, `CreateInstance`,
`GetCustomAttributes`, `GetCustomAttribute`, `IsDefined`), which neither cut nor
route but *open* a reachability route so a reflected member stays invokable —
they ride in `CoreIntrinsics.ScanNeedsParentTypeName` as a hand-written residue
beside the scan rows' names, and deriving them away leaves a green transpile and
a shipped game that throws on its first `PropertyInfo.GetValue`.

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
  the bytes; the native gates diff it against real .NET, so a wrong lowering
  fails on the machine that has the instruction rather than in a user's program.
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
- **The mask intersects detection.** `DN2CPP_CPU_FEATURES` narrows the detected
  set (the effective set is the implication closure of detected ∩ allowed) and
  can never make a getter true that detection left false.

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
(`Compilation.DeclineOutOfContractAdoptions`) declines the adoption of any
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
consults after the type's own rows miss. Four rules: synthesized rows stay out of
`GetMethods()`; the answer comes from layout reasoning, never from `instanceSize`
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
  corresponding IL instruction. Recording arms only when a method opts in, and
  verification runs after the emit fixpoint, failing with a
  `NotSupportedException`.

Auto-`noexcept` is a closed non-goal, not a missing knob; the carve-out is in
`docs/STATUS.md`.

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
| Runtime-instantiation templates (`MakeGenericType` over a typeof-only definition, any argument) | `Compilation.BuildRuntimeInstantiationTemplates` (trigger + `$CnAny` rooting + shape bound), `Compilation.JudgeRuntimeTemplates` (per-body/per-slot eligibility), `CppEmitter.EmitRuntimeTemplates` (the `dn2cpp_runtime_templates` rows), `runtime/core/intrinsics/dn2cpp_system_reflection.cpp` (`dn2cpp_try_synthesize_generic`: clone, intern, rgctx fill) |
| Reachability tree-shake | `Compilation` (walks call/newobj/ldftn from roots). Unreached bodies are never decoded |
| Const-folded capability getters + dead-arm pruning | `CoreIntrinsics.ConstFoldedGetter` (the oracle: add a `(type, method) → bool` entry), `BranchLiveness` (per-body live-offset walk shared by the reachability scanner and `MethodCompiler`, so a dead arm's icalls/P-Invokes never enter the tree), `Compilation.ResolveCallTarget` (the getter's own body edge is cut) |
| Platform ISA contract (`X86.*` / `Arm.*` / `Wasm.*` `IsSupported` and instructions) | `src/Dn2Cpp.Transpiler/CoreIntrinsics.PlatformIsa.g.cs` (the generated family table: token, enclosing family, Lowered), the `MdPlatformIsa` intercept row keyed on the `ClassInfo.PlatformIsa` stamp, `runtime/core/dn2cpp_cpu_features.h` + `runtime/core/intrinsics/dn2cpp_cpu_features.cpp` (detection, the `DN2CPP_CPU_FEATURES` mask), `runtime/core/isa/` (tokens and per-family helpers, written by `tools/gen-isa-map`) |
| Multi-assembly | the `Module` abstraction + `ResolveTypeRef` (cross-assembly TypeRef/MemberRef resolution) |
| Exception handling | `MethodCompiler` (structural EH-region reconstruction → nested C++ try/catch, `isinst` dispatch) |
| vtables / interfaces | `Compilation` (slot assignment), `CppEmitter` (vtable/interface table emission) |
| Arrays | `runtime/core` (`Dn2CppArrayI4/Ref/N`), `MethodCompiler` (representation chosen by element type) |
| Godot bridge | `runtime/godot/dn2cpp_godot.cpp` (ClassDB registration, ptrcall, method binds) |

## 6. Verification gates

A change is DONE only when the full gate suite (`gates/build-and-run-*.sh`)
exits 0. Run it via the parallel runner:

```bash
./gates/run-all-gates.sh               # all gates (pre-build once → parallel; Godot chained)
SKIP_GODOT=1 ./gates/run-all-gates.sh  # skip Godot for a faster smoke check
./gates/pre-merge.sh                   # the merge gate: Release + Debug, no cache, no skips

# the representative trio per lane (manual smoke check)
./gates/build-and-run-sample.sh        # console
./gates/build-and-run-multiassembly.sh # multi-assembly
./gates/build-and-run-godot-sample.sh  # Godot GDExtension (real engine)
```

Gate structure, the skip/partial protocol and the result-cache rules are in
`AGENTS.md`.
