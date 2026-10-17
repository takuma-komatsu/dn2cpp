# ILDiet

ILDiet removes unreachable managed definitions before dn2cpp constructs its
transpiler model. Mono.Cecil writes the retained load set; input files remain
unchanged. Pass the complete reference set with repeatable `-r` options. ILDiet
never resolves references from installed frameworks or a NuGet cache. dn2cpp
resolves that set before invoking the companion.

```sh
dotnet run --project src/ILDiet -c Release -- \
    App.dll -r Library.dll -r System.Private.CoreLib.dll \
    --link-xml link.xml -o stripped
```

Read `stripped/ildiet-result.xml` for the generated input, reference and
preservation paths. Assemblies live in the generation named by that manifest;
there are no flat DLL outputs. A manual transpile must pass those ordered paths,
`--no-ildiet`, and `--link-xml` with the manifest's preservation path.

`--project-root` discovers files named exactly `link.xml`, excluding `bin`,
`obj`, `.godot` and `.git`. `--link-feature` enables `com`, `sre` or `remoting`.
The shared preservation reader resolves descriptors and `PreserveAttribute`
against original metadata. `preservation.xml` exports those original policies as
signatures, including conditional rules. Backend and reflection roots belong to
the request contract and do not become synthetic preservation rules.

An assembly-wide `preserve="all"` directive copies the original assembly bytes
without serialization, including when the assembly is otherwise rewritable.
The empty assembly element's default `all` has the same effect after feature and
reference conditions are resolved. Exported preservation retains this directive
for replay. Type-level preservation does not request assembly copying, and
unsupported IL or metadata does not automatically trigger it.

`--ildiet-skip-assembly <name>` copies a loaded assembly unchanged without adding
transpiler executable roots or exported preservation rules. Repeat it for multiple
assemblies, using exact simple assembly names; unknown names are errors. Existing
copied-body dependency closure still protects references into rewritten assemblies.
Captured requests retain this option; manual re-stripping must pass it again. The
option is inert when dn2cpp does not run ILDiet. Unlike assembly-wide
`preserve="all"`, it does not request that the transpiler retain every body.

## Retention contract

The graph has metadata, runtime, members and explicit-root states for each type,
and metadata and body states for each method. State only grows until the plan
barrier. A definition's signature and body are each decoded at most once;
promotion from metadata to body applies only the additional body dependencies.
Every IL instruction contributes its operands, including instructions in dead
branches. Body promotion also activates the method's compiler state-machine type
arguments; lookup-only methods keep those declarations without their executable
state-machine dependencies. Analysis never edits method bodies.

Ordinary metadata retention keeps declaring types, bases, interfaces, generic constraints,
field layouts, custom modifiers, marshaling and attribute dependencies. A retained
application type keeps its declared members' metadata. A metadata-only method
with an original body receives a marked throwing stub. dn2cpp rejects admitting
that stub with an invariant error naming the method. Property rows and accessor
selections are independent: selecting a missing setter can retain an accessorless
scalar property without preserving the getter's executable body. Exported
preservation keeps that selection for replay. Removing every accessor from an
indexer fails original-signature validation.

Runtime retention adds type initializers and virtual, override, interface and
delegate bodies. It follows executable type references and explicit runtime
roots. A signature alone does not promote a type to runtime. Static initializers
retain their original bodies or are removed, except that an admitted
attribute-provider row may retain an otherwise-unused initializer as a metadata
stub.

Executable input roots include the entry point, application static initializers,
module initializers, native exports and preservation. A library input retains
its public application surface. Backends can suppress default seeds for selected
types and supply their own entry contract. Ordinary `--cut` targets keep their
original metadata, bodies and physical token closure after validation on the
original load set. A cut alone supplies no runtime or executable attribute
authority. Later calls, roots and preservation retain their ordinary metadata
effects, but do not analyze the cut body's executable dependencies or reflection.

