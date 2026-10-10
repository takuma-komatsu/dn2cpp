#!/usr/bin/env bash
# Runtime-owned class method groups share direct-call dispatch and callable identity.
# Virtual delegate equality and removal retain the selected method under identical-code folding.
# Property accessor arrays retain visibility, order, reflected handle identity and boxed invocation.
# Delegate method names select strict signatures, inherited private methods and virtual slots.
# Delegate ABI compatibility retains by-ref, pointer and function-pointer signature identity.
# Unanchored intrinsic pointer pointees retain binding identity independently of Invoke boxing.
# Array shapes and constant nested arguments distinguish runtime pointer overloads.
# Actual array elements and constant leaves retain known type-name inequality without a Type-object cache.
# Closed public generic arguments retain family and child identity without a Type-object cache.
# Known pointee names skip incompatible runtime-generic argument and family overloads.
# Opaque generic pointees and dynamic array arguments preserve overload inequality.
# Named constructors run on existing receivers; cold initializer binds do not initialize.
# Direct initializer calls repeat the body, and constructor Delegate.Method faults.
# Application-derived types retain callable cold initializers from library bases.
# By-value delegate binding matches enums to their exact underlying type by name and MethodInfo.
# Canonical function-pointer descriptors preserve Invoke checks independently of binding identity.
# Unsupported by-ref referents retain distinct identities without enabling Invoke marshalling.
# Unresolved template by-ref/pointer identities refuse binding after known mismatches.
# Provably incompatible template overloads permit a fixed body without selecting a coincident wrong target.
# Invoke replaces the canonical Missing singleton with recorded defaults and copies back only after success.
# A delegate constructor refuses a target whose method-load origin was lost.
# Virtual and generic virtual reflection dispatch, by-reference copy-back,
# null-bound delegates, DynamicInvoke and catchable stripped-body refusals, which a
# nested reflective call raises to the outer call as a fault of its target.
# DefaultBinder primitive widening and specificity differ from Invoke's ushort-to-char policy.
# The metadata codec probe checks shared missing-body/invoker refusals through every entry.
# Generic method lookups and enumeration return one definition with stable parameter identities.
# Formal parameters retain their defining assembly and refuse allocation and open type composition.
# Mixed member lookups share generic definition identities; open signature types and constraints are refused.
# A formal parameter's declaring and reflected types share the typical owner, independent of the lookup.
# Special constraints classify formal parameters; unavailable unused signatures never force eager decode.
# Metadata-answerable generic definitions retain formal names and special attributes.
# Formal parameters remain TypeInfo members while reporting a declaring owner and IsNested.
# A metadata-only generic definition retains closed signature types that nothing else reaches.
# Optional definition dependencies are probed without publishing partial or self-expanding layouts.
# Concrete interface dispatch traps retain signature dependencies that relation-only rows do not need.
# Completion-signature layout probes remain bounded to the original owner snapshot.
# A class first named by an abstract slot's signature retains callable attribute constructors.
# Consolidated reflection-invocation gate. Merges the former reflect dynamic-use
# subset gates into one multi-section program, transpiled once against the
# tree-shaken real CoreLib and diffed exactly against real .NET. Covers:
#   MethodInfo.Invoke (instance/static, args, return boxing, void, private,
#   and target exception wrapping), the receiver, arity and argument checks
#   MethodInfo/ConstructorInfo/PropertyInfo run before the target with .NET's
#   messages and the by-value argument conversions they accept
#   (ReflectInvokeValidationSubset, which also pins that a CreateDelegate-bound
#   delegate skips those checks, that a Nullable<T> result boxes as .NET's, and
#   that a boxed built-in or a string passes the argument check for every CLR
#   interface its type implements and fails it for one it does not),
#   delegate/interface dynamic dispatch via reflection, FieldInfo.GetValue/SetValue
#   (instance/static/value-type/unbox), and a reflection-driven serializer
#   (attribute-named members + enum names).
# Also covers reflected member-handle IDENTITY (ReflectMemberIdentitySubset):
# the runtime interns Field/Method/Property/ConstructorInfo wrappers per
# metadata row like real .NET's RuntimeType member cache, so ReferenceEquals /
# virtual Equals / GetHashCode / HashSet dedup / List<MemberInfo>.Contains
# agree across repeated Get* calls — including the Newtonsoft
# GetSerializableMembers two-enumeration Contains-selection shape whose
# fresh-handle failure silently dropped every unattributed public member from
# the serialization contract (Thrive's MembraneType boot blocker).
# Two non-reflecting sections live here because the surface they exercise is the
# same "the real body reflects, so it is lowered inline" lane: ActivatorSubset
# (Activator.CreateInstance<T> / the new() constraint idiom, including an
# intrinsic-mapped reference T whose ctor is never transpiled and an intrinsic
# value T) and EventSubset (field-like `event` += / -= / invoke, whose
# compiler-generated accessors run through Interlocked.CompareExchange, plus the
# integral Interlocked overloads).
# MemberwiseCloneSubset's section 5 is in this bucket for the CoreLib surface
# it needs, not for its theme: its SUBJECT is the runtime's instance-extent model, and
# what it asserts is that a clone of an INTRINSIC-represented reference type — a
# StringBuilder, an exception (the opaque shells, whose extent is derived from the
# ALLOCATOR's floor rather than from a stamped number), a CancellationTokenSource, a
# ThreadLocal<T>, a Type handle, a CultureInfo — has the same shallow-copy semantics
# real .NET gives. A truncated clone would still print a plausible line for most of
# them, which is why the exception rows read a field that lives PAST the header. The
# seven types dn2cpp still refuses are frozen in the reflect-types bucket
# (ReflectShallowCloneRefusalSubset); the finalizability of a clone is asserted in the
# finalizers bucket (FinalizerClonedSubset).
# GetInterfaceSubset asserts Type.GetInterface(name[, ignoreCase]) against
# real .NET: simple/namespace-qualified matching, ignoreCase folding the simple-name
# part ONLY (a wrong-cased namespace misses even under ignoreCase), closed generics
# matched by the definition's mangled simple name, AmbiguousMatchException on two
# matching rows, null on no match, ArgumentNullException on a null name. Its tail is
# a second subject: the single-attribute getters (Attribute.GetCustomAttribute, the
# CustomAttributeExtensions member and Assembly forms) throw a catchable — and
# exactly-typed — AmbiguousMatchException when a base-typed filter matches two
# attribute rows, member-level and assembly-level both.
# ReflectedTypeSubset asserts MemberInfo.ReflectedType and the
# (row, reflectedType)-keyed handle identity it forces: typeof(D).GetMethod(m) !=
# typeof(Base).GetMethod(m) for an inherited m (==, .Equals, HashSet count 2)
# while same-type queries stay ReferenceEquals-identical (the Newtonsoft Contains
# selection); plus the mint-side normalizations measured on real .NET —
# delegate.Method and GetBaseDefinition answer the DECLARING-typed instance,
# MakeGenericMethod propagates the receiver's reflected type where
# GetGenericMethodDefinition normalizes it away, a property's GetGetMethod
# inherits the property handle's reflected type, and ParameterInfo.Member is the
# very instance GetParameters was called on.
# ReflectDelegateIdentitySubset asserts Delegate.Method for IL-bound delegates:
# class and generic virtual overrides (new-slot hiders and covariant returns
# included), interface bindings over class, struct, explicit, default and
# generic implementations, array generic arguments, and runtime-owned declaring
# types, which may answer null but never a wrong method. Its interface section
# pins the selected method for competing plain and explicit generic bodies in
# either metadata order, and for a derived interface's override of a default
# over class, struct, inherited, typed, generic, identical-body and
# MakeGenericType receivers, and the generic virtual body a MakeGenericType
# receiver runs and reports: a derived interface's generic override and an
# inherited class generic override. Its interface generic dispatch section pins
# which body a call binds: explicit overloads through a plain and a closed generic
# interface, a plain overload beside an explicit sibling, and explicit bodies
# for an interface whose name extends the called one's or differs in arity.
# Its interface redeclaration section pins which class level supplies an
# interface body, plain and generic, for the call and Delegate.Method: a level
# listing the interface again prefers its own public method to a base's
# explicit body, a level that does not list it neither displaces the inherited
# body with a same-name method or hider nor hides a default, a subclass
# override takes the class slot the mapping chose (abstract bases included),
# and a base without the interface fills a listing level's empty slot, over
# closed generic interfaces, shared generic classes and MakeGenericType receivers.
# Its runtime-level section pins the generic virtual body a MakeGenericType
# receiver runs when one of the instantiation's own generic levels declares it,
# and the method Delegate.Method reports on that level: an override of a generic
# base's method with a base call, over a constructed and an unconstructed base,
# a two-parameter level, overrides of a non-generic base's method on the leaf and
# on a middle level (minted, or the image's own abstract type without that
# instantiation), and an interface implementation, beside a plain virtual and
# an interface method of the same instantiations and a delegate created from
# the plain virtual's reflected method row.
# LdftnLocalSubset uses hand-authored IL (gates/fixtures/ldftn-local/Program.cs
# rewrites the built sample) to store method pointers before delegate
# construction, separate ldftn from newobj with a nop or native-int conversion,
# select two targets through one local or a stack join, snapshot a loaded pointer
# before overwriting its local, store ldvirtftn, instance and int64-converted
# pointers, leave an unresolvable ldftn in code that never runs, and call a
# stored raw pointer through calli. The delegate address and method identity
# follow the selected pointer; calli keeps the raw address. Its Object MethodImpl
# section checks differently named slot bodies, inherited overrides and newslot
# hiders through delegates and callvirt. Only the stored-pointer bodies
# carry delegate tags. A local whose address is taken keeps no delegate identity,
# because a byref write would leave it stale: a delegate built from it is refused
# when transpiled, from a native-int or int64 local alike, and one built from a
# copy of it throws NotSupportedException when constructed. Address-taken locals
# beside a delegate in plain C# still transpile.
# LdftnLocalSubset's renamed-override section binds a class slot and the
# differently named MethodImpl body filling it through reflection: one delegate,
# with one hash code, alone and in a chain.
# ReflectToStringSubset asserts MethodInfo/ConstructorInfo/FieldInfo/PropertyInfo/
# ParameterInfo and CustomAttributeData signature display through typed, base, and
# object dispatch, including byref, indexer, generic-method, and attribute arguments.
# RuntimeHandleRelationSubset asserts the CLR relations of objects whose type-info
# the runtime writes by hand — the reflection objects, Assembly and Module,
# StringBuilder, Exception and the exceptions the runtime raises from real faults,
# the synchronization handles, Thread, Task, the culture wrappers — and of
# System.Array: the type test, IsAssignableFrom, BaseType chains, named interface
# membership and the invoke argument checks, then `using`, an IDisposable-typed
# Dispose and a reflected IDisposable.Dispose over the synchronization handles. The
# final section distinguishes Type, TypeInfo and runtime Type object identities,
# preserves Type clone/Object-family answers, and reads closed/open Task interfaces.
# Its greps pin the init-prologue installs those answers come from: the relation rows,
# SystemException spliced under the runtime NullReferenceException's handle, and
# SemaphoreSlim's IDisposable map.
# Mixed native/packed metadata preserves inherited members, closed generics,
# parameter identity, and interface receiver dispatch across cache eviction.
# Disabling compression forces native metadata even for explicit packed selectors.
# The emitted metadata block registry carries a packed block's constructor table
# extent, and an uncompressed image registers none.
# NoCompressMetadata and derived attributes select native owner/member metadata
# through class inheritance without changing containing types or interface users.
# NullDelegateTargetSubset checks delegate construction over a nonvirtual
# instance method: a null receiver faults when bound, before invocation.
# ReflectRouteClassSubset's attribute-minted section reads the attribute rows of a
# chain of closed generics that only the row before each one names: every level
# keeps its row, however deep the chain.
# ReflectBoundDelegateSubset's template accessor section binds the property
# accessors of MakeGenericType instantiations minted at run time, which are their
# template's rows: static ones open and over their first argument, instance ones
# over a receiver and over null, through every CreateDelegate overload and
# DynamicInvoke, with Delegate.Method, DeclaringType and equality per instantiation.
# ReflectInvokeValidationSubset's pointer return section boxes the unmanaged
# pointer results of Invoke and DynamicInvoke, by-ref ones included, as
# System.Reflection.Pointer and function pointers as IntPtr, and passes such a
# box to a pointer parameter only under .NET's pointer type rules: a pointer
# type counts every level, a function pointer type is its signature and calling
# convention, and a refused argument names the parameter type as .NET formats it.
# Former gates: reflect-invoke, reflect-dispatch, reflect-field-value,
# reflect-serializer, activator-subset, event-subset.
# Empty string MemberwiseClone retains a distinct reference.
# Delegate list removal, original-entry identity, real-body enumeration and GC cache.
# DelegateInvokeTargetSubset asserts that a delegate bound to another delegate's
# Invoke (a delegate-type conversion, an Invoke method group, or an ldvirtftn of
# Invoke) runs the source's invocation list: generic, multicast, by-ref,
# struct-returning, variant and headerless signatures, with .NET's Target, Method,
# DynamicInvoke and null-source answers.
# Null-bound delegates over a long chain or a dense cycle of receiver-forwarding
# non-virtual calls run every body .NET's do and fault where .NET's do.
# ReflectDelegateIdentitySubset's settled Object virtual section binds ToString,
# GetHashCode and Equals of an exception subclass that overrides them and of one
# that inherits those overrides: Delegate.Method names the override and its
# GetBaseDefinition Object's method, though the exception levels above carry no
# Object-member rows. A string, a boxed Int32 and an exception that overrides
# nothing inherit the member through such a level and may answer null.
# LdftnLocalSubset's renamed slot filler section binds interface and Object slots
# whose filler is a differently named MethodImpl body, an override of one or a
# newslot hider's inherited one: a delegate through the slot equals and hashes
# like one through the body, and Delegate.Method names the body, for
# reflection-bound and ldvirtftn delegates alike, on MakeGenericType
# instantiations and through a variant instantiation of the slot's interface.
# StrippedOverrideRefusals runs the stripped-body refusals in an image of their
# own, so no other section can compile the CoreLib overrides they need stripped.
# ReflectBindOnly makes CreateDelegate its program's only reflection call: the
# binding alone reaches each bound body and the types its delegate type names.
# ReflectFrameworkBind's last section passes Pointer boxes between methods of the
# application and ReflectReturnLib whose function pointer types name each one's
# same-named types: a box over one assembly's type is refused for a parameter over
# the other's with .NET's ArgumentException.
# Same-module TypeDef-parent MemberRefs bind overloads, instance and generic
# methods; closed generic owners retain their TypeSpec identity.
# Object-family enumeration includes inherited metadata answers under binding flags.
# Pointer field accessors box and validate unmanaged/function addresses with
# packed and native metadata, preserving static-readonly accessor refusal order.
# Unused definition signatures may omit unloaded generic layout dependencies;
# loaded dependencies retain their closed types, and body-required layouts still fail.
# Open definition signatures retain their rows without requiring their base's field layout.
source "$(dirname "$0")/_common.sh"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|reflection-measure:${DN2CPP_REFLECTION_MEASURE:-}|existing-constructor-prefix:${DN2CPP_BEFORE_EXISTING_CONSTRUCTOR:-}|cold-activator-prefix:${DN2CPP_BEFORE_COLD_ACTIVATOR:-}|delegate-method-prefix:${DN2CPP_BEFORE_DELEGATE_METHOD:-}|ldftn-local-prefix:${DN2CPP_BEFORE_LDFTN_LOCAL:-}|invoke-validation-prefix:${DN2CPP_BEFORE_INVOKE_VALIDATION:-}|runtime-handle-relations-prefix:${DN2CPP_BEFORE_RUNTIME_HANDLE_RELATIONS:-}"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|interface-selection-prefix:${DN2CPP_BEFORE_INTERFACE_SELECTION:-}|interface-redeclaration-prefix:${DN2CPP_BEFORE_INTERFACE_REDECLARATION:-}|runtime-level-gvm-prefix:${DN2CPP_BEFORE_RUNTIME_LEVEL_GVM:-}"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|typedef-memberref-prefix:${DN2CPP_BEFORE_TYPEDEF_MEMBERREF:-}|primitive-binder-prefix:${DN2CPP_BEFORE_PRIMITIVE_BINDER:-}|optional-arguments-prefix:${DN2CPP_BEFORE_OPTIONAL_ARGUMENTS:-}"
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} gates/_ordinary-reflection.sh samples/dotnet/ReflectInvoke/OrdinaryAmbiguousMatchSubset.cs samples/dotnet/ReflectInvoke/OrdinaryReflectionLeaves.csproj samples/dotnet/ReflectInvoke/OrdinaryReflectionLeavesProgram.cs samples/dotnet/ReflectInvoke/OrdinaryWideLookupSubset.cs samples/dotnet/ReflectInvoke/ReflectBindOnly.csproj samples/dotnet/ReflectInvoke/ReflectBindOnlyProgram.cs samples/dotnet/ReflectInvoke/ReflectFieldValidationSubset.cs samples/dotnet/ReflectInvoke/ReflectInvoke.csproj samples/dotnet/ReflectInvoke/ReflectMetadataMeasureSubset.cs samples/dotnet/ReflectInvoke/ReflectionMethodGroupsOnly.csproj samples/dotnet/ReflectInvoke/ReflectionMethodGroupsOnlyProgram.cs samples/dotnet/ReflectInvoke/StrippedOverrideRefusals.csproj samples/dotnet/ReflectInvoke/StrippedOverrideRefusalsProgram.cs"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|ordinary-reflection-leaves-v1|runtime-member-attributes-prefix:${DN2CPP_BEFORE_RUNTIME_MEMBER_ATTRIBUTES:-}|runtime-return-modifiers-prefix:${DN2CPP_BEFORE_RUNTIME_RETURN_MODIFIERS:-}"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|reflection-dispatch-v1|dispatch-prefix:${DN2CPP_BEFORE_REFLECTION_DISPATCH:-}|attribute-minted-prefix:${DN2CPP_BEFORE_ATTRIBUTE_MINTED:-}|template-accessors-prefix:${DN2CPP_BEFORE_TEMPLATE_ACCESSORS:-}|pointer-returns-prefix:${DN2CPP_BEFORE_POINTER_RETURNS:-}|delegate-invoke-targets-prefix:${DN2CPP_BEFORE_DELEGATE_INVOKE_TARGETS:-}|null-bound-chains-prefix:${DN2CPP_BEFORE_NULL_BOUND_CHAINS:-}|renamed-slot-bindings-prefix:${DN2CPP_BEFORE_RENAMED_SLOT_BINDINGS:-}|settled-object-virtual-prefix:${DN2CPP_BEFORE_SETTLED_OBJECT_VIRTUAL:-}|renamed-slot-fillers-prefix:${DN2CPP_BEFORE_RENAMED_SLOT_FILLERS:-}"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|stripped-overrides:${DN2CPP_STRIPPED_OVERRIDES:-}|library-struct-prefix:${DN2CPP_BEFORE_LIBRARY_STRUCT_RETURN:-}|function-pointer-identity-prefix:${DN2CPP_BEFORE_FUNCTION_POINTER_IDENTITY:-}"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|pointer-fields-prefix:${DN2CPP_BEFORE_POINTER_FIELDS:-}|object-methods-prefix-argv:before-object-method-enumeration|property-accessors-prefix-argv:before-property-accessors"
DN2CPP_GATE_EXTRA_INPUTS="$DN2CPP_GATE_EXTRA_INPUTS samples/dotnet/ReflectFrameworkBind/keep-library-override.xml samples/dotnet/ReflectInvoke/keep-object-methods.xml"
DN2CPP_GATE_EXTRA_INPUTS="$DN2CPP_GATE_EXTRA_INPUTS samples/dotnet/ReflectInvoke/ReflectPointerFieldsOnly.csproj samples/dotnet/ReflectInvoke/ReflectPointerFieldsPreserved.csproj samples/dotnet/ReflectInvoke/ReflectPointerFieldsOnlyProgram.cs samples/dotnet/ReflectInvoke/keep-pointer-field.xml samples/dotnet/ReflectReturnLib/PointerFields.cs"

