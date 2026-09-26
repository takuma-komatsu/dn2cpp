#!/usr/bin/env bash
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
# generic implementations, array generic arguments, runtime-owned declaring
# types, which may answer null but never a wrong method, and Object virtuals'
# method groups, which name the method their receiver runs. Its interface section
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
# follow the selected pointer; calli keeps the raw address. Only those bodies
# carry delegate tags. A local whose address is taken keeps no delegate identity,
# because a byref write would leave it stale: a delegate built from it is refused
# when transpiled, from a native-int or int64 local alike, and one built from a
# copy of it throws NotSupportedException when constructed. Address-taken locals
# beside a delegate in plain C# still transpile.
# ReflectToStringSubset asserts MethodInfo/ConstructorInfo/FieldInfo/PropertyInfo/
# ParameterInfo and CustomAttributeData signature display through typed, base, and
# object dispatch, including byref, indexer, generic-method, and attribute arguments.
# RuntimeHandleRelationSubset asserts the CLR relations of objects whose type-info
# the runtime writes by hand — the reflection objects, Assembly and Module,
# StringBuilder, Exception and the exceptions the runtime raises from real faults,
# the synchronization handles, Thread, Task, the culture wrappers — and of
# System.Array: the type test, IsAssignableFrom, BaseType chains, named interface
# membership and the invoke argument checks, then `using`, an IDisposable-typed
# Dispose and a reflected IDisposable.Dispose over the synchronization handles. Its
# greps pin the init-prologue installs those answers come from: the relation rows,
# SystemException spliced under the runtime NullReferenceException's handle, and
# SemaphoreSlim's IDisposable map.
# ReflectVirtualInvokeSubset asserts that MethodInfo.Invoke,
# PropertyInfo.GetValue/SetValue and a CreateDelegate binding entered through a
# virtual row run the receiver's most derived body in that row's slot, as a callvirt
# does: base and middle rows over overrides, abstract rows (which check the receiver
# and arguments first), new-slot hiders and their overrides, sealed and setter-only
# overrides, generic bases over shared and value arguments, a MakeGenericType
# receiver, a boxed enum, compiled framework overrides of abstract rows, framework
# overrides only reflection reaches, which a string literal after typeof names, and
# interface rows whose declaration has a default body, beside a non-virtual
# interface member that runs its own body, and an application interface's static,
# non-virtual and private members that only reflection calls. A closed binding
# reports the body it runs as its Method, and a boxed value binds as the receiver of
# an interface or System.Enum row, or as a static method's first argument, each call
# running on the box the delegate holds. Bindings through a declaration and through
# the override it resolves to are equal delegates, and a binding that does not fit
# fails with .NET's message. A static virtual interface member's default body runs
# through Invoke and a static abstract one faults as bad IL; a delegate binds either
# only open and finds no entry point when called. With DN2CPP_STRIPPED_OVERRIDES=1 it asserts dn2cpp alone: a
# receiver's body the image stripped raises a catchable NotSupportedException naming
# the member and the remedy, for every trap shape a vtable or interface slot holds.
# Its generic virtual section asserts the same for a closed generic virtual row,
# which has no slot and runs the override a call through it binds: class rows over
# inherited overrides, new-slot hiders, sealed and covariant overrides, abstract
# rows (which check the receiver and arguments first), a row only a base call
# names, generic-class rows over shared and value arguments, a MakeGenericType
# receiver, and interface rows over plain, explicit, class-override, abstract-level,
# default, derived-interface and struct bodies; a closed binding reports the body it
# runs. Its direct calls include a struct's generic interface method through its box
# and a delegate. An open binding of a generic virtual row is refused with .NET's
# NotSupportedException. An override hides the generic virtual method it overrides
# from GetMethod and GetMethods while a new slot or a new method stays a second
# method, and GetBaseDefinition answers the definition that introduces the chain. A
# binding closed over null runs the row's own body, a bodiless row faulting as bad
# IL, and an open binding runs a non-virtual row over a null receiver. A call
# through System.Object runs the override of Object's member past a non-virtual
# or new-slot redeclaration. Its System.Object and System.ValueType section
# asserts that a named lookup answers their members through levels with a row for
# each override, hides one behind such an override and reports an overload beside
# one as ambiguous, and that Invoke, CreateDelegate and a method group run and
# report what a callvirt runs, with .NET's receiver and arity faults. Its
# visibility section asserts MethodBase's and FieldInfo's access, hide-by-signature,
# not-serialized and p/invoke predicates over every accessibility of a method,
# constructor and field, and over System.Object's rows.
# ReflectFieldValidationSubset asserts that FieldInfo.GetValue/SetValue check the
# receiver, then the value, with .NET's exceptions, HResults and messages: an
# instance field refuses a null or foreign receiver and takes a derived instance, a
# boxed struct and a MakeGenericType instantiation's own instance, a static field
# ignores its receiver, a value converts as a reflected argument does, and null
# stores the default of a value-type field. A constant answers from metadata, boxed
# at its encoded type, and SetValue refuses it before any check; SetValue refuses a
# static read-only field once the value checks, naming the declaring TypeDef; a
# Nullable<T> field reads back as null or a boxed T; an enum that only a reflected
# member row or a closed generic argument names reports its own type; SetValue on a
# boxed enum's value__ writes the box; GetRawConstantValue answers a constant at its
# encoded type, an enum's underlying primitive, and refuses any other field.
# AmbiguousMatchMessageSubset asserts .NET's AmbiguousMatchException message and
# HResult for each ambiguous lookup: GetMethod over overloads, beside the
# System.Object Equals row and over Object's own rows, GetProperty over indexers,
# GetInterface and Activator.CreateInstance's constructor binding name the first
# match after its DeclaringType, and a single-attribute getter names the first
# attribute's type.
# ReflectBindOnly, a program whose only reflection call is CreateDelegate, asserts
# that the binding alone reaches the uncalled application bodies it binds: static
# (also through the generic MethodInfo.CreateDelegate over a delegate type nothing
# else names), instance, an override through its base row and an interface's static
# member.
# Mixed native/packed metadata preserves inherited members, closed generics,
# parameter identity, and interface receiver dispatch across cache eviction.
# Disabling compression forces native metadata even for explicit packed selectors.
# NoCompressMetadata and derived attributes select native owner/member metadata
# through class inheritance without changing containing types or interface users.
# Former gates: reflect-invoke, reflect-dispatch, reflect-field-value,
# reflect-serializer, activator-subset, event-subset.
source "$(dirname "$0")/_common.sh"

