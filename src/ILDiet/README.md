# ILDiet

ILDiet removes unreachable managed type and method definitions before dn2cpp
constructs its transpilation model. It uses Mono.Cecil to write new assemblies;
input files are never modified.

Rewritten signatures retain the distinction between vectors and multidimensional
arrays, including rank-one arrays without a lower bound inside other signature
shapes. ILDiet encodes those arrays with an explicit zero lower bound so Cecil keeps
their `ARRAY` kind without changing their CLR type.

```sh
dotnet run --project src/ILDiet -c Release -- \
    App.dll -r Library.dll -r System.Private.CoreLib.dll \
    --link-xml link.xml -o stripped

dn2cpp stripped/App.dll -r stripped/Library.dll \
    -r stripped/System.Private.CoreLib.dll \
    --no-ildiet --link-xml stripped/preservation.xml -o generated
```

Pass the complete managed reference set with repeatable `-r` options. ILDiet
does not resolve assemblies from the machine's installed frameworks or a NuGet
cache. dn2cpp's automatic preprocessing resolves its reference set before
invoking ILDiet and passes every output assembly to the transpiler.

`--link-xml` accepts an explicit Unity-format descriptor. Repeatable
`--project-root` searches for files named exactly `link.xml`, excluding `bin`,
`obj`, `.godot`, and `.git`. `--link-feature` enables `com`, `sre`, or `remoting`.
Descriptors and `PreserveAttribute` use dn2cpp's shared preservation reader.
The generated `preservation.xml` carries applicable descriptor rules into a
subsequent manual transpile; include it even when the required methods already
exist in the stripped DLLs.

Executable assemblies retain their entry point and application static
initializers. Non-framework module initializers, native exports, managed native
implementation adapters, and explicit preservation are roots. A library without
an entry point retains its public application surface. Unreachable executable
method bodies remain eligible for removal.

Every retained application type keeps its declared member metadata. Unreached
methods retain signatures and a throwing stub without retaining their original
body dependencies. Compiler state-machine Type arguments retain declarations
without rooting `MoveNext` or its body dependencies. Other scalar Type attribute
arguments retain their runtime roots. A later call or reflection-invoke root retains the original
body before stubs are written. Constant-name `GetMethod`, `GetMethods`,
`GetProperty` and `GetField` queries therefore match the unstripped application
without requiring an invoke. Event declarations and accessors also survive in
the rewritten DLL; native event lookup still requires an event metadata and
handle representation that dn2cpp does not supply.

`FieldInfo.GetValue` boxes user value types declared by retained application
fields. Their virtual dispatch bodies survive even when the field owner or its
value type was retained only through member signatures, including user-library
value types. Closed signature owners substitute their field type parameters
through application base types, reference field owners and array-shaped generic
arguments. Field type shapes carry closed owner contexts through array elements
and generic arguments, including arguments of framework containers. Reference
owners carry metadata context without opening their virtual bodies. Closed
contexts deduplicate by resolved argument identity, allowing finite cycles while
emission's reflection route bounds limit growing contexts. Ordinary type
instantiations from every module and generic method instantiations seed the
walk depth; a method adds its own instantiation level. Reached user bodies carry
closed caller type and method argument depths through their IL operands without
opening additional bodies. Depth contexts deduplicate within the operator's
nesting and record budgets. Exhaustion aborts before rewriting rather than
retaining a partial summary. Fatal diagnostics name the target and its recorded
executable caller chain. The summary counts depth-context records separately
from emission's type and method instantiations; changing a budget cannot change
a successful rewrite. Copied framework bases and interfaces contribute their substituted shape
depths without opening method bodies. Retained virtual bodies receive closed
receiver contexts and generic dispatch arguments even when the call names an
object or interface slot. Inherited bodies receive the substituted base receiver
context, including receivers allocated by closed caller bodies. Caller type and
method argument depths propagate as vectors. Symbolic receivers combine their
vectors with generic slot arguments for retained implementations without becoming
closed type identities. Depth-only receiver and slot joins settle before the
application-signature snapshot regardless of their discovery order.
Receiver propagation enters retained virtual bodies; non-virtual callees receive
contexts from their ordinary calls. Dispatch signatures substitute the closed receiver and inherited slot
owner before comparing parameter types. Nullable fields retain
the underlying value type's dispatch. Closed application member return and
parameter types are substituted once over the retained owner contexts and their
closed application shapes, including base types, interfaces and their generic
arguments, matching emission's shape and signature closure. Newly
named contexts participate in armed field, constructor and invoke routes without
recursively opening further member signatures.
Closed owners admitted by the bounded shape walk also join this snapshot without
raising the depth seed for subsequent walks or opening their bodies.