py="$(resolve_python)"
source gates/_delegate-identity.sh
DN2CPP_GATE_EXTRA_INPUTS="$DN2CPP_GATE_EXTRA_INPUTS gates/_delegate-identity.sh gates/fold-delegate-fixture.py samples/dotnet/ReflectInvoke/DelegateVirtualIdentityOnly.csproj samples/dotnet/ReflectInvoke/DelegateVirtualIdentityLibrary.csproj samples/dotnet/ReflectInvoke/DelegateVirtualIdentityOnlyProgram.cs"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|virtual-delegate-identity:default,trimmed,unshared,folded|virtual-delegate-prefix-argv:before-virtual-delegate-identity|runtime-owned-method-groups-prefix-argv:before-runtime-owned-method-groups"
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} gates/fixtures/check-reflection-layout.py gates/measure-reflection-metadata.py gates/expected/reflection-allocations.csv gates/fixtures/delegate-invocation-cache/DelegateInvocationCache.csproj gates/fixtures/delegate-invocation-cache/Program.cs gates/fixtures/reflection-metadata-codec.cpp"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|empty-string-clone-prefix:${DN2CPP_BEFORE_EMPTY_STRING_CLONE:-}|delegate-list-prefix:${DN2CPP_BEFORE_DELEGATE_LISTS:-}|recursive-delegate-prefix:${DN2CPP_BEFORE_RECURSIVE_DELEGATE:-}|ordinary-interface-prefix:${DN2CPP_BEFORE_ORDINARY_IL_INTERFACE:-}|object-methodimpl-prefix:${DN2CPP_BEFORE_OBJECT_METHODIMPL:-}"
DN2CPP_GATE_EXTRA_INPUTS="$DN2CPP_GATE_EXTRA_INPUTS gates/fixtures/recursive-delegate/RecursiveDelegate.csproj gates/fixtures/recursive-delegate/Program.cs"
DN2CPP_GATE_EXTRA_INPUTS="$DN2CPP_GATE_EXTRA_INPUTS samples/dotnet/ReflectInvoke/ReflectNameBindOnly.csproj samples/dotnet/ReflectInvoke/ReflectNameBindOnlyProgram.cs"
DN2CPP_GATE_EXTRA_INPUTS="$DN2CPP_GATE_EXTRA_INPUTS samples/dotnet/ReflectInvoke/ReflectAttributeDiscoveryOnly.csproj samples/dotnet/ReflectInvoke/ReflectAttributeDiscoveryOnlyProgram.cs"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|attribute-discovery-prefix-argv:before-attribute-discovery"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|delegate-name-boundary-argv:delegate-name-boundary-outcomes"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|delegate-signature-prefix-argv:before-delegate-signature-bindings|delegate-signature-boundary-argv:delegate-signature-boundary-outcomes"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|function-pointer-invoke-prefix-argv:before-runtime-function-pointer-invoke"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|unsupported-referent-prefix-argv:before-unsupported-referent-signatures"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|ordinary-template-prefix-argv:before-ordinary-template-signatures|ordinary-template-boundary-prefix-argv:delegate-signature-before-ordinary-boundary"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|ordinary-overload-prefix-argv:before-ordinary-overload-selection|ordinary-overload-boundary-prefix-argv:delegate-signature-before-overload-boundary|shape-overload-prefix-argv:before-shape-overload-selection|shape-overload-boundary-prefix-argv:delegate-signature-before-shape-boundary|leaf-overload-prefix-argv:before-leaf-overload-selection|leaf-overload-boundary-prefix-argv:delegate-signature-before-leaf-boundary"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|intrinsic-only-prefix-argv:before-intrinsic-overloads|intrinsic-only-boundary-argv:intrinsic-boundary"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|runtime-argument-prefix-argv:before-runtime-argument-overloads|runtime-argument-boundary-prefix-argv:delegate-signature-before-runtime-argument-boundary"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|family-type-prefix-argv:before-family-type-overloads"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|enum-signature-prefix-argv:before-enum-signature-bindings"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|intrinsic-pointer-prefix-argv:before-intrinsic-pointer-bindings"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|intrinsic-pointer-overload-prefix-argv:before-intrinsic-pointer-overloads"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|intrinsic-pointer-family-prefix-argv:before-intrinsic-pointer-families"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|intrinsic-generic-prefix-argv:before-intrinsic-generic-pointers|intrinsic-array-prefix-argv:before-intrinsic-array-arguments"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|intrinsic-constant-prefix-argv:before-intrinsic-constant-arguments"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|intrinsic-array-child-prefix-argv:before-intrinsic-array-children"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|intrinsic-MD-array-child-prefix-argv:before-intrinsic-MD-array-children"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|intrinsic-only-array-prefix-argv:before-intrinsic-array-elements"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|intrinsic-array-leaf-prefix-argv:before-intrinsic-array-leaves|intrinsic-constant-leaf-prefix-argv:before-intrinsic-constant-leaves"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|intrinsic-closed-generic-prefix-argv:before-intrinsic-closed-generic-arguments"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|constructor-name-prefix-argv:before-delegate-constructor-names|initializer-name-prefix-argv:before-delegate-initializer-names"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|app-library-initializer-prefix-argv:before-app-library-initializer-names"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|identity-overload-prefix-argv:before-identity-overload-selection|identity-overload-boundary-prefix-argv:delegate-signature-before-identity-boundary"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|delegate-origin-prefix-argv:before-delegate-origin-boundaries|delegate-origin-modes:argument,field,array,checked-conv,arithmetic,box,call,local,stack-join,byref-argument"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|runtime-type-relations-prefix-argv:before-runtime-type-relations"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|generic-method-definitions-prefix-argv:before-generic-method-definitions|generic-method-boundary-argv:generic-method-boundary-outcomes"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|formal-method-parameters-prefix-argv:before-formal-method-parameters|formal-type-boundary-argv:formal-type-boundary-outcomes"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|mixed-generic-definitions-prefix-argv:before-mixed-generic-definitions|generic-signature-boundary-argv:generic-signature-boundary-outcomes"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|formal-reflected-owners-prefix-argv:before-formal-reflected-owners"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|formal-classification-prefix-argv:before-formal-classification|unused-signatures:absent-ext,default,trim,strict-completion"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|metadata-formal-attributes-prefix-argv:before-metadata-formal-attributes"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|formal-member-types-prefix-argv:before-formal-member-types"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|definition-signature-closure-prefix-argv:before-definition-signature-closure"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|delegate-name-bindings-prefix-argv:before-delegate-name-bindings"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|nested-signature-layouts:absent-ext,loaded-ext,body-required,default,trim|nested-signature-layout-argv:describe-unused-layouts"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|open-signature-layouts:absent-ext,loaded-ext,default,trim|open-signature-boundary-argv:return-type"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|signature-dependencies:absent-ext,loaded-ext,default,trim,depth40,body-required|signature-dependencies-argv:describe-layout-dependencies|signature-paths-argv:describe-layout-paths|default-interface-layout-argv:describe-default-interface-layout|completion-layout-argv:describe-completion-layouts"
DN2CPP_GATE_EXTRA_INPUTS="$DN2CPP_GATE_EXTRA_INPUTS gates/fixtures/reflection-unused-signatures/Ext/Ext.csproj gates/fixtures/reflection-unused-signatures/Ext/Box.cs gates/fixtures/reflection-unused-signatures/Lib/Lib.csproj gates/fixtures/reflection-unused-signatures/Lib/Subject.cs gates/fixtures/reflection-unused-signatures/App/App.csproj gates/fixtures/reflection-unused-signatures/App/Program.cs gates/fixtures/reflection-unused-signatures/AppOwned/AppOwned.csproj gates/fixtures/reflection-unused-signatures/AppOwned/Program.cs gates/fixtures/reflection-unused-signatures/Enumeration/Enumeration.csproj gates/fixtures/reflection-unused-signatures/Enumeration/Program.cs"
DN2CPP_GATE_EXTRA_INPUTS="$DN2CPP_GATE_EXTRA_INPUTS gates/fixtures/ldftn-local/Program.cs"
DN2CPP_GATE_EXTRA_INPUTS="$DN2CPP_GATE_EXTRA_INPUTS gates/fixtures/reflection-unused-signatures/AppNested/AppNested.csproj gates/fixtures/reflection-unused-signatures/AppNested/Program.cs gates/fixtures/reflection-unused-signatures/BodyNeeded/BodyNeeded.csproj"
DN2CPP_GATE_EXTRA_INPUTS="$DN2CPP_GATE_EXTRA_INPUTS gates/fixtures/reflection-unused-signatures/AppOpen/AppOpen.csproj gates/fixtures/reflection-unused-signatures/AppOpen/Program.cs"
DN2CPP_GATE_EXTRA_INPUTS="$DN2CPP_GATE_EXTRA_INPUTS gates/fixtures/reflection-unused-signatures/AppDependencies/AppDependencies.csproj gates/fixtures/reflection-unused-signatures/AppDependencies/Program.cs gates/fixtures/reflection-unused-signatures/MethodBodyNeeded/MethodBodyNeeded.csproj gates/fixtures/reflection-unused-signatures/GrowthBodyNeeded/GrowthBodyNeeded.csproj"
gate_optional_argument_asserts() {
    local out="$1" native line
    native=$(strip_cr_win_file "$out/metadata-layout.stdout")
    DN2CPP_BEFORE_OPTIONAL_ARGUMENTS=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-optional-arguments.stdout"
    sed '/^== reflective optional arguments ==/,$d' "$out/metadata-layout.stdout" > "$out/optional-arguments-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-optional-arguments.stdout") \
        <(strip_cr_win_file "$out/optional-arguments-prefix.stdout")
    for line in '== reflective optional arguments ==' 'missing aliases: True' 'missing address alias: True' \
        'default metadata: True:True:True:False:False' \
        'integer first: int:12' 'integer cached: int:12' 'integer cached args: Int32:12' \
        'Boxed: boxed:5' 'Boxed args: missing' 'Decimal: decimal:1.25' 'Date: date:123' \
        'NullableEnum: nullable-enum:7' 'NullableEnum args: Tiny:B' \
        'enums args: Tiny:B,Wide:High' 'byref args: Int32:7,String:ref' \
        'validation failure args: missing,Object:System.Object' 'target failure args: missing' \
        'generic value args: missing' 'generic reference args: null' \
        'constructor: 27:Int32:27' 'dynamic invoke: int:23:Int32:23' \
        'direct delegate: int:31' 'reflective optional arguments end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: optional argument witness missing: $line" >&2; return 1; }
    done
}

gate_empty_string_clone_asserts() {
    local out="$1" native line
    native=$(strip_cr_win_file "$out/metadata-layout.stdout")
    DN2CPP_BEFORE_EMPTY_STRING_CLONE=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-empty-string-clone.stdout"
    sed '/^== empty string clone identity ==/,$d' "$out/metadata-layout.stdout" > "$out/empty-string-clone-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-empty-string-clone.stdout") \
        <(strip_cr_win_file "$out/empty-string-clone-prefix.stdout")
    for line in '== empty string clone identity ==' \
        'empty clone 0=0:System.String:False:False:False' \
        'empty clone 1=0:System.String:False:False:False' \
        'empty clone 2=0:System.String:False:False:False' \
        'empty clone 3=0:System.String:False:False:False' \
        'empty string clone identity end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: empty string clone witness missing: $line" >&2; return 1; }
    done
}