py="$(resolve_python)"
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} gates/fixtures/check-reflection-layout.py gates/measure-reflection-metadata.py gates/expected/reflection-allocations.csv"
gate_extra_asserts() {
    local out="$1"
    "$py" gates/fixtures/check-reflection-layout.py "$out" "$reflection_layout_axis"
    run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/metadata-layout.stdout"
    grep -Fxq 'metadata-layout-begin' "$out/metadata-layout.stdout"
    grep -Fxq 'metadata-layout-cache-capacity=72/1296' "$out/metadata-layout.stdout"
    grep -Fxq 'metadata-layout-cache-threads=1296/1296' "$out/metadata-layout.stdout"
    grep -Fxq 'metadata-layout-interface-receivers=21000' "$out/metadata-layout.stdout"
    grep -Fxq 'metadata-layout-end' "$out/metadata-layout.stdout"
    grep -Fxq 'metadata-compression-begin' "$out/metadata-layout.stdout"
    grep -Fxq 'metadata-compression-labels=field/property/constructor/method/parameter' "$out/metadata-layout.stdout"
    grep -Fxq 'metadata-compression-inheritance=v5/Direct' "$out/metadata-layout.stdout"
    grep -Fxq 'metadata-compression-generic-value=15/Int32/True' "$out/metadata-layout.stdout"
    grep -Fxq 'metadata-compression-generic-reference=text/String/True' "$out/metadata-layout.stdout"
    grep -Fxq 'metadata-compression-plain-generic=True/True' "$out/metadata-layout.stdout"
    grep -Fxq 'metadata-compression-end' "$out/metadata-layout.stdout"
    grep -Fxq 'existing-constructor-message: Exception has been thrown by the target of an invocation.' "$out/metadata-layout.stdout"
    grep -Fxq 'existing-constructor-method-composed-flags: TargetInvocationException InvalidOperationException 80131604' "$out/metadata-layout.stdout"
    grep -Fxq 'existing-constructor-end' "$out/metadata-layout.stdout"
    grep -Fxq 'activator-cold-generic=73' "$out/metadata-layout.stdout"
    DN2CPP_BEFORE_EXISTING_CONSTRUCTOR=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-existing-constructor.stdout"
    sed '/^existing-constructor-begin/,$d' "$out/metadata-layout.stdout" > "$out/existing-constructor-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-existing-constructor.stdout") \
        <(strip_cr_win_file "$out/existing-constructor-prefix.stdout")
    DN2CPP_BEFORE_COLD_ACTIVATOR=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-cold-activator.stdout"
    sed '/^activator-cold-generic=/,$d' "$out/metadata-layout.stdout" > "$out/cold-activator-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-cold-activator.stdout") \
        <(strip_cr_win_file "$out/cold-activator-prefix.stdout")
    grep -Fxq 'delegate-method-shared=True/True' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-generic=Int32/String' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-runtime-owned=True/True' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-struct-interface=StructProbe/Value/31/31' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-explicit-interface=ExplicitProbe/True/41/41' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-default-interface=IDefaultProbe/Default/101' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-interface-generic=ImplicitGeneric/String/ExplicitGeneric/True/Int32/p5' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-array-generic=Int32[]/String[]' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-generic-hider=GvmBase/base/GvmLeaf/leaf' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-generic-covariant=CovariantLeaf/CovariantLeaf/CovariantLeaf' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-object-virtual=Object.ToString/ValueType/ValueType/ReflectDelegateIdentitySubset.Program+StructProbe/True' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-end' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-interface-begin' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-generic-explicit-order=explicit/PlainFirstGeneric/True/explicit/ExplicitFirstGeneric/True' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-derived-default=derived/IDerivedDefault/True' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-derived-generic=derived/IDerivedGenericDefault/True' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-derived-struct=derived/IDerivedDefault' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-derived-inherited=derived/IDerivedDefault' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-derived-typed=derived/True' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-derived-same=same/IDerivedSame' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-derived-runtime-type=derived/IRuntimeDerivedDefault/True' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-derived-generic-runtime-type=derived/derived/IRuntimeGenericDerivedDefault/True' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-inherited-generic-runtime-type=mid/mid/RuntimeGvmMid' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-interface-end' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-gvm-dispatch-begin' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-gvm-explicit-overloads=generic/integer' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-gvm-explicit-overloads-generic-interface=generic/integer' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-gvm-plain-and-explicit-overload=plain/int-explicit/Pick/True' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-gvm-qualifier-prefix=plain/longer/Pick/True' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-gvm-qualifier-arity=plain/explicit-generic/Pick/True' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-gvm-dispatch-end' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-begin' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-plain=derived-plain/derived-plain/RedeclaredDerived/plain' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-unlisted=base-explicit/base-explicit/RedeclaredBase/explicit' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-hider=implicit/implicit/ImplicitRedeclared/plain' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-abstract=abstract-leaf/abstract-leaf/AbstractLeaf/plain' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-generic-class=shared-box-String/shared-box-String/SharedRedeclaredBox`1/plain' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-fill=fill-source/fill-source/FillSource/plain/fill-override/fill-override/FillOverride/plain' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-explicit-mid=explicit-mid/explicit-mid/ExplicitMidRedeclared/explicit' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-default=default/default/IRedeclaredDefault/plain/default-mid/default-mid/DefaultRedeclared/explicit' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-closed-generic=of-derived-plain/of-derived-plain/RedeclaredOfDerived/plain' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-runtime-type=runtime-box/runtime-box/Tag' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-pick-plain=pick-derived-plain/pick-derived-plain/PickDerived/plain' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-pick-unlisted=pick-base-explicit/pick-base-explicit/PickBase/explicit' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-pick-hider=pick-implicit/pick-implicit/PickImplicit/plain/pick-virtual/pick-virtual/PickVirtual/plain' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-pick-override=pick-override/pick-override/PickOverride/plain' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-pick-fill=pick-source/pick-source/PickSource/plain/pick-target-override/pick-target-override/PickTargetOverride/plain' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-end' "$out/metadata-layout.stdout"
    grep -Fxq 'runtime-level-gvm-begin' "$out/metadata-layout.stdout"
    grep -Fxq 'runtime-level-gvm-generic-base=root:Int32/String|leaf:Int32/String+root:Int32/String|leaf:Int32/String+root:Int32/String|Tag|True|True|True' "$out/metadata-layout.stdout"
    grep -Fxq 'runtime-level-gvm-unconstructed-base=leaf:String/Int32+root:String/Int32|leaf:String/Int32+root:String/Int32|True|Int32' "$out/metadata-layout.stdout"
    grep -Fxq 'runtime-level-gvm-two-arguments=pair:Int32,String/String|pair:Int32,Boolean/String|pair:Int32,Boolean/String|True' "$out/metadata-layout.stdout"
    grep -Fxq 'runtime-level-gvm-plain-base=own:Decimal/String|own:Decimal/String|True|own:Decimal|True' "$out/metadata-layout.stdout"
    grep -Fxq 'runtime-level-method-row=True|own:Decimal|True|Who' "$out/metadata-layout.stdout"
    grep -Fxq 'runtime-level-gvm-chain=chain:String+mid:String/Int32|chain:String+mid:String/Int32|True' "$out/metadata-layout.stdout"
    grep -Fxq 'runtime-level-gvm-inherited=mid:Boolean/Int32|mid:Boolean/Int32|True|Boolean' "$out/metadata-layout.stdout"
    grep -Fxq 'runtime-level-gvm-image-level=abstract-mid:Int32/Int32|True|True|True' "$out/metadata-layout.stdout"
    grep -Fxq 'runtime-level-gvm-interface=picker:Int32/String|picker:Int32/String|True|picker:Int32|True|Name' "$out/metadata-layout.stdout"
    grep -Fxq 'runtime-level-gvm-end' "$out/metadata-layout.stdout"
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
    grep -Fxq 'ldftn-local-direct=12/Add' "$out/metadata-layout.stdout"
    grep -Fxq 'ldftn-local-nop=12/Add' "$out/metadata-layout.stdout"
    grep -Fxq 'ldftn-local-conv=12/Add' "$out/metadata-layout.stdout"
    grep -Fxq 'ldftn-local-snapshot=12/Add' "$out/metadata-layout.stdout"
    grep -Fxq 'ldftn-local-selected=12/Add/2/Subtract' "$out/metadata-layout.stdout"
    grep -Fxq 'ldftn-local-stack-join=12/Add/2/Subtract' "$out/metadata-layout.stdout"
    grep -Fxq 'ldftn-local-closed=C:x/Decorate' "$out/metadata-layout.stdout"
    grep -Fxq 'ldftn-local-calli=14' "$out/metadata-layout.stdout"
    grep -Fxq 'ldftn-local-dead-origin=2/Subtract' "$out/metadata-layout.stdout"
    grep -Fxq 'ldftn-local-virtual=15/VirtualDerived.Scale' "$out/metadata-layout.stdout"
    grep -Fxq 'ldftn-local-instance=15/Offset' "$out/metadata-layout.stdout"
    grep -Fxq 'ldftn-local-int64=12/Add' "$out/metadata-layout.stdout"
    grep -Fxq 'ldftn-local-address-taken=42/9/12/Add' "$out/metadata-layout.stdout"
    grep -Fxq 'ldftn-local-end' "$out/metadata-layout.stdout"
    # Every emitted body follows its `// Type::Method` line, CRLF-terminated on a
    # Windows host. Delegate tags belong to the rewritten bodies alone, since C#
    # never builds a delegate from a stored or joined address.
    local tag_owners stray_owners
    tag_owners=$(LC_ALL=C awk '{ sub(/\r$/, "") } /^\/\/ .*::/ { owner = substr($0, 4) }
        /int32_t [A-Za-z0-9_]+_delegate_tag/ { print owner }' "$out"/generated*.cpp | LC_ALL=C sort -u)
    grep -Fxq 'LdftnLocalSubset.Program::Selected' <<<"$tag_owners"
    stray_owners=$(grep -Ev '^LdftnLocalSubset\.Program::(Stored|NopSeparated|NativeConvert|SnapshotBeforeOverwrite|Selected|StackJoin|ClosedStored|RawCalli|DeadOrigins|VirtualStored|InstanceStored|Int64Stored)$' <<<"$tag_owners" || true)
    if [ -n "$stray_owners" ]; then
        printf 'error: delegate tags outside the rewritten bodies:\n%s\n' "$stray_owners" >&2
        return 1
    fi
    DN2CPP_BEFORE_LDFTN_LOCAL=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-ldftn-local.stdout"
    sed '/^ldftn-local-begin/,$d' "$out/metadata-layout.stdout" > "$out/ldftn-local-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-ldftn-local.stdout") \
        <(strip_cr_win_file "$out/ldftn-local-prefix.stdout")
    grep -Fxq '== reflection invoke validation ==' "$out/metadata-layout.stdout"
    grep -Fxq 'target calls: 2' "$out/metadata-layout.stdout"
    grep -Fxq 'plain get, stray index: TargetParameterCountException' "$out/metadata-layout.stdout"
    grep -Fxq 'bound ValueType delegate: boxed:5' "$out/metadata-layout.stdout"
    grep -Fxq 'nullable result without value: null' "$out/metadata-layout.stdout"
    grep -Fxq 'current number format: separator:.' "$out/metadata-layout.stdout"
    grep -Fxq 'number from int: number:7' "$out/metadata-layout.stdout"
    grep -Fxq 'number from long: ArgumentException' "$out/metadata-layout.stdout"
    DN2CPP_BEFORE_INVOKE_VALIDATION=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-invoke-validation.stdout"
    sed '/^== reflection invoke validation ==/,$d' "$out/metadata-layout.stdout" > "$out/invoke-validation-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-invoke-validation.stdout") \
        <(strip_cr_win_file "$out/invoke-validation-prefix.stdout")
    grep -Fxq '== runtime handle relations ==' "$out/metadata-layout.stdout"
    grep -Fxq 'runtime NullReferenceException chain: NullReferenceException > SystemException > Exception > Object' "$out/metadata-layout.stdout"
    grep -Fxq 'ManualResetEvent after IDisposable: ObjectDisposedException' "$out/metadata-layout.stdout"
    grep -Fxq 'runtime handle relations end' "$out/metadata-layout.stdout"
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
    grep -Fxq '== virtual invoke ==' "$out/metadata-layout.stdout"
    grep -Fxq 'base row, leaf: leaf' "$out/metadata-layout.stdout"
    grep -Fxq 'abstract row: square' "$out/metadata-layout.stdout"
    grep -Fxq 'abstract row, null receiver: TargetException 0x80131603 Non-static method requires a target.' "$out/metadata-layout.stdout"
    grep -Fxq 'animal row, loud puppy: dog' "$out/metadata-layout.stdout"
    grep -Fxq 'puppy row, loud puppy: loud-puppy' "$out/metadata-layout.stdout"
    grep -Fxq 'generic base, shared override: wrapper<String>:b' "$out/metadata-layout.stdout"
    grep -Fxq 'minted receiver, abstract row: hello:Int64' "$out/metadata-layout.stdout"
    grep -Fxq 'framework abstract property: True' "$out/metadata-layout.stdout"
    grep -Fxq 'closed delegate method: Leaf.Who' "$out/metadata-layout.stdout"
    grep -Fxq 'default row, class body: custom-hello' "$out/metadata-layout.stdout"
    grep -Fxq 'sealed interface row, class: CUSTOM-HELLO' "$out/metadata-layout.stdout"
    grep -Fxq 'interface static row: made' "$out/metadata-layout.stdout"
    grep -Fxq 'interface non-virtual row: stamp:box' "$out/metadata-layout.stdout"
    grep -Fxq 'interface private row: secret:box' "$out/metadata-layout.stdout"
    grep -Fxq 'reflection-only framework setter: 1/1' "$out/metadata-layout.stdout"
    grep -Fxq 'reflection-only struct-returning override: 2020-2-29' "$out/metadata-layout.stdout"
    grep -Fxq 'reflection-only framework interface impl: 1.2' "$out/metadata-layout.stdout"
    grep -Fxq 'boxed struct delegate: 7/7/True' "$out/metadata-layout.stdout"
    grep -Fxq 'boxed struct first argument: 700/True' "$out/metadata-layout.stdout"
    grep -Fxq 'boxed enum delegate: 1/Enum.CompareTo/High' "$out/metadata-layout.stdout"
    grep -Fxq 'boxed struct delegate equality across rows: True/True' "$out/metadata-layout.stdout"
    grep -Fxq 'closed delegate equality across rows: True/True' "$out/metadata-layout.stdout"
    grep -Fxq 'closed delegate, unrelated receiver: ArgumentException 0x80070057 Cannot bind to the target method because its signature is not compatible with that of the delegate type.' "$out/metadata-layout.stdout"
    grep -Fxq 'static abstract row: TargetInvocationException 0x80131604/BadImageFormatException 0x8007000B' "$out/metadata-layout.stdout"
    grep -Fxq 'static virtual delegate: IFactory.Virt/True/EntryPointNotFoundException 0x80131523' "$out/metadata-layout.stdout"
    grep -Fxq 'static abstract row, typed catch: caught:True' "$out/metadata-layout.stdout"
    grep -Fxq 'object callvirt past new slots: ReflectVirtualInvokeSubset.HiddenText/ReflectVirtualInvokeSubset.SlotTextLeaf/slot-leaf' "$out/metadata-layout.stdout"
    grep -Fxq 'object rows: System.String ToString()|Boolean Equals(System.Object)|Int32 GetHashCode()|System.Type GetType()' "$out/metadata-layout.stdout"
    grep -Fxq 'object Equals, no types: AmbiguousMatchException' "$out/metadata-layout.stdout"
    grep -Fxq 'value Equals, no types: ValueType.Equals' "$out/metadata-layout.stdout"
    grep -Fxq 'overload beside object row: AmbiguousMatchException' "$out/metadata-layout.stdout"
    grep -Fxq 'object row, receivers: labeled/ReflectVirtualInvokeSubset.Leaf/42/shown:2/ReflectVirtualInvokeSubset.HiddenText/ReflectVirtualInvokeSubset.SlotTextLeaf' "$out/metadata-layout.stdout"
    grep -Fxq 'object row, null receiver: TargetException 0x80131603 Non-static method requires a target.' "$out/metadata-layout.stdout"
    grep -Fxq 'value row, class receiver: TargetException 0x80131603 Object type System.ValueType does not match target type ReflectVirtualInvokeSubset.Leaf.' "$out/metadata-layout.stdout"
    grep -Fxq 'object Equals, no argument: TargetParameterCountException 0x8002000E Parameter count mismatch.' "$out/metadata-layout.stdout"
    grep -Fxq 'closed object row, labeled: labeled/Labeled.ToString' "$out/metadata-layout.stdout"
    grep -Fxq 'closed value row, class: ArgumentException' "$out/metadata-layout.stdout"
    grep -Fxq 'object method groups: 5/ReflectVirtualInvokeSubset.Mark/True/True/ReflectVirtualInvokeSubset.Leaf/ValueType.ToString/Object.ToString' "$out/metadata-layout.stdout"
    grep -Fxq 'object method group overrides: shown:2/ShownMark.ToString/labeled/Labeled.ToString' "$out/metadata-layout.stdout"
    grep -Fxq 'method visibility: Hidden=ph Fam=fh Pub=uh Asm=ah FamOrAsm=oh FamAndAsm=nh' "$out/metadata-layout.stdout"
    grep -Fxq 'constructor visibility: Int32=ph Int64=fh uh String=ah Double=oh Char=nh' "$out/metadata-layout.stdout"
    grep -Fxq 'field visibility: Hidden=p Fam=f Pub=u Asm=a FamOrAsm=o FamAndAsm=n Skipped=us' "$out/metadata-layout.stdout"
    grep -Fxq 'object row visibility: uh uh oh fh uh' "$out/metadata-layout.stdout"
    grep -Fxq 'virtual invoke end' "$out/metadata-layout.stdout"
    DN2CPP_BEFORE_VIRTUAL_INVOKE=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-virtual-invoke.stdout"
    sed '/^== virtual invoke ==/,$d' "$out/metadata-layout.stdout" > "$out/virtual-invoke-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-virtual-invoke.stdout") \
        <(strip_cr_win_file "$out/virtual-invoke-prefix.stdout")
    # .NET runs these bodies; this image stripped them, and each slot shape reports
    # that as a catchable NotSupportedException rather than aborting in its trap.
    DN2CPP_STRIPPED_OVERRIDES=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/stripped-overrides.stdout"
    local stripped_body="the receiver's body was stripped from this image; preserve it with a link.xml descriptor to reach it through reflection"
    grep -Fxq "stripped struct-returning slot: NotSupportedException 0x80131515 System.Globalization.GregorianCalendar.AddYears: $stripped_body" "$out/stripped-overrides.stdout"
    grep -Fxq "stripped slot: NotSupportedException 0x80131515 System.Globalization.GregorianCalendar.GetDayOfMonth: $stripped_body" "$out/stripped-overrides.stdout"
    grep -Fxq "stripped interface struct-returning slot: NotSupportedException 0x80131515 System.DBNull.ToDateTime: $stripped_body" "$out/stripped-overrides.stdout"
    grep -Fxq "stripped interface slot: NotSupportedException 0x80131515 System.DBNull.ToInt32: $stripped_body" "$out/stripped-overrides.stdout"
    grep -Fxq "stripped slot, delegate: NotSupportedException 0x80131515 System.Globalization.GregorianCalendar.GetDayOfMonth: $stripped_body" "$out/stripped-overrides.stdout"
    grep -Fxq 'stripped end' "$out/stripped-overrides.stdout"
    grep -Fxq '== field validation ==' "$out/metadata-layout.stdout"
    grep -Fxq 'null receiver, get int: TargetException 0x80131603 Non-static field requires a target.' "$out/metadata-layout.stdout"
    grep -Fxq "stranger receiver, set: ArgumentException 0x80070057 Field 'Number' defined on type 'ReflectFieldValidationSubset.Target' is not a field on the target object which is of type 'ReflectFieldValidationSubset.Stranger'." "$out/metadata-layout.stdout"
    grep -Fxq 'boxed struct, set: Int32:8' "$out/metadata-layout.stdout"
    grep -Fxq 'null into struct: Spot:(0,null)' "$out/metadata-layout.stdout"
    grep -Fxq "string into int: ArgumentException 0x80070057 Object of type 'System.String' cannot be converted to type 'System.Int32'." "$out/metadata-layout.stdout"
    grep -Fxq 'int into nullable: Int32:5' "$out/metadata-layout.stdout"
    grep -Fxq 'null receiver, wrong value: TargetException 0x80131603 Non-static field requires a target.' "$out/metadata-layout.stdout"
    grep -Fxq 'null into unnamed enum: String:stored' "$out/metadata-layout.stdout"
    grep -Fxq 'const Native IntPtr literal=True initonly=False: Int32:7' "$out/metadata-layout.stdout"
    grep -Fxq 'const bits: String:FFC00000/8000000000000000' "$out/metadata-layout.stdout"
    grep -Fxq 'const set, wrong value: FieldAccessException 0x80131507 Cannot set a constant field.' "$out/metadata-layout.stdout"
    grep -Fxq "readonly static set: FieldAccessException 0x80131507 Cannot set initonly static field 'Count' after type 'ReflectFieldValidationSubset.ReadOnlyStatics' is initialized." "$out/metadata-layout.stdout"
    grep -Fxq "nested readonly set: FieldAccessException 0x80131507 Cannot set initonly static field 'Value' after type 'Nested' is initialized." "$out/metadata-layout.stdout"
    grep -Fxq 'nullable get, no value: null' "$out/metadata-layout.stdout"
    grep -Fxq 'unnamed field type: String:ReflectFieldValidationSubset.Unnamed' "$out/metadata-layout.stdout"
    grep -Fxq 'unnamed return invoke: UnnamedRet:R1' "$out/metadata-layout.stdout"
    grep -Fxq 'unnamed generic argument: String:ReflectFieldValidationSubset.Marker`1[ReflectFieldValidationSubset.UnnamedArg]' "$out/metadata-layout.stdout"
    grep -Fxq 'enum value__ set: Level:High' "$out/metadata-layout.stdout"
    grep -Fxq 'raw long enum const: Int64:5' "$out/metadata-layout.stdout"
    grep -Fxq 'raw decimal const: InvalidOperationException 0x80131509 Operation is not valid due to the current state of the object.' "$out/metadata-layout.stdout"
    grep -Fxq 'field validation end' "$out/metadata-layout.stdout"
    DN2CPP_BEFORE_FIELD_VALIDATION=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-field-validation.stdout"
    sed '/^== field validation ==/,$d' "$out/metadata-layout.stdout" > "$out/field-validation-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-field-validation.stdout") \
        <(strip_cr_win_file "$out/field-validation-prefix.stdout")
    grep -Fxq '== generic virtual invoke ==' "$out/metadata-layout.stdout"
    grep -Fxq 'direct interface calls: struct:9:Int32|struct:9:String|fallback:Int32|concrete:Int32' "$out/metadata-layout.stdout"
    grep -Fxq 'interface delegate, struct: struct:9:Int32/GvmStructPick.Pick' "$out/metadata-layout.stdout"
    grep -Fxq 'root row, leaf: leaf:Int32' "$out/metadata-layout.stdout"
    grep -Fxq 'mid row, leaf: leaf:Int32' "$out/metadata-layout.stdout"
    grep -Fxq 'root row, hider leaf: mid:Int32' "$out/metadata-layout.stdout"
    grep -Fxq 'hider row, hider leaf: hider-leaf:Int32' "$out/metadata-layout.stdout"
    grep -Fxq 'abstract row, reference argument: square<String>:x' "$out/metadata-layout.stdout"
    grep -Fxq 'base-call row, override: loud:Int64' "$out/metadata-layout.stdout"
    grep -Fxq 'generic class row, value class argument: wrapper<Int32,String>:2/b' "$out/metadata-layout.stdout"
    grep -Fxq 'minted receiver: minted:Int64/Int32' "$out/metadata-layout.stdout"
    grep -Fxq 'interface row, struct: struct:4:Int32' "$out/metadata-layout.stdout"
    grep -Fxq 'abstract implementation row: concrete:Int32' "$out/metadata-layout.stdout"
    grep -Fxq 'default row, derived interface body: fancy-fallback:Int32' "$out/metadata-layout.stdout"
    grep -Fxq 'closed delegate, root row: leaf:Int32/GvmLeaf.Tag' "$out/metadata-layout.stdout"
    grep -Fxq 'closed delegate, abstract row: square<Int32>:6/GvmSquare.Kind' "$out/metadata-layout.stdout"
    grep -Fxq 'closed delegate, minted receiver: minted:Int64/Int32/True' "$out/metadata-layout.stdout"
    grep -Fxq 'closed delegate, derived interface default: fancy-fallback:Int32/IGvmFancyPick.ReflectVirtualInvokeSubset.IGvmPick.Fallback' "$out/metadata-layout.stdout"
    grep -Fxq 'open delegate, sealed override row: NotSupportedException 0x80131515 Specified method is not supported.' "$out/metadata-layout.stdout"
    grep -Fxq 'GetMethod, override: GvmLeaf.Pair' "$out/metadata-layout.stdout"
    grep -Fxq 'GetMethod, new slot: AmbiguousMatchException' "$out/metadata-layout.stdout"
    grep -Fxq 'GetMethods, hider chain: GvmHiderLeaf.Tag,GvmMid.Tag' "$out/metadata-layout.stdout"
    grep -Fxq 'base definition, hider chain: GvmHider.Tag/True/GvmHider' "$out/metadata-layout.stdout"
    grep -Fxq 'null-bound delegate, generic virtual override row: leaf:Int32' "$out/metadata-layout.stdout"
    grep -Fxq 'null-bound delegate, abstract generic virtual row: BadImageFormatException 0x8007000B' "$out/metadata-layout.stdout"
    grep -Fxq 'open delegate, null receiver, non-virtual row: plain' "$out/metadata-layout.stdout"
    grep -Fxq 'generic virtual invoke end' "$out/metadata-layout.stdout"
    DN2CPP_BEFORE_GENERIC_VIRTUAL_INVOKE=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-generic-virtual-invoke.stdout"
    sed '/^== generic virtual invoke ==/,$d' "$out/metadata-layout.stdout" > "$out/generic-virtual-invoke-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-generic-virtual-invoke.stdout") \
        <(strip_cr_win_file "$out/generic-virtual-invoke-prefix.stdout")
    grep -Fxq '== ambiguous match messages ==' "$out/metadata-layout.stdout"
    grep -Fxq "GetMethod overloads: 8000211D Ambiguous match found for 'AmbiguousMatchMessageSubset.Overloads Void M(Int32)'." \
        "$out/metadata-layout.stdout"
    grep -Fxq "member attribute: 8000211D Multiple custom attributes of the same type 'GetInterfaceSubset.BaseAttr' found." \
        "$out/metadata-layout.stdout"
    DN2CPP_BEFORE_AMBIGUOUS_MESSAGES=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-ambiguous-messages.stdout"
    sed '/^== ambiguous match messages ==/,$d' "$out/metadata-layout.stdout" > "$out/ambiguous-messages-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-ambiguous-messages.stdout") \
        <(strip_cr_win_file "$out/ambiguous-messages-prefix.stdout")

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
    corelib_diff_gate ReflectInvoke --no-ildiet

