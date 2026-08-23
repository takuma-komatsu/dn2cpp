#!/usr/bin/env bash
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
# real .NET: Type.MakeGenericType/MakeArrayType over an instantiation not
# statically reached throw NotSupportedException (the AOT boundary), the
# MakeArrayType(rank) overload always throws it (MD array types — including
# the rank-1 T[*] — are not modeled),
# CustomAttributeData's argument views throw PlatformNotSupportedException,
# ParameterInfo custom modifiers answer from the method signature (including the
# init-only setter return modreq); DefaultValue throws (the Constant blob is not
# carried; HasDefaultValue/IsOut answer exactly from the attributes word), and
# Type.GetGenericParameterConstraints throws it where real .NET throws
# InvalidOperationException (no generic-parameter Type materializes), the
# dynamic-codegen section's Compile()/DynamicMethod throw where the JIT-backed
# runtime succeeds, GetMethod(name, genericParameterCount, types) matches
# the image's CLOSED generic rows (real .NET matches the open definition's
# parameter types, so gm-gen1-closed returns null there), and the assembly
# section's loud cut throws the catchable PlatformNotSupportedException where
# real .NET probes the loader (Assembly.LoadFile -> FileNotFoundException).
# GetManifestResourceStream is no longer among them — it answers for real over
# carried blobs, and build-and-run-manifest-resources.sh diffs that
# surface against real .NET.
#
# The indexer-edge section (ReflectIndexerEdgeSubset) freezes the indexed-
# property error paths: an index-count mismatch surfaces as the accessor
# invoker's ArgumentException (real .NET: TargetParameterCountException, not
# modeled — the MethodInfo.Invoke posture), and reading a set-only / writing a
# get-only property as the null accessor's InvalidOperationException (real
# .NET: ArgumentException). The happy paths live in the reflect-invoke live
# diff (ReflectActivatorSubset).
#
# The generic-method section (ReflectGenericMethodSubset) freezes the AOT
# boundary of the per-closed-instantiation methtab model (no open generic
# method rows exist in the image): GetMethod surfaces a CLOSED instantiation,
# so IsGenericMethodDefinition answers False (real .NET: the open definition,
# True) and GetGenericArguments reports the closed arguments (Int32, not T) —
# on the GetGenericMethodDefinition view too, whose ContainsGenericParameters
# stays False; MakeGenericMethod over a never-reached instantiation throws the
# catchable PlatformNotSupportedException (real .NET JITs it); and re-making
# the in-image instantiation returns the row the lookup surfaced, comparing
# EQUAL to the GetMethod result (real .NET: open definition != closed method).
#
# The MakeGenericType-seed section (ReflectMakeGenericSeedSubset) covers the
# canonical-wrapper seed (Compilation.CollectionInterfaceWrapperDefs): a member
# declared at an abstract BCL collection interface makes the interface's
# canonical materialization (List<T>, ReadOnlyCollection<T>, HashSet<T>,
# Dictionary<K,V>, ReadOnlyDictionary<K,V>) MakeGenericType-resolvable and
# ctor-invokable — the Newtonsoft deserialization shape that blocked Thrive's
# AchievementsManager.PerformLoad. Its positive lines match real .NET; the one
# frozen divergence is the unseeded ReadOnlyCollection<double>, where real .NET
# constructs the type and dn2cpp throws the catchable NotSupportedException
# whose message names the missing instantiation (the AOT boundary).
#
# The serializer-wrapper-seed section (ReflectSerializerWrapperSeedSubset)
# covers its sibling (Compilation.CollectSerializerWrapperSeeds): a collection
# shape on the reflection-ctor route's named surface — creator ARGUMENTS
# included, which List<NoteLine> asserts by being named only as a ctor
# parameter — makes the serializer's OWN adapters (Newtonsoft.Json.Utilities.
# CollectionWrapper<T> / DictionaryWrapper<K,V>, resolved by (ns, name) against
# the loaded modules; the section hosts stand-ins with the real shapes)
# MakeGenericType-resolvable and ctor-invokable — the JsonArrayContract.
# CreateWrapper shape that blocked Thrive's boot at SimulationParameters.
# LoadRegistry (VersionPatchNotes) and Tutorial.AlreadySeenTutorials.Load.
# Its one frozen divergence is the unseeded CollectionWrapper<Unlisted>, the
# same AOT boundary as above.
#
# The array temporary-list transcription (ArrayAssignabilitySubset's Biome
# flow) covers the SZArray seed (Compilation.CollectArrayTemporaryListSeed): an
# ARRAY member's deserialization builds the items into a
# typeof(List<>).MakeGenericType(elem) temporary (Newtonsoft's
# JsonArrayContract.CreateTemporaryCollection -> GetConstructors() ->
# parameterless ConstructorInfo.Invoke) before Array.CreateInstance + CopyTo —
# and the program names NO static List<Biome> anywhere, the exact Thrive blind
# spot (SimulationParameters.LoadRegistry, "Unable to find default constructor
# for ... List_MusicContext"): the earlier MusicContext flow statically
# constructs its List ("eseed"), which is why it kept passing while Thrive
# died. All its lines match real .NET.
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
# The shallow-clone refusal section (ReflectShallowCloneRefusalSubset) is
# NOT about reflection, despite arriving through a reflective Invoke: its subject
# is which hand-written RUNTIME STRUCTS may be copied bitwise. Object.MemberwiseClone
# works for the intrinsic-represented reference types (the agreeing half
# is diffed live in the reflect-invoke bucket, MemberwiseCloneSubset section 5); the
# seven types carrying DN2CPP_TF_NO_SHALLOW_CLONE keep a loud refusal because their
# representation OWNS native state — a native-heap allocation, an embedded
# mutex/condition variable, a running std::thread, a slot in a process-wide registry —
# so a copy would be a second owner rather than a shallow copy. Real .NET clones all
# seven, so this is a declared divergence like the marshal-verdict rows above, and its
# last line is a NEGATIVE control (CancellationTokenSource, an intrinsic type that does
# clone) whose job is to go red if the refusal set ever widens by accident.
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
# The OWNED-HANDLE RESIDUE section (BoundHandleResidueSubset) is in this bucket for
# the same reason TypeIdentitySubset is, and its subject is likewise not reflection:
# it is what a RUNTIME-OWNED Dn2CppTypeInfo answers about its own members, i.e. which
# facts about the CLR the C++ runtime hand-writes. Its field lines — String, Decimal,
# TimeSpan, DateTime, DateTimeOffset, ConstructorInfo, StackTrace, StackFrame,
# ResourceManager, WaitHandle, each row with its FieldType, DeclaringType, flag triple,
# raw attributes word and VALUE — all match real .NET, and pin values that nothing else
# in the tree states twice (DateTime.UnixEpoch's tick count and Utc kind,
# Decimal.MaxValue's 96-bit mantissa, TimeSpan's tick constants). Its five
# "DIVERGES" lines are the ticket's DECLINED half, asserted so the decision is visible
# rather than silent: Type/Module carry no field table (their values are MemberFilter/
# TypeFilter delegates and Missing.Value), no owned handle carries a method or property
# table (its members are intrinsic — lowered at the call site, with no C++ body an
# invoker could name), and enumeration of an owned handle's interfaces stays empty while
# the well-known-interface pair-gate beside it answers exactly like .NET. Delete the
# DIVERGES lines and the boundary becomes a silence again.
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
# (intrinsic levels are shape-ineligible), so the transpile completes and the
# mint throws the catchable NotSupportedException where real .NET constructs.
#
# Every other line matches real .NET (verified against `dotnet run` at capture
# time).
source "$(dirname "$0")/_common.sh"