Generic dependencies bind definition parameters to argument shapes and propagate
finite demands for runtime types, constructors, member rows, executable methods
or accessors. Binding identities preserve each parameter's declaring scope inside
nested array and generic shapes. Generic parameters forward demands to their
bindings without constructing a closed caller-context graph. Explicit MethodImpl declarations
connect generic slot parameters to implementation parameters even when their
method names differ. Structural reflection on symbolic types records finite
projections over existing and later bindings, so base, element and generic
argument queries can forward their demands without closed caller contexts.
Generic definitions in copied assemblies retain opaque argument demands: their
arguments keep the conservative data surface needed by factories and serializers.
Executable reflection demands follow their generic bindings into copied owners,
including constructors, methods, accessors and field-read boxing. Metadata-only
demands do not enumerate copied framework member bodies or construct attributes.
Ordinary method tokens retain signatures and generic bindings without scanning
the named body or arming its call capabilities; calls and function pointers
retain bodies.
Copied assemblies with a direct reference to a stripped assembly conservatively
seed metadata for all original types and bodies for all original methods. Copied
assemblies without such a reference contribute call, initializer, delegate and
reflection effects through reached method bodies and explicitly preserved
members; their unreached helpers do not arm capabilities. Runtime type maps are
shared with admission; intrinsic-only implementation bodies stay opaque while
managed-body families and interface implementations can contribute dependencies.
Non-framework copied modules retain their module initializer and unmanaged entry
points. Copied assemblies remain byte-identical. ILDiet does not use the
transpiler's generic nesting or instantiation limits.

Original model observation is separate from these use demands. It can remain
dormant while the load set has no non-framework generic definitions or generic
preservation demands, Array.Initialize is unarmed, and ordinary use has not needed
generic types, methods, parameters or projections. Before such a consumer
proceeds, the full original model graph is initialized and its model queue is
drained. Every loaded non-generic definition then contributes its base and
interface relations, including framework definitions. Formed generic definitions
recursively contribute those relations and nested arguments through the same
finite binding graph. Formation bindings form a complete union; ordinary use
has a separate monotone subset. Only ordinary bindings replay executable demands,
reflection, projections and runtime-layout consumers. Definition and MemberRef
parameter aliases preserve that distinction, including a later ordinary use of
an already observed formation pair. Passive metadata traversal records its own
pairs so a later independent use can authorize them without rescanning bodies.
Actual MethodImpl resolution and inherited virtual-slot matching observe their requested
signatures; ordinary unused fields and nonvirtual method signatures stay lazy.
Generic intrinsic shells stop their own shape closure, while their argument
formations remain visible. Element-sensitive vector mappings distinguish
primitive elements from real managed layouts. Custom async adoption observes
real shape and possible struct-task backing; an original witness rejected under
every adoption role excludes backing for its entire task assembly. The primary
still decides adoption using its original options. Consequential backing retains
original candidate predicates through shared role rows and task-assembly
coupling, including candidates enabled only when another assembly opts out.
Every original cross-module contract MemberRef for those roles survives, even
when accepted by the default contract. Any passive type or method-owner
restoration that touches an original role or serialized builder lookup activates
this protection, including real-shape sources without possible generic backing.
Earlier passive rows are replayed and incidental rows join this closure;
serialized builder names retain their original first lookup winner so restoring
a later definition cannot turn a failed original predicate into a new candidate.
The finite closure preserves original candidate, role, and method order without
enumerating option subsets. An IL carrier keeps its original body as passive
evidence, separately from executable method retention; its tokens close without
arming calls, reflection or runtime use. The shared body cache records Cecil
operands, locals and catches once. Only executable body admission classifies
reflection calls and prepares a summary, including after a passive carrier scan;
unsupported executable IL retains its ordinary diagnostic. A byte-prefilter hit
without a matching token operand grants no retention or reflection authority. A required reference with no preservable original carrier is
rejected instead of synthesizing metadata.

An observed formation that supplies attribute identity, an array initializer,
or generic preservation retains its original source relations and signature
triggers. This source retention closes original metadata tokens without promoting
the source to runtime use, decoding its ordinary field/signature types as model
formations, or running its initializer. Later independent use follows the normal
graph. Eligible array initializers, effective generic preservation and admitted
attribute identity effects authorize their required formal dependencies. This
promotion follows nested references and later bindings; retaining a provider or
an async contract alone supplies no ordinary authority. The published primary
recreates consequential specializations from those
original rows and the original preservation descriptor; no synthetic roots are
added.

