#!/usr/bin/env bash
# Method attribute inheritance honors override chains and AttributeUsage without constructing IsDefined results.
# Type.GetType(name, throwOnError, ignoreCase) keeps caller/CoreLib lookup and case-collision order.
# Forwarded names resolve in the target assembly again after the source finds a match.
# Nonempty string MemberwiseClone copies preserve UTF-16 contents and collection safety natively.
# Runtime generic template invocation, member rows, hidden contexts and boxed-value copies.
# Assembly.Load distinguishes null AssemblyName objects and missing/empty simple names.
# Single-attribute queries construct every match before ambiguity or a later constructor fault.
# Runtime exception Object-family lookups retain declaring types and callable handles.
# Nested generic type names agree through typeof, GetType and delegate declaring types.
# Array Object.GetType keeps its allocation element type across contents and escapes.
# A template body that calls through a function pointer over its type parameter never
# returns a wrong result: its .NET-diffed lines print alike for a refusal and a correct
# result, and its native-only outcome run pins that the clone refuses every such call
# with PlatformNotSupportedException where .NET runs it.
# Missing reflective bodies share a descriptive refusal across Invoke and delegate creation.
# Argument validation still runs before that refusal, for packed and native metadata alike.
# Reflected template delegates retain their receiver and closed declaring method identity.
# Template-built delegates and GetMethod share the closed member's identity.
# Reflected array templates allocate the runtime argument's array through a closed delegate.
# Template function pointers with a fixed signature run through Invoke and a closed delegate.
# Template rows naming reordered, partial or mixed placeholder classes run or refuse catchably.
# Signature-only template rows retain no override bodies; constructed clones still dispatch.
# An abstract template level's row dispatches to the clone's override or refuses its argument.
# A body reflecting through its type parameter, a generic method, and the member and
# field listings of a MakeGenericType'd clone admit what .NET runs.
# A clone over a known argument serves as another MakeGenericType's or MakeGenericMethod's
# argument, and a CoreLib method instance formed only for its metadata keeps its row
# beside its listed definition.
# Unknown MakeGenericType/MakeGenericMethod arguments retain no executable targets.
# Bodyless runtime-template identity can suppress ClosedGeneric; downstream execution
# still warns, and members without independently retained bodies refuse catchably.
# Static instantiation requires proved constraints, including template body and generic-method owner fallbacks.
# Consolidated reflection-introspection gate. Merges the former per-feature
# reflect-* subset gates (one tiny sample each) into a single multi-section
# program, transpiled once against the tree-shaken real CoreLib. Each section
# keeps its own namespace so reflected type/member names stay byte-identical to
# the originals. Covers:
#   Type metadata (names/namespace/FullName, enum/interface/abstract/array,
#   BaseType, IsAssignableFrom, IsInstanceOfType, GetType), array element types
#   & rank, app-declared nested delegate inheritance/casts through
#   MulticastDelegate and Delegate, enum underlying type / GetNames / GetName /
#   IsDefined / Parse,
#   generic type definitions & arguments, nested types, Nullable<T> metadata,
#   fields / properties / methods / constructors enumeration with binding flags
#   and inheritance, custom attributes (type/field/prop/method/param/ctor,
#   IsDefined, attribute enumeration), Enum.GetValues/GetNames/Parse/TryParse,
#   and assembly-level custom attributes (Assembly.GetCustomAttributes with
#   string/int/Type/Type[] ctor args + named property, Assembly identity:
#   typeof(X).Assembly == GetEntryAssembly(), GetName().Name).
# Former gates: reflecttype, reflect-arraytype, reflect-enumtype, reflect-generic,
# reflect-nested, reflect-nullable, reflect-field, reflect-prop, reflect-method,
# reflect-ctor, reflect-attr, reflect-attr-enumerable, reflectenum,
# reflect-enum-values, gettype, type-category, type-name-registry, type-pattern,
# nested-name.
#
# The five type-* sections sit at the tail, in that order, and every line of all
# five matches real .NET, so they add
# no frozen divergence: the freeze is what carries them only because the
# sections AHEAD of them diverge. Two of the five are the reason the bucket is
# worth reading past its reflection theme: type-pattern's tail is a LINK test
# (a lowering that names an intrinsic type's own ti_/t_ and nothing emits it —
# an all-green transpile whose C++ fails to compile), and nested-name's three
# numbers are an IDENTIFIER-COLLISION test (two same-named methods in different
# declaring types whose compiler-generated state machines share a simple name).
# Neither asserts anything about reflection; both would be silently deleted by
# anyone pruning this bucket by theme.
#
# Also covers the dynamic-code-generation surface cut: Expression tree building
# transpiles, but Expression.Compile / Reflection.Emit construction throws a
# catchable PlatformNotSupportedException (the NativeAOT posture).
# MemberHandleSubset covers Roslyn's legal ldtoken method/field forms (definition,
# specification and cross-module MemberReference) and their catchable raw-handle boundary.
#
# And, right beside it, its OPPOSITE — the degraded stack-trace model
# (StackTraceSubset). The two sections are the two halves of the rule in
# docs/ARCHITECTURE.md §4-B: a DIAGNOSTIC API degrades, a LOAD-BEARING one fails loud.
# Expression.Compile()'s caller cannot proceed without the delegate, so it throws;
# `new StackTrace()`'s caller proceeds regardless of what it finds, so it returns a real
# object with ZERO frames whose ToString() says "   at <stack trace unavailable in AOT>"
# rather than "" (an empty string would read as "the stack was empty" — a silent lie).
# Covers every ctor overload of both types, the accessors, the caller-supplied
# (file, line) frames that are NOT degraded, Environment.StackTrace (whose real IL
# transpiles straight through), and the invariant that an UNTHROWN exception's
# StackTrace stays null.
#
# And, next to those two, a section whose SUBJECT is neither reflection nor
# degradation: EventSourceSubset is a live real-.NET oracle sitting in a frozen
# bucket: every line it prints matches `dotnet run` on this same
# project — provider Name, the SHA-1 name-derived Guid, Settings, the
# per-thread CurrentThreadActivityId round-trip, the (Type)-keyed GetName/GetGuid
# statics, and the no-op returns of WriteEvent/Write/Write<T> — so a line of it
# that stops matching is a REGRESSION, not a boundary that moved. The Guid
# literals are the load-bearing ones: they pin dn2cpp's re-implementation of
# EventSource.GenerateGuidFromName (runtime/core/intrinsics/dn2cpp_eventsource.cpp)
# against .NET's, and a wrong SHA-1 or a wrong Guid byte order would still print
# a plausible-looking guid. It lives here rather than in a diff gate only because
# the sections around it diverge; do not prune it as "the EventSource fake".
#
# That section's OPPOSITE POLARITY is the last arm of this script, and it is not a
# reflection test either: it transpiles samples/dotnet/EventListenerProbe and asserts
# dn2cpp REFUSES it — a program cannot observe its own events, because delivery is
# unmodeled, and an answered EventSource.IsSupported/AddListener would give a listener
# that compiles, links, runs and receives nothing forever with no diagnostic. That
# regression is invisible to every positive assert in the tree (a silent listener prints
# `seen=0` and no fixture disagrees), so the arm asserts the DIAGNOSTIC — the observation-
# side wording, EventListener, the remedy, and the reach chain's tail naming the caller's
# own class — and that nothing was emitted. It is the only check of that contract.
#
# Also covers the COMPLETE Type member-lookup overload surface (every public
# GetMethod/GetConstructor/GetProperty overload incl. the genericParameterCount,
# Binder and CallingConventions forms, GetMember/GetMembers/GetDefaultMembers,
# GetMemberWithSameMetadataDefinitionAs, AmbiguousMatchException cases).
#
# Also covers assembly identity (ReflectAssemblySubset): Type.AssemblyQualifiedName
# (user + BCL type, Type.GetType round-trip), Assembly FullName/GetName()/
# GetExecutingAssembly/GetType(name[,throwOnError[,ignoreCase]])/GetModules/
# IsDynamic/ManifestModule, AssemblyName as a real managed object (ctor/setters/
# display-name parse/Version/FullName), the Module name surface, and NAME-based
# assembly loading (Load(String/AssemblyName), LoadWithPartialName) resolved
# against the linked-assembly registry — hit/case-insensitive/display-name/
# whitespace-pad, miss -> FileNotFoundException / null, empty -> ArgumentException,
# and the Newtonsoft DefaultSerializationBinder probe shape
# LoadWithPartialName(name)?.GetType(fullName) — all matching real .NET.
#
# Expected is a frozen snapshot (gates/expected/reflect-types.txt) rather than a
# live `dotnet $app` diff, because a few sections intentionally diverge from
# real .NET: Type.MakeGenericType over a definition that no compiled
# instantiation, runtime template or static instantiation of known arguments
# serves, and MakeArrayType over an array type not statically reached, throw
# NotSupportedException (the AOT boundary), the
# MakeArrayType(rank) overload always throws it (MD array types — including
# the rank-1 T[*] — are not modeled),
# CustomAttributeData's argument views throw PlatformNotSupportedException,
# ParameterInfo custom modifiers answer from the method signature (including the
# init-only setter return modreq); DefaultValue throws (the Constant blob is not
# carried; HasDefaultValue/IsOut answer exactly from the attributes word), and
# Type.GetGenericParameterConstraints throws it where real .NET throws
# InvalidOperationException for its non-parameter receiver, the
# dynamic-codegen section's Compile()/DynamicMethod throw where the JIT-backed
# runtime succeeds, and the assembly section's loud cut throws the catchable
# PlatformNotSupportedException where real .NET probes the loader
# (Assembly.LoadFile -> FileNotFoundException).
# GetManifestResourceStream is no longer among them — it answers for real over
# carried blobs, and build-and-run-manifest-resources.sh diffs that
# surface against real .NET.
#
# The indexer-edge section (ReflectIndexerEdgeSubset) freezes reading a
# set-only / writing a get-only property as the null accessor's
# InvalidOperationException (real .NET: ArgumentException). The parity paths
# live in the reflect-invoke live diff.
#
# The generic-method section (ReflectGenericMethodSubset) matches real .NET:
# MakeGenericMethod over known arguments instantiates the method, one no other
# body reaches included. Definition lookups and formal-parameter identities follow
# CLR; their live parity paths are covered by build-and-run-reflect-invoke.sh.
#
# The MakeGenericType sections (ReflectMakeGenericSeedSubset,
# ReflectSerializerWrapperSeedSubset, ArrayAssignabilitySubset's Biome flow)
# replay the Newtonsoft deserialization shapes that blocked Thrive: collection
# materializations (List<T>, ReadOnlyCollection<T>, HashSet<T>, Dictionary<K,V>,
# ReadOnlyDictionary<K,V>), the serializer's own CollectionWrapper<T> /
# DictionaryWrapper<K,V> adapters (stand-ins with the real shapes), and an ARRAY
# member's List<elem> temporary (JsonArrayContract.CreateTemporaryCollection ->
# GetConstructors() -> parameterless ConstructorInfo.Invoke) for an element the
# program names in no static List. Each MakeGenericType receives its arguments
# through recognized reflection (PropertyType, ParameterType, GetElementType,
# GetGenericArguments, typeof), and a recognized site takes an instantiation the
# image already has, else the definition's runtime template, else a static
# instantiation when the known arguments meet the constraints; a site none of
# those serves is reported as a ClosedGeneric diagnostic. Every line of the three
# sections matches real .NET.
#
# The framework-attribute section (ReflectFrameworkAttrSubset) covers the
# typeof-named keep rule for FRAMEWORK-declared attributes on app elements
# (Compilation.DecodeCustomAttributes): [Description] enum-field reads through
# the Thrive EnumHelper shape GetMember(...)[0].GetCustomAttributes(typeof(T),
# false).Cast<T>().First() — the NewGameSettings boot blocker — match real
# .NET; its one frozen divergence is the boundary's negative, [Browsable]
# applied but typeof-named nowhere, whose row stays dropped so the untyped
# enumeration reports 1 attribute where real .NET reports 2 (the IL2CPP-
# managed-stripping bound).
#
# The interface-table residue section (ReflectGenericSubset.RunItfResidue) is NOT
# about generic reflection, whatever the namespace it sits in says. Its subject is
# WHICH TYPES GET AN INTERFACE TABLE AT ALL — CppEmitter.RenderItfTables' guards —
# and it is the only place the three answers below are asserted:
#   - a value type the program never boxes (relation-only table, no unboxing thunks);
#   - a FRAMEWORK type reached by a typeof token alone (the opaque shell; an
#     app-module one proves nothing, since every app-module type is an emit-set seed);
#   - that the shared-generics canonical ALIAS rows stay out of Type.GetInterfaces()
#     and Type.FindInterfaces() while dispatch and the isinst walks keep seeing them.
# The tail's "canonical rows reported ... = 0" scan covers typeof(string) and
# typeof(int[]) as well, whose tables are the string / SZArray dispatch maps, where
# the alias rows are INTERLEAVED rather than trailing. Every line of the section
# matches real .NET. A later reader pruning this bucket by its reflection theme would
# delete the only assertion that a never-boxed struct is reflectable at all.
#
# The MARSHAL-VERDICT section (ReflectMarshalVerdictSubset) is not about reflection either:
# its subject is the MARSHALLED-LAYOUT MODEL's refusals, and specifically the two-bit verdict
# — ArgumentException means ".NET refuses this too", PlatformNotSupportedException means
# "dn2cpp declines to model it". The rows are the COM surface: the BStr/TBStr string forms,
# and an OBJECT field, whose real-.NET answer SPLITS BY HOST (8 on Windows, refused on POSIX)
# where a transpile-time verdict cannot. Its agreeing half is a LIVE diff, in MarshalPinning's
# SizeOfOffsetOfSubset; only a deliberate divergence may live here.
#
# ReflectShallowCloneRefusalSubset freezes native cloning divergences. Runtime
# structs carrying DN2CPP_TF_NO_SHALLOW_CLONE refuse a second owner of native state;
# CancellationTokenSource remains a copying control. Nonempty strings copy their
# full UTF-16 buffer through an explicit native-only argument: reflective cloning
# can corrupt the CLR heap, including in culture verification of this bucket.
# Safe empty strings and the agreeing runtime shapes remain in reflect-invoke.
# The TYPE-IDENTITY section (TypeIdentitySubset) is in this bucket for the CoreLib
# surface it needs, not for its theme, so read what it actually asserts before
# pruning: it is the invariant that ONE CLR type has exactly ONE
# Dn2CppTypeInfo. A dn2cpp Type IS its type-info pointer, so ==, Equals,
# GetHashCode and ReferenceEquals all reduce to it — and the routes that had a
# SECOND one are the ones nothing spells out: an emitted class's BASE pointer
# (typeof(MyClass).BaseType == typeof(object) was False), and a reflected
# member's FieldType/ParameterType against typeof of the same type (Type,
# StringBuilder, MemberInfo, MethodInfo). The obvious probe, typeof(object) ==
# new object().GetType(), passed either way, which is why this went unseen. It is
# VoidIdentitySubset's argument generalized, and it sits beside the
# g_meta_members declaring-type check, which met the same defect.
# Its companion is the SOURCE-level check at the very bottom of this file, which
# re-derives the runtime's whole type-info handle set and refuses one that
# CoreLib's registry does not account for — the section covers the types this
# bucket names, that check covers the ones no corpus happens to name.
#
# TaskRuntimeIdentitySubset asserts the producer side of the same identity
# contract: every intrinsic Task/ValueTask/TCS mouth stamps the exact closed
# Task<T>, including nested/array result arguments and source-backed pending
# tasks, while wrong closed casts stay false. Its generic SharedArg calls are
# behavioural identity checks; canonical-body retention itself is asserted by
# the Task rgctx section of build-and-run-shared-generics.sh.
#
# The OWNED-HANDLE RESIDUE section (BoundHandleResidueSubset) is in this bucket for
# the same reason TypeIdentitySubset is, and its subject is likewise not reflection:
# it is what a RUNTIME-OWNED Dn2CppTypeInfo answers about its own members, i.e. which
# facts about the CLR the C++ runtime hand-writes. Its field lines — String, Decimal,
# TimeSpan, DateTime, DateTimeOffset, ConstructorInfo, StackTrace, StackFrame,
# ResourceManager, WaitHandle, each row with its FieldType, DeclaringType, flag triple,
# raw attributes word and VALUE — all match real .NET, and pin values that nothing else
# in the tree states twice (DateTime.UnixEpoch's tick count and Utc kind,
# Decimal.MaxValue's 96-bit mantissa, TimeSpan's tick constants). Its
# "DIVERGES" lines are the DECLINED half, asserted so the decision is visible
# rather than silent: Type/Module carry no field table (their values are MemberFilter/
# TypeFilter delegates and Missing.Value), and no owned handle carries a method or
# property table (its members are intrinsic — lowered at the call site, with no C++ body
# an invoker could name). Its interface lines ask named membership, which matches .NET:
# an owned handle enumerates the interfaces of its CLR type that the image defines.
# Delete the DIVERGES lines and the boundary becomes a silence again.
#
# That section's TAIL has a fourth subject again — an SZArray's interface ENUMERATION —
# and its last line (r-late) is about neither reflection nor owned handles but TIMING:
# LateNoted[] is named by a method-table parameter row and by no IL site at all, so its
# element is noted after the emit fixpoint, where the relation-only row planting used to
# be unreachable. It is the behavioural half of the artifact scan further down.
#
# The INTRINSIC TYPE-NAME section (IntrinsicTypeNameSubset) is the third in this
# bucket for the CoreLib surface it needs rather than its theme, and its subject is
# the TYPE-NAME REGISTRY: whether a referenced intrinsic's minimal type-info is in it
# at all. Such a type is emitted because a lowering names its own &ti_ and nothing
# else would define the symbol; it used to be absent from the registry, so
# Type.GetType answered null about a type typeof answers about, and MakeGenericType
# and the composed-name resolution — which scan that same table for a candidate —
# found none. Every line but one matches real .NET; the exception is the
# dn2cpp-MANGLED instantiation name, which is this handle's registry key as it is for
# every closed generic and which real .NET knows nothing about. Prune it as "the
# Vector128 names test" and the only assertion that a non-emitted type is reachable by
# name goes with it.
#
# The REGISTRY-MOUTHS section (TypeRegistryMouthsSubset) is the fourth here for the
# CoreLib surface it needs rather than its theme, and it asserts two things that are not
# reflection features but routes. Its `regdef` lines are the KIND BITS of a
# generic definition dn2cpp mints neither ClassInfo nor Template for — an intrinsic-
# modeled one, which decodes to a bare External name, so its synthetic gendef must read
# its own metadata through the type index; they would otherwise read Unknown, and
# typeof(Vector128<>) answered IsValueType False about a sealed struct. The List`1 and
# IComparable`1 lines beside them are the CONTROL down the Template route, without which
# a green would say nothing about the route that was broken. Its `regname` lines are the
# other half: a type whose Dn2CppTypeInfo the C++ RUNTIME owns must be reachable by NAME
# as well as by typeof, because Type.GetType's registry seed and typeof's answer are two
# mouths of the same three CoreIntrinsics tables — while the seed was a hand-written
# second list, StringBuilder/StackTrace/WaitHandle/ManualResetEventSlim/AggregateException
# answered null by name here while typeof handed back a live handle, and null is what a
# caller cannot tell from "no such type". Which names a hand-written seed happened to miss
# depended on the PROGRAM too (one the emit set carries gets its row from the emitted-class
# loop), which is why the seed is derived. Every line matches real .NET.
#
# ReflectIntrinsicTemplateSubset freezes the intrinsic-open-definition negative:
# typeof(BlockingCollection<>) + MakeGenericType roots no runtime template
# (intrinsic levels are shape-ineligible), so the transpile completes, the site
# warns as ClosedGeneric, and the mint throws the catchable NotSupportedException
# where real .NET constructs. Every BlockingCollection<T> is one runtime type, so an
# instantiation the program constructs does not resolve either: both sites' fix and
# the refusal construct the closed type directly.
#
# ReflectAttrNullTypeSubset verifies that a null Type attribute argument survives
# decoding in a positional value, a Type[] element, and a named value.
#
# Every other line matches real .NET (verified against `dotnet run` at capture
# time).
source "$(dirname "$0")/_common.sh"
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} gates/_ordinary-reflection.sh samples/dotnet/ReflectTypes/AttributeTypePropertySubset.cs samples/dotnet/ReflectTypes/DataOnlyAttributeRowsOnly.csproj samples/dotnet/ReflectTypes/DataOnlyAttributeRowsOnlyProgram.cs samples/dotnet/ReflectTypes/OrdinaryReflectionTypeLeaves.csproj samples/dotnet/ReflectTypes/OrdinaryReflectionTypeLeavesProgram.cs samples/dotnet/ReflectTypes/PropertyAccessorRowsSubset.cs samples/dotnet/ReflectTypes/ReflectAssemblyErrorSubset.cs samples/dotnet/ReflectTypes/ReflectAttrBoxedSubset.cs samples/dotnet/ReflectTypes/ReflectRuntimeTypeParitySubset.cs samples/dotnet/ReflectTypes/ReflectTypes.csproj samples/dotnet/ReflectTypes/UnreadAttributeRowsOnly.csproj samples/dotnet/ReflectTypes/UnreadAttributeRowsOnlyProgram.cs"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|before-nested-generic-names"
DN2CPP_GATE_EXTRA_INPUTS="$DN2CPP_GATE_EXTRA_INPUTS samples/dotnet/ReflectTypes/NestedGenericTypeNameSubset.cs samples/dotnet/ReflectTypes/GenericDefinitionSymbolNeighbors.cs"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|ordinary-type-leaves-v1|before-attribute-display-code-units|exception-object-prefix-argv:before-exception-object-members|assembly-name-prefix-argv:before-assembly-name-validation|attribute-construction-prefix-argv:before-attribute-construction-order"
DN2CPP_GATE_EXTRA_INPUTS="$DN2CPP_GATE_EXTRA_INPUTS samples/dotnet/ReflectTypes/AttributeConstructionOrderSubset.cs samples/dotnet/ReflectTypes/MethodAttributeInheritanceSubset.cs"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|before-method-attribute-inheritance"
DN2CPP_GATE_EXTRA_INPUTS="$DN2CPP_GATE_EXTRA_INPUTS samples/dotnet/ReflectTypes/keep-ordinary-type-leaves.xml samples/dotnet/ReflectTypes/keep-template-signatures.xml samples/dotnet/ReflectTypes/keep-attribute-enumeration.xml"
DN2CPP_GATE_EXTRA_INPUTS="$DN2CPP_GATE_EXTRA_INPUTS samples/dotnet/ReflectTypes/ClosedGenericDiagnosticsOnly.csproj samples/dotnet/ReflectTypes/ClosedGenericDiagnosticsUnshared.csproj samples/dotnet/ReflectTypes/ClosedGenericDiagnosticsOnlyProgram.cs gates/expected/reflect-types-closed-generic.txt gates/expected/reflect-types-closed-generic-unshared.txt"
DN2CPP_GATE_EXTRA_INPUTS="$DN2CPP_GATE_EXTRA_INPUTS samples/dotnet/ReflectTypes/ReflectionTemplateDispatch.csproj samples/dotnet/ReflectTypes/ReflectionTemplateDispatchProgram.cs samples/dotnet/ReflectTypes/ReflectRuntimeInstantiationSubset.cs"
DN2CPP_GATE_EXTRA_INPUTS="$DN2CPP_GATE_EXTRA_INPUTS samples/dotnet/ReflectTypes/TemplateSignatureAllocationOnly.csproj samples/dotnet/ReflectTypes/TemplateSignatureAllocationProgram.cs"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|invoke-diagnostics-prefix-argv:before-invoke-diagnostics|invoke-diagnostics-argv:invoke-diagnostics"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|reflected-delegates-prefix-argv:before-reflected-delegates"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|reflected-arrays-prefix-argv:before-reflected-arrays"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|fixed-function-pointers-prefix-argv:before-fixed-function-pointers"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|template-delegate-method-identity-prefix-argv:before-template-delegate-method-identity"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|template-signature-placeholders-prefix-argv:before-template-signature-placeholders"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|template-signature-placeholder-outcomes-argv:template-signature-placeholder-outcomes"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|template-consumers-prefix-argv:before-template-consumers"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|template-arguments-prefix-argv:before-template-arguments"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|template-signature-allocation-prefix-argv:before-template-signature-allocation|uninitialized-template-outcomes"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|forbidden-generic-arguments-prefix-argv:before-forbidden-generic-arguments"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|string-clone-copies-native-argv:string-clone-copies"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|type-gettype-ignorecase|before-type-gettype-ignorecase"
DN2CPP_GATE_EXTRA_INPUTS="$DN2CPP_GATE_EXTRA_INPUTS samples/dotnet/TypeGetTypeIgnoreCase/TypeGetTypeIgnoreCase.csproj samples/dotnet/TypeGetTypeIgnoreCase/Program.cs samples/dotnet/TypeGetTypeIgnoreCase/ShadowCoreType.cs samples/dotnet/TypeGetTypeIgnoreCase/PaddingTypes.cs samples/dotnet/TypeGetTypeIgnoreCase/ForwardedNestedTypes.cs samples/dotnet/TypeGetTypeIgnoreCaseForwarder/TypeGetTypeIgnoreCaseForwarder.csproj samples/dotnet/TypeGetTypeIgnoreCaseForwarder/Forwarder.cs samples/dotnet/TypeGetTypeIgnoreCaseForwarded/TypeGetTypeIgnoreCaseForwarded.csproj samples/dotnet/TypeGetTypeIgnoreCaseForwarded/Program.cs"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|template-constraint-fallbacks-prefix-argv:before-template-constraint-fallbacks"