Once a retained body calls or binds `MethodBase.Invoke`, `PropertyInfo.GetValue`,
`PropertyInfo.SetValue`, `EventInfo.AddEventHandler`, `EventInfo.RemoveEventHandler`
or `CreateDelegate`, directly or through a method group, or a copied assembly
references one of them (framework event handlers and `CreateDelegate` excepted),
ILDiet retains the ordinary application type definitions and their non-constructor
bodies, matching emission's armed application route. Generic definitions already
named by retained signatures keep the same bodies. Retained closed application
signatures promote their generic arguments as runtime dependencies, preserving
dispatch when invocation boxes a result. This does not create new closed AOT
instantiations. Every library type a retained body names with `typeof` then
retains its instance property accessors and its public parameterless non-generic
instance methods, except in an engine binding that a backend rewrites, which
generated trampolines invoke instead. Unnamed library types and library members
outside that surface still require a descriptor or `PreserveAttribute`.
Backend exclusions from default application roots remain in force.

Depth summaries follow executable calls, dispatch slots and allocation hooks.
A nonvirtual call keeps its exact target. Virtual call/delegate-slot opcodes
join allocated receivers. Base constructor calls do not allocate the base type,
and receiver lookup selects the most derived implementation. Abstract types
retain their required constructor metadata and bodies without becoming allocated
receivers. Depth summaries share emission's branch graph and boolean capability
getters. Loaded exact type identities can prune dead branches; open, unresolved
and canonical identities keep both branches live. Explicit MethodImpl mappings
precede implicit interface matches. Only a level that lists an interface maps it
by a public virtual name match; derived overrides follow the selected class slot.
Newslot hiders keep separate slot identities. Primitive constrained calls bind directly
without joining unrelated receivers. Interface instance virtual bodies need a
receiver slot rather than a whole-class reflection depth root.
Generic data closures and accessor/event association retain original bodies
without supplying executable depth roots. A later actual call or reflection
construction route promotes the already retained surface. Whole-class reflection
roots share emission's fixed first-walk depth and bounded minted-context admission;
the calls inside admitted bodies still obey the fatal operator budgets.
Conservatively retained virtual and interface bodies keep their original IL
without contributing caller instantiations until an execution root reaches them.
Their closed type references remain available without consuming copied-shape
depth budgets; executable caller contexts record those shapes independently.
Reflection retention and executable reflection depth roots remain separate.
Attribute rows retain original constructors and named accessors without making
those bodies executable. A live attribute-reading descriptor promotes eligible
user-element and assembly rows' constructors and the first named setter on the
attribute's base chain; named getters remain conservative. Framework attribute
rows join only when executable user IL names their type, matching emission.
Validated ordinary `--cut` targets retain their metadata and body dependencies
without contributing executable body summaries or arming reflection depth routes.
Closed generic `Activator.CreateInstance<T>()` calls pass the caller's type
arguments to the selected public parameterless constructor's depth summary.
Other retained constructors and accessors do not become execution roots.

