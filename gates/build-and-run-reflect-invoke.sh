#!/usr/bin/env bash
# Consolidated reflection-invocation gate. Merges the former reflect dynamic-use
# subset gates into one multi-section program, transpiled once against the
# tree-shaken real CoreLib and diffed exactly against real .NET. Covers:
#   MethodInfo.Invoke (instance/static, args, return boxing, void, private),
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
# ReflectToStringSubset asserts MethodInfo/ConstructorInfo/FieldInfo/PropertyInfo/
# ParameterInfo and CustomAttributeData signature display through typed, base, and
# object dispatch, including byref, indexer, generic-method, and attribute arguments.
# Former gates: reflect-invoke, reflect-dispatch, reflect-field-value,
# reflect-serializer, activator-subset, event-subset.
source "$(dirname "$0")/_common.sh"

corelib_diff_gate ReflectInvoke