EXPFILE="$(dirname "$0")/expected/reflect-types.txt"
ATTRIBUTE_ENUMERATION_KEEP=(--link-xml samples/dotnet/ReflectTypes/keep-attribute-enumeration.xml)
BCL=(System.Linq.Expressions System.Linq System.Collections \
    System.Reflection.Emit System.Reflection.Emit.Lightweight System.Reflection.Emit.ILGeneration \
    System.ComponentModel.Primitives System.Collections.Concurrent)

# These comparisons inspect the original metadata through C++ reflection policies.
# Managed preservation is covered by build-and-run-preserve-control.sh.
gate_string_clone_prefix_asserts() {
    local out="$1"
    gate_capture_ok "$out/string-clone-before" run_bounded "$out/ReflectTypes$EXE_EXT"
    gate_output select "$out/string-clone-before.stdout" "$out/string-clone-prefix.stdout" --before '== Type.GetType throwOnError and ignoreCase ==' --policy crlf
    gate_output select "$EXPFILE" "$out/string-clone-expected.stdout" --before '== safe string memberwise copies ==' --policy crlf
    gate_output compare "$out/string-clone-prefix.stdout" "$out/string-clone-expected.stdout" --policy crlf
}
gate_type_gettype_ignorecase_asserts() {
    local out="$1" native before prefix expected
    native="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-native-2.stdout"
    gate_capture_ok "${native%.stdout}" run_bounded "$out/ReflectTypes$EXE_EXT" string-clone-copies || return $?
    prefix="$native.before"
    gate_output select "$native" "$prefix" --before '== Type.GetType throwOnError and ignoreCase ==' --policy crlf
    before="$out/type-gettype-expected.stdout"
    gate_output select "$EXPFILE" "$before" --before '== Type.GetType throwOnError and ignoreCase ==' --policy crlf
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    native="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-native-3.stdout"
    gate_capture_ok "${native%.stdout}" run_bounded "$out/ReflectTypes$EXE_EXT" type-gettype-ignorecase || return $?
    expected="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-expected-4.stdout"
    gate_capture_ok "${expected%.stdout}" run_bounded dotnet "samples/dotnet/ReflectTypes/bin/$CONFIG/$TFM/ReflectTypes.dll" type-gettype-ignorecase || return $?
    gate_output compare "$native" "$expected" || return $?
    gate_output witness "$native" 'Type.GetType throwOnError and ignoreCase end' --count any --policy crlf \
        || { echo 'FAIL: Type.GetType ignoreCase section did not run' >&2; return 1; }
    grep -Fq '{ "DelegateBaseTypeSubset.GenericOwner`1+Handler`1",' "$out/generated.cpp" \
        || { echo 'FAIL: nested definition registry row missing' >&2; return 1; }
    if grep -Fq '{ "DelegateBaseTypeSubset.GenericOwner`1",' "$out/generated.cpp"; then
        echo 'FAIL: nested-name direct lookup no longer needs its missing-outer regression' >&2
        return 1
    fi
    for name in 'Q.Alpha`1+Mid`1+Leaf`1' 'Q.aLpha`1+Mid`1+Leaf`1'; do
        grep -Fq "{ \"$name\"," "$out/generated.cpp" \
            || { echo "FAIL: nested collision leaf row missing: $name" >&2; return 1; }
    done
    for name in 'Q.Alpha`1' 'Q.Alpha`1+Mid`1' 'Q.aLpha`1' 'Q.aLpha`1+Mid`1'; do
        if grep -Fq "{ \"$name\"," "$out/generated.cpp"; then
            echo "FAIL: nested collision enclosing row unexpectedly retained: $name" >&2
            return 1
        fi
    done
}
gate_intrinsic_template_row_asserts() {
    local row=$'\tSystem.Type.MakeGenericType(Type[])\tSystem.Collections.Concurrent.BlockingCollection`1 is modeled by the runtime as one type for every instantiation, so MakeGenericType cannot form it\tconstruct the closed type directly instead of through MakeGenericType'
    grep -Fxq -- $'ClosedGeneric\tReflectTypes\tReflectIntrinsicTemplateSubset.Program::Run()\tIL_003e'"$row" \
        "$1/reflection-unrecognized.tsv" \
        && [ "$(grep -Fc -- "$row" "$1/reflection-unrecognized.tsv")" = 2 ] \
        && [ "$(awk -F'\t' '$3 == "ReflectIntrinsicTemplateSubset.Program::Run()"' "$1/reflection-unrecognized.tsv" | wc -l | tr -d ' ')" = 2 ] \
        || { echo "FAIL: the unnamed and the constructed runtime-modeled MakeGenericType sites do not both report" >&2; return 1; }
}
gate_extra_asserts() {
    gate_string_clone_prefix_asserts "$1"
    gate_type_gettype_ignorecase_asserts "$1"
    gate_intrinsic_template_row_asserts "$1"
}
DN2CPP_GATE_RUN_ARGS=string-clone-copies \
    corelib_freeze_gate ReflectTypes "$EXPFILE" "${BCL[@]}" --no-ildiet "${ATTRIBUTE_ENUMERATION_KEEP[@]}"