Attribute reads on known copied owners select original providers and rows. Data
reads retain metadata; instance reads execute selected constructors and named
setters. Only type owners inherit rows from their base chain. Framework rows
require a reached user-module type token naming their attribute class. Naming
and later generic bindings replay the pending selections without constructing
data-only rows or executing the owning member. Direct constructor calls retain
their normal body edges.

An unknown attribute owner with a known filter does not discover unrelated
providers or execute their constructors and named setters. Ordinary attribute
inventory, known-owner queries, copied-IL safety and explicit preservation keep
their metadata and identity effects. A data query on a known owner observes row
identities and Type-valued arguments without constructing attributes; an instance
query on that owner can upgrade the selected rows. Unknown generic argument
vectors similarly select no executable definition or member. Finite known
alternatives and independently reached bodies keep their ordinary closure.
Selected metadata rows retain their original constructor, named fields and
properties, and accessor signatures without requiring executable bodies. Their
transitive attribute metadata is passive: its discovery does not itself count
as an attribute query or activate conditional preservation. Original row
inventories cover non-generic user definitions, observed generic formations, and
loaded assembly/module providers. A class inventory includes fields, every
original method and its parameter/return rows, and properties, even when the
member body is unused. App owners, reached member bodies and actual allocation
carry unretained rows; an independently reached attribute constructor also admits
its rows on modeled library owners. Framework attribute eligibility still applies.

Admitted rows separately expose their attribute-class and argument identities.
Identity-only references can activate explicit conditional preservation without
an attribute read; a later read upgrades argument observation without selecting
a factory. The original provider metadata survives so the published input retains
that conditional evidence. An admitted initializer attribute row can retain its
MethodDef with a stub; ordinary initializer metadata still requires a body or is
removed. Identity observation never constructs an attribute or executes its
accessors. Independently reached bodies and backend attribute reads retain their
executable dependencies. Backend registration arrays expose only their effective
entries throughout metadata retention, identity observation and final validation.
Independent runtime use replays the row when it admits another entry; a filtered
script cannot retain itself through its original registration array.

Preservation contributes executable dependencies independently of copied payload
bytes. Conditional rules activate when an owner is otherwise referenced, forced,
used at runtime or selected by an explicit reflection root. Named and generic type
formation, listed members and preserved signatures carry these identity demands
through later bindings. Copying an assembly or discovering its policy does not
activate the rule.

## Explicit rules

- Allocation, boxing, initialization and static field use retain runtime type
  dependencies. A method body retains its declaring type at runtime.
- Delegate binding retains the target body, except for recognized deferred
  constructor-registry entries. Named binding also selects matching constructors
  and static initializers along the owner's base chain, including private
  constructors. Delegate types retain their methods' bodies.
- Generic construction demands the public parameterless constructor of each
  bound argument. A generic-parameter argument forwards that demand.
- Ordinary construction selects only the requested owner's instance constructors.
  Construction flags normalize to admission's visibility and instance defaults.
  Selected bodies retain the base constructors their original IL calls.
- `Array.Initialize`, including a method group, retains the parameterless
  constructors of user value types the primary can model: non-generic definitions,
  runtime-used generic definitions, and generic layouts observed in signatures,
  bindings, original eager relations or reflection formation, including copied user libraries. Later
  layout and runtime observations replay the same rule.
  Metadata for a bare generic definition or reference-type field does not open
  bodies.
- Constructed attributes keep their constructor and named field or
  property-setter dependencies along the attribute's base chain. Attribute rows
  retained only for a selected metadata read do not execute their factories.