The graph preserves live field layouts, virtual and interface implementations,
generic signatures and constraints, attributes, delegates, and referenced IL
operands. Generic arguments retain instance constructors and property accessors,
including private setters, because generic factories and serializers can select
them without direct call tokens. Their base types, instance field and property
types, and constructor parameter types retain the same data surface recursively.
Once a retained body calls non-generic `Activator.CreateInstance` or
`ConstructorInfo.Invoke`, directly or through a method group, or a copied
assembly references either, ILDiet retains ordinary application type definitions
and their instance constructors, including application generic definitions named
only by retained member signatures. Retained closed application signatures
promote their generic arguments as runtime dependencies, so allocated types keep
constrained virtual dispatch bodies. Constructor discovery carries closed
application reference-field owners into the member snapshot without boxing the
field values or opening unused method bodies. This allows selection through runtime type
names and reflected field or method-signature types as well as `typeof`. Declared field, property and
constructor-parameter types open the same data surface recursively in user
libraries: instance constructors, property accessors and parameterless instance
void methods. Generic arguments and base types participate in that closure.
Framework assemblies and backend-rewritten protected assemblies keep their
separate policy. Reflection selected only through runtime strings without an
armed route requires an explicit type root.
Once a retained body calls `Array.Initialize()`, directly or through a method
group, every value type in a stripped assembly retains its parameterless
constructor, which the call runs over the elements although no IL names it.
It does not specialize generics or perform dn2cpp intrinsic lowering.
Framework assemblies and dn2cpp runtime/codec/HTTP shims are copied unchanged.
They contain runtime dependencies that are introduced during later transpilation.
GodotSharp is copied unless the .NET-module backend supplies its engine entry
points and constructor-registry policy. That policy retains factories for live
wrappers and redirects unused wrappers to compatible retained ancestor factories,
preserving every engine-name registration key. Unrecognized factory bodies retain
their original dependencies. dn2cpp also requests copies for hot-update base builds.
When a `--cut` selector names a closed generic specialization, ILDiet reports
that it is copying the complete load set and leaves validation to dn2cpp's
original model. Whether that specialization exists depends on model discovery;
an invalid selector still fails before C++ emission. Ordinary non-generic cut
targets are validated on the original metadata and remain eligible for stripping.

Output goes into a dedicated directory. An existing nonempty directory is
accepted only when it carries ILDiet's ownership marker. A failed write leaves
the previous output intact. Rewritten assemblies retain their assembly identity
and resources, have deterministic metadata, and omit PDBs and strong-name
signatures; they are dn2cpp inputs rather than signed distribution assemblies.

The integration protocol is `--request <request.xml> --result <result.xml>`.
The request contains the resolved input/reference paths, descriptor paths,
features, optional full-type roots, and an optional copy-all setting. Backends can
also declare assembly rewrites, type-only and named-method roots, conditional
member retention for descendants of a loaded base type, individual types excluded
from default seeds, registration attributes, and constructor registries.
A conditional descendant retains its own methods,
constructors, properties and events once reached. Default public-surface and
application-initializer roots do not seed those descendants. Backends can nominate
generated helpers with `<suppressDefaultSeeds assembly="..." type="..." />`;
each row applies only to that type, so ordinary nested types retain the existing
rules. Signature-only retention omits their unrooted static initializers rather
than creating stubs that eager startup would execute. Code and explicit roots
still retain the original initializer bodies. Explicit preservation still applies.
Assembly-level registration attributes retain their constructor and ordinary
arguments, while their `Type[]` arguments are filtered to runtime-used types.
Signature-only declarations do not keep registration entries or select
constructor factories; runtime roots still retain their original bodies.

The result contains the ordered output assembly paths, effective preservation
file, whether cut selectors were validated before stripping, and whether
constructor-registry references were rewritten. The backend uses the last state
to avoid applying a second wrapper-trimming pass. Copy-all paths leave assemblies,
registration attributes and constructor registries byte-identical to their inputs.
ILDiet runs as a companion process; its Cecil dependency is never transpiled
into the native dn2cpp executable.

## Native companion probe

The opt-in probe uses the original ILDiet and Mono.Cecil assemblies and keeps
build, emission and execution logs in a fresh directory under `artifacts/`:

```sh
bash gates/ildiet-native.sh emit
bash gates/ildiet-native.sh build
bash gates/ildiet-native.sh verify
```

ILDiet never supplies a signing key or re-signs rewritten assemblies. The probe
cuts Cecil's `CryptoService.GetPublicKey` and `CryptoService.StrongName` branches;
general Cecil signing is outside its scope. Deterministic MVID hashing remains
enabled.

The build stage uses the shared CMake/Ninja wrapper and compares native help
output with the managed CLI. The verify stage also strips `ILDietControl`
through direct arguments and the XML request protocol. It compares managed and
native DLL bytes, effective preservation and ordered result paths, normalizing
the output directories. Repeated runs check deterministic output and removal of
stale PDBs. Metadata inspection checks that unused code is removed and explicit
preservation retains the selected method; the stripped assemblies run under .NET
with the original fixture's output. A signed reference checks that rewriting
preserves assembly identity and public-key bytes while clearing the strong-name
signature flag.

`DN2CPP_SKIP_BUILD=1` reuses an already built CLI and ILDiet; `CONFIG=Debug`
selects their Debug outputs. The probe does not change the distributed companion.
Native substitution requires a successful verify run; emission and help output
alone do not establish stripping parity.