unset -f gate_extra_asserts

gate_extra_asserts() {
    local native
    native="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-native-5.stdout"
    gate_capture_ok "${native%.stdout}" run_bounded "$1/TypeGetTypeIgnoreCase$EXE_EXT" || return $?
    gate_output witness "$native" 'grown bucket=TypeCaseCollision.gROWTH' --count any --policy crlf \
        || { echo 'FAIL: grown case-collision section did not run' >&2; return 1; }
    gate_output witness "$native" 'Type.GetType case collisions end' --count any --policy crlf \
        || { echo 'FAIL: Type.GetType case-collision section did not run' >&2; return 1; }
}
corelib_diff_gate TypeGetTypeIgnoreCase --no-ildiet
DN2CPP_OUT_SUFFIX=-diet corelib_diff_gate TypeGetTypeIgnoreCase
unset -f gate_extra_asserts

gate_extra_asserts() {
    local native
    native="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-native-6.stdout"
    gate_capture_ok "${native%.stdout}" run_bounded "$1/TypeGetTypeIgnoreCaseForwarded$EXE_EXT" || return $?
    gate_output witness "$native" 'forwarded folded target=True' --count any --policy crlf \
        || { echo 'FAIL: staged folded forwarder did not run' >&2; return 1; }
    gate_output witness "$native" 'forwarded folded source=False' --count any --policy crlf \
        || { echo 'FAIL: source definition displaced its forwarder' >&2; return 1; }
    gate_output witness "$native" 'nested folded local=True' --count any --policy crlf \
        || { echo 'FAIL: losing nested forwarder displaced the source leaf' >&2; return 1; }
    gate_output witness "$native" 'nested folded forwarded=False' --count any --policy crlf \
        || { echo 'FAIL: losing nested forwarder was selected' >&2; return 1; }
    for name in 'Q.Alpha`1+Mid`1+Leaf`1' 'Q.aLpha`1+Mid`1+Leaf`1'; do
        grep -Fq "{ \"$name\"," "$1/generated.cpp" \
            || { echo "FAIL: staged nested leaf row missing: $name" >&2; return 1; }
    done
    for name in 'Q.Alpha`1' 'Q.Alpha`1+Mid`1' 'Q.aLpha`1' 'Q.aLpha`1+Mid`1'; do
        if grep -Fq "{ \"$name\"," "$1/generated.cpp"; then
            echo "FAIL: staged nested enclosing row unexpectedly retained: $name" >&2
            return 1
        fi
    done
    gate_output witness "$native" 'Type.GetType staged forwarding end' --count any --policy crlf \
        || { echo 'FAIL: staged forwarder section did not finish' >&2; return 1; }
}
corelib_diff_gate TypeGetTypeIgnoreCaseForwarded \
    -r "samples/dotnet/TypeGetTypeIgnoreCase/bin/$CONFIG/$TFM/TypeGetTypeIgnoreCase.dll" \
    -r "samples/dotnet/TypeGetTypeIgnoreCaseForwarder/bin/$CONFIG/$TFM/TypeGetTypeIgnoreCaseForwarder.dll" \
    --no-ildiet
unset -f gate_extra_asserts

# --trim-reflection arm. The flag ships ON for the Godot Web export, and its whole promise
# is that it touches only the reflection metadata of types the program cannot reflect over —
# so this bucket, whose every type IS in the app module (kept whole under the trim), must
# transpile to output that diffs byte-for-byte against the SAME frozen snapshot. This is the
# assertion that a future change to the keep-set cannot quietly start stripping an app type
# the game reflects over: [Export]/[Signal] scanning, serialization, attribute reads — the
# reflection real games depend on — all live here. The dedicated
# build-and-run-trim-reflection.sh gate covers the other side (framework types that DO get
# stripped, and the roots/typo surface); this arm pins the "unchanged" side against the
# richest reflection program in the suite.
echo "== --trim-reflection arm: app-module reflection is unchanged =="
corelib=$(locate_corelib)
bcl=$(dirname "$corelib")
app="samples/dotnet/ReflectTypes/bin/$CONFIG/$TFM/ReflectTypes.dll"
refs=(-r "$corelib")
# A requested extra is a hard requirement, not a wish — dropping an absent one
# runs the --trim-reflection arm over a smaller program and still diffs it
# against the same frozen snapshot (same rule as _corelib_gate_core).
for name in "${BCL[@]}"; do
    [ -f "$bcl/$name.dll" ] \
        || { echo "error: requested reference $name not found beside the CoreLib: $bcl/$name.dll" >&2; exit 1; }
    refs+=(-r "$bcl/$name.dll")
done
out=artifacts/reflecttypes-trim
invoke_cli "$app" "${refs[@]}" --no-ildiet --trim-reflection "${ATTRIBUTE_ENUMERATION_KEEP[@]}" -o "$out"
if gate_cache_check "$out" "reflect-types-trim|no-ildiet|${ATTRIBUTE_ENUMERATION_KEEP[*]}|native-argv:string-clone-copies|$corelib" \
        "$app" "${app%.dll}.runtimeconfig.json" "${app%.dll}.deps.json" "$EXPFILE" "${ATTRIBUTE_ENUMERATION_KEEP[1]}"; then
    gate_cache_hit_msg
else
    compile_console "$out" ReflectTypes
    gate_capture_ok "$out/trim-check" run_bounded "./$out/ReflectTypes$EXE_EXT" string-clone-copies
    gate_output compare "$out/trim-check.stdout" "$EXPFILE" --policy crlf
    gate_string_clone_prefix_asserts "$out"
    gate_cache_commit
fi

# ── The precise ti_arr_ funnel: exercised, and degrade-free ───────────────────
# CppEmitter.ArrayTypeInfoDeclared is the one answer in the emitter's type-info
# funnel whose "no" is not a throw: the two mouths that ask it — a reflected
# member type and a custom-attribute array argument's allocation tag — degrade
# to the System.Object handle / an untagged object[] and emit well-formed C++
# either way. The transpile FAILS on the first such degrade
# (DN2CPP_MAX_ARRAY_TI_DEGRADES, default 0), which is why the transpiles above
# passing is already half the assertion. The other half is that it is not
# vacuous: a bucket where the funnel is never ASKED would satisfy a zero the
# same way an intact one does. So read the artifact, on both mouths.
#
# This bucket is where the ask lives — ReflectArrayTypeSubset reflects over
# array-typed members, ReflectAttrEnumerableSubset LINQs over an attribute's
# array argument — and the two greps below are the two mouths'
# "yes": a metadata row naming &ti_arr_, and an attribute array allocated
# through the _t (typed) allocator. The untagged-allocation grep is the direct
# negative for the second mouth, and it is exact rather than indicative: an
# attrarrN local is emitted by RenderAttrArray and by nothing else, so an
# attrarrN assigned from a non-_t allocator IS a silent degrade, whatever the
# count said.
echo "== ti_arr_ funnel: both mouths asked, none degraded =="
gen_untrimmed=(artifacts/reflecttypes/generated*.cpp)
# grep exits 1 on no match and the suite runs under `set -o pipefail`, so every
# count below is taken through a tolerant wrapper — a zero must reach the assert
# that reports it, not abort the gate with no line printed.
count_in_gen() { { grep -hoE "$1" "${gen_untrimmed[@]}" || true; } | wc -l | tr -d ' '; }
n_member_ti=$(count_in_gen '&ti_arr_')
n_tagged=$(count_in_gen 'attrarr[0-9]+ = dn2cpp_newarr_[a-z0-9_]*_t\(')
n_untagged=$(count_in_gen 'attrarr[0-9]+ = dn2cpp_newarr_(ref|i4|n)\(')
echo "precise array handles named: $n_member_ti; attribute arrays tagged: $n_tagged, untagged: $n_untagged"
[ "$n_member_ti" -gt 0 ] || { echo "error: no &ti_arr_ reference in the emitted metadata — the reflected-member mouth was never asked, so its zero-degrade result is vacuous" >&2; exit 1; }
[ "$n_tagged" -gt 0 ]    || { echo "error: no attribute array allocated with a precise handle — the attribute-argument mouth was never asked, so its zero-degrade result is vacuous" >&2; exit 1; }
[ "$n_untagged" -eq 0 ]  || { echo "error: $n_untagged attribute array(s) allocated untagged — the precise ti_arr_ handle was not declared and the row degraded to the shared object[] handle, whose missing interface-dispatch map aborts interface dispatch over the array" >&2; exit 1; }
echo "OK — both mouths of CppEmitter.ArrayTypeInfoDeclared answered, none degraded."

# ── Every noted array carries interface rows, whenever it was noted ───────────
# The r-late line above is the behavioural half and covers ONE element. This is the
# corpus-wide half, and it is the one that can see the class of bug: the rows used to
# be planted at NOTING time, so an element first noted after the emit fixpoint — which
# is where TypeMetadataEmitter.NoteReflectedMemberTypes notes every array a
# reflection table types a member with — got none, and its GetInterfaces() answered six
# where .NET says eleven while the type TEST already answered eleven by
# DN2CPP_TF_ARRAY_GEN_ITF. Nothing about that is loud: the transpile is green, the C++
# links, and only a program that enumerates that array's interfaces disagrees. Read the
# artifact instead — an interfaces field of `nullptr, 0` on a ti_arr_ IS the residue.
echo "== every ti_arr_ carries interface rows, whenever its element was noted =="
# resolve_python (_common.sh) picks an interpreter by RUNNING one: a bare `python3` on
# Windows is the Store alias stub, which resolves and then fails. No gate_skip — these
# two scans are source reads, so an absent interpreter is a broken box, not a missing
# prerequisite, and the failed assignment must take the gate down.
py="$(resolve_python)"
arr_itf_missing=$($py - <<'PY'
import csv, re, glob
# Resolve initializer positions from the ABI declaration so metadata compaction
# cannot silently make this scan read a different pointer or count.
header = open('runtime/core/dn2cpp_core.h', encoding='utf-8').read()
body = re.search(r'struct Dn2CppTypeInfo\s*\{(.*?)\n\};', header, re.S).group(1)
fields = []
for line in body.splitlines():
    line = line.split('//', 1)[0].strip()
    if '{' in line:
        break
    if not line.endswith(';'):
        continue
    field = re.search(r'\(\*(\w+)\)', line) or re.search(r'(\w+)\s*;', line)
    if field is None:
        raise RuntimeError('unrecognized type-info field: ' + line)
    fields.append(field.group(1))
itfs_index = fields.index('interfaces')
count_index = fields.index('interfaceCount')
row = re.compile(r'^const Dn2CppTypeInfo (ti_arr_\S+) = \{ (.*) \};$')
total = relation = 0
for f in sorted(glob.glob('artifacts/reflecttypes/generated*.cpp')):
    for line in open(f, encoding='utf-8', errors='replace'):
        m = row.match(line)
        if not m:
            continue
        total += 1
        values = next(csv.reader([m.group(2)], skipinitialspace=True))
        itfs, count = values[itfs_index], values[count_index]
        if itfs.startswith('arrgenitf_'):
            relation += 1
        if itfs == 'nullptr' or count == '0':
            print(m.group(1))
if total == 0:
    print('(no ti_arr_ emitted at all — the scan matched nothing and asserts nothing)')
elif relation == 0:
    print('(no relation-only arrgenitf_ table — the planting route is unexercised)')
PY
)
if [ -n "$arr_itf_missing" ]; then
    echo "error: array type-infos with no interface rows:" >&2
    printf '  %s\n' $arr_itf_missing >&2
    echo "Compilation.PlantUnmappedArrayGenericItfRows runs at the confirmation point so" >&2
    echo "every noted element gets rows regardless of WHEN it was noted; a hole here means" >&2
    echo "the planting is keyed on noting order again, and such an array enumerates the six" >&2
    echo "shared non-generic interfaces where real .NET reports eleven." >&2
    exit 1