Reflection can box a field value, an invocation result or a by-reference argument
without an explicit IL boxing instruction. These paths retain the payload's
runtime dispatch after unwrapping nullable and by-reference types and propagating
through generic bindings. Non-generic array searches retain dispatch for recorded
array element types. When such a search can receive runtime-constructed arrays,
or `RuntimeHelpers.Box` can box an emitted layout directly, the closure also
covers metadata-retained value types, including observed copied user layouts.
Original base, interface and field layout edges propagate this observation;
late layouts and late capabilities replay the same finite demand. Unused copied
definitions do not participate. This fallback promotes value-type dispatch
without registering reference-type scripts.

Array signatures preserve vector versus multidimensional kinds, including
rank-one arrays without explicit bounds inside other signatures. An explicit
zero lower bound preserves Cecil's `ARRAY` encoding without changing CLR type
identity.

## Recognized and unknown reflection

ILDiet shares `ReflectionApis` and `ReflectionSummary` with dn2cpp. Each site's
summary selects its own metadata rows or executable dependencies. Identity and
lookup operations keep metadata. Explicit static-initializer selections,
including `TypeInitializer` and constructor or member queries, keep the requested
type's original initializer body. Ordinary member-row retention does not keep it;
the passive attribute-provider exception preserves only the original row.
Execution keeps selected bodies; a known owner with an unknown member keeps the corresponding
member kind conservatively. General and default-member selection covers the
corresponding member kinds, including fields, events and nested types. Known
compiler-generated closure objects retain their type identity for named delegate
binding.
An assembly enumeration with a known assembly keeps that assembly's type rows.
`InvokeMember` retention follows possible binding-flag operations, with default
visibility and storage flags matching admission. A possible construction ignores
the member name. Symbolic owners forward the corresponding constructor, member
and boxing demands conservatively. Boxed nullable object types normalize to
their underlying payload before member lookup, including generic payloads and
symbolic bindings.
Generic-parameter targets record demands that flow to their argument bindings.
Interface enumeration follows base classes and inherited interfaces with their
generic bindings. Generic-argument arrays preserve ordered positions, including
repeated and symbolic arguments, when used to construct generic types or methods.
When a member's owner remains symbolic, execution conservatively propagates
boxing demands for the bound field, result and by-reference parameter surfaces.
Assembly enumeration through a symbolic owner covers the type definitions in
each bound owner's module. Symbolic nested-type lookup covers its immediate
nested definitions; symbolic interface lookup covers its inherited interface
closure. Requested names do not narrow these deferred nested and interface
supersets. Their targets stay within the bound owners' original metadata.

An unknown type selection, such as a runtime string passed to type lookup,
retains metadata and members of every stripped type. An unknown member selection
on an unknown owner keeps member rows of all metadata-retained types, including
new types reached through those signatures. These scopes add neither runtime
state nor executable bodies. Unknown execution adds no body; dn2cpp's warning
and preservation remedy still apply.

A reflection root keeps the selected type's members and explicit-root state,
and member metadata on its ancestors and interfaces. It does not retain member
bodies. A full `<root assembly="..." type="..." />` retains the entire type
surface. Explicit roots can retain concrete wrapper factories without adding a
script registration. Recognized reflection and explicit preservation must have
the same observable behavior with and without ILDiet; DLL contents need not be
identical.

## Fallback

Graph transitions, dependency edges, demands and scope expansion consume a
deterministic work budget derived from the original metadata. Standalone
`--work-budget <n>` and request `workBudget="n"` set a positive explicit budget.
A summary's local budget produces an unknown summary; it does not by itself
trigger whole-load-set fallback.

Exhausting the graph budget copies the entire resolved load set byte for byte.
Hot-update copy-all requests and generic `--cut` selectors that require model
specialization also copy the original set. Roots, descriptors and policies are
validated before these paths publish. Fallback exports original preservation
only and changes neither registries nor registration attributes. Other input
errors and broken invariants fail the run. No fallback occurs after plan apply.

## Backend policy and registries

Backends declare rewritable assemblies, type and method roots, conditional
member retention, suppressed default seeds, registration attributes and
constructor registries. Engine resource parsing stays in the backend.