gate_extra_asserts() {
    gate_virtual_delegate_prefix_asserts "$1"
    gate_runtime_owned_delegate_prefix_asserts "$1"
    local out="$1" native line registry boundary axis route query expected parameter
    "$py" gates/fixtures/check-reflection-layout.py "$out" "$reflection_layout_axis"
    # The runtime publishes a constructor's invoke plan only for a record inside a
    # registered extent, so a packed constructor table must appear in the registry.
    registry=$(awk '/^const Dn2CppMetadataBlock dn2cpp_metadata_blocks\[\] = \{$/ { p = 1; next }
        p && /^\};$/ { exit } p' "$out"/generated*.cpp)
    case "$reflection_layout_axis" in
        default)
            grep -Ewq 'md_record_ctortab_ReflectMetadataLayoutSubset_PackedDerived, [1-9][0-9]* \}' <<< "$registry" \
                || { echo 'FAIL: packed PackedDerived registers no constructor table extent' >&2; return 1; } ;;
        uncompressed)
            { grep -Fxq '    { md_ptr_0, nullptr, nullptr, 0, nullptr, 0 },' <<< "$registry" \
                && ! grep -Fq 'md_record_ctortab_' <<< "$registry"; } \
                || { echo 'FAIL: uncompressed metadata registers a constructor table extent' >&2; return 1; } ;;
    esac
    run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/metadata-layout.stdout"
    native=$(strip_cr_win_file "$out/metadata-layout.stdout")
    gate_optional_argument_asserts "$out"
    local primitive_before primitive_prefix
    primitive_before=$(DN2CPP_BEFORE_PRIMITIVE_BINDER=1 run_bounded dotnet "$_CG_APP")
    primitive_prefix=$(awk '/^== default binder primitive widening ==$/ { exit } { print }' <<< "$native")
    assert_output "$primitive_prefix" "$(strip_cr_win "$primitive_before")"
    for line in '== default binder primitive widening ==' \
        'binder ushort to char => threw MissingMethodException' \
        'binder flags ushort to char => threw MissingMethodException' \
        'binder byte to char => OnlyChar(65)' 'binder char exact => OnlyChar(65)' \
        'binder null to char => OnlyChar(0)' \
        'binder sbyte to char => threw MissingMethodException' \
        'binder short to char => threw MissingMethodException' \
        'binder int to char => threw MissingMethodException' \
        'binder char to ushort => OnlyUShort(65535)' 'binder char to int => OnlyInt(65535)' \
        'binder ushort to int => OnlyInt(65535)' 'binder float to double => OnlyDouble(1.5)' \
        'binder double to int => threw MissingMethodException' \
        'binder byte prefers char => char' 'binder ushort prefers ushort => ushort' \
        'invoke ushort to char => 65535' 'constructor invoke ushort to char => OnlyChar(65535)' \
        'default binder primitive widening end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: primitive binder witness missing: $line" >&2; return 1; }
    done
    DN2CPP_BEFORE_REFLECTION_DISPATCH=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-reflection-dispatch.stdout"
    sed '/^== reflection dispatch extensions ==/,$d' "$out/metadata-layout.stdout" > "$out/reflection-dispatch-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-reflection-dispatch.stdout") \
        <(strip_cr_win_file "$out/reflection-dispatch-prefix.stdout")
    for line in '== reflection dispatch extensions ==' '== dynamic invoke ==' \
        'dynamic invoke end' 'reflection dispatch extensions end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: reflection dispatch witness missing: $line" >&2; return 1; }
    done
    DN2CPP_BEFORE_ATTRIBUTE_MINTED=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-attribute-minted.stdout"
    sed '/^== reflection route attribute-minted classes ==/,$d' "$out/metadata-layout.stdout" > "$out/attribute-minted-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-attribute-minted.stdout") \
        <(strip_cr_win_file "$out/attribute-minted-prefix.stdout")
    for line in '== reflection route attribute-minted classes ==' \
        'AttrLevel9`1 rows=1 next=AttrRoot' 'reflection route attribute-minted classes end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: attribute-minted witness missing: $line" >&2; return 1; }
    done
    DN2CPP_BEFORE_TEMPLATE_ACCESSORS=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-template-accessors.stdout"
    sed '/^== template accessor bindings ==/,$d' "$out/metadata-layout.stdout" > "$out/template-accessors-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-template-accessors.stdout") \
        <(strip_cr_win_file "$out/template-accessors-prefix.stdout")
    for line in '== template accessor bindings ==' 'template Int32, static: static:Int32' \
        'template String, setter over its argument: closed:String' \
        'template Int32, closed: minted:Int32' 'template Int32, null-bound context: Int32' \
        'template String, null-bound context: NullReferenceException' \
        'template String, method: get_Shared/True/True/True' \
        'template Int32, null-bound method: NullReferenceException' \
        'template across instantiations: False/False static:Int32 static:String' \
        'template accessor bindings end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: template accessor witness missing: $line" >&2; return 1; }
    done
    DN2CPP_BEFORE_POINTER_RETURNS=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-pointer-returns.stdout"
    sed '/^== pointer returns ==/,$d' "$out/metadata-layout.stdout" > "$out/pointer-returns-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-pointer-returns.stdout") \
        <(strip_cr_win_file "$out/pointer-returns-prefix.stdout")
    for line in '== pointer returns ==' \
        'pointer: Pointer:System.Reflection.Pointer:0x1230' \
        'pointer to pointer: Pointer:System.Reflection.Pointer:0x80' \
        'function pointer: IntPtr:256' 'ref pointer: Pointer:System.Reflection.Pointer:0x200' \
        'dynamic invoke: Pointer:System.Reflection.Pointer:0x1230' \
        'int*, int* box: Int64:4656' 'uint*, int* box: Int64:4656' 'void*, int** box: Int64:128' \
        "long**, int** box: ArgumentException: Object of type 'System.Reflection.Pointer' cannot be converted to type 'System.Int64**'." \
        'five levels: Pointer:System.Reflection.Pointer:0x50' 'int*****, ref int***** box: Int64:88' \
        "int*****, int****** box: ArgumentException: Object of type 'System.Reflection.Pointer' cannot be converted to type 'System.Int32*****'." \
        "delegate*<long>*, delegate*<int>* box: ArgumentException: Object of type 'System.Reflection.Pointer' cannot be converted to type 'System.Int64()*'." \
        "delegate*<int>, string: ArgumentException: Object of type 'System.String' cannot be converted to type 'System.Int32()'." \
        'pointer returns end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: pointer return witness missing: $line" >&2; return 1; }
    done
    # A fresh object's managed-reference stores take the write barrier too
    # (runtime/core/dn2cpp_core.h), so the pointer box stores its type through it.
    line=$(awk '/dn2cpp_set_pointer_box_type\(/ { getline; print; exit }' "$out"/generated*.cpp)
    grep -Fq 'dn2cpp_gc_store_ref(&o->' <<< "$line" \
        || { echo "FAIL: the pointer box stores its type without the write barrier: $line" >&2; return 1; }
    DN2CPP_BEFORE_DELEGATE_INVOKE_TARGETS=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-delegate-invoke-targets.stdout"
    sed '/^== delegate invoke targets ==/,$d' "$out/metadata-layout.stdout" > "$out/delegate-invoke-targets-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-delegate-invoke-targets.stdout") \
        <(strip_cr_win_file "$out/delegate-invoke-targets-prefix.stdout")
    for line in '== delegate invoke targets ==' \
        'converter=n3,n1,n2 target=True method=Invoke/Func`2 entries=1 dynamic=n9' \
        'same type=11 equal=True target=True self=False' 'generic=a!/b!/42/c?/42/L7' \
        'multicast=12121 entries=1/2 last=2' 'by-ref=22/u22' 'struct=w:12/(5, p)' \
        'variance=covariant/sink:x' 'headerless=./,' 'null source=ArgumentException' \
        'virtual load=201 target=True method=Invoke entries=1' 'virtual load null=ArgumentException' \
        'delegate invoke targets end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: delegate invoke target witness missing: $line" >&2; return 1; }
    done
    DN2CPP_BEFORE_NULL_BOUND_CHAINS=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-null-bound-chains.stdout"
    sed '/^== null-bound call chains ==/,$d' "$out/metadata-layout.stdout" > "$out/null-bound-chains-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-null-bound-chains.stdout") \
        <(strip_cr_win_file "$out/null-bound-chains-prefix.stdout")
    for line in '== null-bound call chains ==' 'null-bound long chain: 42' \
        'null-bound long chain, field: NullReferenceException' 'open null long chain: 42' \
        'null-bound struct long chain: 7' 'null-bound dense cycle: 16384' \
        'template Int32 long chain, null-bound context: Int32' \
        'template Int32 long chain, null-bound tested: null Int32' \
        'template String long chain, null-bound context: NullReferenceException' \
        'template String long chain, null-bound tested: NullReferenceException' \
        'null-bound call chains end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: null-bound chain witness missing: $line" >&2; return 1; }
    done
    DN2CPP_BEFORE_RENAMED_SLOT_BINDINGS=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-renamed-slot-bindings.stdout"
    sed '/^== reflection-bound delegates over a renamed override ==/,$d' "$out/metadata-layout.stdout" \
        > "$out/renamed-slot-bindings-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-renamed-slot-bindings.stdout") \
        <(strip_cr_win_file "$out/renamed-slot-bindings-prefix.stdout")
    for line in '== reflection-bound delegates over a renamed override ==' \
        'renamed-slot-calls=3/3/3/4' 'renamed-slot-identity=True/True/True/False/Rescale/Rescale' \
        'renamed-slot-dedup=1/True/True' 'renamed-slot-chain=True/True' \
        'reflection-bound delegates over a renamed override end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: renamed slot binding witness missing: $line" >&2; return 1; }
    done
    DN2CPP_BEFORE_SETTLED_OBJECT_VIRTUAL=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-settled-object-virtual.stdout"
    sed '/^== settled Object virtual answers ==/,$d' "$out/metadata-layout.stdout" > "$out/settled-object-virtual-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-settled-object-virtual.stdout") \
        <(strip_cr_win_file "$out/settled-object-virtual-prefix.stdout")
    for line in '== settled Object virtual answers ==' \
        'settled-object-virtual-own=OverridingException.ToString>Object.ToString|OverridingException.GetHashCode>Object.GetHashCode|OverridingException.Equals>Object.Equals|overriding/23/True/False' \
        'settled-object-virtual-inherited=OverridingException.ToString>Object.ToString|OverridingException.GetHashCode>Object.GetHashCode|OverridingException.Equals>Object.Equals|overriding/23/True/False' \
        'settled-object-virtual-reflected=Object.ToString|Object.GetHashCode|Object.Equals/Object.ToString|Object.GetHashCode|Object.Equals' \
        'settled-object-virtual-unsettled=True/True/True/True/True/text/5/True/True' \
        'settled Object virtual answers end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: settled Object virtual witness missing: $line" >&2; return 1; }
    done
    DN2CPP_BEFORE_RENAMED_SLOT_FILLERS=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-renamed-slot-fillers.stdout"
    sed '/^== renamed slot fillers ==/,$d' "$out/metadata-layout.stdout" > "$out/renamed-slot-fillers-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-renamed-slot-fillers.stdout") \
        <(strip_cr_win_file "$out/renamed-slot-fillers-prefix.stdout")
    for line in '== renamed slot fillers ==' \
        'renamed-filler-calls=42/43/reuse/RenamedFillerDerived.Weigh' \
        'renamed-filler-interface=True/True/True/1/42/42/RenamedFillerImpl.Weigh/RenamedFillerImpl.Weigh' \
        'renamed-filler-interface-inherited=True/True/True/1/43/43/RenamedFillerDerived.Weigh/RenamedFillerDerived.Weigh' \
        'renamed-filler-object-derived=True/True/True/1/derived/derived/ObjectMethodImplDerived.Render/ObjectMethodImplDerived.Render|True/True/True/1/907/907/ObjectMethodImplDerived.Hash/ObjectMethodImplDerived.Hash' \
        'renamed-filler-object-hider=True/True/True/1/alias/alias/ObjectMethodImpl.Render/ObjectMethodImpl.Render|True/True/True/1/701/701/ObjectMethodImpl.Hash/ObjectMethodImpl.Hash' \
        'renamed-filler-object-load-generic=ObjectMethodImplGeneric`1.Render/ObjectMethodImplGeneric`1.Hash/generic/1103' \
        'renamed-filler-reuse=True/True/True/1/reuse/reuse/RenamedObjectReuse.Show/RenamedObjectReuse.Show' \
        'renamed-filler-minted=True/True/True/1/44/44/RenamedFillerBox`1.Weigh/RenamedFillerBox`1.Weigh|True/True/True/1/42/42/RenamedFillerImpl.Weigh/RenamedFillerImpl.Weigh|True/True/True/1/45/45/RenamedFillerOverrideBox`1.Weigh/RenamedFillerOverrideBox`1.Weigh' \
        'renamed-filler-minted-object=True/True/True/1/box/box/ObjectMethodImplBox`1.Render/ObjectMethodImplBox`1.Render|True/True/True/1/1201/1201/ObjectMethodImplBox`1.Hash/ObjectMethodImplBox`1.Hash|True/True/True/1/alias/alias/ObjectMethodImpl.Render/ObjectMethodImpl.Render' \
        'renamed-filler-minted-load=ObjectMethodImplBox`1.Render/ObjectMethodImpl.Render/RenamedFillerBox`1.Weigh/RenamedFillerImpl.Weigh' \
        'renamed-filler-variant=True/True/True/1/source/source/RenamedSource.Fetch/RenamedSource.Fetch|True/True/True/1/source/source/RenamedSource.Fetch/RenamedSource.Fetch' \
        'renamed-filler-variant-load=source/RenamedSource.Fetch' \
        'renamed slot fillers end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: renamed slot filler witness missing: $line" >&2; return 1; }
    done
    DN2CPP_BEFORE_DELEGATE_LISTS=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-delegate-lists.stdout"
    sed '/^== remove runs ==/,$d' "$out/metadata-layout.stdout" > "$out/delegate-lists-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-delegate-lists.stdout") \
        <(strip_cr_win_file "$out/delegate-lists-prefix.stdout")
    for line in '== remove runs ==' 'remove runs end' \
        '== invocation lists ==' 'invocation lists end' \
        '== invocation list entries ==' 'invocation list entries end' \
        '== invocation list enumeration ==' 'invocation list enumeration end' \
        '== invocation list enumeration scale ==' 'long chain: 512/512 entries True' \
        'invocation list enumeration scale end' \
        '== delegate invoker declarations ==' 'cold invoker=ColdInvoker' \
        'cold ref invoker=ColdRefInvoker' 'cold shared distinct=True' \
        'unconstructed variance view=null' \
        'delegate invoker declarations end' \
        '== recursive delegate declarations ==' 'recursive delegate identities=True/True' \
        'recursive delegate declarations end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: delegate invocation list witness missing: $line" >&2; return 1; }
    done
    DN2CPP_BEFORE_RECURSIVE_DELEGATE=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-recursive-delegate.stdout"
    sed '/^== recursive delegate declarations ==/,$d' "$out/metadata-layout.stdout" > "$out/recursive-delegate-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-recursive-delegate.stdout") \
        <(strip_cr_win_file "$out/recursive-delegate-prefix.stdout")
    grep -Fxq 'metadata-layout-begin' <<< "$native"
    grep -Fxq 'metadata-layout-cache-capacity=72/1296' <<< "$native"
    grep -Fxq 'metadata-layout-cache-threads=1296/1296' <<< "$native"
    grep -Fxq 'metadata-layout-interface-receivers=21000' <<< "$native"
    grep -Fxq 'metadata-layout-end' <<< "$native"
    grep -Fxq 'metadata-compression-begin' <<< "$native"
    grep -Fxq 'metadata-compression-labels=field/property/constructor/method/parameter' <<< "$native"
    grep -Fxq 'metadata-compression-inheritance=v5/Direct' <<< "$native"
    grep -Fxq 'metadata-compression-generic-value=15/Int32/True' <<< "$native"
    grep -Fxq 'metadata-compression-generic-reference=text/String/True' <<< "$native"
    grep -Fxq 'metadata-compression-plain-generic=True/True' <<< "$native"
    grep -Fxq 'metadata-compression-end' <<< "$native"
    grep -Fxq 'existing-constructor-message: Exception has been thrown by the target of an invocation.' <<< "$native"
    grep -Fxq 'existing-constructor-method-composed-flags: TargetInvocationException InvalidOperationException 80131604' <<< "$native"
    grep -Fxq 'existing-constructor-end' <<< "$native"
    grep -Fxq 'activator-cold-generic=73' <<< "$native"
    DN2CPP_BEFORE_EXISTING_CONSTRUCTOR=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-existing-constructor.stdout"
    sed '/^existing-constructor-begin/,$d' "$out/metadata-layout.stdout" > "$out/existing-constructor-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-existing-constructor.stdout") \
        <(strip_cr_win_file "$out/existing-constructor-prefix.stdout")
    DN2CPP_BEFORE_COLD_ACTIVATOR=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-cold-activator.stdout"
    sed '/^activator-cold-generic=/,$d' "$out/metadata-layout.stdout" > "$out/cold-activator-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-cold-activator.stdout") \
        <(strip_cr_win_file "$out/cold-activator-prefix.stdout")
    grep -Fxq 'delegate-method-shared=True/True' <<< "$native"
    grep -Fxq 'delegate-method-generic=Int32/String' <<< "$native"
    grep -Fxq 'delegate-method-runtime-owned=True/True' <<< "$native"
    grep -Fxq 'delegate-method-struct-interface=StructProbe/Value/31/31' <<< "$native"
    grep -Fxq 'delegate-method-explicit-interface=ExplicitProbe/True/41/41' <<< "$native"
    grep -Fxq 'delegate-method-default-interface=IDefaultProbe/Default/101' <<< "$native"
    grep -Fxq 'delegate-method-interface-generic=ImplicitGeneric/String/ExplicitGeneric/True/Int32/p5' <<< "$native"
    grep -Fxq 'delegate-method-array-generic=Int32[]/String[]' <<< "$native"
    grep -Fxq 'delegate-method-generic-hider=GvmBase/base/GvmLeaf/leaf' <<< "$native"
    grep -Fxq 'delegate-method-generic-covariant=CovariantLeaf/CovariantLeaf/CovariantLeaf' <<< "$native"
    grep -Fxq 'delegate-method-end' <<< "$native"
    grep -Fxq 'delegate-method-interface-begin' <<< "$native"
    grep -Fxq 'delegate-method-generic-explicit-order=explicit/PlainFirstGeneric/True/explicit/ExplicitFirstGeneric/True' <<< "$native"
    grep -Fxq 'delegate-method-derived-default=derived/IDerivedDefault/True' <<< "$native"
    grep -Fxq 'delegate-method-derived-generic=derived/IDerivedGenericDefault/True' <<< "$native"
    grep -Fxq 'delegate-method-derived-struct=derived/IDerivedDefault' <<< "$native"
    grep -Fxq 'delegate-method-derived-inherited=derived/IDerivedDefault' <<< "$native"
    grep -Fxq 'delegate-method-derived-typed=derived/True' <<< "$native"
    grep -Fxq 'delegate-method-derived-same=same/IDerivedSame' <<< "$native"
    grep -Fxq 'delegate-method-derived-runtime-type=derived/IRuntimeDerivedDefault/True' <<< "$native"
    grep -Fxq 'delegate-method-derived-generic-runtime-type=derived/derived/IRuntimeGenericDerivedDefault/True' <<< "$native"
    grep -Fxq 'delegate-method-inherited-generic-runtime-type=mid/mid/RuntimeGvmMid' <<< "$native"
    grep -Fxq 'delegate-method-interface-end' <<< "$native"
    grep -Fxq 'interface-gvm-dispatch-begin' <<< "$native"
    grep -Fxq 'interface-gvm-explicit-overloads=generic/integer' <<< "$native"
    grep -Fxq 'interface-gvm-explicit-overloads-generic-interface=generic/integer' <<< "$native"
    grep -Fxq 'interface-gvm-plain-and-explicit-overload=plain/int-explicit/Pick/True' <<< "$native"
    grep -Fxq 'interface-gvm-qualifier-prefix=plain/longer/Pick/True' <<< "$native"
    grep -Fxq 'interface-gvm-qualifier-arity=plain/explicit-generic/Pick/True' <<< "$native"
    grep -Fxq 'interface-gvm-dispatch-end' <<< "$native"
    grep -Fxq 'interface-redeclaration-begin' <<< "$native"
    grep -Fxq 'interface-redeclaration-plain=derived-plain/derived-plain/RedeclaredDerived/plain' <<< "$native"
    grep -Fxq 'interface-redeclaration-unlisted=base-explicit/base-explicit/RedeclaredBase/explicit' <<< "$native"
    grep -Fxq 'interface-redeclaration-hider=implicit/implicit/ImplicitRedeclared/plain' <<< "$native"
    grep -Fxq 'interface-redeclaration-abstract=abstract-leaf/abstract-leaf/AbstractLeaf/plain' <<< "$native"
    grep -Fxq 'interface-redeclaration-generic-class=shared-box-String/shared-box-String/SharedRedeclaredBox`1/plain' <<< "$native"
    grep -Fxq 'interface-redeclaration-fill=fill-source/fill-source/FillSource/plain/fill-override/fill-override/FillOverride/plain' <<< "$native"
    grep -Fxq 'interface-redeclaration-explicit-mid=explicit-mid/explicit-mid/ExplicitMidRedeclared/explicit' <<< "$native"
    grep -Fxq 'interface-redeclaration-default=default/default/IRedeclaredDefault/plain/default-mid/default-mid/DefaultRedeclared/explicit' <<< "$native"
    grep -Fxq 'interface-redeclaration-closed-generic=of-derived-plain/of-derived-plain/RedeclaredOfDerived/plain' <<< "$native"
    grep -Fxq 'interface-redeclaration-runtime-type=runtime-box/runtime-box/Tag' <<< "$native"
    grep -Fxq 'interface-redeclaration-pick-plain=pick-derived-plain/pick-derived-plain/PickDerived/plain' <<< "$native"
    grep -Fxq 'interface-redeclaration-pick-unlisted=pick-base-explicit/pick-base-explicit/PickBase/explicit' <<< "$native"
    grep -Fxq 'interface-redeclaration-pick-hider=pick-implicit/pick-implicit/PickImplicit/plain/pick-virtual/pick-virtual/PickVirtual/plain' <<< "$native"
    grep -Fxq 'interface-redeclaration-pick-override=pick-override/pick-override/PickOverride/plain' <<< "$native"
    grep -Fxq 'interface-redeclaration-pick-fill=pick-source/pick-source/PickSource/plain/pick-target-override/pick-target-override/PickTargetOverride/plain' <<< "$native"
    grep -Fxq 'interface-redeclaration-end' <<< "$native"
    grep -Fxq 'runtime-level-gvm-begin' <<< "$native"
    grep -Fxq 'runtime-level-gvm-generic-base=root:Int32/String|leaf:Int32/String+root:Int32/String|leaf:Int32/String+root:Int32/String|Tag|True|True|True' <<< "$native"
    grep -Fxq 'runtime-level-gvm-unconstructed-base=leaf:String/Int32+root:String/Int32|leaf:String/Int32+root:String/Int32|True|Int32' <<< "$native"
    grep -Fxq 'runtime-level-gvm-two-arguments=pair:Int32,String/String|pair:Int32,Boolean/String|pair:Int32,Boolean/String|True' <<< "$native"
    grep -Fxq 'runtime-level-gvm-plain-base=own:Decimal/String|own:Decimal/String|True|own:Decimal|True' <<< "$native"
    grep -Fxq 'runtime-level-method-row=True|own:Decimal|True|Who' <<< "$native"
    grep -Fxq 'runtime-level-gvm-chain=chain:String+mid:String/Int32|chain:String+mid:String/Int32|True' <<< "$native"
    grep -Fxq 'runtime-level-gvm-inherited=mid:Boolean/Int32|mid:Boolean/Int32|True|Boolean' <<< "$native"
    grep -Fxq 'runtime-level-gvm-image-level=abstract-mid:Int32/Int32|True|True|True' <<< "$native"
    grep -Fxq 'runtime-level-gvm-interface=picker:Int32/String|picker:Int32/String|True|picker:Int32|True|Name' <<< "$native"
    grep -Fxq 'runtime-level-gvm-end' <<< "$native"
    DN2CPP_BEFORE_RUNTIME_LEVEL_GVM=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-runtime-level-gvm.stdout"
    sed '/^runtime-level-gvm-begin/,$d' "$out/metadata-layout.stdout" > "$out/runtime-level-gvm-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-runtime-level-gvm.stdout") \
        <(strip_cr_win_file "$out/runtime-level-gvm-prefix.stdout")
    DN2CPP_BEFORE_INTERFACE_REDECLARATION=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-interface-redeclaration.stdout"
    sed '/^interface-redeclaration-begin/,$d' "$out/metadata-layout.stdout" > "$out/interface-redeclaration-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-interface-redeclaration.stdout") \
        <(strip_cr_win_file "$out/interface-redeclaration-prefix.stdout")
    DN2CPP_BEFORE_INTERFACE_SELECTION=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-interface-selection.stdout"
    sed '/^delegate-method-interface-begin/,$d' "$out/metadata-layout.stdout" > "$out/interface-selection-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-interface-selection.stdout") \
        <(strip_cr_win_file "$out/interface-selection-prefix.stdout")
    DN2CPP_BEFORE_DELEGATE_METHOD=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-delegate-method.stdout"
    sed '/^delegate-method-begin/,$d' "$out/metadata-layout.stdout" > "$out/delegate-method-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-delegate-method.stdout") \
        <(strip_cr_win_file "$out/delegate-method-prefix.stdout")
    grep -Fxq 'ldftn-local-direct=12/Add' <<< "$native"
    grep -Fxq 'ldftn-local-nop=12/Add' <<< "$native"
    grep -Fxq 'ldftn-local-conv=12/Add' <<< "$native"
    grep -Fxq 'ldftn-local-snapshot=12/Add' <<< "$native"
    grep -Fxq 'ldftn-local-selected=12/Add/2/Subtract' <<< "$native"
    grep -Fxq 'ldftn-local-stack-join=12/Add/2/Subtract' <<< "$native"
    grep -Fxq 'ldftn-local-closed=C:x/Decorate' <<< "$native"
    grep -Fxq 'ldftn-local-calli=14' <<< "$native"
    grep -Fxq 'ldftn-local-dead-origin=2/Subtract' <<< "$native"
    grep -Fxq 'ldftn-local-virtual=15/VirtualDerived.Scale' <<< "$native"
    grep -Fxq 'ldftn-local-instance=15/Offset' <<< "$native"
    grep -Fxq 'ldftn-local-int64=12/Add' <<< "$native"
    grep -Fxq 'ldftn-local-address-taken=42/9/12/Add' <<< "$native"
    grep -Fxq 'ldftn-local-end' <<< "$native"
    DN2CPP_BEFORE_TYPEDEF_MEMBERREF=1 run_bounded dotnet "$_CG_APP" \
        > "$out/before-typedef-memberref.stdout"
    sed '/^== same-module TypeDef MemberRefs ==/,$d' "$out/metadata-layout.stdout" \
        > "$out/typedef-memberref-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-typedef-memberref.stdout") \
        <(strip_cr_win_file "$out/typedef-memberref-prefix.stdout")
    for line in '== same-module TypeDef MemberRefs ==' \
        'typedef overloads=25/x:body' 'typedef instance=15' \
        'typedef generic method=9' 'typedef generic owner=31/owner' \
        'same-module TypeDef MemberRefs end'; do
        grep -Fxq "$line" <<< "$native" \
            || { echo "FAIL: TypeDef-parent MemberRef witness missing: $line" >&2; exit 1; }
    done
    DN2CPP_BEFORE_ORDINARY_IL_INTERFACE=1 run_bounded dotnet "$_CG_APP" \
        > "$out/before-ordinary-interface-il.stdout"
    sed '/^== ordinary interface and ValueType IL ==/,$d' "$out/metadata-layout.stdout" \
        > "$out/ordinary-interface-il-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-ordinary-interface-il.stdout") \
        <(strip_cr_win_file "$out/ordinary-interface-il-prefix.stdout")
    for line in '== ordinary interface and ValueType IL ==' \
        'ldftn-local-sealed-interface=105/ISealedScale.Scale/205/ISealedScale.Shift' \
        'ldftn-local-valuetype-null=NRE/NRE/NRE/NRE' \
        'ldftn-local-valuetype-boxed=True/False/5/5' \
        '== value-type predicates folded for an enum ==' \
        'folded: DayOfWeek=True/False Shade=True/False Enum=False/True' \
        'folded in a generic body: Shade=True/False DayOfWeek=True/False int=True/False string=False/True' \
        'value-type predicates folded for an enum end' \
        'ordinary interface and ValueType IL end'; do
        grep -Fxq "$line" <<< "$native" \
            || { echo "FAIL: ordinary interface IL witness missing: $line" >&2; exit 1; }
    done
    DN2CPP_BEFORE_OBJECT_METHODIMPL=1 run_bounded dotnet "$_CG_APP" \
        > "$out/before-object-methodimpl.stdout"
    sed '/^== Object slots with MethodImpl bodies ==/,$d' "$out/metadata-layout.stdout" \
        > "$out/object-methodimpl-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-object-methodimpl.stdout") \
        <(strip_cr_win_file "$out/object-methodimpl-prefix.stdout")
    for line in '== Object slots with MethodImpl bodies ==' \
        'object-methodimpl-body-base=alias/True/False/701' \
        'object-methodimpl-base=alias/alias/True/True/False/701' \
        'object-methodimpl-call-base=alias/True/True/False/701' \
        'object-methodimpl-body-derived=derived/True/False/907' \
        'object-methodimpl-derived=derived/derived/True/True/False/907' \
        'object-methodimpl-call-derived=derived/True/True/False/907' \
        'object-methodimpl-body-hider=alias/True/False/701' \
        'object-methodimpl-hider=alias/alias/True/True/False/701' \
        'object-methodimpl-call-hider=alias/True/True/False/701' \
        'object-methodimpl-body-generic-string=generic/True/False/1103' \
        'object-methodimpl-generic-string=generic/generic/True/True/False/1103' \
        'object-methodimpl-call-generic-string=generic/True/True/False/1103' \
        'object-methodimpl-body-generic-object=generic/True/False/1103' \
        'object-methodimpl-generic-object=generic/generic/True/True/False/1103' \
        'object-methodimpl-call-generic-object=generic/True/True/False/1103' \
        'Object slots with MethodImpl bodies end'; do
        grep -Fxq "$line" <<< "$native" \
            || { echo "FAIL: Object MethodImpl witness missing: $line" >&2; exit 1; }
    done
    # Every emitted body follows its `// Type::Method` line, CRLF-terminated on a
    # Windows host. Delegate tags belong to the rewritten bodies alone, since C#
    # never builds a delegate from a stored or joined address.
    local tag_owners stray_owners
    tag_owners=$(LC_ALL=C awk '{ sub(/\r$/, "") } /^\/\/ .*::/ { owner = substr($0, 4) }
        /int32_t [A-Za-z0-9_]+_delegate_tag/ { print owner }' "$out"/generated*.cpp | LC_ALL=C sort -u)
    grep -Fxq 'LdftnLocalSubset.Program::Selected' <<<"$tag_owners"
    stray_owners=$(grep -Ev '^LdftnLocalSubset\.Program::(Stored|NopSeparated|NativeConvert|SnapshotBeforeOverwrite|Selected|StackJoin|ClosedStored|RawCalli|DeadOrigins|VirtualStored|InstanceStored|Int64Stored|SealedInterface|SealedGenericInterface|OriginBoundary)$' <<<"$tag_owners" || true)
    if [ -n "$stray_owners" ]; then
        printf 'error: delegate tags outside the rewritten bodies:\n%s\n' "$stray_owners" >&2
        return 1
    fi
    DN2CPP_BEFORE_LDFTN_LOCAL=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-ldftn-local.stdout"
    sed '/^ldftn-local-begin/,$d' "$out/metadata-layout.stdout" > "$out/ldftn-local-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-ldftn-local.stdout") \
        <(strip_cr_win_file "$out/ldftn-local-prefix.stdout")
    grep -Fxq '== reflection invoke validation ==' <<< "$native"
    grep -Fxq 'target calls: 2' <<< "$native"
    grep -Fxq 'plain get, stray index: TargetParameterCountException' <<< "$native"
    grep -Fxq 'bound ValueType delegate: boxed:5' <<< "$native"
    grep -Fxq 'nullable result without value: null' <<< "$native"
    grep -Fxq 'current number format: separator:.' <<< "$native"
    grep -Fxq 'number from int: number:7' <<< "$native"
    grep -Fxq 'number from long: ArgumentException' <<< "$native"
    DN2CPP_BEFORE_INVOKE_VALIDATION=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-invoke-validation.stdout"
    sed '/^== reflection invoke validation ==/,$d' "$out/metadata-layout.stdout" > "$out/invoke-validation-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-invoke-validation.stdout") \
        <(strip_cr_win_file "$out/invoke-validation-prefix.stdout")
    grep -Fxq '== runtime handle relations ==' <<< "$native"
    grep -Fxq 'runtime NullReferenceException chain: NullReferenceException > SystemException > Exception > Object' <<< "$native"
    grep -Fxq 'ManualResetEvent after IDisposable: ObjectDisposedException' <<< "$native"
    grep -Fxq 'runtime handle relations end' <<< "$native"
    local install
    for install in 'dn2cpp_set_relation_rows(rel_itf_sets, ' \
            'dn2cpp_intrinsic_set_base(&dn2cpp_null_reference_exception_type, &ti_System_SystemException);' \
            'dn2cpp_intrinsic_set_interfaces(&dn2cpp_semaphore_type, '; do
        if ! grep -Fq "$install" "$out"/generated*.cpp; then
            printf 'error: the init prologue lacks %s\n' "$install" >&2
            return 1
        fi
    done
    DN2CPP_BEFORE_RUNTIME_HANDLE_RELATIONS=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-runtime-handle-relations.stdout"
    sed '/^== runtime handle relations ==/,$d' "$out/metadata-layout.stdout" > "$out/runtime-handle-relations-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-runtime-handle-relations.stdout") \
        <(strip_cr_win_file "$out/runtime-handle-relations-prefix.stdout")
    gate_empty_string_clone_asserts "$out"

    run_bounded dotnet "$_CG_APP" before-delegate-origin-boundaries > "$out/origin-before.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" before-delegate-origin-boundaries > "$out/origin-before.native.stdout"
    sed '/^== delegate origin boundaries ==/,$d' "$out/metadata-layout.stdout" > "$out/origin-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/origin-before.dotnet.stdout") \
        <(strip_cr_win_file "$out/origin-prefix.stdout")
    diff -u <(strip_cr_win_file "$out/origin-before.native.stdout") \
        <(strip_cr_win_file "$out/origin-prefix.stdout")
    for line in '== delegate origin boundaries ==' 'delegate-origin-first=12/Add/True' \
        'delegate-origin-second=2/Subtract/True' 'delegate origin boundaries end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: delegate origin positive witness missing: $line" >&2; return 1; }
    done

    run_bounded dotnet "$_CG_APP" > "$out/runtime-type-relations.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/runtime-type-relations.native.stdout"
    diff -u <(strip_cr_win_file "$out/runtime-type-relations.dotnet.stdout") \
        <(strip_cr_win_file "$out/runtime-type-relations.native.stdout")
    run_bounded dotnet "$_CG_APP" before-runtime-type-relations > "$out/runtime-type-relations-before.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" before-runtime-type-relations > "$out/runtime-type-relations-before.native.stdout"
    sed '/^== runtime Type and Task relations ==/,$d' "$out/runtime-type-relations.dotnet.stdout" \
        > "$out/runtime-type-relations-prefix.dotnet.stdout"
    sed '/^== runtime Type and Task relations ==/,$d' "$out/runtime-type-relations.native.stdout" \
        > "$out/runtime-type-relations-prefix.native.stdout"
    diff -u <(strip_cr_win_file "$out/runtime-type-relations-before.dotnet.stdout") \
        <(strip_cr_win_file "$out/runtime-type-relations-prefix.dotnet.stdout")
    diff -u <(strip_cr_win_file "$out/runtime-type-relations-before.native.stdout") \
        <(strip_cr_win_file "$out/runtime-type-relations-prefix.native.stdout")
    for line in '== runtime Type and Task relations ==' 'runtime Type and Task relations end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: runtime Type relation block witness missing: $line" >&2; return 1; }
    done

    run_bounded dotnet "$_CG_APP" > "$out/generic-method-definitions.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/generic-method-definitions.native.stdout"
    diff -u <(strip_cr_win_file "$out/generic-method-definitions.dotnet.stdout") \
        <(strip_cr_win_file "$out/generic-method-definitions.native.stdout")
    run_bounded dotnet "$_CG_APP" before-generic-method-definitions > "$out/generic-method-definitions-before.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" before-generic-method-definitions > "$out/generic-method-definitions-before.native.stdout"
    sed '/^== generic method definitions ==/,$d' "$out/generic-method-definitions.dotnet.stdout" \
        > "$out/generic-method-definitions-prefix.dotnet.stdout"
    sed '/^== generic method definitions ==/,$d' "$out/generic-method-definitions.native.stdout" \
        > "$out/generic-method-definitions-prefix.native.stdout"
    diff -u <(strip_cr_win_file "$out/generic-method-definitions-before.dotnet.stdout") \
        <(strip_cr_win_file "$out/generic-method-definitions-prefix.dotnet.stdout")
    diff -u <(strip_cr_win_file "$out/generic-method-definitions-before.native.stdout") \
        <(strip_cr_win_file "$out/generic-method-definitions-prefix.native.stdout")
    for line in '== generic method definitions ==' 'generic method definitions end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: generic method definition block witness missing: $line" >&2; return 1; }
    done
    for line in class struct owners overloads; do
        grep -Eq "^definition roots $line=" <<< "$native" \
            || { echo "FAIL: generic method definition root witness missing: $line" >&2; return 1; }
    done
    for line in class struct owner-int owner-string inherited echo private unreached template-unreached; do
        grep -Fxq -- "definition $line list: count=1 definitions=True identity=True" <<< "$native" \
            || { echo "FAIL: generic method definition list mismatch: $line" >&2; return 1; }
        grep -Fxq -- "definition $line flags: generic=True definition=True contains=True" <<< "$native" \
            || { echo "FAIL: generic method definition flags mismatch: $line" >&2; return 1; }
        grep -Fxq -- "definition $line parameter 0: name=U generic=True contains=True identity=True" <<< "$native" \
            || { echo "FAIL: generic method formal parameter mismatch: $line" >&2; return 1; }
        grep -Fxq -- "definition $line open invoke=InvalidOperationException/InvalidOperationException/InvalidOperationException" <<< "$native" \
            || { echo "FAIL: generic method definition invocation mismatch: $line" >&2; return 1; }
    done
    for line in class struct owner-int owner-string echo private; do
        grep -Fxq -- "definition $line canonical: operator=True equals=True reference=True lookup=True self=True" <<< "$native" \
            || { echo "FAIL: generic method definition identity mismatch: $line" >&2; return 1; }
    done
    grep -Fxq -- 'definition inherited canonical: operator=True equals=True reference=True lookup=False self=False' <<< "$native"
    grep -Fxq -- 'definition echo signature: return=True parameter=True' <<< "$native"
    run_bounded dotnet "$_CG_APP" generic-method-boundary-outcomes > "$out/generic-method-boundary.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" generic-method-boundary-outcomes > "$out/generic-method-boundary.native.stdout"
    for axis in dotnet native; do
        boundary=$(strip_cr_win_file "$out/generic-method-boundary.$axis.stdout")
        for line in '== generic method AOT instantiations ==' 'generic method AOT instantiations end'; do
            grep -Fxq -- "$line" <<< "$boundary" \
                || { echo "FAIL: generic method boundary block witness missing ($axis): $line" >&2; return 1; }
        done
        for line in unreached missing-argument template-unreached; do
            grep -Fxq -- "definition boundary $line found=True" <<< "$boundary" \
                || { echo "FAIL: generic method boundary definition missing ($axis): $line" >&2; return 1; }
            if [ "$axis" = native ]; then
                grep -Fxq -- "definition boundary $line fault=PlatformNotSupportedException" <<< "$boundary" \
                    || { echo "FAIL: missing AOT instantiation was not refused: $line" >&2; return 1; }
            fi
        done
        if [ "$axis" = dotnet ]; then
            for line in 'definition boundary unreached result=never:Int32:8' \
                'definition boundary missing-argument result=Boolean:8' \
                'definition boundary template-unreached result=Int64:8'; do
                grep -Fxq -- "$line" <<< "$boundary" \
                    || { echo "FAIL: CLR generic method boundary oracle mismatch: $line" >&2; return 1; }
            done
        fi
    done

    run_bounded dotnet "$_CG_APP" before-formal-method-parameters > "$out/formal-method-parameters-before.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" before-formal-method-parameters > "$out/formal-method-parameters-before.native.stdout"
    for axis in dotnet native; do
        sed '/^== formal method parameters ==/,$d' "$out/generic-method-definitions.$axis.stdout" \
            > "$out/formal-method-parameters-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/formal-method-parameters-before.$axis.stdout") \
            <(strip_cr_win_file "$out/formal-method-parameters-prefix.$axis.stdout")
    done
    for line in '== formal method parameters ==' 'formal method parameters end' \
        'formal names: name=U full=<null> qualified=<null>' 'formal assembly owner=True' \
        'formal namespaces: parameter=ReflectGenericMethodSubset declaring=ReflectGenericMethodSubset' \
        'formal global names: namespace=<null> name=U' \
        'formal assembly closed-owner=True inherited=True' 'formal activation=ArgumentException' \
        'formal array allocation=NotSupportedException' 'formal uninitialized=ArgumentException' \
        'formal signature: found=True reference=True' 'formal foreign signature found=False'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: formal method parameter witness missing: $line" >&2; return 1; }
    done
    # The CLR oracle decides normalization for both definition and closed sources.
    grep -Eq '^formal mapped definition: ' <<< "$native"
    grep -Eq '^formal mapped closed: ' <<< "$native"
    run_bounded dotnet "$_CG_APP" formal-type-boundary-outcomes > "$out/formal-type-boundary.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" formal-type-boundary-outcomes > "$out/formal-type-boundary.native.stdout"
    for axis in dotnet native; do
        boundary=$(strip_cr_win_file "$out/formal-type-boundary.$axis.stdout")
        for line in '== formal type compositions ==' 'formal type compositions end' \
            '== formal method compositions ==' 'formal method compositions end'; do
            grep -Fxq -- "$line" <<< "$boundary" \
                || { echo "FAIL: formal type composition block witness missing ($axis): $line" >&2; return 1; }
        done
        for line in generic array rank2-array; do
            if [ "$axis" = native ]; then
                grep -Fxq -- "formal composition $line fault=PlatformNotSupportedException" <<< "$boundary" \
                    || { echo "FAIL: composed open type was not refused: $line" >&2; return 1; }
            else
                grep -Fxq -- "formal composition $line contains=True" <<< "$boundary" \
                    || { echo "FAIL: CLR formal type composition oracle mismatch: $line" >&2; return 1; }
            fi
        done
        if [ "$axis" = dotnet ]; then
            grep -Fxq -- 'formal composition generic activation=ArgumentException' <<< "$boundary"
        fi
        for line in metadata-answer compiled; do
            if [ "$axis" = native ]; then
                grep -Fxq -- "formal method composition $line fault=PlatformNotSupportedException" <<< "$boundary" \
                    || { echo "FAIL: open generic method was not refused: $line" >&2; return 1; }
            else
                grep -Fxq -- "formal method composition $line: definition=False contains=True" <<< "$boundary" \
                    || { echo "FAIL: CLR open generic method oracle mismatch: $line" >&2; return 1; }
                grep -Fxq -- "formal method composition $line invoke=InvalidOperationException" <<< "$boundary" \
                    || { echo "FAIL: CLR open generic method invocation oracle mismatch: $line" >&2; return 1; }
            fi
        done
    done

    run_bounded dotnet "$_CG_APP" before-mixed-generic-definitions > "$out/mixed-generic-definitions-before.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" before-mixed-generic-definitions > "$out/mixed-generic-definitions-before.native.stdout"
    for axis in dotnet native; do
        sed '/^== mixed generic method definitions ==/,$d' "$out/generic-method-definitions.$axis.stdout" \
            > "$out/mixed-generic-definitions-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/mixed-generic-definitions-before.$axis.stdout") \
            <(strip_cr_win_file "$out/mixed-generic-definitions-prefix.$axis.stdout")
    done
    for line in '== mixed generic method definitions ==' 'mixed generic method definitions end' 'mixed roots=7/text' \
        'formal declaring own: reference=True operator=True method-owner=True' \
        'formal declaring owner-int: reference=True operator=True method-owner=False' \
        'formal declaring owner-string: reference=True operator=True method-owner=False' \
        'formal declaring inherited: reference=True operator=True method-owner=True'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: mixed generic definition witness missing: $line" >&2; return 1; }
    done
    for line in Never Echo; do
        grep -Fxq -- "mixed $line GetMethods count=1" <<< "$native"
        for route in GetMember GetMembers; do
            grep -Fxq -- "mixed $line $route count=1" <<< "$native"
            grep -Fxq -- "mixed $line $route flags: generic=True definition=True contains=True" <<< "$native"
            grep -Fxq -- "mixed $line $route arguments=1 name=U" <<< "$native"
            grep -Fxq -- "mixed $line $route identity: reference=True operator=True equals=True" <<< "$native"
            grep -Fxq -- "mixed $line $route signature: return=True parameter=True return-parameter=True" <<< "$native"
            grep -Fxq -- "mixed $line $route invoke=InvalidOperationException" <<< "$native"
        done
    done
    run_bounded dotnet "$_CG_APP" generic-signature-boundary-outcomes > "$out/generic-signature-boundary.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" generic-signature-boundary-outcomes > "$out/generic-signature-boundary.native.stdout"
    for axis in dotnet native; do
        boundary=$(strip_cr_win_file "$out/generic-signature-boundary.$axis.stdout")
        for line in '== generic signature type boundaries ==' 'generic signature type boundaries end' \
            'signature roots: array=1 byref=7 list=1 enumerable=True function=True task=True' \
            'signature closed byref invoke=7 value=7'; do
            grep -Fxq -- "$line" <<< "$boundary" \
                || { echo "FAIL: generic signature boundary witness missing ($axis): $line" >&2; return 1; }
        done
        for line in ArrayShape RefShape ListShape EnumerableShape FunctionShape TaskShape; do
            grep -Fxq -- "signature $line GetMethods count=1" <<< "$boundary"
            grep -Fxq -- "signature $line closed definition=False contains=False" <<< "$boundary"
            for route in GetMethod GetMethods; do
                grep -Fxq -- "signature $line $route definition=True contains=True" <<< "$boundary"
                for query in return parameter return-parameter; do
                    if [ "$line" = RefShape ] && [ "$query" != parameter ]; then
                        grep -Fxq -- "signature $line $route $query=type:U|parameter=True|contains=True" <<< "$boundary"
                    elif [ "$axis" = native ]; then
                        grep -Fxq -- "signature $line $route $query=fault:PlatformNotSupportedException" <<< "$boundary" \
                            || { echo "FAIL: composed signature query was not refused: $line $route $query" >&2; return 1; }
                    else
                        grep -Eq "^signature $line $route $query=type:.*\\|parameter=False\\|contains=True$" <<< "$boundary" \
                            || { echo "FAIL: CLR composed signature oracle missing: $line $route $query" >&2; return 1; }
                    fi
                done
            done
            if [ "$line" != RefShape ]; then
                case "$line" in
                    ArrayShape) expected='System.Int32[]' ;;
                    ListShape) expected='System.Collections.Generic.List`1[System.Int32]' ;;
                    EnumerableShape) expected='System.Collections.Generic.IEnumerable`1[System.Int32]' ;;
                    FunctionShape) expected='System.Func`2[System.Int32,System.Boolean]' ;;
                    TaskShape) expected='System.Threading.Tasks.Task`1[System.Int32]' ;;
                esac
                for query in return parameter return-parameter; do
                    grep -Fxq -- "signature $line closed $query=type:$expected|parameter=False|contains=False" <<< "$boundary"
                done
            fi
        done
        # Closed byref ParameterType retains the separate signature-handle limit.
        grep -Fxq -- 'signature RefShape closed return=type:System.Int32|parameter=False|contains=False' <<< "$boundary"
        grep -Fxq -- 'signature RefShape closed return-parameter=type:System.Int32|parameter=False|contains=False' <<< "$boundary"
        if [ "$axis" = native ]; then
            grep -Fxq -- 'signature RefShape closed parameter=type:System.Object|parameter=False|contains=False' <<< "$boundary"
            for line in Unconstrained ValueConstrained InterfaceConstrained ReferenceConstrained BaseConstrained; do
                grep -Fxq -- "signature constraints $line fault=PlatformNotSupportedException" <<< "$boundary"
            done
        else
            for line in 'signature RefShape closed parameter=type:System.Int32&|parameter=False|contains=False' \
                'signature constraints Unconstrained count=0' 'signature constraints ValueConstrained count=1' \
                'signature constraints ValueConstrained type=System.ValueType' \
                'signature constraints InterfaceConstrained count=1' \
                'signature constraints InterfaceConstrained type=ReflectGenericMethodSubset.IDefinitionConstraint' \
                'signature constraints ReferenceConstrained count=0' 'signature constraints BaseConstrained count=1' \
                'signature constraints BaseConstrained type=ReflectGenericMethodSubset.DefinitionConstraintBase'; do
                grep -Fxq -- "$line" <<< "$boundary"
            done
        fi
        if [ "$axis" = native ]; then
            expected='System.Object'
            grep -Fxq -- 'signature constrained InterfaceConstrained interfaces=0' <<< "$boundary"
        else
            expected='ReflectGenericMethodSubset.DefinitionConstraintBase'
            grep -Fxq -- 'signature constrained InterfaceConstrained interfaces=1' <<< "$boundary"
            grep -Fxq -- 'signature constrained InterfaceConstrained interface=ReflectGenericMethodSubset.IDefinitionConstraint' <<< "$boundary"
        fi
        grep -Fxq -- "signature constrained BaseConstrained base=$expected" <<< "$boundary"
        grep -Fxq -- 'signature constrained BaseConstrained interfaces=0' <<< "$boundary"
        grep -Fxq -- 'signature constrained InterfaceConstrained base=System.Object' <<< "$boundary"
    done

    run_bounded dotnet "$_CG_APP" before-formal-reflected-owners > "$out/formal-reflected-owners-before.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" before-formal-reflected-owners > "$out/formal-reflected-owners-before.native.stdout"
    for axis in dotnet native; do
        sed '/^== formal parameter reflected owners ==/,$d' "$out/generic-method-definitions.$axis.stdout" \
            > "$out/formal-reflected-owners-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/formal-reflected-owners-before.$axis.stdout") \
            <(strip_cr_win_file "$out/formal-reflected-owners-prefix.$axis.stdout")
    done
    for line in '== formal parameter reflected owners ==' 'formal parameter reflected owners end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: formal reflected-owner block witness missing: $line" >&2; return 1; }
    done
    for line in own owner-int owner-string inherited global; do
        grep -Fxq -- "formal reflected $line: declaring-reference=True declaring-operator=True reflected-reference=True reflected-operator=True same-reference=True same-operator=True" <<< "$native"
        case "$line" in
            own|global) expected=True ;;
            *) expected=False ;;
        esac
        grep -Fxq -- "formal reflected $line lookup: expected=True parameter-same=$expected" <<< "$native"
    done

    run_bounded dotnet "$_CG_APP" before-formal-classification > "$out/formal-classification-before.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" before-formal-classification > "$out/formal-classification-before.native.stdout"
    for axis in dotnet native; do
        sed '/^== formal method parameter classification ==/,$d' "$out/generic-method-definitions.$axis.stdout" \
            > "$out/formal-classification-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/formal-classification-before.$axis.stdout") \
            <(strip_cr_win_file "$out/formal-classification-prefix.$axis.stdout")
    done
    for line in '== formal method parameter classification ==' 'formal method parameter classification end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: formal classification block witness missing: $line" >&2; return 1; }
    done
    for line in Free Value Reference; do
        expected='nested=True value=False class=True base=System.Object interfaces=0'
        if [ "$line" = Value ]; then
            expected='nested=True value=True class=False base=System.ValueType interfaces=0'
        fi
        for route in GetMethod GetMethods; do
            grep -Fxq -- "formal classification $line $route: $expected" <<< "$native"
        done
        grep -Fxq -- "formal classification $line GetMethods count=1" <<< "$native"
    done

    grep -Fxq -- 'formal classification Value allocation: activation=ArgumentException array=NotSupportedException uninitialized=ArgumentException' <<< "$native"

    run_bounded dotnet "$_CG_APP" before-metadata-formal-attributes > "$out/metadata-formal-attributes-before.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" before-metadata-formal-attributes > "$out/metadata-formal-attributes-before.native.stdout"
    for axis in dotnet native; do
        sed '/^== metadata formal parameter attributes ==/,$d' "$out/generic-method-definitions.$axis.stdout" \
            > "$out/metadata-formal-attributes-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/metadata-formal-attributes-before.$axis.stdout") \
            <(strip_cr_win_file "$out/metadata-formal-attributes-prefix.$axis.stdout")
    done
    for line in '== metadata formal parameter attributes ==' 'metadata formal parameter attributes end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: metadata formal attribute block witness missing: $line" >&2; return 1; }
    done
    for line in metadata ordinary; do
        case "$line" in
            metadata) expected=32; parameter=T ;;
            ordinary) expected=0; parameter=U ;;
        esac
        grep -Fxq -- "metadata formal $line: name=$parameter attributes=$expected definition=True contains=True nested=True value=False class=True" <<< "$native"
        grep -Fxq -- "metadata formal $line definition: attributes=$expected canonical=True" <<< "$native"
        grep -Fxq -- "metadata formal $line closed definition: attributes=$expected canonical=True" <<< "$native"
    done

    run_bounded dotnet "$_CG_APP" before-formal-member-types > "$out/formal-member-types-before.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" before-formal-member-types > "$out/formal-member-types-before.native.stdout"
    for axis in dotnet native; do
        sed '/^== formal parameter member types ==/,$d' "$out/generic-method-definitions.$axis.stdout" \
            > "$out/formal-member-types-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/formal-member-types-before.$axis.stdout") \
            <(strip_cr_win_file "$out/formal-member-types-prefix.$axis.stdout")
    done
    for line in '== formal parameter member types ==' 'formal parameter member types end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: formal MemberType block witness missing: $line" >&2; return 1; }
    done
    for line in free value reference metadata; do
        grep -Fxq -- "formal member $line: type=32 memberinfo=32 nested=True" <<< "$native"
    done
    grep -Fxq -- 'formal member nested: type=128 memberinfo=128 nested=True' <<< "$native"
    grep -Fxq -- 'formal member top-level: type=32 memberinfo=32 nested=False' <<< "$native"

    run_bounded dotnet "$_CG_APP" before-definition-signature-closure > "$out/definition-signature-closure-before.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" before-definition-signature-closure > "$out/definition-signature-closure-before.native.stdout"
    for axis in dotnet native; do
        sed '/^== definition signature closure ==/,$d' "$out/generic-method-definitions.$axis.stdout" \
            > "$out/definition-signature-closure-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/definition-signature-closure-before.$axis.stdout") \
            <(strip_cr_win_file "$out/definition-signature-closure-prefix.$axis.stdout")
    done
    for line in '== definition signature closure ==' 'definition signature closure end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: definition signature closure block witness missing: $line" >&2; return 1; }
    done
    for line in Pick QueuePick; do
        case "$line" in
            Pick) expected='DefinitionSignatureHolder`1' ;;
            QueuePick) expected='Queue`1' ;;
        esac
        for route in GetMethod GetMethods; do
            grep -Fxq -- "signature closure $line $route definition=True" <<< "$native"
            for query in return parameter return-parameter; do
                grep -Eq -- "^signature closure $line $route $query: name=$expected full=.+ generic=True$" <<< "$native" \
                    || { echo "FAIL: definition's closed signature type missing: $line $route $query" >&2; return 1; }
            done
            grep -Fxq -- "signature closure $line $route identity: parameter=True return-parameter=True" <<< "$native"
        done
        grep -Fxq -- "signature closure $line GetMethods count=1" <<< "$native"
    done

    run_bounded dotnet "$_CG_APP" before-delegate-name-bindings > "$out/delegate-name-before.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" before-delegate-name-bindings > "$out/delegate-name-before.native.stdout"
    for axis in dotnet native; do
        sed '/^== delegate method name bindings ==/,$d' "$out/generic-method-definitions.$axis.stdout" \
            > "$out/delegate-name-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/delegate-name-before.$axis.stdout") \
            <(strip_cr_win_file "$out/delegate-name-prefix.$axis.stdout")
    done
    for line in '== delegate method name bindings ==' 'delegate method name bindings end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: delegate name binding block witness missing: $line" >&2; return 1; }
    done

    run_bounded dotnet "$_CG_APP" before-delegate-signature-bindings > "$out/delegate-signature-before.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" before-delegate-signature-bindings > "$out/delegate-signature-before.native.stdout"
    for axis in dotnet native; do
        sed '/^== delegate signature compatibility ==/,$d' "$out/generic-method-definitions.$axis.stdout" \
            > "$out/delegate-signature-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/delegate-signature-before.$axis.stdout") \
            <(strip_cr_win_file "$out/delegate-signature-prefix.$axis.stdout")
    done
    for line in '== delegate signature compatibility ==' \
        'signature ref object to value => null/null' 'signature value to ref object => null/null' \
        'signature ref to out => bound/bound' 'signature out to ref => bound/bound' \
        'signature pointer signedness => bound/null' 'signature native vs fixed pointer => null/null' \
        'signature native pointer signedness => bound/null' 'signature bool vs byte pointer => null/null' \
        'signature reference pointer argument => bound/null' 'signature reference pointer return => bound/null' \
        'signature reference deep pointer => null/null' 'signature function leaf to object pointer => null/null' \
        'signature headerless value return to ref => null/null' 'signature headerless ref alias => True' \
        'signature byref-like ref referent mismatch => null/null' 'signature byref-like ref exact => bound/bound' \
        'signature reference pointer calls => 46/256' \
        'signature function return mismatch => null/null' 'signature managed vs Cdecl => null/null' \
        'signature Cdecl vs Stdcall => bound/bound' 'signature suppressed order => bound/bound' \
        'signature function argument mismatch => null/null' 'signature closed static byref => null/null' \
        'signature shell MulticastDelegate => ArgumentException/type/ArgumentException/delegateType' \
        'signature byref modes => 44/44/45/closed' 'signature relaxed pointer call => 42' \
        'signature function return calls => 256/512' 'signature ref function alias => 1280' \
        'signature template known return mismatch => null/null' 'signature template known argument mismatch => null/null' \
        'signature template fixed => 1536/1792/2048/2304/True' 'delegate signature compatibility end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: delegate signature witness missing: $line" >&2; return 1; }
    done

    run_bounded dotnet "$_CG_APP" before-runtime-function-pointer-invoke > "$out/function-pointer-invoke-before.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" before-runtime-function-pointer-invoke > "$out/function-pointer-invoke-before.native.stdout"
    for axis in dotnet native; do
        sed '/^== runtime function pointer invocation descriptors ==/,$d' "$out/generic-method-definitions.$axis.stdout" \
            > "$out/function-pointer-invoke-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/function-pointer-invoke-before.$axis.stdout") \
            <(strip_cr_win_file "$out/function-pointer-invoke-prefix.$axis.stdout")
    done
    for line in '== runtime function pointer invocation descriptors ==' \
        'signature invoke string => ArgumentException/0' 'signature invoke boxed integer => ArgumentException/0' \
        'signature invoke int pointer box => ArgumentException/0' 'signature invoke IntPtr => 256/1' \
        'signature invoke nested box => True/512' 'signature invoke nested box roundtrip => 512/1' \
        'runtime function pointer invocation descriptors end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: function-pointer Invoke witness missing: $line" >&2; return 1; }
    done

    run_bounded dotnet "$_CG_APP" before-unsupported-referent-signatures > "$out/unsupported-referent-before.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" before-unsupported-referent-signatures > "$out/unsupported-referent-before.native.stdout"
    for axis in dotnet native; do
        sed '/^== unsupported referent delegate signatures ==/,$d' "$out/generic-method-definitions.$axis.stdout" \
            > "$out/unsupported-referent-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/unsupported-referent-before.$axis.stdout") \
            <(strip_cr_win_file "$out/unsupported-referent-prefix.$axis.stdout")
    done
    for line in '== unsupported referent delegate signatures ==' \
        'signature opaque return to object => null/null' 'signature opaque return mismatch => null/null' \
        'signature opaque return opposite => null/null' 'signature opaque return exact => bound/bound' \
        'signature opaque argument mismatch => null/null' 'signature opaque argument opposite => null/null' \
        'signature opaque argument exact => bound/bound' 'signature opaque typed alias => False/True/True' \
        'unsupported referent delegate signatures end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: unsupported referent signature witness missing: $line" >&2; return 1; }
    done

    run_bounded dotnet "$_CG_APP" before-ordinary-template-signatures > "$out/ordinary-template-before.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" before-ordinary-template-signatures > "$out/ordinary-template-before.native.stdout"
    for axis in dotnet native; do
        sed '/^== ordinary runtime delegate signature controls ==/,$d' "$out/generic-method-definitions.$axis.stdout" \
            > "$out/ordinary-template-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/ordinary-template-before.$axis.stdout") \
            <(strip_cr_win_file "$out/ordinary-template-prefix.$axis.stdout")
    done
    for line in '== ordinary runtime delegate signature controls ==' \
        'signature ordinary known return mismatch => null/null' \
        'signature ordinary known argument mismatch => null/null' \
        'signature ordinary known shape mismatch => null/null' \
        'signature ordinary template fixed => 7/8/True' \
        'signature ordinary compiled ref => 42/42/42' \
        'ordinary runtime delegate signature controls end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: ordinary template signature witness missing: $line" >&2; return 1; }
    done

    run_bounded dotnet "$_CG_APP" before-ordinary-overload-selection > "$out/ordinary-overload-before.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" before-ordinary-overload-selection > "$out/ordinary-overload-before.native.stdout"
    for axis in dotnet native; do
        sed '/^== ordinary runtime overload selection ==/,$d' "$out/generic-method-definitions.$axis.stdout" \
            > "$out/ordinary-overload-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/ordinary-overload-before.$axis.stdout") \
            <(strip_cr_win_file "$out/ordinary-overload-prefix.$axis.stdout")
    done
    for line in '== ordinary runtime overload selection ==' \
        'signature overload ref => bound/7/11/bound/7/11' \
        'signature overload declaring base => bound/37/11/bound/37/11' \
        'signature overload reference arguments => 27/fixed' \
        'signature overload pointer => 17/11' 'signature overload pointer return => 256' \
        'signature overload ref return => 19/19' \
        'signature overload composed types => True/True/True' \
        'signature overload composed ref => bound/47/True/bound/47/True' \
        'signature overload generic family => 57/True' 'signature overload constant argument => 67/True' \
        'ordinary runtime overload selection end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: ordinary overload witness missing: $line" >&2; return 1; }
    done

    run_bounded dotnet "$_CG_APP" before-shape-overload-selection > "$out/shape-overload-before.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" before-shape-overload-selection > "$out/shape-overload-before.native.stdout"
    for axis in dotnet native; do
        sed '/^== structured runtime overload selection ==/,$d' "$out/generic-method-definitions.$axis.stdout" \
            > "$out/shape-overload-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/shape-overload-before.$axis.stdout") \
            <(strip_cr_win_file "$out/shape-overload-prefix.$axis.stdout")
    done
    for line in '== structured runtime overload selection ==' \
        'signature overload function => 87/87' 'signature overload deep pointer => 97/97' \
        'signature overload distinct deep levels => 107/107' 'structured runtime overload selection end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: structured overload witness missing: $line" >&2; return 1; }
    done

    run_bounded dotnet "$_CG_APP" before-leaf-overload-selection > "$out/leaf-overload-before.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" before-leaf-overload-selection > "$out/leaf-overload-before.native.stdout"
    for axis in dotnet native; do
        sed '/^== function leaf overload selection ==/,$d' "$out/generic-method-definitions.$axis.stdout" \
            > "$out/leaf-overload-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/leaf-overload-before.$axis.stdout") \
            <(strip_cr_win_file "$out/leaf-overload-prefix.$axis.stdout")
    done
    for line in '== function leaf overload selection ==' \
        'signature overload array => 117/117' 'signature overload matrix => 127/127' \
        'signature overload array rank => 137/137' 'signature overload opaque function leaf => 157/157/157/157' \
        'function leaf overload selection end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: function leaf overload witness missing: $line" >&2; return 1; }
    done

    run_bounded dotnet "$_CG_APP" before-identity-overload-selection > "$out/identity-overload-before.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" before-identity-overload-selection > "$out/identity-overload-before.native.stdout"
    for axis in dotnet native; do
        sed '/^== omitted identity overload selection ==/,$d' "$out/generic-method-definitions.$axis.stdout" \
            > "$out/identity-overload-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/identity-overload-before.$axis.stdout") \
            <(strip_cr_win_file "$out/identity-overload-prefix.$axis.stdout")
    done
    for line in '== omitted identity overload selection ==' \
        'signature overload absent generic family => 167/167' \
        'omitted identity overload selection end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: omitted identity overload witness missing: $line" >&2; return 1; }
    done

    run_bounded dotnet "$_CG_APP" before-runtime-argument-overloads > "$out/runtime-argument-before.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" before-runtime-argument-overloads > "$out/runtime-argument-before.native.stdout"
    for axis in dotnet native; do
        sed '/^== runtime type argument overload selection ==/,$d' "$out/generic-method-definitions.$axis.stdout" \
            > "$out/runtime-argument-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/runtime-argument-before.$axis.stdout") \
            <(strip_cr_win_file "$out/runtime-argument-prefix.$axis.stdout")
    done
    for line in '== runtime type argument overload selection ==' \
        'signature overload synthesized argument => 87/87' 'signature overload generic argument difference => 177/177' \
        'runtime type argument overload selection end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: runtime type argument overload witness missing: $line" >&2; return 1; }
    done

    run_bounded dotnet "$_CG_APP" before-family-type-overloads > "$out/family-type-before.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" before-family-type-overloads > "$out/family-type-before.native.stdout"
    for axis in dotnet native; do
        sed '/^== generic family and runtime type overload selection ==/,$d' "$out/generic-method-definitions.$axis.stdout" \
            > "$out/family-type-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/family-type-before.$axis.stdout") \
            <(strip_cr_win_file "$out/family-type-prefix.$axis.stdout")
    done
    for line in '== generic family and runtime type overload selection ==' \
        'signature overload nongeneric shape => 197/197' \
        'signature overload runtime child difference => 197/197' \
        'signature overload runtime family difference => 197/197' \
        'generic family and runtime type overload selection end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: generic family/type overload witness missing: $line" >&2; return 1; }
    done

    run_bounded dotnet "$_CG_APP" before-enum-signature-bindings > "$out/enum-signature-before.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" before-enum-signature-bindings > "$out/enum-signature-before.native.stdout"
    for axis in dotnet native; do
        sed '/^== delegate enum signature compatibility ==/,$d' "$out/generic-method-definitions.$axis.stdout" \
            > "$out/enum-signature-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/enum-signature-before.$axis.stdout") \
            <(strip_cr_win_file "$out/enum-signature-prefix.$axis.stdout")
    done
    for line in '== delegate enum signature compatibility ==' \
        'enum closed instance name => -113/-113/1/True' \
        'enum closed instance method => -113/-113/1/True' \
        'enum MethodInfo type => 227/227/1' 'enum MethodInfo generic => 60001/60001/1' \
        'enum runtime clone name => 4045620583/4045620583/1' \
        'enum runtime clone method => 4045620583/4045620583/1' \
        'enum closed static => 60001/60004/1' 'enum closed static value => null' \
        'delegate enum signature compatibility end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: enum delegate witness missing: $line" >&2; return 1; }
    done
    for backing_value in S8:-113 U8:227 S16:-30001 U16:60001 S32:-2000000001 \
        U32:4045620583 S64:-8000000000000000001 U64:17293822569102704641; do
        backing=${backing_value%%:*}
        value=${backing_value#*:}
        for relation in 'underlying to enum' 'enum to underlying' 'distinct enums'; do
            for api in name method; do
                grep -Fxq -- "enum $backing $relation $api => $value/$value/1/True" <<< "$native" \
                    || { echo "FAIL: enum delegate value changed: $backing/$relation/$api" >&2; return 1; }
            done
        done
    done
    for query in signedness width bool char 'native int' 'ordinary widening' \
        'ref enum to int' 'ref int to enum' 'out enum to int' 'ref return enum' 'function identity'; do
        for api in name method; do
            grep -Fxq -- "enum $query $api => null" <<< "$native" \
                || { echo "FAIL: enum delegate accepted an incompatible signature: $query/$api" >&2; return 1; }
        done
    done
    for query in 'pointer enum to int' 'pointer enum to uint'; do
        grep -Fxq -- "enum $query name => null" <<< "$native" \
            || { echo "FAIL: named enum pointer identity changed: $query" >&2; return 1; }
        grep -Fxq -- "enum $query method => bound" <<< "$native" \
            || { echo "FAIL: MethodInfo enum pointer compatibility changed: $query" >&2; return 1; }
    done

    run_bounded dotnet "$_CG_APP" before-intrinsic-pointer-bindings > "$out/intrinsic-pointer-before.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" before-intrinsic-pointer-bindings > "$out/intrinsic-pointer-before.native.stdout"
    for axis in dotnet native; do
        sed '/^== intrinsic pointer identity actual ==/,$d' "$out/generic-method-definitions.$axis.stdout" \
            > "$out/intrinsic-pointer-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/intrinsic-pointer-before.$axis.stdout") \
            <(strip_cr_win_file "$out/intrinsic-pointer-prefix.$axis.stdout")
        for line in '== intrinsic pointer identity actual ==' 'intrinsic pointer identity actual end' \
            'Invoke return => True/4660/1' 'Invoke wrong argument => ArgumentException/0'; do
            test "$(grep -Fxc -- "$line" "$out/generic-method-definitions.$axis.stdout")" = 1 \
                || { echo "FAIL: intrinsic pointer witness must run once: $axis/$line" >&2; return 1; }
        done
        for api in name method; do
            for query in 'Token object' 'Token registration' 'parameter object' 'parameter registration' \
                'return object' 'return registration' 'deep registration' 'deep depth' 'ordinary leaf' 'ordinary depth' 'clone object'; do
                grep -Fxq -- "bind $query $api => null/calls=0" "$out/generic-method-definitions.$axis.stdout" \
                    || { echo "FAIL: incompatible intrinsic pointer accepted: $axis/$query/$api" >&2; return 1; }
            done
            for line in "call Token $api => True/4660/4660/2" "call parameter $api => 7/0/1" \
                "call return $api => 4660/1" "call deep $api => 4660/4660/1" "call ordinary $api => 4660/1" \
                "call clone $api => 4660/4660/1"; do
                grep -Fxq -- "$line" "$out/generic-method-definitions.$axis.stdout" \
                    || { echo "FAIL: intrinsic pointer call changed: $axis/$line" >&2; return 1; }
            done
        done
        for query in primitive reference; do
            for api_result in name:null method:bound; do
                api=${api_result%%:*}
                result=${api_result#*:}
                grep -Fxq -- "bind $query relaxation $api => $result/calls=0" "$out/generic-method-definitions.$axis.stdout" \
                    || { echo "FAIL: ordinary pointer relaxation changed: $axis/$query/$api" >&2; return 1; }
            done
        done
    done

    run_bounded dotnet "$_CG_APP" before-intrinsic-pointer-overloads > "$out/intrinsic-pointer-overload-before.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" before-intrinsic-pointer-overloads > "$out/intrinsic-pointer-overload-before.native.stdout"
    for axis in dotnet native; do
        sed '/^== intrinsic pointer overload identity ==/,$d' "$out/generic-method-definitions.$axis.stdout" \
            > "$out/intrinsic-pointer-overload-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/intrinsic-pointer-overload-before.$axis.stdout") \
            <(strip_cr_win_file "$out/intrinsic-pointer-overload-prefix.$axis.stdout")
        for line in '== intrinsic pointer overload identity ==' 'intrinsic pointer overload identity end' \
            'call pointer overload soft => True/4660/4660/14' 'call pointer overload hard => True/4660/4660/14'; do
            test "$(grep -Fxc -- "$line" "$out/generic-method-definitions.$axis.stdout")" = 1 \
                || { echo "FAIL: intrinsic pointer overload witness must run once: $axis/$line" >&2; return 1; }
        done
    done

    run_bounded dotnet "$_CG_APP" before-intrinsic-pointer-families > "$out/intrinsic-pointer-family-before.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" before-intrinsic-pointer-families > "$out/intrinsic-pointer-family-before.native.stdout"
    for axis in dotnet native; do
        sed '/^== intrinsic pointer family identity ==/,$d' "$out/generic-method-definitions.$axis.stdout" \
            > "$out/intrinsic-pointer-family-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/intrinsic-pointer-family-before.$axis.stdout") \
            <(strip_cr_win_file "$out/intrinsic-pointer-family-prefix.$axis.stdout")
        for line in '== intrinsic pointer family identity ==' 'intrinsic pointer family identity end' \
            'pointer family => ValueTask`1' 'call pointer family soft => True/4660/4660/14' \
            'call pointer family hard => True/4660/4660/14'; do
            test "$(grep -Fxc -- "$line" "$out/generic-method-definitions.$axis.stdout")" = 1 \
                || { echo "FAIL: intrinsic pointer family witness must run once: $axis/$line" >&2; return 1; }
        done
    done

    for section in generic array; do
        if [ "$section" = generic ]; then
            before=before-intrinsic-generic-pointers
            begin='== intrinsic generic pointer identity =='
            end='intrinsic generic pointer identity end'
        else
            before=before-intrinsic-array-arguments
            begin='== intrinsic pointer array argument identity =='
            end='intrinsic pointer array argument identity end'
        fi
        run_bounded dotnet "$_CG_APP" "$before" > "$out/intrinsic-$section-before.dotnet.stdout"
        run_bounded "$out/ReflectInvoke$EXE_EXT" "$before" > "$out/intrinsic-$section-before.native.stdout"
        for axis in dotnet native; do
            sed "/^$begin/,\$d" "$out/generic-method-definitions.$axis.stdout" > "$out/intrinsic-$section-prefix.$axis.stdout"
            diff -u <(strip_cr_win_file "$out/intrinsic-$section-before.$axis.stdout") \
                <(strip_cr_win_file "$out/intrinsic-$section-prefix.$axis.stdout") \
                || { echo "FAIL: opaque pointer prefix changed: $axis/$section" >&2; return 1; }
            for line in "$begin" "$end" \
                "call pointer $section soft => True/4660/4660/14/0/True" \
                "call pointer $section hard => True/4660/4660/14/0/True"; do
                test "$(grep -Fxc -- "$line" "$out/generic-method-definitions.$axis.stdout")" = 1 \
                    || { echo "FAIL: opaque pointer witness must run once: $axis/$line" >&2; return 1; }
            done
            if [ "$section" = array ]; then
                test "$(grep -Fxc -- 'pointer dynamic array => 1/True' "$out/generic-method-definitions.$axis.stdout")" = 1 \
                    || { echo "FAIL: dynamic array must run once: $axis" >&2; return 1; }
            fi
        done
    done

    for section in 'constant argument' 'array child' 'MD array child'; do
        case "$section" in
            'constant argument') before=before-intrinsic-constant-arguments ;;
            'array child') before=before-intrinsic-array-children ;;
            'MD array child') before=before-intrinsic-MD-array-children ;;
        esac
        key=${section// /-}
        begin="== intrinsic pointer $section identity =="
        end="intrinsic pointer $section identity end"
        run_bounded dotnet "$_CG_APP" "$before" > "$out/intrinsic-$key-before.dotnet.stdout"
        run_bounded "$out/ReflectInvoke$EXE_EXT" "$before" > "$out/intrinsic-$key-before.native.stdout"
        for axis in dotnet native; do
            sed "/^$begin/,\$d" "$out/generic-method-definitions.$axis.stdout" > "$out/intrinsic-$key-prefix.$axis.stdout"
            diff -u <(strip_cr_win_file "$out/intrinsic-$key-before.$axis.stdout") \
                <(strip_cr_win_file "$out/intrinsic-$key-prefix.$axis.stdout") \
                || { echo "FAIL: pointer structure prefix changed: $axis/$section" >&2; return 1; }
            for line in "$begin" "$end" \
                "call pointer $section soft => True/4660/4660/14/0/True" \
                "call pointer $section hard => True/4660/4660/14/0/True"; do
                test "$(grep -Fxc -- "$line" "$out/generic-method-definitions.$axis.stdout")" = 1 \
                    || { echo "FAIL: pointer structure witness must run once: $axis/$line" >&2; return 1; }
            done
        done
    done

    # Matching dependent signatures still lack an invokable substituted body.
    run_bounded dotnet "$_CG_APP" delegate-signature-boundary-outcomes > "$out/delegate-signature-boundary.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" delegate-signature-boundary-outcomes > "$out/delegate-signature-boundary.native.stdout"
    for axis in dotnet native; do
        boundary=$(strip_cr_win_file "$out/delegate-signature-boundary.$axis.stdout")
        for line in '== runtime delegate signature boundaries ==' 'signature template missing => null' \
            'runtime delegate signature boundaries end'; do
            grep -Fxq -- "$line" <<< "$boundary"
        done
        for query in 'return' 'nested return' 'nested argument'; do
            if [ "$axis" = dotnet ]; then
                grep -Fxq -- "signature template dependent $query => null/null" <<< "$boundary"
                grep -Fxq -- "signature template matching $query => bound/bound" <<< "$boundary"
            else
                grep -Fxq -- "signature template dependent $query => unsupported/unsupported" <<< "$boundary"
                grep -Fxq -- "signature template matching $query => unsupported/unsupported" <<< "$boundary"
            fi
        done
        if [ "$axis" = dotnet ]; then
            grep -Fxq -- 'signature opaque return invoke => returned' <<< "$boundary"
            grep -Fxq -- 'signature opaque argument invoke => ArgumentException' <<< "$boundary"
        else
            grep -Fxq -- 'signature opaque return invoke => NotSupportedException' <<< "$boundary"
            grep -Fxq -- 'signature opaque argument invoke => NotSupportedException' <<< "$boundary"
        fi
        for query in 'ref' 'pointer' 'ref return' 'pointer return' 'class ref' 'class return'; do
            if [ "$axis" = dotnet ]; then
                grep -Fxq -- "signature ordinary $query matching => bound/bound" <<< "$boundary"
                grep -Fxq -- "signature ordinary $query mismatch => null/null" <<< "$boundary"
            else
                grep -Fxq -- "signature ordinary $query matching => unsupported/unsupported" <<< "$boundary"
                grep -Fxq -- "signature ordinary $query mismatch => unsupported/unsupported" <<< "$boundary"
            fi
        done
        if [ "$axis" = dotnet ]; then
            grep -Fxq -- 'signature ordinary ref object => null/null' <<< "$boundary"
            grep -Fxq -- 'signature ordinary ref invoke => null' <<< "$boundary"
            grep -Fxq -- 'signature ordinary pointer invoke => returned' <<< "$boundary"
        else
            grep -Fxq -- 'signature ordinary ref object => unsupported/unsupported' <<< "$boundary"
            grep -Fxq -- 'signature ordinary ref invoke => PlatformNotSupportedException' <<< "$boundary"
            grep -Fxq -- 'signature ordinary pointer invoke => PlatformNotSupportedException' <<< "$boundary"
        fi
        for line in '== ordinary runtime delegate signature boundaries ==' \
            'signature ordinary template missing => null' 'ordinary runtime delegate signature boundaries end'; do
            grep -Fxq -- "$line" <<< "$boundary"
        done
    done
    run_bounded dotnet "$_CG_APP" delegate-signature-before-ordinary-boundary > "$out/ordinary-template-boundary-before.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" delegate-signature-before-ordinary-boundary > "$out/ordinary-template-boundary-before.native.stdout"
    for axis in dotnet native; do
        sed '/^== ordinary runtime delegate signature boundaries ==/,$d' "$out/delegate-signature-boundary.$axis.stdout" \
            > "$out/ordinary-template-boundary-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/ordinary-template-boundary-before.$axis.stdout") \
            <(strip_cr_win_file "$out/ordinary-template-boundary-prefix.$axis.stdout")
    done
    run_bounded dotnet "$_CG_APP" delegate-signature-before-overload-boundary > "$out/ordinary-overload-boundary-before.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" delegate-signature-before-overload-boundary > "$out/ordinary-overload-boundary-before.native.stdout"
    for axis in dotnet native; do
        sed '/^== unresolved ordinary overload selection ==/,$d' "$out/delegate-signature-boundary.$axis.stdout" \
            > "$out/ordinary-overload-boundary-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/ordinary-overload-boundary-before.$axis.stdout") \
            <(strip_cr_win_file "$out/ordinary-overload-boundary-prefix.$axis.stdout")
        boundary=$(strip_cr_win_file "$out/delegate-signature-boundary.$axis.stdout")
        for line in '== unresolved ordinary overload selection ==' 'unresolved ordinary overload selection end'; do
            grep -Fxq -- "$line" <<< "$boundary"
        done
        if [ "$axis" = dotnet ]; then
            grep -Fxq -- 'signature overload coincident ref => bound/8/10/bound/8/10' <<< "$boundary"
            grep -Fxq -- 'signature overload coincident composed => bound/48/False/bound/48/False' <<< "$boundary"
            result=bound
        else
            grep -Fxq -- 'signature overload coincident ref => unsupported/unsupported' <<< "$boundary"
            grep -Fxq -- 'signature overload coincident composed => unsupported/unsupported' <<< "$boundary"
            result=unsupported
        fi
        for query in 'coincident reference' 'coincident pointer' 'coincident pointer return' \
            'coincident ref return' 'reordered base' 'coincident constant'; do
            grep -Fxq -- "signature overload $query => $result" <<< "$boundary"
        done
    done

    run_bounded dotnet "$_CG_APP" delegate-signature-before-shape-boundary > "$out/shape-overload-boundary-before.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" delegate-signature-before-shape-boundary > "$out/shape-overload-boundary-before.native.stdout"
    for axis in dotnet native; do
        sed '/^== unresolved structured overload selection ==/,$d' "$out/delegate-signature-boundary.$axis.stdout" \
            > "$out/shape-overload-boundary-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/shape-overload-boundary-before.$axis.stdout") \
            <(strip_cr_win_file "$out/shape-overload-boundary-prefix.$axis.stdout")
        boundary=$(strip_cr_win_file "$out/delegate-signature-boundary.$axis.stdout")
        for line in '== unresolved structured overload selection ==' 'unresolved structured overload selection end'; do
            grep -Fxq -- "$line" <<< "$boundary"
        done
        if [ "$axis" = dotnet ]; then result=bound; else result=unsupported; fi
        for query in 'coincident function' 'coincident deep pointer'; do
            grep -Fxq -- "signature overload $query => $result" <<< "$boundary"
        done
    done

    run_bounded dotnet "$_CG_APP" delegate-signature-before-leaf-boundary > "$out/leaf-overload-boundary-before.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" delegate-signature-before-leaf-boundary > "$out/leaf-overload-boundary-before.native.stdout"
    for axis in dotnet native; do
        sed '/^== unresolved function leaf overload selection ==/,$d' "$out/delegate-signature-boundary.$axis.stdout" \
            > "$out/leaf-overload-boundary-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/leaf-overload-boundary-before.$axis.stdout") \
            <(strip_cr_win_file "$out/leaf-overload-boundary-prefix.$axis.stdout")
        boundary=$(strip_cr_win_file "$out/delegate-signature-boundary.$axis.stdout")
        for line in '== unresolved function leaf overload selection ==' 'unresolved function leaf overload selection end'; do
            grep -Fxq -- "$line" <<< "$boundary"
        done
        if [ "$axis" = dotnet ]; then result=bound; else result=unsupported; fi
        for query in 'coincident array' 'coincident matrix' 'direct array identity'; do
            grep -Fxq -- "signature overload $query => $result" <<< "$boundary"
        done
    done

    run_bounded dotnet "$_CG_APP" delegate-signature-before-identity-boundary > "$out/identity-overload-boundary-before.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" delegate-signature-before-identity-boundary > "$out/identity-overload-boundary-before.native.stdout"
    for axis in dotnet native; do
        sed '/^== unresolved omitted identity overload selection ==/,$d' "$out/delegate-signature-boundary.$axis.stdout" \
            > "$out/identity-overload-boundary-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/identity-overload-boundary-before.$axis.stdout") \
            <(strip_cr_win_file "$out/identity-overload-boundary-prefix.$axis.stdout")
        boundary=$(strip_cr_win_file "$out/delegate-signature-boundary.$axis.stdout")
        for line in '== unresolved omitted identity overload selection ==' 'unresolved omitted identity overload selection end'; do
            grep -Fxq -- "$line" <<< "$boundary"
        done
        if [ "$axis" = dotnet ]; then result=bound; else result=unsupported; fi
        for query in 'coincident generic family'; do
            for mode in soft hard; do
                grep -Fxq -- "signature overload $query $mode => $result" <<< "$boundary"
            done
        done
    done

    run_bounded dotnet "$_CG_APP" delegate-signature-before-runtime-argument-boundary > "$out/runtime-argument-boundary-before.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" delegate-signature-before-runtime-argument-boundary > "$out/runtime-argument-boundary-before.native.stdout"
    for axis in dotnet native; do
        sed '/^== matching runtime generic identity boundaries ==/,$d' "$out/delegate-signature-boundary.$axis.stdout" \
            > "$out/runtime-argument-boundary-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/runtime-argument-boundary-before.$axis.stdout") \
            <(strip_cr_win_file "$out/runtime-argument-boundary-prefix.$axis.stdout")
        boundary=$(strip_cr_win_file "$out/delegate-signature-boundary.$axis.stdout")
        if [ "$axis" = dotnet ]; then result=bound; else result=unsupported; fi
        for line in '== matching runtime generic identity boundaries ==' 'matching runtime generic identity boundaries end' \
            "signature overload generic identity soft => $result" "signature overload generic identity hard => $result"; do
            grep -Fxq -- "$line" <<< "$boundary" \
                || { echo "FAIL: runtime generic identity witness missing: $line" >&2; return 1; }
        done
    done

    for axis in dotnet native; do
        if [ "$axis" = dotnet ]; then
            run_bounded dotnet "$_CG_APP" before-delegate-constructor-names > "$out/constructor-name-before.$axis.stdout"
            run_bounded dotnet "$_CG_APP" > "$out/constructor-name-full.$axis.stdout"
        else
            run_bounded "$out/ReflectInvoke$EXE_EXT" before-delegate-constructor-names > "$out/constructor-name-before.$axis.stdout"
            run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/constructor-name-full.$axis.stdout"
        fi
        sed '/^== delegate constructor body names ==/,$d' "$out/constructor-name-full.$axis.stdout" \
            > "$out/constructor-name-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/constructor-name-before.$axis.stdout") \
            <(strip_cr_win_file "$out/constructor-name-prefix.$axis.stdout") \
            || { echo "FAIL: constructor name prefix changed: $axis" >&2; return 1; }
        for line in '== delegate constructor body names ==' 'delegate constructor body names end' \
            'constructor soft => 5/True/InvalidCastException' \
            'constructor hard => 17/InvalidCastException' \
            'constructor normalized => 5/InvalidCastException' \
            'constructor private base => 29/9/InvalidCastException' \
            'constructor soft mismatches => True/True/True' 'constructor missing => True' \
            'constructor hard mismatch => ArgumentException' 'constructor hard missing => ArgumentException'; do
            test "$(grep -Fxc -- "$line" "$out/constructor-name-full.$axis.stdout")" = 1 \
                || { echo "FAIL: constructor name witness must run once: $axis/$line" >&2; return 1; }
        done
    done

    run_bounded dotnet "$_CG_APP" delegate-name-boundary-outcomes > "$out/delegate-name-boundary.dotnet.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" delegate-name-boundary-outcomes > "$out/delegate-name-boundary.native.stdout"
    for axis in dotnet native; do
        boundary=$(strip_cr_win_file "$out/delegate-name-boundary.$axis.stdout")
        for line in '== delegate name signature boundaries ==' \
            'name boundary unrelated function name => null' \
            'name boundary function argument mismatch => null' \
            'name boundary function return => bound' \
            'name boundary function return mismatch => null' \
            'name boundary MethodInfo function return mismatch => null' \
            'name boundary MethodInfo ref object mismatch => null' \
            'name boundary constructor return mismatch => null' \
            'name boundary constructor argument mismatch => null' \
            'name boundary initializer argument mismatch => null' \
            'name boundary MethodInfo ref value mismatch => null' \
            'name boundary enum underlying => bound' \
            'name boundary MethodInfo enum underlying => bound' \
            'delegate name signature boundaries end'; do
            grep -Fxq -- "$line" <<< "$boundary" \
                || { echo "FAIL: delegate signature boundary witness missing ($axis): $line" >&2; return 1; }
        done
        for line in \
            'own constructor => bound' 'base constructor => bound' 'static initializer => bound' \
            'constructor query normalization => bound' 'initializer query normalization => bound'; do
            grep -Fxq -- "name boundary $line" <<< "$boundary" \
                || { echo "FAIL: constructor signature oracle missing ($axis): $line" >&2; return 1; }
        done
    done

    # Initial intrinsic lookup allocates one native method row; enumeration can
    # allocate inherited Object rows. Each native row includes its binding pointee pointer.
    # Enforce each operation's first and repeated allocation budget independently.
    # The capture reports time too, but timing is not a pass/fail threshold.
    DN2CPP_REFLECTION_MEASURE=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/allocations.csv"
    if ! "$py" gates/measure-reflection-metadata.py --check-allocation-limits \
            gates/expected/reflection-allocations.csv "$out/allocations.csv" \
            > "$out/allocation-check.json"; then
        cat "$out/allocation-check.json" >&2
        return 1
    fi
    echo "reflection allocation budgets OK"
}