fi
echo "OK — no array type-info left without interface rows."

# ── One CLR type, one type-info: the source-level half ────────────────────────
# The section TypeIdentitySubset above asserts the BEHAVIOUR — a type reached
# through a base pointer or a reflected member's FieldType compares equal to
# typeof of the same type — and a behaviour test only covers the types the
# bucket happens to name. The invariant is wider than any corpus: whenever the
# C++ runtime hand-writes a Dn2CppTypeInfo for a CLR type, the emitter must
# reference THAT handle rather than mint a rival, because a dn2cpp Type IS its
# type-info pointer. The registry that decides it is
# CoreIntrinsics.RuntimeTypeInfoSymbol; a handle added to the runtime without a
# row there silently goes back to shadowing, which is the fail-OPEN direction —
# the transpile is green, the C++ links, and `o is StringBuilder` answers False
# on an object whose type IS StringBuilder (measured on 710e9932, along with an
# InvalidCastException whose message names the same type on both sides).
#
# So re-derive the runtime's handle set from the runtime sources and require
# every one of them to be accounted for: either carried as a row, or named in
# CoreIntrinsics.s_runtimeTypeInfoNotRowed with the reason it cannot be one
# (a handle that models MORE than one CLR type, or one whose CLR name is not a
# ClassInfo.FullName at all). Neither list may be silently short.
#
# This is a source read, not a build: it costs the gate nothing and cannot skip.
echo "== runtime type-info handles: every one is rowed or declared not-rowable =="
ti_missing=$($py - <<'PY'
import re, glob, sys
# Match definitions independently of their initializer: aggregate braces,
# dn2cpp_ti_with_* wrappers and lambdas all define runtime-owned handles.
runtime = set()
for f in glob.glob('runtime/**/*.cpp', recursive=True):
    src = open(f, encoding='utf-8', errors='replace').read()
    for m in re.finditer(
            r'\b(?:static\s+)?(?:const\s+)?Dn2CppTypeInfo\s+(dn2cpp_[A-Za-z0-9_]+)\s*=', src):
        runtime.add(m.group(1))
cs = open('src/Dn2Cpp.Transpiler/CoreIntrinsics.cs', encoding='utf-8').read()
rowed = set(re.findall(r'"&(dn2cpp_[A-Za-z0-9_]+)"', cs))
# A not-rowed entry is `["<sym>"] = "<reason>"`, and the reason must be a non-empty
# string: an entry with nothing to say is the silence this check exists to break.
notrowed = set(re.findall(r'\["(dn2cpp_[A-Za-z0-9_]+)"\]\s*=\s*\n?\s*"[^"]', cs))
for sym in sorted(runtime - rowed - notrowed):
    print(sym)
# The other direction: a row naming a handle the runtime no longer defines would
# emit a reference to an undeclared identifier, which the C++ compile catches —
# but it catches it in a toolchain that knows nothing about any of this, so say it
# here instead.
for sym in sorted(rowed - runtime):
    print(sym + " (rowed, but the runtime defines no such handle)")
PY
)
if [ -n "$ti_missing" ]; then
    echo "error: runtime type-info handles unaccounted for in CoreIntrinsics:" >&2
    printf '  %s\n' $ti_missing >&2
    echo "Each must be a row in s_runtimeExceptionTypeInfo / s_runtimeBoundTypeInfo /" >&2
    echo "s_runtimeOwnedTypeInfo, or an entry in s_runtimeTypeInfoNotRowed saying why it" >&2
    echo "cannot be one. Unaccounted, the emitter mints a rival ti_ and one CLR type has" >&2
    echo "two type-infos again — green transpile, green link, wrong isinst." >&2
    exit 1
fi
echo "OK — every runtime-defined Dn2CppTypeInfo is rowed or declared not-rowable."

# ── EventSource's OBSERVATION side stays refused, and the refusal is loud ──────
# The sibling of the EventSourceSubset section above, and its opposite polarity. That
# section asserts, against a live real-.NET oracle, everything dn2cpp DOES answer about
# EventSource — identity (Name, the SHA-1 name-derived Guid, Settings, the activity-id
# round-trip) and the write side, whose no-op returns are what real .NET with no listener
# attached performs too. This arm asserts the one thing it must NOT answer: delivery.
#
# A gate that only ever asserts what a transpiler does cannot see this regression. Answer
# EventSource.IsSupported or AddListener — both look obviously answerable, one of them
# "true", the other "a no-op like the write side" — and samples/dotnet/EventListenerProbe
# transpiles, compiles, links, runs, and receives nothing. Forever, with no diagnostic. The
# suite stays green the whole time, because a listener that observes nothing prints
# `seen=0` and no fixture in this tree says otherwise. That is the load-bearing-consumer
# failure of docs/ARCHITECTURE.md §4-B, and this arm is the only thing that reports it.
#
# It asserts the DIAGNOSTIC, not merely the non-zero exit:
# `EventListener..ctor` opens at least two doors into the unmapped observation surface —
# IsSupported (via CallBackForExistingEventSources -> EnsurePreregisteredEventSourcesExist)
# and AddListener — and which one aborts is decided by drain order, not by the program. So
# the substring tested here is the one every door shares, and the reach chain's tail is
# tested separately because that tail is the only part of a CoreLib-raised abort that names
# the caller's own class.
#
# Not cached: a ~1s transpile that must produce NO output has nothing to key a cache on
# (arm 4 of build-and-run-trim-reflection.sh, same argument), and re-running it every time
# is cheap insurance on a contract whose failure mode is silence.
echo "== EventSource observation: an EventListener program must FAIL the transpile =="
build_proj "samples/dotnet/EventListenerProbe/EventListenerProbe.csproj"
ES_APP="samples/dotnet/EventListenerProbe/bin/$CONFIG/$TFM/EventListenerProbe.dll"
ES_OUT=artifacts/eventlistenerprobe-refused
rm -rf "$ES_OUT"
set +e
es_err=$(invoke_cli "$ES_APP" -r "$corelib" -o "$ES_OUT" 2>&1)
es_code=$?
set -e
printf '%s\n' "$es_err" | tail -2
if [ "$es_code" -eq 0 ]; then
    echo "FAIL: an EventListener program transpiled — delivery is unmodeled, so it would" >&2
    echo "      run and observe nothing, forever, with no diagnostic (ARCHITECTURE.md §4-B)." >&2
    exit 1
fi
grep -q "is deliberately unmapped — it is part of the observation side" <<<"$es_err" \
    || { echo "FAIL: the refusal did not name the observation side; a bare 'no intrinsic" >&2
         echo "      mapping yet' reads as a missing overload somebody should add." >&2; exit 1; }
grep -q "System.Diagnostics.Tracing.EventListener" <<<"$es_err" \
    || { echo "FAIL: the refusal did not name EventListener" >&2; exit 1; }
grep -q "Remove the EventListener, or reimplement delivery in the runtime" <<<"$es_err" \
    || { echo "FAIL: the refusal named no remedy" >&2; exit 1; }
# The abort is raised inside CoreLib, so without the reach chain the message says nothing
# about the program that has to change. This is the tail that names it.
grep -q "EventListenerProbe.ProbeListener..ctor <- EventListenerProbe.Program.Main" <<<"$es_err" \
    || { echo "FAIL: the reach chain did not reach the caller's own listener class" >&2; exit 1; }
# generated*, not generated.cpp: emission streams its TUs out during compilation, so a
# probe on the last-written file cannot see a transpile that died mid-emission.
! compgen -G "$ES_OUT/generated*" >/dev/null \
    || { echo "FAIL: the refused transpile still emitted C++: $(ls -1 "$ES_OUT" | tr '\n' ' ')" >&2; exit 1; }
echo "refusal OK: exit $es_code, named the observation side + EventListener + a remedy + the caller, emitted nothing"