A recognized factory's constructed type at runtime or explicitly rooted retains
that factory. Deferred entries without a compatible retained ancestor factory
remain original; closure repeats until no new factory body is needed. The plan
then redirects eligible entries while preserving engine class-name keys.
Unknown factory shapes retain their original dependencies. Script-registration
`Type[]` values contain runtime-used types only; metadata and reflection-root
retention alone do not register scripts. Diagnostics identify conservative
factory keeps.

The result records whether each assembly's registry changed. The consumer
validates and passes this state to the backend for contract diagnostics. These
values never enable or suppress emission-stage trimming.

A cut factory keeps its original registry entry without making its body
executable. Its retained original method can also serve as a compatible ancestor
redirect target; arbitrary passive evidence does not make a factory eligible.
A cut registry body keeps its original tokens without executing registry rules.

## Output generations and publication

A nonempty output directory must carry ILDiet's ownership marker. Outputs are
published as immutable generations:

```text
stripped/
  .ildiet-output
  ildiet-result.xml
  g-<hash>/
    App.dll
    Library.dll
    preservation.xml
```

After the graph freezes, ILDiet builds and validates a complete plan before
applying changes. It writes a stage directory, rereads assembly identities and
hashes the staged bytes. The generation hash covers ordered filenames and
contents, including preservation. Hashing uses portable FNV arithmetic shared
with the consumer; it requires no cryptography implementation. Existing identical
generations can be reused; modified contents are rejected.

Renaming the stage publishes the generation. Atomic replacement of
`ildiet-result.xml` publishes its pointer, so a failure before that replacement
leaves the preceding pointer and generation intact. `--result` writes an
additional manifest with an atomic file replacement. Obsolete generations,
stages, temporary files and legacy flat outputs are cleaned up after publication.

Rewritten assemblies retain identities, public keys and resources, omit PDBs,
and clear strong-name signatures. ILDiet never signs them; they are dn2cpp
inputs rather than signed distribution assemblies.

## Protocol v2

`--request <request.xml> --result <result.xml>` accepts a strict request:

```xml
<ildiet version="2" input="App.dll" output="stripped" workBudget="100000">
  <reference path="Library.dll" />
  <reflectionRoot type="App.LookupTarget" />
  <root assembly="Library" type="Library.Exported" />
</ildiet>
```

Full roots require an assembly. Reflection roots have their own element. Unknown
attributes, elements, versions and invalid budgets fail. The result's
`version="2"` manifest gives the generation, ordered paths, preservation,
cut-validation state, fallback reason, contract digest and per-assembly mode,
content hash and registry-rewrite state. The digest identifies the canonical
backend/reflection contract independently of output paths and budgets. The
consumer verifies the generation, hashes, copied bytes, identity, load order and
registry consistency before loading any output.

## Counters

A deterministic `ILDiet: counters` line follows the removal summary.
`methods.body` counts executable body retention; `methods.evidence-body` counts
additional original bodies retained for passive adoption evidence or validated
cuts. Evidence bodies are neither stubs nor removed methods. Later executable
promotion of an uncut body moves it into `methods.body` without decoding its
facts again; a cut continues to exclude executable body analysis. The same
named counters appear in the result manifest: type and method states, signature
and body decodes, summary outcomes, reflection and generic demands, conservative
scopes, work and budget, fallback, and factory and registration decisions.
`decode.max-per-method` exposes repeated-body-scan violations. Counters contain
no elapsed timings.

## Native companion probe

The companion uses BCL operations supported by dn2cpp's self-hosted path. Its
Cecil dependency is confined to the companion, so the native dn2cpp executable
does not load Cecil.

```sh
bash gates/ildiet-native.sh emit
bash gates/ildiet-native.sh build
bash gates/ildiet-native.sh verify
```

The probe retains build, emission and execution logs under `artifacts/`. It cuts
Cecil's signing-only `CryptoService.GetPublicKey` and `CryptoService.StrongName`
branches; deterministic MVID hashing remains enabled. The build stage uses the
shared CMake/Ninja wrapper and compares managed and native help. Verification
checks direct and protocol requests, generated DLL bytes, preservation,
normalized manifest paths, hashes and repeated-run determinism. A signed fixture
checks identity and public-key retention with the signing flag cleared.