# This gate measures C++ member inference from the original assembly metadata.
# Managed preservation is covered by build-and-run-preserve-control.sh.
reflection_layout_axis=default
DN2CPP_STRICT_COMPLETION=1 DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|strict-completion" \
    corelib_diff_gate ReflectInvoke --no-ildiet System.ComponentModel.TypeConverter
typeconverter="$(dirname "$_CG_CORELIB")/System.ComponentModel.TypeConverter.dll"

reflection_layout_axis=overrides
DN2CPP_OUT_SUFFIX="${DN2CPP_OUT_SUFFIX:-}-metadata-overrides" \
    corelib_diff_gate ReflectInvoke --no-ildiet System.ComponentModel.TypeConverter \
        --reflection-metadata 'ReflectInvoke::ReflectMetadataLayoutSubset.NativeBase=packed' \
        --reflection-metadata 'ReflectMetadataLayoutSubset.PackedBase=native' \
        --reflection-metadata 'ReflectMetadataLayoutSubset.Generic`1[System.String]=packed' \
        --reflection-metadata 'ReflectMetadataLayoutSubset.Generic`1[System.Object]=native' \
        --reflection-metadata 'ReflectMetadataCompressionSubset.Direct=packed' \
        --reflection-metadata 'ReflectMetadataCompressionSubset.Generic`1[System.String]=packed' \
        --reflection-metadata 'ReflectMetadataCompressionSubset.CliBase=native' \
        --reflection-metadata 'System.String=native'