# Native String rows, Type construction faults, lossless attribute displays,
# and complete matching attribute construction before single-query ambiguity.
unset -f gate_extra_asserts
source gates/_ordinary-reflection.sh
gate_extra_asserts() {
    local out="$1" native line before prefix witness label simple mouth display
    native="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-native-7.stdout"
    gate_capture_ok "${native%.stdout}" run_bounded "$out/OrdinaryReflectionTypeLeaves$EXE_EXT" || return $?
    gate_output view "$native" "$native.records" --policy crlf
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-8.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded "$out/OrdinaryReflectionTypeLeaves$EXE_EXT" before-attribute-chain || return $?
    prefix="$native.before"
    gate_output select "$native" "$prefix" --before '== chained attribute Type roots ==' --policy crlf
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    for line in '== ordinary reflection type leaves ==' \
        'attribute Type property: LinkTarget' 'attribute construction: constructed from attribute' \
        '== boxed attribute arguments ==' \
        'Type identity: 10 of 10' 'Foreign enum names: 3 of 3' \
        'string props=2:True:True' 'runtime type reflection end' \
        'assembly load invalid name complete' 'ordinary reflection type leaves end' \
        '== chained attribute Type roots ==' 'chain holder=1' 'chain first=1' \
        'chain second=1' 'chain third=1' 'chain last=ChainFourth`1' \
        'chained attribute Type roots end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: ordinary type witness missing: $line" >&2; return 1; }
    done
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-9.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-nested-generic-names || return $?
    prefix="$native.before"
    gate_output select "$native" "$prefix" --before '== nested generic type names ==' --policy crlf
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    for line in '== nested generic type names ==' 'nested generic type names end' \
        'neighbor nested definition=NestedGenericTypeNameSubset.Outer+Box`1' \
        'neighbor flat definition=NestedGenericTypeNameSubset.Outer_Box`1' \
        'neighbor definitions distinct=True' 'reserved shared=String[]/Object[]' \
        'reserved closed definition=True' 'reserved open-only=dn2cpp_nested_neighbors.Open`1'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: nested generic type-name block missing: $line" >&2; return 1; }
    done
    for witness in 'top|Box`1' 'one class|Box`1' 'one struct|Point`1' \
        'two class|Box`1' 'two struct|Point`1' 'enclosing class|Box`1' \
        'enclosing struct|Point`1' 'enclosing node|Node' \
        'deep enclosing class|Box`1' 'deep enclosing struct|Point`1' \
        'flat neighbor|Outer_Box`1' 'escape neighbor|Outer_002BBox`1'; do
        label=${witness%%|*}
        simple=${witness#*|}
        for mouth in typeof instance delegate; do
            line="$label $mouth Name=$simple"
            gate_output witness "$native" "$line" --count any --policy crlf \
                || { echo "FAIL: nested generic type-name witness missing: $line" >&2; return 1; }
        done
    done
    if ! awk '
        FNR == 1 { body = 0 }
        { sub(/\r$/, "") }
        /^\/\/ dn2cpp_nested_neighbors[.]Closed_\$CnRef::ArrayName$/ { body = 1 }
        body && /dn2cpp_rgctx/ && /&gendef_dn2cpp_nested_/ { anchor = 1 }
        body && /^}$/ { body = 0 }
        END { exit !anchor }
    ' "$out"/generated*.cpp "$out/generated.h"; then
        echo 'FAIL: reserved definition symbol was not used by its shared receiver anchor' >&2
        return 1
    fi
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-10.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded "$out/OrdinaryReflectionTypeLeaves$EXE_EXT" before-property-accessors || return $?
    prefix="$native.before"
    gate_output select "$native" "$prefix" --before '== retained property accessor rows ==' --policy crlf
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    for line in '== retained property accessor rows ==' \
        'Capacity=True:True:get_Capacity:set_Capacity' \
        'Count=True:False:get_Count:<null>' \
        'System.Collections.Generic.ICollection<T>.IsReadOnly=True:False:System.Collections.Generic.ICollection<T>.get_IsReadOnly:<null>' \
        'retained property accessor rows end' '== nested open definition identity ==' \
        'definitions=True:True:True' 'direct nested flags=True:True:True:False:True' \
        'nested open definition identity end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: property accessor witness missing: $line" >&2; return 1; }
    done
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-11.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-attribute-display-code-units || return $?
    prefix="$native.before"
    gate_output select "$native" "$prefix" --before '== attribute display code units ==' --policy crlf
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    for line in '== attribute display code units ==' 'scalar high value=D800' \
        'scalar low value=DFFF' 'scalar nul value=0000' \
        'boxed array values=D800 DFFF 0000 03A9 empty=0 pair=D83D DE42' \
        'attribute display code units end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: attribute code-unit witness missing: $line" >&2; return 1; }
    done
    for label in 'scalar high' 'scalar low' 'scalar nul' 'boxed array' unicode empty; do
        grep -Eq "^$label display=" "$native.records" \
            || { echo "FAIL: attribute display not read: $label" >&2; return 1; }
    done
    for witness in 'scalar high|0027 D800 0027' 'scalar low|0027 DFFF 0027' \
        'scalar nul|0027 0000 0027' 'boxed array|0027 D800 0027' \
        'boxed array|0027 DFFF 0027' 'boxed array|0027 0000 0027' \
        'unicode|0022 03A9 D83D DE42 0022' 'empty|0022 0022'; do
        label=${witness%%|*}
        line=${witness#*|}
        display=$(grep -E "^$label display=" "$native.records") || return $?
        grep -Fq -- "$line" <<< "$display" \
            || { echo "FAIL: attribute display changed code units: $witness" >&2; return 1; }
    done
    grep -Eq '^extern const char md_display_[0-9]+\[\] = \{ \(char\)254,' "$out"/generated*.cpp \
        || { echo 'FAIL: lossless attribute display stream was not emitted' >&2; return 1; }
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-12.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-exception-object-members || return $?
    prefix="$native.before"
    gate_output select "$native" "$prefix" --before '== exception Object member lookup ==' --policy crlf
    gate_output compare "$prefix" "$before" --policy crlf
    for line in '== exception Object member lookup ==' \
        'Exception.GetHashCode=Object:Exception:True:False:454:base=Object:same=True' \
        'Exception.ToString=Exception:Exception:True:False:198:base=Object:same=True' \
        'NeverAllocatedFault.GetType=Exception:NeverAllocatedFault:False:False:134:base=Exception:same=True' \
        'DescribedFault.ToString=DescribedFault:DescribedFault:True:False:198:base=Object:same=True' \
        'IndexOutOfRangeException.Equals=Object:IndexOutOfRangeException:True:False:454:base=Object:same=True' \
        'exception Object member lookup end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: exception member witness missing: $line" >&2; return 1; }
    done
    for label in base allocated override raised 'unbound raised'; do
        gate_output witness "$native" "$label invoke=True:True:True:True" --count any --policy crlf \
            && gate_output witness "$native" "$label null receiver=TargetException:null" --count any --policy crlf \
            && gate_output witness "$native" "$label wrong receiver=TargetException:null" --count any --policy crlf \
            || { echo "FAIL: exception invocation or receiver validation changed: $label" >&2; return 1; }
    done
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-13.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-assembly-name-validation || return $?
    prefix="$native.before"
    gate_output select "$native" "$prefix" --before '== assembly name load validation ==' --policy crlf
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    for line in '== assembly name load validation ==' 'load null object param=assemblyRef' \
        'load null name param=<null>' 'load empty name file=[<Unknown>]' \
        'load blank name file=[" ", Culture=neutral, PublicKeyToken=null]' \
        'load string null param=assemblyName' 'load string empty param=assemblyName' \
        'load object identity=True' 'load string identity=True' 'assembly name load validation end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: assembly name validation witness missing: $line" >&2; return 1; }
    done
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-14.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-attribute-construction-order || return $?
    prefix="$native.before"
    gate_output select "$native" "$prefix" --before '== single attribute construction order ==' --policy crlf
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-15.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded "$out/OrdinaryReflectionTypeLeaves$EXE_EXT" before-attribute-construction-order || return $?
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    for line in '== single attribute construction order ==' 'single attribute construction order end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: attribute construction block missing: $line" >&2; return 1; }
    done
    local subject query ambiguity
    ambiguity="AmbiguousMatchException|8000211D|Multiple custom attributes of the same type 'ordered-1' found. trace=ctor:1,ctor:2,ctor:3,display:1"
    for subject in type method field property parameter assembly; do
        for query in static extension generic; do
            for line in "$subject $query/no-fault=$ambiguity" \
                "$subject $query/second-fault=InvalidOperationException|80131509|attribute fault 2 trace=ctor:1,ctor:2" \
                "$subject $query/third-fault=InvalidOperationException|80131509|attribute fault 3 trace=ctor:1,ctor:2,ctor:3"; do
                gate_output witness "$native" "$line" --count any --policy crlf \
                    || { echo "FAIL: single attribute order witness missing: $line" >&2; return 1; }
            done
        done
        for query in static generic; do
            for line in "$subject single-$query=value:1 trace=ctor:1" "$subject absent-$query=null trace="; do
                gate_output witness "$native" "$line" --count any --policy crlf \
                    || { echo "FAIL: single attribute filter witness missing: $line" >&2; return 1; }
            done
        done
        for line in "$subject defined-static=True trace=" "$subject defined-instance=True trace=" \
            "$subject absent-defined=False trace="; do
            gate_output witness "$native" "$line" --count any --policy crlf \
                || { echo "FAIL: constructor-free IsDefined witness missing: $line" >&2; return 1; }
        done
    done
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-16.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-method-attribute-inheritance || return $?
    prefix="$native.before"
    gate_output select "$native" "$prefix" --before '== inherited method attributes ==' --policy crlf
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-17.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded "$out/OrdinaryReflectionTypeLeaves$EXE_EXT" before-method-attribute-inheritance || return $?
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    for line in '== inherited method attributes ==' 'override:defined=True:constructors=0' \
        'override:inherited=hud-key,hud-multi,middle-multi,middle-single,base-multi' \
        'other-override:inherited=base-key,base-multi,base-single' 'new-slot:inherited=' \
        'generic-method:inherited=base-generic' 'declared-data=KeyDownAttribute,KeyAttribute' \
        'fresh=True' 'selected-base=1' 'non-inherited=0' \
        'type-inherited=1' 'type-declared=0' 'type-non-inherited=0' 'inherited method attributes end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: inherited attribute witness missing: $line" >&2; return 1; }
    done
    grep -Fwxq -- '// SelectedAttribute::.ctor' "$out"/generated*.cpp \
        || { echo 'FAIL: inherited attribute constructor absent' >&2; return 1; }
    if grep -Fwxq -- '// NonInheritedAttribute::.ctor' "$out"/generated*.cpp; then
        echo 'FAIL: non-inherited attribute constructor retained' >&2
        return 1
    fi
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-18.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-array-object-types || return $?
    prefix="$native.before"
    gate_output select "$native" "$prefix" --before '== allocated array object types ==' --policy crlf
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-19.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded "$out/OrdinaryReflectionTypeLeaves$EXE_EXT" before-array-object-types || return $?
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    for line in '== allocated array object types ==' 'empty array element=empty target' \
        'filled array identity=True:True' 'filled array element=declared element' \
        'escaped array element=escaped target' 'allocated array object types end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: allocated array type witness missing: $line" >&2; return 1; }
    done
    for subject in EmptyArrayElement FilledArrayElement EscapedArrayElement; do
        grep -Fwxq -- "// $subject::Run" "$out"/generated*.cpp "$out/generated.h" \
            || { echo "FAIL: selected array element body absent: $subject.Run" >&2; return 1; }
        if grep -Fwxq -- "// $subject::Unselected" "$out"/generated*.cpp "$out/generated.h"; then
            echo "FAIL: unrelated array element body retained: $subject.Unselected" >&2
            return 1
        fi
    done
    if grep -Fwxq -- '// ArrayContentsElement::Run' "$out"/generated*.cpp "$out/generated.h"; then
        echo 'FAIL: array contents changed the selected element type' >&2
        return 1
    fi
}
# Helpers that read a Type parameter's attributes or construct a Type an attribute
# carries are preserved explicitly; their callers' typeof arguments select nothing.
ORDINARY_TYPE_LEAVES_KEEP=(--link-xml samples/dotnet/ReflectTypes/keep-ordinary-type-leaves.xml)
DN2CPP_STRICT_COMPLETION=1 ordinary_fixture_diff_gate ReflectTypes OrdinaryReflectionTypeLeaves \
    System.Collections System.ComponentModel.Primitives --no-ildiet "${ORDINARY_TYPE_LEAVES_KEEP[@]}"
DN2CPP_OUT_SUFFIX=-diet DN2CPP_STRICT_COMPLETION=1 \
    ordinary_fixture_diff_gate ReflectTypes OrdinaryReflectionTypeLeaves \
    System.Collections System.ComponentModel.Primitives "${ORDINARY_TYPE_LEAVES_KEEP[@]}"
DN2CPP_OUT_SUFFIX=-native DN2CPP_STRICT_COMPLETION=1 \
    ordinary_fixture_diff_gate ReflectTypes OrdinaryReflectionTypeLeaves \
    System.Collections System.ComponentModel.Primitives --no-ildiet --no-metadata-compression \
    "${ORDINARY_TYPE_LEAVES_KEEP[@]}"
# With no attribute read, the closed owner's retained row must reach the deepest
# Type argument. Interning may share that row with its generic definition.
gate_extra_asserts() {
    local out="$1" native
    $py gates/fixtures/unread-attribute-rows/check-chain.py "$out" || return $?
    native="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-native-20.stdout"
    gate_capture_ok "${native%.stdout}" run_bounded "$out/UnreadAttributeRowsOnly$EXE_EXT" || return $?
    gate_output witness "$native" 'chain holder: ChainHolder' --count any --policy crlf \
        || { echo "FAIL: attribute chain block did not run" >&2; return 1; }
}
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} gates/fixtures/unread-attribute-rows/check-chain.py" \
DN2CPP_STRICT_COMPLETION=1 ordinary_fixture_diff_gate ReflectTypes UnreadAttributeRowsOnly --no-ildiet
unset -f gate_extra_asserts
DN2CPP_STRICT_COMPLETION=1 ordinary_fixture_diff_gate ReflectTypes DataOnlyAttributeRowsOnly --no-ildiet
signature_abstract_refusal() {
    local file="$1" name="$2"
    gate_output match-record "$file" "^sigabstract $name Mixed .*: this closed generic instantiation has no compiled code; fix: name the closed generic instantiation statically\$" --count any --policy crlf
}
gate_extra_asserts() {
    local out="$1" native line before prefix name path tail expected_tail
    native="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-native-21.stdout"
    gate_capture_ok "${native%.stdout}" run_bounded "$out/ReflectionTemplateDispatch$EXE_EXT" || return $?
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-22.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded "$out/ReflectionTemplateDispatch$EXE_EXT" before-template-members || return $?
    prefix="$native.before"
    gate_output select "$native" "$prefix" --before '== template members ==' --policy crlf
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-23.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded "$out/ReflectionTemplateDispatch$EXE_EXT" before-template-function-pointers || return $?
    prefix="$native.before"
    gate_output select "$native" "$prefix" --before '== template function pointers ==' --policy crlf
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    prefix="$native.before"
    gate_output select "$native" "$prefix" --before '== missing reflection invocation diagnostics ==' --policy crlf
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-24.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded "$out/ReflectionTemplateDispatch$EXE_EXT" before-invoke-diagnostics || return $?
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-25.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-invoke-diagnostics || return $?
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    prefix="$native.before"
    gate_output select "$native" "$prefix" --before '== reflected template delegates ==' --policy crlf
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-26.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded "$out/ReflectionTemplateDispatch$EXE_EXT" before-reflected-delegates || return $?
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-27.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-reflected-delegates || return $?
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    prefix="$native.before"
    gate_output select "$native" "$prefix" --before '== reflected template arrays ==' --policy crlf
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-28.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded "$out/ReflectionTemplateDispatch$EXE_EXT" before-reflected-arrays || return $?
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-29.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-reflected-arrays || return $?
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    prefix="$native.before"
    gate_output select "$native" "$prefix" --before '== fixed template function pointers ==' --policy crlf
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-30.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded "$out/ReflectionTemplateDispatch$EXE_EXT" before-fixed-function-pointers || return $?
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-31.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-fixed-function-pointers || return $?
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    prefix="$native.before"
    gate_output select "$native" "$prefix" --before '== template delegate method identity ==' --policy crlf
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-32.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded "$out/ReflectionTemplateDispatch$EXE_EXT" before-template-delegate-method-identity || return $?
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-33.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-template-delegate-method-identity || return $?
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    prefix="$native.before"
    gate_output select "$native" "$prefix" --before '== template signature placeholders ==' --policy crlf
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-34.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded "$out/ReflectionTemplateDispatch$EXE_EXT" before-template-signature-placeholders || return $?
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-35.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-template-signature-placeholders || return $?
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    prefix="$native.before"
    gate_output select "$native" "$prefix" --before '== template reflection consumers ==' --policy crlf
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-36.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded "$out/ReflectionTemplateDispatch$EXE_EXT" before-template-consumers || return $?
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-37.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-template-consumers || return $?
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    # Reflection a template's body, generic method, member listing, field listing or
    # interface found by name runs is admitted for an argument the program names in no
    # instantiation.
    for line in '== template reflection consumers ==' 'factory made=product' \
        'generic method=pair:Int32,String' 'members describe=listed:Int64' 'fields Stamp=stamp:7' \
        'constrained object=refused' 'interface named=named:Int16' 'template reflection consumers end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: template reflection consumer witness missing: $line" >&2; return 1; }
    done
    prefix="$native.before"
    gate_output select "$native" "$prefix" --before '== template generic arguments ==' --policy crlf
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-38.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded "$out/ReflectionTemplateDispatch$EXE_EXT" before-template-arguments || return $?
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-39.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-template-arguments || return $?
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    # A clone over a known argument closes another type or method over its exact
    # instantiation, and a CoreLib instance formed for its metadata answers from its row.
    for line in '== template generic arguments ==' \
        'outer ReflectRuntimeInstantiationSubset.ArgumentOuter`1[ReflectRuntimeInstantiationSubset.ArgumentInner`1[System.Int32]] field=True value=inner:Int32' \
        'plain plain:ArgumentInner`1 argument=True' \
        'method name:ReflectRuntimeInstantiationSubset.ArgumentInner`1[System.Int32]' \
        'spoken say:Int64 holder=True' 'library definition=Create:True' \
        'library closed=Create:String:True' 'template generic arguments end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: template generic argument witness missing: $line" >&2; return 1; }
    done
    prefix="$native.before"
    gate_output select "$native" "$prefix" --before '== template signature allocation ==' --policy crlf
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-40.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded "$out/ReflectionTemplateDispatch$EXE_EXT" before-template-signature-allocation || return $?
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-41.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-template-signature-allocation || return $?
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    for line in '== template signature allocation ==' 'ordinary dispatch=base' \
        'metadata rows=3' 'signature row=Payload' \
        'constructed dispatch=constructed:String' 'static call=static:String' \
        'template signature allocation end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: template signature allocation witness missing: $line" >&2; return 1; }
    done
    prefix="$native.before"
    gate_output select "$native" "$prefix" --before '== forbidden generic arguments ==' --policy crlf
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-42.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded "$out/ReflectionTemplateDispatch$EXE_EXT" before-forbidden-generic-arguments || return $?
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-43.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-forbidden-generic-arguments || return $?
    gate_output compare "$prefix" "$before" --policy crlf || return $?
    for line in '== forbidden generic arguments ==' 'valid=Int32:False' \
        "void known=ArgumentException: The type 'System.Void' may not be used as a type argument." \
        "void opaque=ArgumentException: The type 'System.Void' may not be used as a type argument." \
        "pointer=ArgumentException: The type 'System.Int32*' may not be used as a type argument." \
        "byref=ArgumentException: The type 'System.Int32&' may not be used as a type argument." \
        "function pointer=ArgumentException: The type 'System.Int32()' may not be used as a type argument." \
        'open=T:True' 'valid tuple=Int32,String:False' 'open tuple=T,String:True' \
        'void,null=ArgumentNullException: Value cannot be null.' \
        'null,void=ArgumentNullException: Value cannot be null.' \
        "void,parameter=ArgumentException: The type 'System.Void' may not be used as a type argument." \
        "parameter,void=ArgumentException: The type 'System.Void' may not be used as a type argument." \
        "int,void=ArgumentException: The type 'System.Void' may not be used as a type argument." \
        "void,extra=ArgumentException: The number of generic arguments provided doesn't equal the arity of the generic type definition. (Parameter 'typeArguments')" \
        "null,extra=ArgumentException: The number of generic arguments provided doesn't equal the arity of the generic type definition. (Parameter 'typeArguments')" \
        'forbidden generic arguments end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: forbidden generic argument witness missing: $line" >&2; return 1; }
    done
    for line in '== reflection template dispatch ==' 'reflection template dispatch end' \
        '== template members ==' \
        'members Int32 get identity: True/True/True/True/True/True' \
        'members String set identity: True/True/True/True/True' \
        'sub-members identity: True/True/False/False/True' 'template members end' \
        '== template function pointers ==' 'fnptr Int32: row=True mismatched=False' \
        'fnptr Int64: row=True mismatched=False' 'fnptr String: row=True mismatched=False' \
        'template function pointers end' \
        '== missing reflection invocation diagnostics ==' 'diag control=True' \
        'missing reflection invocation diagnostics end' \
        '== reflected template delegates ==' 'reflected template delegates end' \
        '== reflected template arrays ==' 'reflected template arrays end' \
        '== fixed template function pointers ==' 'fixed template function pointers end' \
        'fixedfn Int32 row=True' 'fixedfn Int64 row=True' 'fixedfn String row=True' \
        '== template delegate method identity ==' 'template delegate method identity end' \
        'method identity Int32 rows=True' 'method identity Int64 rows=True' \
        'method identity String rows=True' 'method identity Pair rows=True' \
        '== template signature placeholders ==' 'template signature placeholders end' \
        'sigplace Int32,String Fixed parameter=Dn2Cpp.SignatureSlot`1[System.Int64]' \
        'sigplace Guid,Boolean Fixed parameter=Dn2Cpp.SignatureSlot`1[System.Int64]' \
        'sigabstract Int32,String Pair safe=True' 'sigabstract Guid,Boolean Pair safe=True' \
        'sigabstract Int32,String Plain result:plain:String' 'sigabstract Guid,Boolean Plain result:plain:Boolean' \
        'sigabstract Int32,String Mixed safe=True' 'sigabstract Guid,Boolean Mixed safe=True' \
        'sigremedy Int32 Kept result:kept:Int32' 'sigremedy Guid Kept result:kept:Guid' \
        'sigremedy Int32 Bare safe=True' 'sigremedy Guid Bare safe=True'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: template dispatch witness missing: $line" >&2; return 1; }
    done
    for name in Int32 Int64 String; do
        for line in "diag $name summary: row=True mismatched=False" \
            "diag $name validation=TargetException/TargetParameterCountException/ArgumentException/ArgumentException"; do
            gate_output witness "$native" "$line" --count any --policy crlf \
                || { echo "FAIL: missing-body invocation control missing: $line" >&2; return 1; }
        done
    done
    for name in Int32 Int64 String Pair; do
        for line in "bound $name invoke=$name" \
            "bound $name identity: target=True method=Kind declaring=True"; do
            gate_output witness "$native" "$line" --count any --policy crlf \
                || { echo "FAIL: reflected template delegate witness missing: $line" >&2; return 1; }
        done
        for line in "method identity $name bound: operator=True equals=True reference=True reflected=True" \
            "method identity $name invoke=$name" \
            "method identity $name created: operator=True equals=True reference=True reflected=True"; do
            gate_output witness "$native" "$line" --count any --policy crlf \
                || { echo "FAIL: template delegate method identity missing: $line" >&2; return 1; }
        done
    done
    for name in Int32 Int64 Byte String Pair; do
        for line in "array $name row=True" "array $name delegate=$name[]" \
            "array $name identity: target=True method=ArrayName declaring=True" \
            "array $name storage: length=2 element=True stored=True" \
            "array $name negative=OverflowException"; do
            gate_output witness "$native" "$line" --count any --policy crlf \
                || { echo "FAIL: reflected array template witness missing: $line" >&2; return 1; }
        done
    done
    for path in Invoke CreateDelegate; do
        for line in "fixedfn Int32 $path=result:int:42" "fixedfn Int64 $path=result:int:-7" \
            "fixedfn String $path=result:int:0"; do
            gate_output witness "$native" "$line" --count any --policy crlf \
                || { echo "FAIL: fixed function pointer result missing: $line" >&2; return 1; }
        done
    done
    # No clone shares a body that calls through a function pointer over its type
    # parameter. Exact, so a call that runs or refuses differently shows.
    native="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-native-44.stdout"
    gate_capture_ok "${native%.stdout}" run_bounded "$out/ReflectionTemplateDispatch$EXE_EXT" template-function-pointer-outcomes || return $?
    prefix="$native.before"
    gate_output select "$native" "$prefix" --before '== fixed template function pointers ==' --policy crlf
    gate_assert_text "$prefix" "$(printf '%s\n' '== template function pointers ==' \
        'fnptr Int32 outcome: refused PlatformNotSupportedException' \
        'fnptr Int64 outcome: refused PlatformNotSupportedException' \
        'fnptr String outcome: refused PlatformNotSupportedException' \
        'template function pointers end')" || return $?
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-45.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" template-function-pointer-outcomes || return $?
    tail="$native.fixed-tail"
    expected_tail="$before.fixed-tail"
    gate_output select "$native" "$tail" --from '== fixed template function pointers ==' --policy crlf
    gate_output select "$before" "$expected_tail" --from '== fixed template function pointers ==' --policy crlf
    gate_output compare "$tail" "$expected_tail" || return $?
    for line in '== fixed template function pointers ==' 'fixed template function pointers end' \
        'fixedfn Int32 row=True' 'fixedfn Int64 row=True' 'fixedfn String row=True'; do
        gate_output witness "$tail" "$line" --count any --policy crlf \
            || { echo "FAIL: fixed function pointer outcome witness missing: $line" >&2; return 1; }
    done
    # A row whose signature names a placeholder-bearing class no clone respells
    # refuses; a row over closed arguments runs.
    native="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-native-46.stdout"
    gate_capture_ok "${native%.stdout}" run_bounded "$out/ReflectionTemplateDispatch$EXE_EXT" template-signature-placeholder-outcomes || return $?
    gate_output view "$native" "$native.records" --policy crlf
    for line in 'sigplace Int32,String Fixed result:fixed:Int32' 'sigplace Guid,Boolean Fixed result:fixed:Guid'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: template signature placeholder result missing: $line" >&2; return 1; }
    done
    for name in 'Int32,String' 'Guid,Boolean'; do
        for row in Swapped Second Mixed; do
            grep -Eq "^sigplace $name $row PlatformNotSupportedException\\|80131539\\|.*: this closed generic instantiation has no compiled code; fix: " "$native.records" \
                || { echo "FAIL: $name $row placeholder signature row did not refuse" >&2; return 1; }
        done
        # The base row of a placeholder-bearing signature refuses as itself, before any
        # thunk reads the argument, not through the clone's override slot.
        grep -Fq -- "sigabstract $name Mixed PlatformNotSupportedException|80131539|Dn2Cpp.SignatureAbstract\`2[System.${name/,/,System.}]::System.String Mixed(" "$native.records" \
            && signature_abstract_refusal "$native" "$name" \
            || { echo "FAIL: $name abstract Mixed base row did not refuse" >&2; return 1; }
    done
    # A clone's unretained override refuses with the remedy that retains it: Kept, whose
    # printed preserve rule keep-template-signatures.xml applies, runs; Bare, which no
    # clone can run, names the closed instantiation.
    for name in Int32 Guid; do
        for line in "sigremedy $name Kept result:kept:$name" \
            "sigremedy $name Bare PlatformNotSupportedException|80131539|Dn2Cpp.RemedyImpl\`1[System.$name]::Bare in ReflectionTemplateDispatch: this closed generic instantiation has no compiled code; fix: name the closed generic instantiation statically"; do
            gate_output witness "$native" "$line" --count any --policy crlf \
                || { echo "FAIL: template override remedy outcome missing: $line" >&2; return 1; }
        done
    done
    # An abstract level a deriving template clones serves MakeGenericType, so it reports
    # no ClosedGeneric row.
    if grep -E 'SignatureAbstract`2|RemedyBase`1' "$out/reflection-unrecognized.tsv"; then
        echo "FAIL: a definition a template level serves reports ClosedGeneric" >&2
        return 1
    fi
    native="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-native-47.stdout"
    gate_capture_ok "${native%.stdout}" run_bounded "$out/ReflectionTemplateDispatch$EXE_EXT" invoke-diagnostics || return $?
    gate_output view "$native" "$native.records" --policy crlf
    for name in Int32 Int64 String; do
        for path in Invoke DoNotWrap CreateDelegate CreateDelegateFalse; do
            grep -Eq "^diag $name $path=PlatformNotSupportedException\\|80131539\\|" "$native.records" \
                || { echo "FAIL: $name $path missing-body refusal is not a platform exception" >&2; return 1; }
        done
        for line in "diag $name same refusal=True" \
            "diag $name message owner=True member=True reason=True unwrapped=True"; do
            gate_output witness "$native" "$line" --count any --policy crlf \
                || { echo "FAIL: incomplete shared invocation diagnosis: $line" >&2; return 1; }
        done
    done
    for line in '== missing reflection invocation diagnostics ==' 'diag control=True' \
        'missing reflection invocation diagnostics end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: invocation diagnosis block missing: $line" >&2; return 1; }
    done
}
TEMPLATE_SIGNATURES_KEEP=(--link-xml samples/dotnet/ReflectTypes/keep-template-signatures.xml)
DN2CPP_STRICT_COMPLETION=1 ordinary_fixture_diff_gate ReflectTypes ReflectionTemplateDispatch --no-ildiet \
    "${TEMPLATE_SIGNATURES_KEEP[@]}"