EXPFILE="$(dirname "$0")/expected/reflect-types.txt"
BCL=(System.Linq.Expressions System.Linq System.Collections \
    System.Reflection.Emit System.Reflection.Emit.Lightweight System.Reflection.Emit.ILGeneration \
    System.ComponentModel.Primitives System.Collections.Concurrent)

corelib_freeze_gate ReflectTypes "$EXPFILE" "${BCL[@]}"

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
invoke_cli "$app" "${refs[@]}" --trim-reflection -o "$out"
if gate_cache_check "$out" "reflect-types-trim|$corelib" \
        "$app" "${app%.dll}.runtimeconfig.json" "${app%.dll}.deps.json" "$EXPFILE"; then
    gate_cache_hit_msg
else
    compile_console "$out" ReflectTypes
    set +e
    native=$("./$out/ReflectTypes"); native_code=$?
    set -e
    assert_output "$(strip_cr_win "$native")" "$(cat "$EXPFILE")"
    assert_exit_code "$native_code" 0
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
# is where TypeMetadataEmitter.NoteReflectedMemberArrayElements notes every array a
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
import re, glob
# `const Dn2CppTypeInfo ti_arr_<key> = { "<clrname>", nullptr, 0, nullptr, <itfs>, <n>,`
row = re.compile(r'^const Dn2CppTypeInfo (ti_arr_\S+) = \{ "[^"]*", nullptr, 0, nullptr, '
                 r'(\S+), (\d+),')
total = relation = 0
for f in sorted(glob.glob('artifacts/reflecttypes/generated*.cpp')):
    for line in open(f, encoding='utf-8', errors='replace'):
        m = row.match(line)
        if not m:
            continue
        total += 1
        if m.group(2).startswith('arrgenitf_'):
            relation += 1
        if m.group(2) == 'nullptr' or m.group(3) == '0':
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
# Every Dn2CppTypeInfo the runtime DEFINES, by C++ symbol. The definition shape is
# `[static] [const] Dn2CppTypeInfo <sym> =` followed by a brace, optionally behind
# any nesting of the dn2cpp_ti_with_* wrappers (TimeSpan/DateTime/Decimal use two);
# nothing else in the tree matches it.
runtime = set()
for f in glob.glob('runtime/**/*.cpp', recursive=True):
    src = open(f, encoding='utf-8', errors='replace').read()
    for m in re.finditer(
            r'\b(?:static\s+)?(?:const\s+)?Dn2CppTypeInfo\s+(dn2cpp_[A-Za-z0-9_]+)\s*=\s*'
            r'(?:dn2cpp_ti_with_[a-z_]+\(\s*)*\{', src):
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
