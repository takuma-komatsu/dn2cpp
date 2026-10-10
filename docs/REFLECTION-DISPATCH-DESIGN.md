# Reflection dispatch design

This document describes reflection and generic virtual dispatch contracts.
It records their dependency boundaries and acceptance criteria; the storage
implementation is documented in [ARCHITECTURE.md](ARCHITECTURE.md#reflection-metadata-representation).
Open implementation work remains in [STATUS.md](STATUS.md).

## Scope and baseline

Ordinary array operations, delegate invocation lists, argument validation,
field access, attribute queries and type identity keep the answers their
gate buckets pin; those buckets are the compatibility baseline.
Reflection discovery must retain only the additional types, boxes and bodies
that a runtime operation can actually select.

Ordinary virtual and generic virtual calls have call-site-driven
reachability and dispatch. Reflection extends this to method-definition
collisions, rows entered only through reflection, stripped implementations and
runtime-created generic receivers without weakening those existing calls.

The runtime supports native and packed metadata handles. Each native row, each
packed record inside a generated block's emitted method or constructor table and
each synthesized delta (a clone's method and constructor rows, and the rows a
clone level interns) publishes one immutable invoke plan, which every thread
reads in place; a local codec record is planned per call. Invocation descriptors
extend the information those rows carry and the bodies reflection can select,
and keep the lifetime rule published plans rely on: every row with a published
plan stays resident and unchanged for the life of the process.

Ordinary array provenance, delegate storage, the ordinary equality and ordering
predicates and global exception defaults are outside these contracts.
Runtime-created generic types, richer reflective binding and generic virtual
calls have separate capabilities with explicit support boundaries.

## Invariants

- A method's definition, closed declaring type, reflected type and selected
  dispatch row are separate identities. Shared code addresses do not identify
  methods or delegate bindings.
- Native rows, packed records and synthesized deltas must answer the same
  supported queries. A decoded temporary view cannot become a persistent handle.
- A stripped type or member must reach the existing metadata refusal path.
  A missing invoker cannot masquerade as an empty member set or a successful call.
- ILDiet arms on the predicates the reflection-invoke route arms on and retains
  the bodies that route reaches in the retained types, so stripping the managed
  input keeps every member of a retained type that a reflective invoke can
  select by name. A type that no retained body names is removed whether or not
  either arms (`src/ILDiet/README.md`).
- Signature decoding and type materialization may grow the model. Discovery
  must close those dependencies before emission freezes them; emission streams
  completed translation units and never retains the complete generated program.
- Runtime-produced boxes are allocation facts even when no IL `box` names them.
  Their Object slots, interface maps and array type identities must be reached
  through the same rules as ordinary boxes.
- Compiler and runtime schema changes land together. Preserve ordinary object
  headers, array graphs, typed interface owners, hot-update compatibility checks,
  deterministic pool indices and the supported self-host subset.

## Dependencies

| Contract | Covers | Depends on |
|----------|--------|------------|
| Row identity | Method definition keys, signature pass modes, generic-chain roots and selected class/interface row identity, which native and packed readers and the codec fixtures read alike. | The metadata handle API |
| Discovery closure | Reflection-created boxes, constructors, accessor bodies, delegate invokers and closed generic templates, closed with class routes, attribute routes and used/allocated dispatch in one bounded incremental fixpoint. | Row identity |
| Invocation contract | Target and argument validation, by-reference cells, value and pointer returns, copy-back by the declared pass mode and exception wrapping. Immutable plans hold no caller state. | Row identity and discovery closure |
| Generic virtual dispatch | Generic override and interface chains, and selection of the receiver's row with its closed context. Import identity and runtime dispatch agree across ordinary and hot-update calls. | Row identity and discovery closure |
| Reflective binding | Virtual methods and delegates bound by selected method identity, verified null-bound receivers and runtime-owned metadata answerers. | Invocation contract and selected dispatch |

An operation whose contract an image cannot meet is rejected explicitly; the
image never emits a placeholder that a runtime operation can bind.

## Discovery and termination

`Compilation` records triggers while scanning direct calls, method groups,
method specifications and framework bodies. Invocation, field-read, constructor,
attribute and delegate-binding triggers remain distinct; observing one must
not retain every framework body.

Field reads, constructor results, reflected method returns and delegate invokers
can introduce boxes. Discovery records those producer edges before applying
the existing Object equality and interface reach rules. It revisits only
owners or types introduced since the last stable scan, except when a widened
attribute or named-type policy requires a controlled rewalk. Classes and methods
that may grow during signature decoding are walked through snapshots or an
explicit cursor.

Runtime generic templates need a declared depth and shape boundary. Recursive
expansion must terminate with a supported template or a diagnostic before the
metadata and generic-context tables freeze. Strict completion checks must prove
that row construction, invoker rendering and generic-context emission cannot add
an undiscovered layout or body.

Structural boundaries leave a reflectable method without a compiled body where
.NET runs it. `MethodInfo.Invoke` and delegate binding refuse that missing body
with a shared descriptive `PlatformNotSupportedException`, naming the declaring
instantiation and member and explaining that the body was not compiled into the
image. This refusal is not wrapped in `TargetInvocationException`. A runtime
template method whose body calls through a function pointer over the template's
type parameter is not shared with a clone, since no clone can respell that
signature per type argument, so the clone has no body for it. The reflection-invoke
route walks a bounded number of the instantiations its bodies mint for each generic
definition, so a minted instantiation past that bound has no body for a method
that no call site reaches.

## Invocation and binding

A parameter's pass mode tells an ordinary value from a by-reference cell, an
unmanaged or function pointer and a by-ref-like value; `ref`, `in` and `out`
parameters share the by-reference mode. Runtime validation establishes target
compatibility and argument shape before entering generated code. Each
by-reference argument passes as a copy in its own cell. A call that returns
writes every cell back into the argument array, a value-type cell as a fresh
box and a reference cell as its reference; a call that throws writes nothing
back, and its exception reaches the caller wrapped in
`TargetInvocationException` unless the caller passed
`BindingFlags.DoNotWrapExceptions`.

An unmanaged pointer result, read through a by-reference result as well, returns
as a `System.Reflection.Pointer` of its pointer type, and a function pointer
result as an `IntPtr`. The compiler keeps the `Pointer` class only when a
reflectively invoked method or reflected field returns an unmanaged pointer. A pointer parameter
takes an `IntPtr`, or a `Pointer` of a type .NET passes to it: the same type,
a primitive pointee of the same width and kind one level deep, or any pointer
for `void*`. Pointer types differ at every level and function pointer types by
signature and calling convention. A function pointer type, and a pointer's levels
past those a return row's depth bits count, name a stand-in type-info that the
compiler interns per type and spells as .NET formats it.

Pointer field accessors use the same boxing and value validation, without wrapping
setter exceptions; a `void*` field also accepts `UIntPtr`. A static read-only field's
first store validates its value before refusal. Once its reflected getter has
read it successfully, later stores refuse before value conversion.

Immutable invoke plans may contain metadata, ABI descriptors and code pointers.
They must not retain a receiver, arguments, results or a receiver-dependent
interface implementation. Generated metadata images remain resident and immutable;
image replacement or unloading requires a separate invalidation contract.

Generic virtual dispatch selects by definition and closed-context identity,
never by a function pointer or display string. Hidden members, override chains,
interface implementation owners, boxed value receivers and hot-update patch
receivers must choose the same method under reflection and ordinary dispatch.
A hot-update import of a closed base-image generic virtual method binds the
call-site dispatcher the base image holds for that instantiation
([BPI-FORMAT.md](BPI-FORMAT.md#generic-methods)), and `--emit-patch` rejects a
patch type that declares a generic virtual method.

IL delegate equality compares the selected closed method, including its method
arguments, after matching the receiver and callable address. Emitted receiver
cases carry that key independently of reflection rows, so trimming and identical
code folding cannot merge distinct methods. Class slots, interface table order
and generic virtual branches choose the same declaration as ordinary dispatch;
runtime template owners normalize to the receiver's closed level. These equality
cases do not widen `Delegate.Method` metadata access or stripped-row refusals.
Existing String, Enum and intrinsic interface maps also supply selected keys.
Array interface keys use the real Array implementation for non-generic slots and
the existing closed wrapper's owner and method as a private SZArrayHelper identity
namespace for generic slots. That namespace follows the requested element argument,
including when native variance dispatch shares another element's callable body.
Runtime-created delegates without a static identity, patch receivers without a
recorded selection, and opaque owner bindings retain their existing compatibility
path.

A null-bound instance delegate may run only when verified IL proves that the
selected body does not require a receiver. That proof must cover the closed
signature and generic context. It cannot borrow an unrelated receiver or reuse a
proof from another row. Trap state, where required, must remain scoped to the
invocation and thread. A normal CLR fault must remain a managed fault rather than
native memory access.

Runtime-owned Object and ValueType members answer through the common handle
API. Each descriptor carries its parameters, attributes, virtual identity and
invoker callback together: a lookup matches its row by parameter types,
`Invoke` runs what a call of the member runs, and a delegate bound to a virtual
member names the override its receiver runs. A gated descriptor, one a derived
level can override or hide, is inherited only through levels whose rows cover
every method they declare under an Object member name; past any other level it
answers null, never a wrong method.

## Verification

New cases belong in the closest themed bucket, with invariant culture and an
unchanged old-output prefix. Run the relevant CLI build and individual gates;
Debug is required for signature contexts, shared generics and emission changes.

| Concern | Individual gates and evidence |
|---------|-------------------------------|
| Metadata and invocation | `build-and-run-reflect-invoke.sh`, `build-and-run-reflect-types.sh`; native/packed/explicit-format controls, codec boundaries, full exception fields and strict completion |
| Boxed and interface owners | `build-and-run-boxing-primitives.sh`, `build-and-run-shared-generics.sh`, `build-and-run-ambiguous-default-interface.sh`; isolated producer-only programs and closed value/reference contexts |
| Preservation and trimming | `build-and-run-preserve-control.sh`, `build-and-run-trim-reflection.sh`, `build-and-run-multiassembly.sh`; direct calls, method groups, attribute Type roots, constant-name lookups after ILDiet and stripped refusal paths |
| Hot update | `build-and-run-hotupdate-subset.sh`; import-shape and payload-version negatives, declaration fences and supported receiver/context controls |
| Allocation and self-hosting | `build-and-run-hotpath.sh`, `selfhost-emit-console.sh`; allocation refusal siblings, deterministic emitted output and console self-host emission |
| Host determinism | `verify-culture-invariance.sh`; pin both cultures and preserve complete build diagnostics |

Each new trigger needs an isolated fixture whose only producer is the operation
under test. A nearby IL box, `typeof`, direct call or unrelated attribute can hide
missing reachability. Delegate equality cases must include shared code with
distinct closed identities. Invocation cases must include native and packed rows,
throwing calls and positive siblings of every rejection.

Use .NET parity for behavior supported by the host. A host-unconditionally
unsupported shape needs an explicit structural boundary and native controls; a
skipped oracle is not a passing parity result. A change must not raise a
repeated-operation allocation budget in
`gates/expected/reflection-allocations.csv`; a first-lookup budget may grow only
with the descriptor that lookup allocates. Measure first and repeated operations
when changing plan construction.

A change to one of these contracts requires independent review of the selected
row and reach closure, passing relevant individual gates, and a regenerated
compiler/runtime pair. Source examples and metadata claims are evidence to
verify, not support promises to copy into the README. Existing limitations
remain open until their own behavior is implemented and verified.