DN2CPP_OUT_SUFFIX=-native DN2CPP_STRICT_COMPLETION=1 \
    ordinary_fixture_diff_gate ReflectTypes ReflectionTemplateDispatch --no-ildiet --no-metadata-compression \
    "${TEMPLATE_SIGNATURES_KEEP[@]}"
unset -f gate_extra_asserts

# An isolated owner supplies the used base slot, a signature-only derived template,
# a constructed derived template, and a template entered only through a static row.
gate_extra_asserts() {
    local out="$1" native line symbol
    native="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-native-48.stdout"
    gate_capture_ok "${native%.stdout}" run_bounded "$out/TemplateSignatureAllocationOnly$EXE_EXT" || return $?
    for line in '== template signature allocation ==' 'ordinary dispatch=base' \
        'metadata rows=3' 'signature row=Payload' \
        'constructed dispatch=constructed:String' 'static call=static:String' \
        'template signature allocation end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: isolated template allocation witness missing: $line" >&2; return 1; }
    done
    for symbol in 'Dn2Cpp.SignatureAllocationBase::Describe' \
        'Dn2Cpp.SignatureConstructedPayload_$CnAny0::Describe' \
        'Dn2Cpp.SignatureStaticOwner_$CnAny0::Call'; do
        grep -Fwq -- "// $symbol" "$out"/generated*.cpp "$out/generated.h" \
            || { echo "FAIL: allocated template body control missing: $symbol" >&2; return 1; }
    done
    for symbol in 'Dn2Cpp.SignatureOnlyPayload_$CnAny0::Describe' \
        'Dn2Cpp.SignatureMetadataOwner_$CnAny0::Describe' \
        'Dn2Cpp.SignatureStaticOwner_$CnAny0::Describe' \
        'Dn2Cpp.SignatureOnlyDependency::Marker'; do
        if grep -Fwq -- "// $symbol" "$out"/generated*.cpp "$out/generated.h"; then
            echo "FAIL: metadata-only template retained an unused body: $symbol" >&2
            return 1
        fi
    done
    native="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-native-49.stdout"
    gate_capture_ok "${native%.stdout}" run_bounded "$out/TemplateSignatureAllocationOnly$EXE_EXT" uninitialized-template-outcomes || return $?
    for line in '== uninitialized template outcomes ==' \
        'static-only blank=PlatformNotSupportedException' 'static-only remedy=True:True:True' \
        'constructed blank=constructed:String' 'uninitialized template outcomes end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: unknown template allocation outcome missing: $line" >&2; return 1; }
    done
}
DN2CPP_STRICT_COMPLETION=1 ordinary_fixture_diff_gate ReflectTypes TemplateSignatureAllocationOnly --no-ildiet
DN2CPP_OUT_SUFFIX=-diet DN2CPP_STRICT_COMPLETION=1 \
    ordinary_fixture_diff_gate ReflectTypes TemplateSignatureAllocationOnly
DN2CPP_OUT_SUFFIX=-native DN2CPP_STRICT_COMPLETION=1 \
    ordinary_fixture_diff_gate ReflectTypes TemplateSignatureAllocationOnly --no-ildiet --no-metadata-compression
unset -f gate_extra_asserts