reflection_layout_axis=overrides
DN2CPP_OUT_SUFFIX="${DN2CPP_OUT_SUFFIX:-}-metadata-overrides" \
    corelib_diff_gate ReflectInvoke --no-ildiet \
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
    corelib_diff_gate ReflectInvoke --no-ildiet --no-metadata-compression \
        --reflection-metadata 'ReflectMetadataLayoutSubset.NativeBase=packed' \
        --reflection-metadata 'ReflectMetadataCompressionSubset.Direct=packed' \
        --reflection-metadata 'System.String=packed'

# A global opt-out dominates packed selectors in either argument order.
uncompressed_reverse=artifacts/reflection-metadata-uncompressed-reverse
run_bounded invoke_cli "$_CG_APP" -r "$_CG_CORELIB" --no-ildiet \
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
    run_bounded invoke_cli "$_CG_APP" -r "$_CG_CORELIB" --no-ildiet \
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
    grep -Fxq 'ldftn-local-direct=2/Subtract' "$byref_dir/dotnet.stdout"
    sed '/^ldftn-local-begin/,$d' "$byref_dir/dotnet.stdout" > "$byref_dir/dotnet-prefix.stdout"
    diff -u <(strip_cr_win_file "$invalid_out/byref-prefix.stdout") \
        <(strip_cr_win_file "$byref_dir/dotnet-prefix.stdout")
    byref_status=0
    run_bounded invoke_cli "$byref_app" -r "$_CG_CORELIB" --no-ildiet \
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

# Exercise representation boundaries that C# metadata cannot express, using
# the production decoder and the same CMake/Ninja path as the parity binary.
codec_out=artifacts/reflection-metadata-codec
mkdir -p "$codec_out"
cp gates/fixtures/reflection-metadata-codec.cpp "$codec_out/generated.cpp"
printf '#pragma once\n' > "$codec_out/generated.h"
compile_console "$codec_out" MetadataCodec
assert_output "$("$codec_out/MetadataCodec$EXE_EXT")" "metadata codec boundaries OK"

# ReflectBindOnly calls CreateDelegate and no other reflection member, so the
# binding alone must reach the application bodies it binds. Its checks are the
# diff; the ReflectInvoke asserts above do not apply to it.
unset -f gate_extra_asserts
corelib_diff_gate ReflectBindOnly --no-ildiet