reflection_layout_axis=uncompressed
DN2CPP_OUT_SUFFIX="${DN2CPP_OUT_SUFFIX:-}-metadata-uncompressed" \
    corelib_diff_gate ReflectInvoke --no-ildiet --no-metadata-compression System.ComponentModel.TypeConverter \
        --reflection-metadata 'ReflectMetadataLayoutSubset.NativeBase=packed' \
        --reflection-metadata 'ReflectMetadataCompressionSubset.Direct=packed' \
        --reflection-metadata 'System.String=packed'

# A global opt-out dominates packed selectors in either argument order.
uncompressed_reverse=artifacts/reflection-metadata-uncompressed-reverse
run_bounded invoke_cli "$_CG_APP" -r "$_CG_CORELIB" -r "$typeconverter" --no-ildiet \
    --reflection-metadata 'ReflectMetadataLayoutSubset.NativeBase=packed' \
    --reflection-metadata 'ReflectMetadataCompressionSubset.Direct=packed' \
    --reflection-metadata 'System.String=packed' --no-metadata-compression \
    -o "$uncompressed_reverse"
"$py" gates/fixtures/check-reflection-layout.py "$uncompressed_reverse" uncompressed

invalid_out=artifacts/reflection-metadata-invalid
mkdir -p "$invalid_out"
expect_policy_rejection() {
    local name="$1" diagnostic="$2" status=0
    shift 2
    run_bounded invoke_cli "$_CG_APP" -r "$_CG_CORELIB" -r "$typeconverter" --no-ildiet \
        -o "$invalid_out/$name" "$@" > "$invalid_out/$name.log" 2>&1 || status=$?
    if [ "$status" -ne 2 ] || ! grep -Fq -- "$diagnostic" "$invalid_out/$name.log"; then
        cat "$invalid_out/$name.log" >&2
        echo "error: invalid reflection metadata policy $name was not rejected" >&2
        return 1
    fi
}
expect_policy_rejection malformed '--reflection-metadata expects <type>=native|packed' \
    --reflection-metadata 'ReflectMetadataLayoutSubset.NativeBase'