# Unknown generic arguments retain no executable target. Independent static calls,
# finite constraint proofs and bodyless runtime-template identity remain usable.
# Freeze catchable refusals and exact site diagnostics in both sharing modes.
closed_generic_asserts() {
    local out="$1" project="$2" report="$1-report" native oracle line kind assembly method offset api reason fix type prefix before
    native="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-native-50.stdout"
    gate_capture_ok "${native%.stdout}" run_bounded "$out/$project$EXE_EXT" || return $?
    for line in '== closed generic diagnostics ==' 'closed generic diagnostics end' \
        'named cell:Int32 holder:Int32 name:Int32' 'holder named: holder:Int32' 'method named: name:Int32' \
        '== closed generic receivers ==' 'named nest:Int32' 'definition parameter: Holder`1' \
        'method parameter: Name' 'closed generic receivers end' \
        '== closed generic templates ==' 'closed generic templates end' \
        '== static generic constraint proof ==' 'static generic constraint proof end' \
        '== template fallback constraint proof ==' 'template fallback constraint proof end'; do
        gate_output witness "$native" "$line" --count 1 --policy crlf \
            || { echo "FAIL: closed generic diagnostics block must run once: $line" >&2; return 1; }
    done
    if grep -Eq '^// ClosedGenericDiagnostics\.(Factory[^:]*::Make|Nest[^:]*::Wrap)$' "$out"/generated*.cpp; then
        echo 'FAIL: unknown generic arguments retained Factory.Make or Nest.Wrap' >&2
        return 1
    fi
    oracle="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-oracle-51.stdout"
    gate_capture_ok "${oracle%.stdout}" run_bounded dotnet "$_CG_APP" || return $?
    gate_output view "$oracle" "$oracle.records" --policy crlf
    for line in 'constraint interface control: dispose:Disposable' 'constraint invariant control: tag:RightTag' \
        'constraint template body control: List`1' 'constraint template method control: Int32'; do
        gate_output witness "$native" "$line" --count any --policy crlf && gate_output witness "$oracle" "$line" --count any --policy crlf \
            || { echo "FAIL: a valid constraint tuple was not instantiated: $line" >&2; return 1; }
    done
    for type in NeedsDispose_Int32Arr NeedsTag_ClosedGenericDiagnostics_WrongTag \
        NeedsTemplateBodyTag_ClosedGenericDiagnostics_WrongTag NeedsTemplateMethodTag_ClosedGenericDiagnostics_WrongTag; do
        if grep -Eqw "[[:alnum:]_]*ClosedGenericDiagnostics_${type}[[:alnum:]_]*" \
            "$out"/generated.h "$out"/generated*.cpp; then
            echo "FAIL: an unproved constraint tuple was instantiated: $type" >&2
            return 1
        fi
    done
    for type in NeedsDispose_ClosedGenericDiagnostics_Disposable NeedsTag_ClosedGenericDiagnostics_RightTag \
        NeedsTemplateBodyTag_ClosedGenericDiagnostics_RightTag NeedsTemplateMethodTag_ClosedGenericDiagnostics_RightTag; do
        grep -qw "ti_ClosedGenericDiagnostics_$type" "$out"/generated.h \
            || { echo "FAIL: a proved constraint tuple was not instantiated: $type" >&2; return 1; }
    done
    for kind in array invariant "template body" "template method"; do
        grep -q "^constraint $kind rejected: ArgumentException " "$oracle.records" \
            || { echo "FAIL: the CLR did not reject the invalid $kind constraint" >&2; return 1; }
    done
    before="${out:-${_CG_OUT:-artifacts/reflect-types-observations}}/check-before-52.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded "$out/$project$EXE_EXT" before-template-constraint-fallbacks || return $?
    prefix="$native.before"
    gate_output select "$native" "$prefix" --before '== template fallback constraint proof ==' --policy crlf
    gate_output compare "$before" "$prefix" --policy crlf || return $?
    diff -u <(printf '%s\n' "${closed_generic_rows[@]}") \
        <(awk -F '\t' -v assembly="$project" '$2 == assembly' "$out/reflection-unrecognized.tsv") \
        || { echo "FAIL: $project ClosedGeneric report rows changed" >&2; return 1; }
    rm -rf "$report"
    invoke_cli "$_CG_APP" -r "$_CG_CORELIB" "${closed_generic_flags[@]}" -o "$report" \
        > /dev/null 2> "$report.stderr" || { cat "$report.stderr" >&2; return 1; }
    cmp "$out/reflection-unrecognized.tsv" "$report/reflection-unrecognized.tsv" \
        || { echo "FAIL: $project report differs between identical transpiles" >&2; return 1; }
    while IFS=$'\t' read -r kind assembly method offset api reason fix; do
        grep -Fxq -- "dn2cpp: warning: reflection target not recognized: $assembly $method $offset $api: $reason" \
            "$report.stderr" && grep -Fxq -- "  fix: $fix" "$report.stderr" \
            || { echo "FAIL: $project lacks the $kind warning for $method $offset" >&2; return 1; }
    done < <(printf '%s\n' "${closed_generic_rows[@]}")
}
gate_extra_asserts() {
    closed_generic_asserts "$1" ClosedGenericDiagnosticsOnly
}
closed_generic_flags=(--no-ildiet)
closed_generic_rows=(
    $'ClosedGeneric\tClosedGenericDiagnosticsOnly\tClosedGenericDiagnostics.Program::InvalidArray()\tIL_001d\tSystem.Type.MakeGenericType(Type[])\tthe constraints of ClosedGenericDiagnostics.NeedsDispose`1 over these type arguments cannot be decided ahead of time, and it has no runtime-instantiation template; only instantiations the program names resolve\tname the closed generic instantiation statically'
    $'ClosedGeneric\tClosedGenericDiagnosticsOnly\tClosedGenericDiagnostics.Program::InvalidInvariant()\tIL_001d\tSystem.Type.MakeGenericType(Type[])\tthe constraints of ClosedGenericDiagnostics.NeedsTag`1 over these type arguments cannot be decided ahead of time, and it has no runtime-instantiation template; only instantiations the program names resolve\tname the closed generic instantiation statically'
    $'ClosedGeneric\tClosedGenericDiagnosticsOnly\tClosedGenericDiagnostics.Program::InvalidTemplateBody()\tIL_002e\tSystem.Reflection.MethodBase.Invoke(Object, Object[])\tthe constraints of ClosedGenericDiagnostics.NeedsTemplateBodyTag`1 over these type arguments cannot be proved, and its runtime-instantiation template cannot serve this member; only instantiations the program names resolve\tname the closed generic instantiation statically'
    $'ClosedGeneric\tClosedGenericDiagnosticsOnly\tClosedGenericDiagnostics.Program::InvalidTemplateMethod()\tIL_003f\tSystem.Reflection.MethodInfo.MakeGenericMethod(Type[])\tthe generic method comes from a method of a runtime-instantiated generic type whose constraints over these type arguments cannot be proved; only instantiations the program names resolve\tname the closed generic instantiation statically'
    $'Execution\tClosedGenericDiagnosticsOnly\tClosedGenericDiagnostics.Program::InvalidTemplateMethod()\tIL_0046\tSystem.Reflection.MethodBase.Invoke(Object, Object[])\tMethodInfo comes from a method of a runtime-instantiated generic type whose constraints over these type arguments cannot be proved — no method body retained\t[Preserve] on the target, or link.xml <assembly fullname="ASSEMBLY"><type fullname="TYPE" preserve="all" /></assembly>'
    $'ClosedGeneric\tClosedGenericDiagnosticsOnly\tClosedGenericDiagnostics.Program::MakeCell(System.Type)\tIL_0014\tSystem.Type.MakeGenericType(Type[])\tthe type arguments come from parameter 1, and ClosedGenericDiagnostics.Cell`1 has no runtime-instantiation template; only instantiations the program names resolve\tname the closed generic instantiation statically'
    $'Execution\tClosedGenericDiagnosticsOnly\tClosedGenericDiagnostics.Program::MakeCell(System.Type)\tIL_0026\tSystem.Activator.CreateInstance(Type)\tType comes from MakeGenericType over parameter 1 — no constructor retained\t[Preserve] on the target, or link.xml <assembly fullname="ASSEMBLY"><type fullname="TYPE"><method name=".ctor" /></type></assembly>'
    $'Execution\tClosedGenericDiagnosticsOnly\tClosedGenericDiagnostics.Program::MakeCell(System.Type)\tIL_002c\tSystem.Reflection.MethodBase.Invoke(Object, Object[])\tMethodInfo comes from GetMethod("Describe") on a Type that comes from MakeGenericType over parameter 1 — no method body retained\t[Preserve] on the target, or link.xml <assembly fullname="ASSEMBLY"><type fullname="TYPE"><method name="Describe" /></type></assembly>'
    $'Execution\tClosedGenericDiagnosticsOnly\tClosedGenericDiagnostics.Program::MakeFactory(System.Type)\tIL_002b\tSystem.Activator.CreateInstance(Type)\tType comes from MakeGenericType over parameter 1 — no constructor retained\t[Preserve] on the target, or link.xml <assembly fullname="ASSEMBLY"><type fullname="TYPE"><method name=".ctor" /></type></assembly>'
    $'Execution\tClosedGenericDiagnosticsOnly\tClosedGenericDiagnostics.Program::MakeFactory(System.Type)\tIL_0031\tSystem.Reflection.MethodBase.Invoke(Object, Object[])\tMethodInfo comes from GetMethod("Make") on a Type that comes from MakeGenericType over parameter 1 — no method body retained\t[Preserve] on the target, or link.xml <assembly fullname="ASSEMBLY"><type fullname="TYPE"><method name="Make" /></type></assembly>'
    $'ClosedGeneric\tClosedGenericDiagnosticsOnly\tClosedGenericDiagnostics.Program::MakeFromDefinition(System.Type)\tIL_0014\tSystem.Type.MakeGenericType(Type[])\tthe generic definition comes from parameter 1; only instantiations the program names resolve\tname the closed generic instantiation statically'
    $'ClosedGeneric\tClosedGenericDiagnosticsOnly\tClosedGenericDiagnostics.Program::MakeFromMethod(System.Reflection.MethodInfo)\tIL_0014\tSystem.Reflection.MethodInfo.MakeGenericMethod(Type[])\tthe generic method comes from parameter 1; only instantiations the program names resolve\tname the closed generic instantiation statically'
    $'Execution\tClosedGenericDiagnosticsOnly\tClosedGenericDiagnostics.Program::MakeHolder(System.Type)\tIL_0026\tSystem.Activator.CreateInstance(Type)\tType comes from MakeGenericType over parameter 1 — no constructor retained\t[Preserve] on the target, or link.xml <assembly fullname="ASSEMBLY"><type fullname="TYPE"><method name=".ctor" /></type></assembly>'
    $'Execution\tClosedGenericDiagnosticsOnly\tClosedGenericDiagnostics.Program::MakeHolder(System.Type)\tIL_002c\tSystem.Reflection.MethodBase.Invoke(Object, Object[])\tMethodInfo comes from GetMethod("Describe") on a Type that comes from MakeGenericType over parameter 1 — no method body retained\t[Preserve] on the target, or link.xml <assembly fullname="ASSEMBLY"><type fullname="TYPE"><method name="Describe" /></type></assembly>'
    $'ClosedGeneric\tClosedGenericDiagnosticsOnly\tClosedGenericDiagnostics.Program::MakeName(System.Type)\tIL_001e\tSystem.Reflection.MethodInfo.MakeGenericMethod(Type[])\tthe type arguments come from parameter 1; only instantiations of ClosedGenericDiagnostics.Names::Name the program names resolve\tname the closed generic instantiation statically'
    $'Execution\tClosedGenericDiagnosticsOnly\tClosedGenericDiagnostics.Program::MakeName(System.Type)\tIL_0025\tSystem.Reflection.MethodBase.Invoke(Object, Object[])\tMethodInfo comes from parameter 1 — no method body retained\t[Preserve] on the target, or link.xml <assembly fullname="ASSEMBLY"><type fullname="TYPE" preserve="all" /></assembly>'
    $'Execution\tClosedGenericDiagnosticsOnly\tClosedGenericDiagnostics.Program::MakeNest(System.Type)\tIL_002b\tSystem.Activator.CreateInstance(Type)\tType comes from MakeGenericType over parameter 1 — no constructor retained\t[Preserve] on the target, or link.xml <assembly fullname="ASSEMBLY"><type fullname="TYPE"><method name=".ctor" /></type></assembly>'
    $'Execution\tClosedGenericDiagnosticsOnly\tClosedGenericDiagnostics.Program::MakeNest(System.Type)\tIL_0031\tSystem.Reflection.MethodBase.Invoke(Object, Object[])\tMethodInfo comes from GetMethod("Wrap") on a Type that comes from MakeGenericType over parameter 1 — no method body retained\t[Preserve] on the target, or link.xml <assembly fullname="ASSEMBLY"><type fullname="TYPE"><method name="Wrap" /></type></assembly>'
    $'Execution\tClosedGenericDiagnosticsOnly\tClosedGenericDiagnostics.Program::MakeShape(System.Type)\tIL_0040\tSystem.Activator.CreateInstance(Type)\tType comes from MakeGenericType over parameter 1 — no constructor retained\t[Preserve] on the target, or link.xml <assembly fullname="ASSEMBLY"><type fullname="TYPE"><method name=".ctor" /></type></assembly>'
    $'Execution\tClosedGenericDiagnosticsOnly\tClosedGenericDiagnostics.Program::MakeShape(System.Type)\tIL_0046\tSystem.Reflection.MethodBase.Invoke(Object, Object[])\tMethodInfo comes from GetMethod("Describe") on a Type that comes from MakeGenericType over parameter 1 — no method body retained\t[Preserve] on the target, or link.xml <assembly fullname="ASSEMBLY"><type fullname="TYPE"><method name="Describe" /></type></assembly>'
    $'ClosedGeneric\tClosedGenericDiagnosticsOnly\tClosedGenericDiagnostics.Program::MakeWrapped(System.Type)\tIL_002c\tSystem.Type.MakeGenericType(Type[])\tthe type arguments come from MakeGenericType over parameter 1, and ClosedGenericDiagnostics.Cell`1 has no runtime-instantiation template; only instantiations the program names resolve\tname the closed generic instantiation statically'
    $'Execution\tClosedGenericDiagnosticsOnly\tClosedGenericDiagnostics.Program::MakeWrapped(System.Type)\tIL_003e\tSystem.Activator.CreateInstance(Type)\tType comes from MakeGenericType over MakeGenericType over parameter 1 — no constructor retained\t[Preserve] on the target, or link.xml <assembly fullname="ASSEMBLY"><type fullname="TYPE"><method name=".ctor" /></type></assembly>'
    $'Execution\tClosedGenericDiagnosticsOnly\tClosedGenericDiagnostics.Program::MakeWrapped(System.Type)\tIL_0044\tSystem.Reflection.MethodBase.Invoke(Object, Object[])\tMethodInfo comes from GetMethod("Describe") on a Type that comes from MakeGenericType over MakeGenericType over parameter 1 — no method body retained\t[Preserve] on the target, or link.xml <assembly fullname="ASSEMBLY"><type fullname="TYPE"><method name="Describe" /></type></assembly>'
)
DN2CPP_SAMPLE_PROJECT_DIR=samples/dotnet/ReflectTypes DN2CPP_STRICT_COMPLETION=1 \
    corelib_freeze_gate ClosedGenericDiagnosticsOnly "$(dirname "$0")/expected/reflect-types-closed-generic.txt" \
    "${closed_generic_flags[@]}"
