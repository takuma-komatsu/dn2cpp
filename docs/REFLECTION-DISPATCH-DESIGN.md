# Reflection dispatch design

This document proposes the next reflection and generic virtual dispatch changes.
It describes their dependency boundaries and acceptance criteria; the current
implementation is documented in [ARCHITECTURE.md](ARCHITECTURE.md#reflection-metadata-representation).
Open implementation work remains in [STATUS.md](STATUS.md).

## Scope and baseline

Ordinary array operations, delegate invocation lists, argument validation,
field access, attribute queries and type identity remain usable without enabling
this design. Their existing regression buckets are the compatibility baseline.
Reflection discovery must retain only the additional types, boxes and bodies
that a runtime operation can actually select.

Ordinary virtual and generic virtual calls already have call-site-driven
reachability and dispatch. The proposed changes cover method-definition
collisions, rows entered only through reflection, stripped implementations and
runtime-created generic receivers without weakening those existing calls.

The current runtime already supports native and packed metadata handles and a
bounded thread-local cache of immutable plans for generated packed method rows.
The changes below will extend the information those rows carry and the bodies
reflection can select. They will preserve the existing native fast path and the
cache admission and image-lifetime rules.

This work does not redesign ordinary array provenance or delegate storage,
replace the ordinary equality and ordering predicates, or change global exception
defaults. Runtime-created generic types, richer reflective binding and generic
virtual calls will be separate capabilities with explicit support boundaries.

## Invariants

- A method's definition, closed declaring type, reflected type and selected
  dispatch row are separate identities. Shared code addresses do not identify
  methods or delegate bindings.
- Native rows, packed records and synthesized deltas must answer the same
  supported queries. A decoded temporary view cannot become a persistent handle.
- A stripped type or member must reach the existing metadata refusal path.
  A missing invoker cannot masquerade as an empty member set or a successful call.
- Signature decoding and type materialization may grow the model. Discovery
  must close those dependencies before emission freezes them; emission streams
  completed translation units and never retains the complete generated program.
- Runtime-produced boxes are allocation facts even when no IL `box` names them.
  Their Object slots, interface maps and array type identities must be reached
  through the same rules as ordinary boxes.
- Compiler and runtime schema changes land together. Preserve ordinary object
  headers, array graphs, typed interface owners, hot-update compatibility checks,
  deterministic pool indices and the supported self-host subset.

## Dependency order

| Stage | Work and boundary | Required predecessor |
|-------|-------------------|----------------------|
| Row identity | Define method definition keys, signature pass modes, generic-chain roots and selected class/interface row identity. Extend native and packed readers and codec fixtures together. | Existing metadata handle API |
| Discovery closure | Close reflection-created boxes, constructors, accessor bodies, delegate invokers and closed generic templates. Integrate class routes, attribute routes and used/allocated dispatch into a bounded incremental fixpoint. | Row identity |
| Invocation contract | Validate target and arguments, materialize by-reference cells, marshal value/pointer returns, copy back according to the declared pass mode and preserve exception wrapping. Extend immutable plans without caching caller state. | Row identity and discovery closure |
| Generic virtual dispatch | Build generic override/interface chains and select the receiver's row with its closed context. Keep import identity and runtime dispatch coherent across ordinary and hot-update calls. | Row identity and discovery closure |
| Reflective binding | Bind virtual methods and delegates by selected method identity. Add verified null-bound receiver handling and runtime-owned metadata answerers only when their row and invocation contracts are available. | Invocation contract and selected dispatch |

Stages should be delivered as coherent compiler/runtime/fixture changes. A stage
must keep unsupported later stages explicitly rejected rather than emitting a
placeholder that a runtime operation can bind.

## Discovery and termination

`Compilation` will record triggers while scanning direct calls, method groups,
method specifications and framework bodies. Invocation, field-read, constructor,
attribute and delegate-binding triggers will remain distinct; observing one must
not retain every framework body.

Field reads, constructor results, reflected method returns and delegate invokers
can introduce boxes. Discovery will record those producer edges before applying
the existing Object equality and interface reach rules. It will revisit only
owners or types introduced since the last stable scan, except when a widened
attribute or named-type policy requires a controlled rewalk. Classes and methods
that may grow during signature decoding will be walked through snapshots or an
explicit cursor.

Runtime generic templates need a declared depth and shape boundary. Recursive
expansion must terminate with a supported template or a diagnostic before the
metadata and generic-context tables freeze. Strict completion checks must prove
that row construction, invoker rendering and generic-context emission cannot add
an undiscovered layout or body.

## Invocation and binding

Pass modes will distinguish ordinary values, by-reference inputs, outputs and
pointer-shaped values. Runtime validation will establish target compatibility
and argument shape before entering generated code. Copy-back behavior and
`TargetInvocationException` wrapping will be covered on both successful calls and
throwing callees, including aliases among by-reference cells.

Immutable cached plans may contain metadata, ABI descriptors and code pointers.
They must not retain a receiver, arguments, results or a receiver-dependent
interface implementation. Generated metadata images remain resident and immutable;
image replacement or unloading requires a separate invalidation contract.

Generic virtual dispatch will use definition and closed-context identity rather
than a function pointer or display string. Hidden members, override chains,
interface implementation owners and boxed value receivers must choose the same
method under reflection and ordinary dispatch. Hot-update payloads must reject
unsupported generic virtual declarations before publishing imports.

A null-bound instance delegate may run only when verified IL proves that the
selected body does not require a receiver. That proof must cover the closed
signature and generic context. It cannot borrow an unrelated receiver or reuse a
proof from another row. Trap state, where required, must remain scoped to the
invocation and thread. A normal CLR fault must remain a managed fault rather than
native memory access.

Runtime-owned Object and ValueType metadata answerers will use the common handle
API. Expanded descriptor fields, virtual identity and invoker callbacks must be
introduced together; ordinary parameterless fallback rows remain the baseline
until that contract is complete.

## Verification

New cases belong in the closest themed bucket, with invariant culture and an
unchanged old-output prefix. Run the relevant CLI build and individual gates;
Debug is required for signature contexts, shared generics and emission changes.

| Concern | Individual gates and evidence |
|---------|-------------------------------|
| Metadata and invocation | `build-and-run-reflect-invoke.sh`, `build-and-run-reflect-types.sh`; native/packed/explicit-format controls, codec boundaries, full exception fields and strict completion |
| Boxed and interface owners | `build-and-run-boxing-primitives.sh`, `build-and-run-shared-generics.sh`, `build-and-run-ambiguous-default-interface.sh`; isolated producer-only programs and closed value/reference contexts |
| Preservation and trimming | `build-and-run-preserve-control.sh`, `build-and-run-trim-reflection.sh`, `build-and-run-multiassembly.sh`; direct calls, method groups, attribute Type roots and stripped refusal paths |
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
skipped oracle is not a passing parity result. Preserve existing allocation
limits and measure first and repeated operations when changing plan construction.

Completion of a stage requires independent review of the selected row and reach
closure, passing relevant individual gates, and a regenerated compiler/runtime
pair. Source examples and metadata claims are evidence to verify, not support
promises to copy into the README. Existing limitations remain open until their
own behavior is implemented and verified.