expect_policy_rejection conflicting 'Duplicate --reflection-metadata selector' \
    --reflection-metadata 'ReflectMetadataLayoutSubset.NativeBase=native' \
    --reflection-metadata 'ReflectMetadataLayoutSubset.NativeBase=packed'
expect_policy_rejection runtime-owned "cannot select packed metadata for runtime-owned type 'System.String'" \
    --reflection-metadata 'System.String=packed'

# Each mode rewrites Stored to overwrite its local's Add with Subtract through
# the local's address. .NET binds Subtract; dn2cpp must refuse, never bind Add.
byref_diagnostic='a delegate target loaded from an address-taken local cannot preserve delegate identity'
DN2CPP_BEFORE_LDFTN_LOCAL=1 run_bounded dotnet "$_CG_APP" \
    > "$invalid_out/byref-prefix.stdout"
for byref_mode in overwrite overwrite-int64 copy; do
    byref_dir="$invalid_out/byref-$byref_mode"
    byref_app="$byref_dir/app/ReflectInvoke.dll"
    mkdir -p "$byref_dir/app"
    cp "$_CG_APP" "$byref_app"
    cp "${_CG_APP%.dll}.runtimeconfig.json" "$byref_dir/app/ReflectInvoke.runtimeconfig.json"
    cp "${_CG_APP%.dll}.deps.json" "$byref_dir/app/ReflectInvoke.deps.json"
    cp "$(dirname "$_CG_APP")/Dn2Cpp.Runtime.dll" "$byref_dir/app/Dn2Cpp.Runtime.dll"
    run_bounded dotnet exec "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
        "$byref_app" "--byref-$byref_mode"
    run_bounded dotnet "$byref_app" > "$byref_dir/dotnet.stdout"
    byref_dotnet=$(strip_cr_win_file "$byref_dir/dotnet.stdout")
    grep -Fxq 'ldftn-local-direct=2/Subtract' <<< "$byref_dotnet"
    sed '/^ldftn-local-begin/,$d' "$byref_dir/dotnet.stdout" > "$byref_dir/dotnet-prefix.stdout"
    diff -u <(strip_cr_win_file "$invalid_out/byref-prefix.stdout") \
        <(strip_cr_win_file "$byref_dir/dotnet-prefix.stdout")
    byref_status=0
    run_bounded invoke_cli "$byref_app" -r "$_CG_CORELIB" -r "$typeconverter" --no-ildiet \
        -o "$byref_dir/out" > "$byref_dir/transpile.log" 2>&1 || byref_status=$?
    if [ "$byref_mode" = copy ]; then
        if [ "$byref_status" -ne 0 ]; then
            cat "$byref_dir/transpile.log" >&2
            echo 'error: a copied address-taken delegate target did not transpile' >&2
            exit 1
        fi
    elif [ "$byref_status" -ne 2 ] \
        || ! grep -Fq "LdftnLocalSubset.Program.Stored: $byref_diagnostic" "$byref_dir/transpile.log"; then
        cat "$byref_dir/transpile.log" >&2
        echo "error: byref-$byref_mode delegate target was not rejected" >&2
        exit 1
    fi
done

# The copy carries its origin only at run time, so construction must throw.
byref_copy="$invalid_out/byref-copy"
compile_console "$byref_copy/out" ReflectInvoke
byref_status=0
run_bounded "$byref_copy/out/ReflectInvoke$EXE_EXT" > "$byref_copy/native.stdout" \
    2> "$byref_copy/native.stderr" || byref_status=$?
if [ "$byref_status" -eq 0 ] \
    || ! grep -Fq "System.NotSupportedException: $byref_diagnostic" "$byref_copy/native.stderr" \
    || grep -q '^ldftn-local-direct=' "$byref_copy/native.stdout"; then
    cat "$byref_copy/native.stderr" >&2
    echo 'error: a copied address-taken delegate target was not refused at construction' >&2
    exit 1
fi
sed '/^ldftn-local-begin/,$d' "$byref_copy/native.stdout" > "$byref_copy/native-prefix.stdout"
diff -u <(strip_cr_win_file "$invalid_out/byref-prefix.stdout") \
    <(strip_cr_win_file "$byref_copy/native-prefix.stdout")

# An argument/field constructor body has no load origin; local and stack joins
# also have a tracked edge, which must succeed before the untracked edge refuses.
origin_diagnostic='a delegate target without a preserved method identity is not supported'
run_bounded dotnet "$_CG_APP" before-delegate-origin-boundaries > "$invalid_out/origin-prefix.stdout"
for origin_mode in argument field array checked-conv arithmetic box call local stack-join byref-argument; do
    origin_dir="$invalid_out/origin-$origin_mode"
    origin_app="$origin_dir/app/ReflectInvoke.dll"
    mkdir -p "$origin_dir/app"
    cp "$_CG_APP" "$origin_app"
    cp "${_CG_APP%.dll}.runtimeconfig.json" "$origin_dir/app/ReflectInvoke.runtimeconfig.json"
    cp "${_CG_APP%.dll}.deps.json" "$origin_dir/app/ReflectInvoke.deps.json"
    cp "$(dirname "$_CG_APP")/Dn2Cpp.Runtime.dll" "$origin_dir/app/Dn2Cpp.Runtime.dll"
    run_bounded dotnet exec "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
        "$origin_app" "--delegate-origin-$origin_mode" > "$origin_dir/fixture.log"
    origin_fixture=$(strip_cr_win_file "$origin_dir/fixture.log")
    grep -Fxq "delegate origin fixture verified: --delegate-origin-$origin_mode" <<< "$origin_fixture"
    run_bounded dotnet "$origin_app" > "$origin_dir/dotnet.stdout"
    origin_dotnet=$(strip_cr_win_file "$origin_dir/dotnet.stdout")
    grep -Fxq 'delegate-origin-first=12/Add/True' <<< "$origin_dotnet"
    origin_second='delegate-origin-second=12/Add/False'
    if [ "$origin_mode" = local ] || [ "$origin_mode" = stack-join ]; then
        origin_second='delegate-origin-second=2/Subtract/True'
    fi
    grep -Fxq "$origin_second" <<< "$origin_dotnet"
    grep -Fxq 'delegate origin boundaries end' <<< "$origin_dotnet"
    sed '/^== delegate origin boundaries ==/,$d' "$origin_dir/dotnet.stdout" > "$origin_dir/dotnet-prefix.stdout"
    diff -u <(strip_cr_win_file "$invalid_out/origin-prefix.stdout") \
        <(strip_cr_win_file "$origin_dir/dotnet-prefix.stdout")
    origin_status=0
    run_bounded invoke_cli "$origin_app" -r "$_CG_CORELIB" -r "$typeconverter" --no-ildiet \
        -o "$origin_dir/out" > "$origin_dir/transpile.log" 2>&1 || origin_status=$?
    if [ "$origin_mode" = local ] || [ "$origin_mode" = stack-join ]; then
        if [ "$origin_status" -ne 0 ]; then
            cat "$origin_dir/transpile.log" >&2
            echo "error: origin-$origin_mode rejected its tracked construction edge" >&2
            exit 1
        fi
        compile_console "$origin_dir/out" ReflectInvoke
        origin_status=0
        run_bounded "$origin_dir/out/ReflectInvoke$EXE_EXT" > "$origin_dir/native.stdout" \
            2> "$origin_dir/native.stderr" || origin_status=$?
        origin_native=$(strip_cr_win_file "$origin_dir/native.stdout")
        if [ "$origin_status" -eq 0 ] \
            || ! grep -Fq "System.NotSupportedException: $origin_diagnostic" "$origin_dir/native.stderr" \
            || ! grep -Fxq 'delegate-origin-first=12/Add/True' <<< "$origin_native" \
            || grep -q '^delegate-origin-second=' <<< "$origin_native"; then
            cat "$origin_dir/native.stderr" >&2
            echo "error: origin-$origin_mode did not refuse its untracked construction edge" >&2
            exit 1
        fi
        sed '/^== delegate origin boundaries ==/,$d' "$origin_dir/native.stdout" > "$origin_dir/native-prefix.stdout"
        diff -u <(strip_cr_win_file "$invalid_out/origin-prefix.stdout") \
            <(strip_cr_win_file "$origin_dir/native-prefix.stdout")
    else
        origin_owner=OriginBoundary
        [ "$origin_mode" = field ] && origin_owner=FromField
        if [ "$origin_mode" = argument ] || [ "$origin_mode" = byref-argument ]; then
            origin_owner=FromArgument
        fi
        if [ "$origin_status" -ne 2 ] \
            || ! grep -Fq "LdftnLocalSubset.Program.$origin_owner: $origin_diagnostic" "$origin_dir/transpile.log"; then
            cat "$origin_dir/transpile.log" >&2
            echo "error: origin-$origin_mode delegate target was not rejected" >&2
            exit 1
        fi
    fi
done

# Exercise representation boundaries that C# metadata cannot express, using
# the production decoder and the same CMake/Ninja path as the parity binary.
codec_out=artifacts/reflection-metadata-codec
mkdir -p "$codec_out"
cp gates/fixtures/reflection-metadata-codec.cpp "$codec_out/generated.cpp"
printf '#pragma once\n' > "$codec_out/generated.h"
compile_console "$codec_out" MetadataCodec
codec_native=$(run_bounded "$codec_out/MetadataCodec$EXE_EXT")
codec_native=$(strip_cr_win "$codec_native")
codec_prefix=$(awk '/^metadata missing invocation diagnostics begin$/ { exit } { print }' <<< "$codec_native")
assert_output "$codec_prefix" \
    "$(printf '%s\n' 'metadata codec boundaries OK' 'metadata invoker refusal fields OK')"
assert_output "$codec_native" "$(printf '%s\n' 'metadata codec boundaries OK' \
    'metadata invoker refusal fields OK' 'metadata missing invocation diagnostics begin' \
    'metadata missing native invoker' 'metadata missing native body' \
    'metadata missing packed invoker' 'metadata missing packed body' \
    'metadata missing invocation diagnostics OK')"

# The full bucket reflects MemberwiseClone over strings; GC probes need a process
# that has not performed that unsupported CoreLib operation.
cache_project=gates/fixtures/delegate-invocation-cache/DelegateInvocationCache.csproj
run_bounded dotnet build "$cache_project" -c "$CONFIG" --nologo -v:q
cache_app=gates/fixtures/delegate-invocation-cache/bin/$CONFIG/net10.0/DelegateInvocationCache.dll
cache_out=artifacts/reflectinvoke-delegate-cache
DN2CPP_STRICT_COMPLETION=1 invoke_cli "$cache_app" -r "$_CG_CORELIB" -o "$cache_out"
compile_console "$cache_out" DelegateInvocationCache
cache_expected=$(run_bounded dotnet "$cache_app")
cache_actual=$(run_bounded "$cache_out/DelegateInvocationCache$EXE_EXT")
cache_actual=$(strip_cr_win "$cache_actual")
assert_output "$cache_actual" "$(strip_cr_win "$cache_expected")"
for cache_line in '== invocation list cache GC ==' 'cache survives GC=True' \
        'span combine=3 True True True' 'empty span null=True' \
        'parallel clone cache=True' 'null span 0=True' \
        'null span 1=NullReferenceException:Object reference not set to an instance of an object.' \
        'null span 2=NullReferenceException:Object reference not set to an instance of an object.' \
        'equals argument evaluated' 'static equals=False/True' \
        'null hash=NullReferenceException:Object reference not set to an instance of an object.' \
        'null equals=NullReferenceException:Object reference not set to an instance of an object.' \
        'delegate cache fixture end'; do
    grep -Fxq -- "$cache_line" <<< "$cache_actual" \
        || { echo "FAIL: delegate cache witness missing: $cache_line" >&2; exit 1; }
done

recursive_project=gates/fixtures/recursive-delegate/RecursiveDelegate.csproj
run_bounded dotnet build "$recursive_project" -c "$CONFIG" --nologo -v:q
recursive_app=gates/fixtures/recursive-delegate/bin/$CONFIG/net10.0/RecursiveDelegate.dll
recursive_expected=$(run_bounded dotnet "$recursive_app")
for recursive_mode in shared-generics no-shared-generics; do
    recursive_out="artifacts/reflectinvoke-recursive-$recursive_mode"
    DN2CPP_STRICT_COMPLETION=1 invoke_cli "$recursive_app" -r "$_CG_CORELIB" \
        "--$recursive_mode" -o "$recursive_out"
    compile_console "$recursive_out" RecursiveDelegate
    recursive_actual=$(run_bounded "$recursive_out/RecursiveDelegate$EXE_EXT")
    assert_output "$(strip_cr_win "$recursive_actual")" "$(strip_cr_win "$recursive_expected")"
    grep -Fxq 'recursive delegate identities=True/True' <<< "$(strip_cr_win "$recursive_actual")" \
        || { echo 'FAIL: recursive delegate declaration witness missing' >&2; exit 1; }
done
# FieldInfo validation, ordinary visibility/binding guards, sealed interface own
# bodies, ByRefLike refusal and uncapped member lookups use an isolated driver.
unset -f gate_extra_asserts
source gates/_ordinary-reflection.sh
gate_extra_asserts() {
    local out="$1" native line before prefix modifiers_before modifiers_prefix
    native=$(run_bounded "$out/OrdinaryReflectionLeaves$EXE_EXT") || return $?
    native=$(strip_cr_win "$native")
    before=$(DN2CPP_BEFORE_RUNTIME_MEMBER_ATTRIBUTES=1 run_bounded dotnet "$_CG_APP") || return $?
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== runtime member attributes ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    modifiers_before=$(DN2CPP_BEFORE_RUNTIME_RETURN_MODIFIERS=1 run_bounded dotnet "$_CG_APP") || return $?
    modifiers_before=$(strip_cr_win "$modifiers_before")
    modifiers_prefix=$(awk '/^== runtime return modifiers ==$/ { exit } { print }' <<< "$native")
    assert_output "$modifiers_prefix" "$modifiers_before"
    before=$(DN2CPP_BEFORE_POINTER_FIELDS=1 run_bounded dotnet "$_CG_APP") || return $?
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== reflected pointer fields ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    before=$(run_bounded dotnet "$_CG_APP" before-object-method-enumeration) || return $?
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== Object family method enumeration ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    before=$(run_bounded dotnet "$_CG_APP" before-property-accessors) || return $?
    prefix=$(awk '/^== property accessor arrays ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$(strip_cr_win "$before")"
    for line in '== property accessor arrays ==' \
        'accessors AccessorCell/Value default=get_Value;false=get_Value;true=get_Value,set_Value;identity=True;declared=AccessorCell;reflected=AccessorCell' \
        'accessors AccessorCell/Hidden default=;false=;true=get_Hidden,set_Hidden;identity=True;declared=AccessorCell;reflected=AccessorCell' \
        'accessors AccessorCell/Reverse default=get_Reverse,set_Reverse;false=get_Reverse,set_Reverse;true=get_Reverse,set_Reverse;identity=True;declared=AccessorCell;reflected=AccessorCell' \
        'accessors AccessorCell/PrivateInherited default=get_PrivateInherited;false=get_PrivateInherited;true=get_PrivateInherited;identity=True;declared=AccessorBase;reflected=AccessorCell' \
        'accessors AccessorCell/WriteInherited default=set_WriteInherited;false=set_WriteInherited;true=set_WriteInherited;identity=True;declared=AccessorBase;reflected=AccessorCell' \
        'accessors AccessorBase/PrivateInherited default=get_PrivateInherited;false=get_PrivateInherited;true=get_PrivateInherited,set_PrivateInherited;identity=True;declared=AccessorBase;reflected=AccessorBase' \
        'accessors AccessorBase/WriteInherited default=set_WriteInherited;false=set_WriteInherited;true=get_WriteInherited,set_WriteInherited;identity=True;declared=AccessorBase;reflected=AccessorBase' \
        'accessors invoke value=15' 'accessors invoke static=23' \
        'accessors invoke indexed=21' 'accessors invoke inherited=17' \
        'accessors null default: NullReferenceException/<null>' \
        'accessors null true: NullReferenceException/<null>' 'property accessor arrays end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: property accessor witness missing: $line" >&2; return 1; }
    done
    for line in '== Object family method enumeration ==' \
        'method order Object/20=GetType/0|ToString/0|Equals/1|GetHashCode/0' \
        'method order Object/28=GetType/0|ToString/0|Equals/1|Equals/2|ReferenceEquals/2|GetHashCode/0' \
        'method order Object/60=GetType/0|MemberwiseClone/0|Finalize/0|ToString/0|Equals/1|Equals/2|ReferenceEquals/2|GetHashCode/0' \
        'method order ObjectLeaf/20=Local/0|GetType/0|ToString/0|Equals/1|GetHashCode/0' \
        'method order VirtualFactory/20=MakeVirtual/1|GetType/0|ToString/0|Equals/1|GetHashCode/0' \
        'methods Object/20=4:same=True' 'methods Object/28=6:same=True' 'methods Object/60=8:same=True' \
        'methods Object/62=8:same=True' 'methods ObjectLeaf/20=5:same=True' \
        'methods ObjectLeaf/28=5:same=True' 'methods ObjectLeaf/60=7:same=True' 'methods ObjectLeaf/124=9:same=True' \
        'methods ObjectLeaf/62=1:same=True' 'methods ObjectLeaf/88=2:same=True' \
        'methods ObjectLeaf/36=2:same=True' 'methods ObjectLeaf/0=0:same=True' \
        'methods ObjectOverride/20=5:same=True' 'methods ObjectNewSlot/20=6:same=True' \
        'methods ObjectNewPlain/20=6:same=True' 'methods ObjectOverload/20=6:same=True' \
        'methods VirtualFactory/20=5:same=True' 'methods VirtualFactory/62=1:same=True' \
        'methods VirtualFactory/124=9:same=True' 'methods ObjectValue/20=4:same=True' \
        'method order ValueType/20=Equals/1|GetHashCode/0|ToString/0|GetType/0' \
        'method order ObjectPlainValue/20=Equals/1|GetHashCode/0|ToString/0|GetType/0' \
        'methods ValueType/20=4:same=True' 'methods ObjectPlainValue/20=4:same=True' \
        'methods ObjectValue/62=1:same=True' 'enumerated invocation=True:True' \
        'Object family method enumeration end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: Object family enumeration witness missing: $line" >&2; return 1; }
    done
    for line in '== reflected pointer fields ==' \
        'Int pointer get: String:System.Reflection.Pointer:120' \
        'Int pointer set null: String:System.Reflection.Pointer:0/storage:0' \
        'Int pointer set IntPtr: String:System.Reflection.Pointer:340/storage:340' \
        'Int pointer set uint*: String:System.Reflection.Pointer:120/storage:120' \
        'Void pointer set UIntPtr: String:System.Reflection.Pointer:340/storage:340' \
        'Twice pointer set int**: String:System.Reflection.Pointer:120/storage:120' \
        'Point pointer set struct*: String:System.Reflection.Pointer:120/storage:120' \
        'Function pointer get: String:System.IntPtr:120' \
        'SharedInt pointer set IntPtr: String:System.Reflection.Pointer:340/storage:340' \
        'SharedVoid pointer set UIntPtr: String:System.Reflection.Pointer:340/storage:340' \
        'SharedTwice pointer set int**: String:System.Reflection.Pointer:120/storage:120' \
        'SharedPoint pointer set struct*: String:System.Reflection.Pointer:120/storage:120' \
        'SharedFunction pointer set IntPtr: String:System.IntPtr:340/storage:340' \
        'readonly ordinary get: Int32:41' 'reflected pointer fields end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: pointer field witness missing: $line" >&2; return 1; }
    done
    local layout=record
    [[ "$out" = *-native ]] && layout=native
    grep -wq "md_${layout}_fldtab_ReflectFieldValidationSubset_PointerFields" "$out"/generated*.cpp \
        || { echo "FAIL: pointer fields lack $layout metadata" >&2; return 1; }
    for line in '== field validation ==' 'field validation end' \
        '== ordinary ambiguous messages ==' 'ordinary ambiguous messages end' \
        'sealed direct=CUSTOM-HELLO' 'sealed bound=CUSTOM-HELLO/IGreeting.Shout' \
        'bind ordinary=42' 'bind generic=12' 'bind boxed=7' \
        'bind first object=7' 'bind first interface=7' \
        'SizeOf attributes=0096:True:0100' \
        'static abstract invoke: TargetInvocationException/BadImageFormatException' \
        'static abstract unwrapped: BadImageFormatException/<null>' \
        'static interface bound Abs: EntryPointNotFoundException/<null>' \
        'static interface bound Virt: EntryPointNotFoundException/<null>' \
        'wide slots=1/0' 'wide get member=1' 'wide get method=SlotWide' \
        'wide properties=270/0' 'wide constructor=A39' \
        '== constructor binder faults ==' 'constructor matched=17' \
        'constructor wrong count inner=<null>' 'constructor binder faults end' \
        'ordinary reflection leaves end' '== runtime member attributes ==' \
        'memberwise clone attributes=0085/False/True' 'runtime member attributes end' \
        '== runtime return modifiers ==' 'return modifiers MemberwiseClone=0/-1:0/0' \
        'return modifiers SizeOf definition=0/-1:0/0' 'return modifiers SizeOf Int32=0/-1:0/0' \
        'return modifiers SizeOf String=0/-1:0/0' \
        'runtime clone method display=System.Object MemberwiseClone()' \
        'runtime clone return display=System.Object' 'runtime SizeOf return display=Int32' \
        'runtime return modifiers end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: ordinary reflection witness missing: $line" >&2; return 1; }
    done
}
build_gate_proj samples/dotnet/ReflectReturnLib/ReflectReturnLib.csproj
object_methods_library="$PWD/samples/dotnet/ReflectReturnLib/bin/$CONFIG/$TFM/ReflectReturnLib.dll"
DN2CPP_STRICT_COMPLETION=1 ordinary_fixture_diff_gate ReflectInvoke OrdinaryReflectionLeaves --no-ildiet \
    -r "$object_methods_library" --link-xml samples/dotnet/ReflectInvoke/keep-object-methods.xml \
    --reflection-metadata ReflectReturnLib.VirtualFactory=packed \
    --reflection-metadata ReflectFieldValidationSubset.PointerFields=packed
DN2CPP_OUT_SUFFIX=-native DN2CPP_STRICT_COMPLETION=1 \
    ordinary_fixture_diff_gate ReflectInvoke OrdinaryReflectionLeaves --no-ildiet --no-metadata-compression \
        -r "$object_methods_library" --link-xml samples/dotnet/ReflectInvoke/keep-object-methods.xml
unset -f gate_extra_asserts
# These drivers have no application pointer field or pointer-returning method.
# One reads an allocated library owner's inherited field; the other names only a
# preserved static field through Type.GetType, so the two rooting paths stand alone.
build_gate_proj samples/dotnet/ReflectReturnLib/ReflectReturnLib.csproj
gate_extra_asserts() {
    local out="$1" native line owner layout=record
    native=$(run_bounded "$out/$(basename "$_CG_APP" .dll)$EXE_EXT") || return $?
    native=$(strip_cr_win "$native")
    local lines=()
    case "$_CG_APP" in
        */ReflectPointerFieldsPreserved.dll)
            owner=ReflectReturnLib_PreservedPointerField
            lines=('preserved pointer=System.Reflection.Pointer:560' 'preserved stored=System.Reflection.Pointer:780') ;;
        *)
            owner=ReflectReturnLib_PointerFieldBase
            lines=('library inherited=System.Reflection.Pointer:120' 'library static=System.Reflection.Pointer:340' \
                'library stored=System.Reflection.Pointer:780') ;;
    esac
    lines+=('pointer fields only end')
    for line in "${lines[@]}"; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: isolated pointer field witness missing: $line" >&2; return 1; }
    done
    [[ "$out" = *-native ]] && layout=native
    grep -wq "md_${layout}_fldtab_${owner}" "$out"/generated*.cpp \
        || { echo "FAIL: isolated pointer field lacks $layout metadata" >&2; return 1; }
}
pointer_field_library="$PWD/samples/dotnet/ReflectReturnLib/bin/$CONFIG/$TFM/ReflectReturnLib.dll"
DN2CPP_STRICT_COMPLETION=1 ordinary_fixture_diff_gate ReflectInvoke ReflectPointerFieldsOnly --no-ildiet -r "$pointer_field_library" \
    --reflection-metadata ReflectReturnLib.PointerFieldBase=packed