# Without shared generics no template serves Holder<T> either. The same source
# builds as ClosedGenericDiagnosticsUnshared because corelib_freeze_gate keys
# its artifacts by project name.
gate_extra_asserts() {
    closed_generic_asserts "$1" ClosedGenericDiagnosticsUnshared
}
closed_generic_flags=(--no-ildiet --no-shared-generics)
closed_generic_rows=(
    $'ClosedGeneric\tClosedGenericDiagnosticsUnshared\tClosedGenericDiagnostics.Program::InvalidArray()\tIL_001d\tSystem.Type.MakeGenericType(Type[])\tthe constraints of ClosedGenericDiagnostics.NeedsDispose`1 over these type arguments cannot be decided ahead of time, and it has no runtime-instantiation template; only instantiations the program names resolve\tname the closed generic instantiation statically'
    $'ClosedGeneric\tClosedGenericDiagnosticsUnshared\tClosedGenericDiagnostics.Program::InvalidInvariant()\tIL_001d\tSystem.Type.MakeGenericType(Type[])\tthe constraints of ClosedGenericDiagnostics.NeedsTag`1 over these type arguments cannot be decided ahead of time, and it has no runtime-instantiation template; only instantiations the program names resolve\tname the closed generic instantiation statically'
    $'ClosedGeneric\tClosedGenericDiagnosticsUnshared\tClosedGenericDiagnostics.Program::InvalidTemplateBody()\tIL_001d\tSystem.Type.MakeGenericType(Type[])\tthe constraints of ClosedGenericDiagnostics.NeedsTemplateBodyTag`1 over these type arguments cannot be decided ahead of time, and it has no runtime-instantiation template; only instantiations the program names resolve\tname the closed generic instantiation statically'
    $'ClosedGeneric\tClosedGenericDiagnosticsUnshared\tClosedGenericDiagnostics.Program::InvalidTemplateMethod()\tIL_001d\tSystem.Type.MakeGenericType(Type[])\tthe constraints of ClosedGenericDiagnostics.NeedsTemplateMethodTag`1 over these type arguments cannot be decided ahead of time, and it has no runtime-instantiation template; only instantiations the program names resolve\tname the closed generic instantiation statically'
    $'ClosedGeneric\tClosedGenericDiagnosticsUnshared\tClosedGenericDiagnostics.Program::MakeCell(System.Type)\tIL_0014\tSystem.Type.MakeGenericType(Type[])\tthe type arguments come from parameter 1, and ClosedGenericDiagnostics.Cell`1 has no runtime-instantiation template; only instantiations the program names resolve\tname the closed generic instantiation statically'
    $'Execution\tClosedGenericDiagnosticsUnshared\tClosedGenericDiagnostics.Program::MakeCell(System.Type)\tIL_0026\tSystem.Activator.CreateInstance(Type)\tType comes from MakeGenericType over parameter 1 — no constructor retained\t[Preserve] on the target, or link.xml <assembly fullname="ASSEMBLY"><type fullname="TYPE"><method name=".ctor" /></type></assembly>'
    $'Execution\tClosedGenericDiagnosticsUnshared\tClosedGenericDiagnostics.Program::MakeCell(System.Type)\tIL_002c\tSystem.Reflection.MethodBase.Invoke(Object, Object[])\tMethodInfo comes from GetMethod("Describe") on a Type that comes from MakeGenericType over parameter 1 — no method body retained\t[Preserve] on the target, or link.xml <assembly fullname="ASSEMBLY"><type fullname="TYPE"><method name="Describe" /></type></assembly>'
    $'ClosedGeneric\tClosedGenericDiagnosticsUnshared\tClosedGenericDiagnostics.Program::MakeFactory(System.Type)\tIL_0014\tSystem.Type.MakeGenericType(Type[])\tthe type arguments come from parameter 1, and ClosedGenericDiagnostics.Factory`1 has no runtime-instantiation template; only instantiations the program names resolve\tname the closed generic instantiation statically'
    $'Execution\tClosedGenericDiagnosticsUnshared\tClosedGenericDiagnostics.Program::MakeFactory(System.Type)\tIL_002b\tSystem.Activator.CreateInstance(Type)\tType comes from MakeGenericType over parameter 1 — no constructor retained\t[Preserve] on the target, or link.xml <assembly fullname="ASSEMBLY"><type fullname="TYPE"><method name=".ctor" /></type></assembly>'
    $'Execution\tClosedGenericDiagnosticsUnshared\tClosedGenericDiagnostics.Program::MakeFactory(System.Type)\tIL_0031\tSystem.Reflection.MethodBase.Invoke(Object, Object[])\tMethodInfo comes from GetMethod("Make") on a Type that comes from MakeGenericType over parameter 1 — no method body retained\t[Preserve] on the target, or link.xml <assembly fullname="ASSEMBLY"><type fullname="TYPE"><method name="Make" /></type></assembly>'
    $'ClosedGeneric\tClosedGenericDiagnosticsUnshared\tClosedGenericDiagnostics.Program::MakeFromDefinition(System.Type)\tIL_0014\tSystem.Type.MakeGenericType(Type[])\tthe generic definition comes from parameter 1; only instantiations the program names resolve\tname the closed generic instantiation statically'
    $'ClosedGeneric\tClosedGenericDiagnosticsUnshared\tClosedGenericDiagnostics.Program::MakeFromMethod(System.Reflection.MethodInfo)\tIL_0014\tSystem.Reflection.MethodInfo.MakeGenericMethod(Type[])\tthe generic method comes from parameter 1; only instantiations the program names resolve\tname the closed generic instantiation statically'
    $'ClosedGeneric\tClosedGenericDiagnosticsUnshared\tClosedGenericDiagnostics.Program::MakeHolder(System.Type)\tIL_0014\tSystem.Type.MakeGenericType(Type[])\tthe type arguments come from parameter 1, and ClosedGenericDiagnostics.Holder`1 has no runtime-instantiation template; only instantiations the program names resolve\tname the closed generic instantiation statically'
    $'Execution\tClosedGenericDiagnosticsUnshared\tClosedGenericDiagnostics.Program::MakeHolder(System.Type)\tIL_0026\tSystem.Activator.CreateInstance(Type)\tType comes from MakeGenericType over parameter 1 — no constructor retained\t[Preserve] on the target, or link.xml <assembly fullname="ASSEMBLY"><type fullname="TYPE"><method name=".ctor" /></type></assembly>'
    $'Execution\tClosedGenericDiagnosticsUnshared\tClosedGenericDiagnostics.Program::MakeHolder(System.Type)\tIL_002c\tSystem.Reflection.MethodBase.Invoke(Object, Object[])\tMethodInfo comes from GetMethod("Describe") on a Type that comes from MakeGenericType over parameter 1 — no method body retained\t[Preserve] on the target, or link.xml <assembly fullname="ASSEMBLY"><type fullname="TYPE"><method name="Describe" /></type></assembly>'
    $'ClosedGeneric\tClosedGenericDiagnosticsUnshared\tClosedGenericDiagnostics.Program::MakeName(System.Type)\tIL_001e\tSystem.Reflection.MethodInfo.MakeGenericMethod(Type[])\tthe type arguments come from parameter 1; only instantiations of ClosedGenericDiagnostics.Names::Name the program names resolve\tname the closed generic instantiation statically'
    $'Execution\tClosedGenericDiagnosticsUnshared\tClosedGenericDiagnostics.Program::MakeName(System.Type)\tIL_0025\tSystem.Reflection.MethodBase.Invoke(Object, Object[])\tMethodInfo comes from parameter 1 — no method body retained\t[Preserve] on the target, or link.xml <assembly fullname="ASSEMBLY"><type fullname="TYPE" preserve="all" /></assembly>'
    $'ClosedGeneric\tClosedGenericDiagnosticsUnshared\tClosedGenericDiagnostics.Program::MakeNest(System.Type)\tIL_0014\tSystem.Type.MakeGenericType(Type[])\tthe type arguments come from parameter 1, and ClosedGenericDiagnostics.Nest`1 has no runtime-instantiation template; only instantiations the program names resolve\tname the closed generic instantiation statically'
    $'Execution\tClosedGenericDiagnosticsUnshared\tClosedGenericDiagnostics.Program::MakeNest(System.Type)\tIL_002b\tSystem.Activator.CreateInstance(Type)\tType comes from MakeGenericType over parameter 1 — no constructor retained\t[Preserve] on the target, or link.xml <assembly fullname="ASSEMBLY"><type fullname="TYPE"><method name=".ctor" /></type></assembly>'
    $'Execution\tClosedGenericDiagnosticsUnshared\tClosedGenericDiagnostics.Program::MakeNest(System.Type)\tIL_0031\tSystem.Reflection.MethodBase.Invoke(Object, Object[])\tMethodInfo comes from GetMethod("Wrap") on a Type that comes from MakeGenericType over parameter 1 — no method body retained\t[Preserve] on the target, or link.xml <assembly fullname="ASSEMBLY"><type fullname="TYPE"><method name="Wrap" /></type></assembly>'
    $'ClosedGeneric\tClosedGenericDiagnosticsUnshared\tClosedGenericDiagnostics.Program::MakeShape(System.Type)\tIL_0014\tSystem.Type.MakeGenericType(Type[])\tthe type arguments come from parameter 1, and ClosedGenericDiagnostics.Shape`1 has no runtime-instantiation template; only instantiations the program names resolve\tname the closed generic instantiation statically'
    $'ClosedGeneric\tClosedGenericDiagnosticsUnshared\tClosedGenericDiagnostics.Program::MakeShape(System.Type)\tIL_002e\tSystem.Type.MakeGenericType(Type[])\tthe type arguments come from parameter 1, and ClosedGenericDiagnostics.Square`1 has no runtime-instantiation template; only instantiations the program names resolve\tname the closed generic instantiation statically'
    $'Execution\tClosedGenericDiagnosticsUnshared\tClosedGenericDiagnostics.Program::MakeShape(System.Type)\tIL_0040\tSystem.Activator.CreateInstance(Type)\tType comes from MakeGenericType over parameter 1 — no constructor retained\t[Preserve] on the target, or link.xml <assembly fullname="ASSEMBLY"><type fullname="TYPE"><method name=".ctor" /></type></assembly>'
    $'Execution\tClosedGenericDiagnosticsUnshared\tClosedGenericDiagnostics.Program::MakeShape(System.Type)\tIL_0046\tSystem.Reflection.MethodBase.Invoke(Object, Object[])\tMethodInfo comes from GetMethod("Describe") on a Type that comes from MakeGenericType over parameter 1 — no method body retained\t[Preserve] on the target, or link.xml <assembly fullname="ASSEMBLY"><type fullname="TYPE"><method name="Describe" /></type></assembly>'
    $'ClosedGeneric\tClosedGenericDiagnosticsUnshared\tClosedGenericDiagnostics.Program::MakeWrapped(System.Type)\tIL_0026\tSystem.Type.MakeGenericType(Type[])\tthe type arguments come from parameter 1, and ClosedGenericDiagnostics.Holder`1 has no runtime-instantiation template; only instantiations the program names resolve\tname the closed generic instantiation statically'
    $'ClosedGeneric\tClosedGenericDiagnosticsUnshared\tClosedGenericDiagnostics.Program::MakeWrapped(System.Type)\tIL_002c\tSystem.Type.MakeGenericType(Type[])\tthe type arguments come from MakeGenericType over parameter 1, and ClosedGenericDiagnostics.Cell`1 has no runtime-instantiation template; only instantiations the program names resolve\tname the closed generic instantiation statically'
    $'Execution\tClosedGenericDiagnosticsUnshared\tClosedGenericDiagnostics.Program::MakeWrapped(System.Type)\tIL_003e\tSystem.Activator.CreateInstance(Type)\tType comes from MakeGenericType over MakeGenericType over parameter 1 — no constructor retained\t[Preserve] on the target, or link.xml <assembly fullname="ASSEMBLY"><type fullname="TYPE"><method name=".ctor" /></type></assembly>'
    $'Execution\tClosedGenericDiagnosticsUnshared\tClosedGenericDiagnostics.Program::MakeWrapped(System.Type)\tIL_0044\tSystem.Reflection.MethodBase.Invoke(Object, Object[])\tMethodInfo comes from GetMethod("Describe") on a Type that comes from MakeGenericType over MakeGenericType over parameter 1 — no method body retained\t[Preserve] on the target, or link.xml <assembly fullname="ASSEMBLY"><type fullname="TYPE"><method name="Describe" /></type></assembly>'
)
DN2CPP_SAMPLE_PROJECT_DIR=samples/dotnet/ReflectTypes DN2CPP_STRICT_COMPLETION=1 \
    corelib_freeze_gate ClosedGenericDiagnosticsUnshared \
    "$(dirname "$0")/expected/reflect-types-closed-generic-unshared.txt" "${closed_generic_flags[@]}"
unset -f gate_extra_asserts closed_generic_asserts