DN2CPP_OUT_SUFFIX=-native DN2CPP_STRICT_COMPLETION=1 ordinary_fixture_diff_gate ReflectInvoke ReflectPointerFieldsOnly --no-ildiet \
    -r "$pointer_field_library" --no-metadata-compression
DN2CPP_STRICT_COMPLETION=1 ordinary_fixture_diff_gate ReflectInvoke ReflectPointerFieldsPreserved --no-ildiet -r "$pointer_field_library" \
    --link-xml samples/dotnet/ReflectInvoke/keep-pointer-field.xml \
    --reflection-metadata ReflectReturnLib.PreservedPointerField=packed
DN2CPP_OUT_SUFFIX=-native DN2CPP_STRICT_COMPLETION=1 ordinary_fixture_diff_gate ReflectInvoke ReflectPointerFieldsPreserved --no-ildiet \
    -r "$pointer_field_library" --link-xml samples/dotnet/ReflectInvoke/keep-pointer-field.xml --no-metadata-compression
unset -f gate_extra_asserts
gate_extra_asserts() {
    local out="$1" native line
    native=$(run_bounded "$out/ReflectionMethodGroupsOnly$EXE_EXT") || return $?
    native=$(strip_cr_win "$native")
    for line in 'activator: made' 'constructor: built' 'method: hello group' \
        'property get: label' 'property set: glad' 'static create delegate: hello static' \
        'create delegate: hello bind' 'make generic: box:String' 'attributes: TagAttribute'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: method-group reflection witness missing: $line" >&2; return 1; }
    done
}
DN2CPP_STRICT_COMPLETION=1 ordinary_fixture_diff_gate ReflectInvoke ReflectionMethodGroupsOnly --no-ildiet
unset -f gate_extra_asserts
DN2CPP_OUT_SUFFIX=-ildiet DN2CPP_STRICT_COMPLETION=1 \
    ordinary_fixture_diff_gate ReflectInvoke ReflectionMethodGroupsOnly

# Other drivers construct reflected application types, which independently roots
# the attribute constructor this abstract-slot signature must discover.
gate_extra_asserts() {
    local out="$1" axis line output
    run_bounded "$out/ReflectAttributeDiscoveryOnly$EXE_EXT" > "$out/attribute-discovery.native.stdout"
    run_bounded dotnet "$_CG_APP" > "$out/attribute-discovery.dotnet.stdout"
    run_bounded "$out/ReflectAttributeDiscoveryOnly$EXE_EXT" before-attribute-discovery \
        > "$out/attribute-discovery-before.native.stdout"
    run_bounded dotnet "$_CG_APP" before-attribute-discovery \
        > "$out/attribute-discovery-before.dotnet.stdout"
    for axis in native dotnet; do
        output=$(strip_cr_win_file "$out/attribute-discovery.$axis.stdout")
        sed '/^== reflection route final-pass attributes ==/,$d' "$out/attribute-discovery.$axis.stdout" \
            > "$out/attribute-discovery-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/attribute-discovery-before.$axis.stdout") \
            <(strip_cr_win_file "$out/attribute-discovery-prefix.$axis.stdout")
        for line in 'reflection discovery baseline=23' '== reflection route final-pass attributes ==' \
            'discovery return rows=1' 'discovery return value=17' \
            'reflection route final-pass attributes end'; do
            [ "$(grep -Fxc -- "$line" <<< "$output")" = 1 ] \
                || { echo "FAIL: attribute-discovery witness must run once ($axis): $line" >&2; return 1; }
        done
    done
}
DN2CPP_STRICT_COMPLETION=1 ordinary_fixture_diff_gate ReflectInvoke ReflectAttributeDiscoveryOnly --no-ildiet
DN2CPP_OUT_SUFFIX=-ildiet DN2CPP_STRICT_COMPLETION=1 \
    ordinary_fixture_diff_gate ReflectInvoke ReflectAttributeDiscoveryOnly
unset -f gate_extra_asserts

# Delegate binding as a program's only reflection call needs an isolated driver.
gate_extra_asserts() {
    local out="$1" native line
    native=$(run_bounded "$out/ReflectBindOnly$EXE_EXT") || return $?
    native=$(strip_cr_win "$native")
    for line in 'static: 42' 'generic: 12' 'instance: hello bind' 'override: circle' \
        'interface static: unit' 'bind-only end' 'contravariant: null' 'type return: Circle' \
        'variance view: null' 'signature types end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: bind-only witness missing: $line" >&2; return 1; }
    done
}
ordinary_fixture_diff_gate ReflectInvoke ReflectBindOnly --no-ildiet
unset -f gate_extra_asserts

gate_extra_asserts() {
    local out="$1" native line axis result boundary
    native=$(run_bounded "$out/ReflectNameBindOnly$EXE_EXT") || return $?
    native=$(strip_cr_win "$native")
    for line in '== delegate names as the only reflection entry ==' \
        'instance names: 42/42/42' 'static names: 42/42/42' 'delegate names only end' \
        '== intrinsic identity overload selection ==' 'intrinsic argument => 181/181' \
        'intrinsic leaf => 191/191/191/191' 'intrinsic identity overload selection end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: isolated delegate name witness missing: $line" >&2; return 1; }
    done
    for axis in dotnet native; do
        if [ "$axis" = dotnet ]; then
            run_bounded dotnet "$_CG_APP" before-delegate-initializer-names > "$out/initializer-name-before.$axis.stdout"
            run_bounded dotnet "$_CG_APP" > "$out/initializer-name-full.$axis.stdout"
        else
            run_bounded "$out/ReflectNameBindOnly$EXE_EXT" before-delegate-initializer-names > "$out/initializer-name-before.$axis.stdout"
            run_bounded "$out/ReflectNameBindOnly$EXE_EXT" > "$out/initializer-name-full.$axis.stdout"
        fi
        sed '/^== delegate cold initializer body names ==/,$d' "$out/initializer-name-full.$axis.stdout" \
            > "$out/initializer-name-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/initializer-name-before.$axis.stdout") \
            <(strip_cr_win_file "$out/initializer-name-prefix.$axis.stdout") \
            || { echo "FAIL: initializer name prefix changed: $axis" >&2; return 1; }
        for line in '== delegate cold initializer body names ==' 'delegate cold initializer body names end' \
            'cold initializer before => 0' 'cold initializer bind => 0/True/InvalidCastException' \
            'cold initializer soft call => 1' 'cold initializer hard call => 2/InvalidCastException' \
            'helper initializer bind => 0' 'helper initializer first => 2' \
            'helper initializer second => 3/InvalidCastException' \
            'inherited initializer bind => 0' 'inherited initializer calls => 2/InvalidCastException' \
            'generic initializer bind => 0' 'generic initializer calls => 2/String/InvalidCastException' \
            'initializer mismatch => True/True' 'initializer absent => True' \
            'initializer absent hard => ArgumentException'; do
            test "$(grep -Fxc -- "$line" "$out/initializer-name-full.$axis.stdout")" = 1 \
                || { echo "FAIL: initializer name witness must run once: $axis/$line" >&2; return 1; }
        done
    done

    for axis in dotnet native; do
        if [ "$axis" = dotnet ]; then
            run_bounded dotnet "$_CG_APP" before-app-library-initializer-names > "$out/app-library-initializer-before.$axis.stdout"
            run_bounded dotnet "$_CG_APP" > "$out/app-library-initializer-full.$axis.stdout"
        else
            run_bounded "$out/ReflectNameBindOnly$EXE_EXT" before-app-library-initializer-names > "$out/app-library-initializer-before.$axis.stdout"
            run_bounded "$out/ReflectNameBindOnly$EXE_EXT" > "$out/app-library-initializer-full.$axis.stdout"
        fi
        sed '/^== application library initializer names ==/,$d' "$out/app-library-initializer-full.$axis.stdout" \
            > "$out/app-library-initializer-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/app-library-initializer-before.$axis.stdout") \
            <(strip_cr_win_file "$out/app-library-initializer-prefix.$axis.stdout") \
            || { echo "FAIL: application library initializer prefix changed: $axis" >&2; return 1; }
        for line in '== application library initializer names ==' 'application library initializer names end' \
            'app library initializer before => 0' \
            'app library initializer bind => 0/True/True/InvalidCastException' \
            'app library initializer soft call => 1' \
            'app library initializer hard call => 2/InvalidCastException'; do
            test "$(grep -Fxc -- "$line" "$out/app-library-initializer-full.$axis.stdout")" = 1 \
                || { echo "FAIL: application library initializer witness must run once: $axis/$line" >&2; return 1; }
        done
    done

    for axis in dotnet native; do
        if [ "$axis" = dotnet ]; then
            run_bounded dotnet "$_CG_APP" before-intrinsic-array-elements > "$out/intrinsic-array-element-before.$axis.stdout"
            run_bounded dotnet "$_CG_APP" > "$out/intrinsic-array-element-full.$axis.stdout"
        else
            run_bounded "$out/ReflectNameBindOnly$EXE_EXT" before-intrinsic-array-elements > "$out/intrinsic-array-element-before.$axis.stdout"
            run_bounded "$out/ReflectNameBindOnly$EXE_EXT" > "$out/intrinsic-array-element-full.$axis.stdout"
        fi
        sed '/^== intrinsic pointer array element identity ==/,$d' "$out/intrinsic-array-element-full.$axis.stdout" \
            > "$out/intrinsic-array-element-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/intrinsic-array-element-before.$axis.stdout") \
            <(strip_cr_win_file "$out/intrinsic-array-element-prefix.$axis.stdout") \
            || { echo "FAIL: array element prefix changed: $axis" >&2; return 1; }
        for line in '== intrinsic pointer array element identity ==' 'intrinsic pointer array element identity end' \
            'pointer Token array => 1/True' \
            'call pointer array element soft => True/4660/4660/14/0/True' \
            'call pointer array element hard => True/4660/4660/14/0/True'; do
            test "$(grep -Fxc -- "$line" "$out/intrinsic-array-element-full.$axis.stdout")" = 1 \
                || { echo "FAIL: array element witness must run once: $axis/$line" >&2; return 1; }
        done
    done

    local section before
    for section in 'array leaf' 'constant leaf'; do
        if [ "$section" = 'array leaf' ]; then
            before=before-intrinsic-array-leaves
        else
            before=before-intrinsic-constant-leaves
        fi
        for axis in dotnet native; do
            if [ "$axis" = dotnet ]; then
                run_bounded dotnet "$_CG_APP" "$before" > "$out/intrinsic-$section-before.$axis.stdout"
                run_bounded dotnet "$_CG_APP" > "$out/intrinsic-$section-full.$axis.stdout"
            else
                run_bounded "$out/ReflectNameBindOnly$EXE_EXT" "$before" > "$out/intrinsic-$section-before.$axis.stdout"
                run_bounded "$out/ReflectNameBindOnly$EXE_EXT" > "$out/intrinsic-$section-full.$axis.stdout"
            fi
            sed '/^== intrinsic pointer '"$section"' identity ==/,$d' "$out/intrinsic-$section-full.$axis.stdout" \
                > "$out/intrinsic-$section-prefix.$axis.stdout"
            diff -u <(strip_cr_win_file "$out/intrinsic-$section-before.$axis.stdout") \
                <(strip_cr_win_file "$out/intrinsic-$section-prefix.$axis.stdout") \
                || { echo "FAIL: pointer leaf prefix changed: $axis/$section" >&2; return 1; }
            for line in "== intrinsic pointer $section identity ==" "intrinsic pointer $section identity end" \
                "call pointer $section soft => True/4660/4660/14/0/True" \
                "call pointer $section hard => True/4660/4660/14/0/True"; do
                test "$(grep -Fxc -- "$line" "$out/intrinsic-$section-full.$axis.stdout")" = 1 \
                    || { echo "FAIL: pointer leaf witness must run once: $axis/$line" >&2; return 1; }
            done
        done
    done

    for axis in dotnet native; do
        if [ "$axis" = dotnet ]; then
            run_bounded dotnet "$_CG_APP" before-intrinsic-closed-generic-arguments > "$out/intrinsic-closed-generic-before.$axis.stdout"
            run_bounded dotnet "$_CG_APP" > "$out/intrinsic-closed-generic-full.$axis.stdout"
        else
            run_bounded "$out/ReflectNameBindOnly$EXE_EXT" before-intrinsic-closed-generic-arguments > "$out/intrinsic-closed-generic-before.$axis.stdout"
            run_bounded "$out/ReflectNameBindOnly$EXE_EXT" > "$out/intrinsic-closed-generic-full.$axis.stdout"
        fi
        sed '/^== intrinsic closed generic pointer argument ==/,$d' "$out/intrinsic-closed-generic-full.$axis.stdout" \
            > "$out/intrinsic-closed-generic-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/intrinsic-closed-generic-before.$axis.stdout") \
            <(strip_cr_win_file "$out/intrinsic-closed-generic-prefix.$axis.stdout") \
            || { echo "FAIL: closed generic pointer prefix changed: $axis" >&2; return 1; }
        for line in '== intrinsic closed generic pointer argument ==' 'intrinsic closed generic pointer argument end' \
            'call pointer closed generic soft => True/4660/4660/14/0/True' \
            'call pointer closed generic hard => True/4660/4660/14/0/True'; do
            test "$(grep -Fxc -- "$line" "$out/intrinsic-closed-generic-full.$axis.stdout")" = 1 \
                || { echo "FAIL: closed generic pointer witness must run once: $axis/$line" >&2; return 1; }
        done
    done

    for axis in dotnet native; do
        if [ "$axis" = dotnet ]; then
            run_bounded dotnet "$_CG_APP" before-intrinsic-overloads > "$out/intrinsic-before.$axis.stdout"
            run_bounded dotnet "$_CG_APP" > "$out/intrinsic-full.$axis.stdout"
            run_bounded dotnet "$_CG_APP" intrinsic-boundary > "$out/intrinsic-boundary.$axis.stdout"
            result=bound
        else
            run_bounded "$out/ReflectNameBindOnly$EXE_EXT" before-intrinsic-overloads > "$out/intrinsic-before.$axis.stdout"
            run_bounded "$out/ReflectNameBindOnly$EXE_EXT" > "$out/intrinsic-full.$axis.stdout"
            run_bounded "$out/ReflectNameBindOnly$EXE_EXT" intrinsic-boundary > "$out/intrinsic-boundary.$axis.stdout"
            result=unsupported
        fi
        sed '/^== intrinsic identity overload selection ==/,$d' "$out/intrinsic-full.$axis.stdout" \
            > "$out/intrinsic-prefix.$axis.stdout"
        diff -u <(strip_cr_win_file "$out/intrinsic-before.$axis.stdout") \
            <(strip_cr_win_file "$out/intrinsic-prefix.$axis.stdout")
        boundary=$(strip_cr_win_file "$out/intrinsic-boundary.$axis.stdout")
        for line in '== matching intrinsic overload boundary ==' 'matching intrinsic overload boundary end' \
            "intrinsic coincident soft => $result" "intrinsic coincident hard => $result"; do
            grep -Fxq -- "$line" <<< "$boundary" \
                || { echo "FAIL: isolated intrinsic matching witness missing: $line" >&2; return 1; }
        done
    done
}
DN2CPP_STRICT_COMPLETION=1 ordinary_fixture_diff_gate ReflectInvoke ReflectNameBindOnly --no-ildiet -r "$pointer_field_library"
DN2CPP_OUT_SUFFIX=-ildiet DN2CPP_STRICT_COMPLETION=1 \
    ordinary_fixture_diff_gate ReflectInvoke ReflectNameBindOnly -r "$pointer_field_library"
unset -f gate_extra_asserts

# The refusals need their CoreLib overrides stripped, so they run in an image
# that holds nothing else, over packed and native metadata.
gate_extra_asserts() {
    local out="$1" line native
    local refusal="the receiver's body was stripped from this image; preserve it with a link.xml descriptor to reach it through reflection"
    DN2CPP_STRIPPED_OVERRIDES=1 run_bounded "$out/StrippedOverrideRefusals$EXE_EXT" > "$out/stripped-overrides.stdout"
    native=$(strip_cr_win_file "$out/stripped-overrides.stdout")
    for line in \
        "stripped struct-returning slot: NotSupportedException 0x80131515 System.Globalization.GregorianCalendar.AddYears: $refusal" \
        "stripped slot: NotSupportedException 0x80131515 System.Globalization.GregorianCalendar.GetDayOfMonth: $refusal" \
        "stripped interface struct-returning slot: NotSupportedException 0x80131515 System.DBNull.ToDateTime: $refusal" \
        "stripped interface slot: NotSupportedException 0x80131515 System.DBNull.ToInt32: $refusal" \
        "stripped slot, delegate: NotSupportedException 0x80131515 System.Globalization.GregorianCalendar.GetDayOfMonth: $refusal" \
        'stripped end' \
        "stripped slot, nested invoke: TargetInvocationException 0x80131604 inner NotSupportedException 0x80131515 System.Globalization.GregorianCalendar.GetDayOfMonth: $refusal" \
        "stripped slot, nested invoke unwrapped: NotSupportedException 0x80131515 System.Globalization.GregorianCalendar.GetDayOfMonth: $refusal" \
        "stripped slot, nested object virtual: TargetInvocationException 0x80131604 inner NotSupportedException 0x80131515 System.Globalization.GregorianCalendar.GetDayOfMonth: $refusal" \
        "stripped slot, nested constructor: TargetInvocationException 0x80131604 inner NotSupportedException 0x80131515 System.Globalization.GregorianCalendar.GetDayOfMonth: $refusal" \
        'stripped nested end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: stripped dispatch witness missing: $line" >&2; return 1; }
    done
}
DN2CPP_STRICT_COMPLETION=1 ordinary_fixture_diff_gate ReflectInvoke StrippedOverrideRefusals --no-ildiet
DN2CPP_OUT_SUFFIX=-native DN2CPP_STRICT_COMPLETION=1 \
    ordinary_fixture_diff_gate ReflectInvoke StrippedOverrideRefusals --no-ildiet --no-metadata-compression
unset -f gate_extra_asserts

gate_extra_asserts() {
    local out="$1" native identity
    run_bounded "$out/ReflectFrameworkBind$EXE_EXT" > "$out/library-struct-return.stdout"
    identity=$(strip_cr_win_file "$out/library-struct-return.stdout")
    grep -Fxq '== direct library struct return ==' <<< "$identity"
    grep -Fxq 'direct library struct return end' <<< "$identity"
    DN2CPP_BEFORE_LIBRARY_STRUCT_RETURN=1 run_bounded "$out/ReflectFrameworkBind$EXE_EXT" \
        > "$out/before-library-struct-return.stdout"
    sed '/^== direct library struct return ==/,$d' "$out/library-struct-return.stdout" \
        > "$out/library-struct-return-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/library-struct-return-prefix.stdout") \
        <(strip_cr_win_file "$out/before-library-struct-return.stdout")
    local refused="ArgumentException 0x80070057 Object of type 'System.Reflection.Pointer' cannot be converted to type" line
    for line in '== function pointer identity ==' 'local leaf cell, local sink: 16' 'peer leaf cell, peer sink: 272' \
        "local leaf cell, peer sink: $refused 'FunctionPointerTwin.Leaf()*'." \
        "peer leaf cell, local sink: $refused 'FunctionPointerTwin.Leaf()*'." \
        "local node cell, peer sink: $refused 'FunctionPointerTwin.Node()*'." \
        "local list cell, peer sink: $refused 'System.Collections.Generic.List\`1[FunctionPointerTwin.Node]()*'." \
        "local nested cell, peer sink: $refused 'System.Void(FunctionPointerTwin.Leaf())*'." \
        'function pointer identity end'; do
        grep -Fxq -- "$line" <<< "$identity" \
            || { echo "FAIL: function pointer identity witness missing: $line" >&2; return 1; }
    done
    DN2CPP_BEFORE_FUNCTION_POINTER_IDENTITY=1 run_bounded "$out/ReflectFrameworkBind$EXE_EXT" \
        > "$out/before-function-pointer-identity.stdout"
    sed '/^== function pointer identity ==/,$d' "$out/library-struct-return.stdout" \
        > "$out/function-pointer-identity-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/function-pointer-identity-prefix.stdout") \
        <(strip_cr_win_file "$out/before-function-pointer-identity.stdout")
    # The reached sibling pins the symbol spelling the absence check below relies on.
    if ! grep -hEq 'LibraryResult_Label_m[0-9]+\(' "$out"/generated*.cpp "$out/generated.h"; then
        echo 'FAIL: reached library result body missing' >&2
        return 1
    fi
    if grep -hEq '(DeadResult|UnusedResult)_Label_m[0-9]+\(' "$out"/generated*.cpp "$out/generated.h"; then
        echo 'FAIL: unrelated library result body was reached' >&2
        return 1
    fi
    DN2CPP_STRIPPED_OVERRIDES=1 run_bounded "$out/ReflectFrameworkBind$EXE_EXT" > "$out/stripped-overrides.stdout"
    native=$(strip_cr_win_file "$out/stripped-overrides.stdout")
    grep -Fxq "framework generic virtual row, library override: NotSupportedException 0x80131515 ReflectFrameworkBindLib.LibraryProvider.RegisterType: the receiver's body was stripped from this image; preserve it with a link.xml descriptor to reach it through reflection" <<< "$native"
    grep -Fxq 'framework bind end' <<< "$native"
    grep -Fxq "framework generic virtual row, library receiver allocated late: NotSupportedException 0x80131515 ReflectFrameworkBindLib.LateProvider.RegisterType: the receiver's body was stripped from this image; preserve it with a link.xml descriptor to reach it through reflection" <<< "$native"
}
corelib_diff_gate ReflectFrameworkBind --no-ildiet System.ComponentModel.TypeConverter \
    -r "samples/dotnet/ReflectFrameworkBind/bin/$CONFIG/$TFM/System.ReflectFrameworkBind.dll" \
    -r "samples/dotnet/ReflectFrameworkBind/bin/$CONFIG/$TFM/ReflectReturnLib.dll"

gate_extra_asserts() {
    local out="$1" dotnet
    DN2CPP_STRIPPED_OVERRIDES=1 run_bounded "$out/ReflectFrameworkBind$EXE_EXT" > "$out/kept-overrides.stdout"
    DN2CPP_STRIPPED_OVERRIDES=1 run_bounded dotnet "$_CG_APP" > "$out/kept-overrides.dotnet.stdout"
    dotnet=$(strip_cr_win_file "$out/kept-overrides.dotnet.stdout")
    grep -Fxq 'framework generic virtual row, library override: library:Tagged' <<< "$dotnet"
    grep -Fxq 'framework generic virtual row, library receiver allocated late: late:Tagged' <<< "$dotnet"
    diff -u <(strip_cr_win_file "$out/kept-overrides.dotnet.stdout") \
        <(strip_cr_win_file "$out/kept-overrides.stdout")
}
DN2CPP_OUT_SUFFIX=-kept-override corelib_diff_gate ReflectFrameworkBind --no-ildiet System.ComponentModel.TypeConverter \
    -r "samples/dotnet/ReflectFrameworkBind/bin/$CONFIG/$TFM/System.ReflectFrameworkBind.dll" \
    -r "samples/dotnet/ReflectFrameworkBind/bin/$CONFIG/$TFM/ReflectReturnLib.dll" \
    --link-xml samples/dotnet/ReflectFrameworkBind/keep-library-override.xml
unset -f gate_extra_asserts

# Each load set omits Ext although the CLR build needs it to describe the unused signature.
unused_fixture="$PWD/gates/fixtures/reflection-unused-signatures"
for subject in App AppOwned; do
    dotnet build "$unused_fixture/$subject/$subject.csproj" -c "$CONFIG" --nologo -v q
    case "$subject" in
        App) unused_assembly=ReflectionUnusedApp ;;
        AppOwned) unused_assembly=ReflectionUnusedAppOwned ;;
    esac
    unused_app="$unused_fixture/$subject/bin/$CONFIG/$TFM/$unused_assembly.dll"
    unused_refs=(-r "$_CG_CORELIB")
    if [ "$subject" = App ]; then
        unused_refs+=(-r "$unused_fixture/Lib/bin/$CONFIG/$TFM/ReflectionUnusedLib.dll")
    fi
    for mode in default trim; do
        unused_out="artifacts/reflection-unused-signatures/$subject-$mode"
        unused_flags=()
        if [ "$mode" = trim ]; then
            unused_flags=(--trim-reflection)
        fi
        DN2CPP_STRICT_COMPLETION=1 run_bounded invoke_cli "$unused_app" "${unused_refs[@]}" \
            --no-ildiet ${unused_flags[@]+"${unused_flags[@]}"} -o "$unused_out"
        compile_console "$unused_out" "$unused_assembly"
        run_bounded dotnet "$unused_app" > "$unused_out/dotnet.stdout"
        run_bounded "$unused_out/$unused_assembly$EXE_EXT" > "$unused_out/native.stdout"
        diff -u <(strip_cr_win_file "$unused_out/dotnet.stdout") \
            <(strip_cr_win_file "$unused_out/native.stdout")
        unused_native=$(strip_cr_win_file "$unused_out/native.stdout")
        for line in '== unused generic signature ==' 'used=42' 'supported=7' \
            'definition=True parameter=U' 'unused generic signature end'; do
            grep -Fxq -- "$line" <<< "$unused_native" \
                || { echo "FAIL: unused signature control missing ($subject/$mode): $line" >&2; exit 1; }
        done
    done
done

# Only additional definitions depend on these absent layouts. Explicit references
# keep the rows; a layout needed by a compiled body cannot use the omission path.
for subject in AppNested BodyNeeded; do
    dotnet build "$unused_fixture/$subject/$subject.csproj" -c "$CONFIG" --nologo -v q
    case "$subject" in
        AppNested) unused_assembly=ReflectionUnusedAppNested ;;
        BodyNeeded) unused_assembly=ReflectionSignatureBodyNeeded ;;
    esac
    unused_app="$unused_fixture/$subject/bin/$CONFIG/$TFM/$unused_assembly.dll"
    unused_ext="$unused_fixture/Ext/bin/$CONFIG/$TFM/ReflectionUnusedExt.dll"
    for mode in default trim; do
        unused_flags=()
        if [ "$mode" = trim ]; then
            unused_flags=(--trim-reflection)
        fi
        for load in absent loaded; do
            unused_out="artifacts/reflection-unused-signatures/$subject-$mode-$load"
            unused_refs=(-r "$_CG_CORELIB")
            if [ "$load" = loaded ]; then
                unused_refs+=(-r "$unused_ext")
            fi
            mkdir -p "$unused_out"
            unused_status=0
            DN2CPP_STRICT_COMPLETION=1 run_bounded invoke_cli "$unused_app" "${unused_refs[@]}" \
                --no-ildiet ${unused_flags[@]+"${unused_flags[@]}"} -o "$unused_out" \
                > "$unused_out/emission.log" 2>&1 || unused_status=$?
            if [ "$subject" = BodyNeeded ] && [ "$load" = absent ]; then
                if [ "$unused_status" -ne 2 ] || ! grep -Fq 'external generic types' "$unused_out/emission.log"; then
                    cat "$unused_out/emission.log" >&2
                    echo "FAIL: a body-required missing layout was not refused ($mode)" >&2
                    exit 1
                fi
                continue
            fi
            if [ "$unused_status" -ne 0 ]; then
                cat "$unused_out/emission.log" >&2
                echo "FAIL: optional signature layout rejected the image ($subject/$mode/$load)" >&2
                exit 1
            fi
            compile_console "$unused_out" "$unused_assembly"
            run_bounded dotnet "$unused_app" > "$unused_out/dotnet.stdout"
            run_bounded "$unused_out/$unused_assembly$EXE_EXT" > "$unused_out/native.stdout"
            diff -u <(strip_cr_win_file "$unused_out/dotnet.stdout") \
                <(strip_cr_win_file "$unused_out/native.stdout")
            unused_native=$(strip_cr_win_file "$unused_out/native.stdout")
            for line in '== nested generic signature ==' 'used=42' 'supported=7' \
                'definition=True parameter=U' 'nested generic signature end'; do
                grep -Fxq -- "$line" <<< "$unused_native" \
                    || { echo "FAIL: nested signature control missing ($subject/$mode/$load): $line" >&2; exit 1; }
            done
            if [ "$subject" = BodyNeeded ]; then
                grep -Fxq -- 'body needed=True' <<< "$unused_native"
            fi
            run_bounded dotnet "$unused_app" describe-unused-layouts > "$unused_out/definitions.dotnet.stdout"
            run_bounded "$unused_out/$unused_assembly$EXE_EXT" describe-unused-layouts > "$unused_out/definitions.native.stdout"
            unused_native=$(strip_cr_win_file "$unused_out/definitions.native.stdout")
            unused_clr=$(strip_cr_win_file "$unused_out/definitions.dotnet.stdout")
            for line in '== unused layout definitions ==' 'unused layout definitions end'; do
                grep -Fxq -- "$line" <<< "$unused_native"
                grep -Fxq -- "$line" <<< "$unused_clr"
            done
            for line in Pick First Second OwnerFirst OwnerSecond TaskShape TaskLayout; do
                grep -Fxq -- "$line found=True" <<< "$unused_clr"
                if [ "$load" = absent ]; then
                    grep -Fxq -- "$line found=False" <<< "$unused_native" \
                        || { echo "FAIL: an unavailable definition layout survived ($mode): $line" >&2; exit 1; }
                else
                    grep -Fxq -- "$line found=True" <<< "$unused_native"
                    grep -Fxq -- "$line identity=True/True" <<< "$unused_native"
                fi
            done
            if [ "$load" = loaded ]; then
                diff -u <(strip_cr_win_file "$unused_out/definitions.dotnet.stdout") \
                    <(strip_cr_win_file "$unused_out/definitions.native.stdout")
            fi
        done
    done
done

# Ordinary retained rows need their shapes; symbolic field growth cannot be a
# successful-output decision made by the environment's monomorphization bound.
for subject in AppDependencies MethodBodyNeeded GrowthBodyNeeded; do
    dotnet build "$unused_fixture/$subject/$subject.csproj" -c "$CONFIG" --nologo -v q
    case "$subject" in
        AppDependencies) unused_assembly=ReflectionSignatureDependencies ;;
        MethodBodyNeeded) unused_assembly=ReflectionMethodBodyNeeded ;;
        GrowthBodyNeeded) unused_assembly=ReflectionGrowthBodyNeeded ;;
    esac
    unused_app="$unused_fixture/$subject/bin/$CONFIG/$TFM/$unused_assembly.dll"
    for mode in default trim; do
        unused_flags=()
        if [ "$mode" = trim ]; then
            unused_flags=(--trim-reflection)
        fi
        for load in absent loaded; do
            unused_out="artifacts/reflection-unused-signatures/$subject-$mode-$load"
            unused_refs=(-r "$_CG_CORELIB" -r "$unused_fixture/Lib/bin/$CONFIG/$TFM/ReflectionUnusedLib.dll")
            if [ "$load" = loaded ]; then
                unused_refs+=(-r "$unused_fixture/Ext/bin/$CONFIG/$TFM/ReflectionUnusedExt.dll")
            fi
            mkdir -p "$unused_out"
            unused_status=0
            DN2CPP_STRICT_COMPLETION=1 run_bounded invoke_cli "$unused_app" "${unused_refs[@]}" \
                --no-ildiet ${unused_flags[@]+"${unused_flags[@]}"} -o "$unused_out" \
                > "$unused_out/emission.log" 2>&1 || unused_status=$?
            if [ "$subject" = GrowthBodyNeeded ] || { [ "$subject" = MethodBodyNeeded ] && [ "$load" = absent ]; }; then
                if [ "$unused_status" -ne 2 ]; then
                    cat "$unused_out/emission.log" >&2
                    echo "FAIL: a required dependency was not refused ($subject/$mode/$load)" >&2
                    exit 1
                fi
                if [ "$subject" = GrowthBodyNeeded ]; then
                    grep -Fq 'DN2CPP_MAX_GENERIC_DEPTH' "$unused_out/emission.log"
                    grep -Eq 'Driven by the signature of field .*Node.*\.Next' "$unused_out/emission.log"
                else
                    grep -Fq 'external generic types' "$unused_out/emission.log"
                fi
                continue
            fi
            if [ "$unused_status" -ne 0 ]; then
                cat "$unused_out/emission.log" >&2
                echo "FAIL: optional dependencies rejected the image ($subject/$mode/$load)" >&2
                exit 1
            fi
            compile_console "$unused_out" "$unused_assembly"
            run_bounded dotnet "$unused_app" > "$unused_out/dotnet.stdout"
            run_bounded "$unused_out/$unused_assembly$EXE_EXT" > "$unused_out/native.stdout"
            diff -u <(strip_cr_win_file "$unused_out/dotnet.stdout") \
                <(strip_cr_win_file "$unused_out/native.stdout")
            if [ "$subject" != AppDependencies ]; then
                continue
            fi
            run_bounded dotnet "$unused_app" describe-layout-dependencies > "$unused_out/dependencies.dotnet.stdout"
            run_bounded "$unused_out/$unused_assembly$EXE_EXT" describe-layout-dependencies > "$unused_out/dependencies.native.stdout"
            unused_clr=$(strip_cr_win_file "$unused_out/dependencies.dotnet.stdout")
            unused_native=$(strip_cr_win_file "$unused_out/dependencies.native.stdout")
            for line in '== signature dependency layouts ==' 'used=42' 'definition=True parameter=U' \
                '== unused signature dependencies ==' 'unused signature dependencies end' 'signature dependency layouts end'; do
                grep -Fxq -- "$line" <<< "$unused_native"
                grep -Fxq -- "$line" <<< "$unused_clr"
            done
            sed '/^== unused signature dependencies ==/,/^unused signature dependencies end/d' \
                <<< "$unused_native" > "$unused_out/dependencies-prefix.stdout"
            diff -u <(strip_cr_win_file "$unused_out/native.stdout") "$unused_out/dependencies-prefix.stdout"
            omitted='Growing|CrossGrowing'
            if [ "$load" = absent ]; then
                omitted="$omitted|MethodOnly|CtorOnly|InterfaceOnly"
            fi
            for line in MethodOnly CtorOnly InterfaceOnly GenericOwner Growing CrossGrowing Reset Swap Converging; do
                grep -Fxq -- "$line found=True" <<< "$unused_clr"
                case "$line/$load" in
                    Growing/*|CrossGrowing/*|MethodOnly/absent|CtorOnly/absent|InterfaceOnly/absent)
                        grep -Fxq -- "$line found=False" <<< "$unused_native" ;;
                    *)
                        grep -Fxq -- "$line found=True" <<< "$unused_native"
                        grep -Fxq -- "$line identity=True/True" <<< "$unused_native" ;;
                esac
            done
            grep -Fxq -- 'GenericOwner member found=True' <<< "$unused_clr"
            if [ "$load" = absent ]; then
                grep -Fxq -- 'GenericOwner member found=False' <<< "$unused_native"
            else
                grep -Fxq -- 'GenericOwner member found=True' <<< "$unused_native"
            fi
            diff -u <(sed -E "/^($omitted)( found=| types=| identity=)/d; /^GenericOwner member found=/d" <<< "$unused_clr") \
                <(sed -E "/^($omitted)( found=| types=| identity=)/d; /^GenericOwner member found=/d" <<< "$unused_native")
            run_bounded dotnet "$unused_app" describe-layout-paths > "$unused_out/paths.dotnet.stdout"
            run_bounded "$unused_out/$unused_assembly$EXE_EXT" describe-layout-paths > "$unused_out/paths.native.stdout"
            unused_clr=$(strip_cr_win_file "$unused_out/paths.dotnet.stdout")
            unused_native=$(strip_cr_win_file "$unused_out/paths.native.stdout")
            for line in '== additional signature paths ==' 'additional signature paths end'; do
                grep -Fxq -- "$line" <<< "$unused_native"
                grep -Fxq -- "$line" <<< "$unused_clr"
            done
            sed '/^== additional signature paths ==/,/^additional signature paths end/d' \
                <<< "$unused_native" > "$unused_out/paths-prefix.stdout"
            diff -u <(strip_cr_win_file "$unused_out/native.stdout") "$unused_out/paths-prefix.stdout"
            omitted='Branched|OriginOverlap'
            if [ "$load" = absent ]; then
                omitted="$omitted|OverrideOnly|InheritedSlot"
            fi
            for line in Branched OriginOverlap BranchFinite OverrideOnly InheritedSlot VirtualSibling; do
                grep -Fxq -- "$line found=True" <<< "$unused_clr"
                case "$line/$load" in
                    Branched/*|OriginOverlap/*|OverrideOnly/absent|InheritedSlot/absent)
                        grep -Fxq -- "$line found=False" <<< "$unused_native" ;;
                    *)
                        grep -Fxq -- "$line found=True" <<< "$unused_native"
                        grep -Fxq -- "$line identity=True/True" <<< "$unused_native" ;;
                esac
            done
            diff -u <(sed -E "/^($omitted)( found=| types=| identity=)/d" <<< "$unused_clr") \
                <(sed -E "/^($omitted)( found=| types=| identity=)/d" <<< "$unused_native")
            run_bounded dotnet "$unused_app" describe-default-interface-layout > "$unused_out/default-interface.dotnet.stdout"
            run_bounded "$unused_out/$unused_assembly$EXE_EXT" describe-default-interface-layout > "$unused_out/default-interface.native.stdout"
            unused_clr=$(strip_cr_win_file "$unused_out/default-interface.dotnet.stdout")
            unused_native=$(strip_cr_win_file "$unused_out/default-interface.native.stdout")
            for line in '== default interface layout ==' 'default interface layout end'; do
                grep -Fxq -- "$line" <<< "$unused_native"
                grep -Fxq -- "$line" <<< "$unused_clr"
            done
            sed '/^== default interface layout ==/,/^default interface layout end/d' \
                <<< "$unused_native" > "$unused_out/default-interface-prefix.stdout"
            diff -u <(strip_cr_win_file "$unused_out/native.stdout") "$unused_out/default-interface-prefix.stdout"
            for line in DefaultInterface SupportedDefaultInterface RelationOnlyInterface; do
                grep -Fxq -- "$line found=True" <<< "$unused_clr"
                if [ "$line/$load" = DefaultInterface/absent ]; then
                    grep -Fxq -- "$line found=False" <<< "$unused_native"
                else
                    grep -Fxq -- "$line found=True" <<< "$unused_native"
                    grep -Fxq -- "$line identity=True/True" <<< "$unused_native"
                fi
            done
            if [ "$load" = absent ]; then
                diff -u <(sed -E '/^DefaultInterface( found=| types=| identity=)/d' <<< "$unused_clr") \
                    <(sed -E '/^DefaultInterface( found=| types=| identity=)/d' <<< "$unused_native")
            else
                diff -u <(strip_cr_win_file "$unused_out/default-interface.dotnet.stdout") \
                    <(strip_cr_win_file "$unused_out/default-interface.native.stdout")
            fi
            run_bounded dotnet "$unused_app" describe-completion-layouts > "$unused_out/completion.dotnet.stdout"
            run_bounded "$unused_out/$unused_assembly$EXE_EXT" describe-completion-layouts > "$unused_out/completion.native.stdout"
            unused_clr=$(strip_cr_win_file "$unused_out/completion.dotnet.stdout")
            unused_native=$(strip_cr_win_file "$unused_out/completion.native.stdout")
            for line in '== completion signature layouts ==' 'completion signature layouts end' \
                'ordinary row=True generic=False' 'OverrideFinite getter=ResetNode`1 identity=True parameters=0'; do
                grep -Fxq -- "$line" <<< "$unused_native"
                grep -Fxq -- "$line" <<< "$unused_clr"
            done
            sed '/^== completion signature layouts ==/,/^completion signature layouts end/d' \
                <<< "$unused_native" > "$unused_out/completion-prefix.stdout"
            diff -u <(strip_cr_win_file "$unused_out/native.stdout") "$unused_out/completion-prefix.stdout"
            for line in OverrideGrowth OverrideFinite ExplicitGrowth ExplicitFinite StructDefinitionOnly StructPair; do
                grep -Fxq -- "$line found=True" <<< "$unused_clr"
                case "$line" in
                    OverrideGrowth|ExplicitGrowth)
                        grep -Fxq -- "$line found=False" <<< "$unused_native" ;;
                    *)
                        grep -Fxq -- "$line found=True" <<< "$unused_native"
                        grep -Fxq -- "$line definition=True formal=U" <<< "$unused_native"
                        grep -Fxq -- "$line identity=True/True" <<< "$unused_native" ;;
                esac
            done
            diff -u <(sed -E '/^(OverrideGrowth|ExplicitGrowth)( found=| definition=| types=| identity=)/d' <<< "$unused_clr") \
                <(sed -E '/^(OverrideGrowth|ExplicitGrowth)( found=| definition=| types=| identity=)/d' <<< "$unused_native")
            DN2CPP_MAX_GENERIC_DEPTH=40 DN2CPP_STRICT_COMPLETION=1 run_bounded invoke_cli \
                "$unused_app" "${unused_refs[@]}" --no-ildiet ${unused_flags[@]+"${unused_flags[@]}"} \
                -o "$unused_out-depth40" > "$unused_out-depth40.log" 2>&1
            for unused_generated in "$unused_out"/generated*.cpp "$unused_out"/generated.h; do
                cmp "$unused_generated" "$unused_out-depth40/$(basename "$unused_generated")"
            done
        done
    done
done

# An open signature needs its base shape but never the base's unused field layout.
dotnet build "$unused_fixture/AppOpen/AppOpen.csproj" -c "$CONFIG" --nologo -v q
unused_app="$unused_fixture/AppOpen/bin/$CONFIG/$TFM/ReflectionUnusedAppOpen.dll"
for mode in default trim; do
    unused_flags=()
    if [ "$mode" = trim ]; then
        unused_flags=(--trim-reflection)
    fi
    for load in absent loaded; do
        unused_out="artifacts/reflection-unused-signatures/AppOpen-$mode-$load"
        unused_refs=(-r "$_CG_CORELIB" -r "$unused_fixture/Lib/bin/$CONFIG/$TFM/ReflectionUnusedLib.dll")
        if [ "$load" = loaded ]; then
            unused_refs+=(-r "$unused_fixture/Ext/bin/$CONFIG/$TFM/ReflectionUnusedExt.dll")
        fi
        DN2CPP_STRICT_COMPLETION=1 run_bounded invoke_cli "$unused_app" "${unused_refs[@]}" \
            --no-ildiet ${unused_flags[@]+"${unused_flags[@]}"} -o "$unused_out"
        compile_console "$unused_out" ReflectionUnusedAppOpen
        run_bounded dotnet "$unused_app" > "$unused_out/dotnet.stdout"
        run_bounded "$unused_out/ReflectionUnusedAppOpen$EXE_EXT" > "$unused_out/native.stdout"
        diff -u <(strip_cr_win_file "$unused_out/dotnet.stdout") \
            <(strip_cr_win_file "$unused_out/native.stdout")
        unused_native=$(strip_cr_win_file "$unused_out/native.stdout")
        for line in '== open signature base layout ==' 'used=42' 'found=True' \
            'definition=True' 'formal=U' 'open signature base layout end'; do
            grep -Fxq -- "$line" <<< "$unused_native" \
                || { echo "FAIL: open signature definition missing ($mode/$load): $line" >&2; exit 1; }
        done
        run_bounded dotnet "$unused_app" return-type > "$unused_out/return.dotnet.stdout"
        run_bounded "$unused_out/ReflectionUnusedAppOpen$EXE_EXT" return-type > "$unused_out/return.native.stdout"
        unused_clr=$(strip_cr_win_file "$unused_out/return.dotnet.stdout")
        unused_native=$(strip_cr_win_file "$unused_out/return.native.stdout")
        grep -Fxq -- 'return=OpenHolder`1 contains=True' <<< "$unused_clr" \
            || { echo "FAIL: CLR open signature type witness missing ($mode/$load)" >&2; exit 1; }
        grep -Fxq -- 'return exception=PlatformNotSupportedException' <<< "$unused_native" \
            || { echo "FAIL: native open signature type boundary missing ($mode/$load)" >&2; exit 1; }
        diff -u <(strip_cr_win_file "$unused_out/dotnet.stdout") \
            <(sed '/^return=/d' <<< "$unused_clr")
        diff -u <(strip_cr_win_file "$unused_out/native.stdout") \
            <(sed '/^return exception=/d' <<< "$unused_native")
    done
done

# This driver has no GetMethod or typeof(ValueType) seed for the formal's base handle.
dotnet build "$unused_fixture/Enumeration/Enumeration.csproj" -c "$CONFIG" --nologo -v q
formal_enumeration_app="$unused_fixture/Enumeration/bin/$CONFIG/$TFM/FormalMethodEnumeration.dll"
formal_enumeration_out=artifacts/formal-method-enumeration
DN2CPP_STRICT_COMPLETION=1 run_bounded invoke_cli "$formal_enumeration_app" -r "$_CG_CORELIB" \
    --no-ildiet -o "$formal_enumeration_out"
compile_console "$formal_enumeration_out" FormalMethodEnumeration
run_bounded dotnet "$formal_enumeration_app" > "$formal_enumeration_out/dotnet.stdout"
run_bounded "$formal_enumeration_out/FormalMethodEnumeration$EXE_EXT" > "$formal_enumeration_out/native.stdout"
diff -u <(strip_cr_win_file "$formal_enumeration_out/dotnet.stdout") \
    <(strip_cr_win_file "$formal_enumeration_out/native.stdout")
formal_enumeration_native=$(strip_cr_win_file "$formal_enumeration_out/native.stdout")
for line in '== enumerated formal parameter ==' \
    'nested=True value=True class=False base=System.ValueType' 'count=1' 'enumerated formal parameter end'; do
    grep -Fxq -- "$line" <<< "$formal_enumeration_native" \
        || { echo "FAIL: enumerated formal parameter witness missing: $line" >&2; exit 1; }
done

# The isolated driver uses the same appended delegate section with actual folding
# and with receiver reflection metadata absent.
delegate_identity_diff_axes
