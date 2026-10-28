namespace Dn2Cpp;

// The reflection API table. Emission, transpiler admission and ILDiet classify a call
// through this one table, so a member set or an overload shape cannot be recognized by
// one of them and missed by another. Classification reads raw metadata names only — no
// model types, no Cecil — and allocates nothing.
//
// Names are the forms RawSignatureProvider produces: namespace-qualified, '+' between
// nested types, the arity suffix kept, "[]" for a vector, "&" for a by-ref. A parent is
// the declaring type's definition name without type arguments.

/// <summary>What a reflection call does with the members it reaches.</summary>
internal enum ReflectionOpKind : byte
{
    /// <summary>Produces the identity of a type or member; no member body runs.</summary>
    TypeIdentity,
    /// <summary>Selects members by name, kind or signature; no member body runs.</summary>
    Lookup,
    /// <summary>Lists the members, parameters or types of an owner; no member body runs.</summary>
    Enumerate,
    /// <summary>Constructs attribute instances: their constructors and named setters run.</summary>
    AttributeInstances,
    /// <summary>Reads attribute metadata without constructing an attribute.</summary>
    AttributeData,
    /// <summary>Runs a member body chosen at run time.</summary>
    Execute,
    /// <summary>Binds a delegate to a method chosen at run time.</summary>
    Bind,
    /// <summary>Allocates a type chosen at run time and runs one of its constructors.</summary>
    Construct,
    /// <summary>Closes a generic definition over type arguments chosen at run time.</summary>
    Instantiate,
    /// <summary>Carries a reflection value from an operand to the result unchanged; it
    /// has no effect of its own.</summary>
    Transfer,
}

internal enum ReflectionOp : byte
{
    TypeGetType, AssemblyGetType, ObjectGetType, GetTypeFromHandle, MemberFromHandle,
    GetMethod, GetConstructor, GetProperty, GetField, GetEvent, GetMember, GetNestedType, GetInterface,
    TypeInitializer,
    GetMethods, GetConstructors, GetProperties, GetFields, GetEvents, GetMembers, GetNestedTypes,
    GetInterfaces, GetDefaultMembers, AssemblyGetTypes,
    Accessor, GetParameters, ParameterType, FieldType, PropertyType, ReturnType,
    GetElementType, MakeArrayType, GetGenericTypeDefinition, GetGenericArguments, BaseType,
    MakeGenericType, MakeGenericMethod,
    MethodInvoke, CtorInvoke, PropertyGet, PropertySet, FieldGet, FieldSet, EventAdd, EventRemove,
    InvokeMember,
    ActivatorCreate, ActivatorCreateGeneric, ActivatorCreateNamed, AssemblyCreateInstance, RuntimeTypeDefaultCtor, RuntimeTypeCreateInstanceOfT,
    CreateDelegate, CreateDelegateNamed, DynamicInvoke, DelegateMethod,
    GetCustomAttributes, IsDefined, GetCustomAttributesData,
    RunClassConstructor, TypeInfoView, SequenceElement, TypeHandle, GetUninitializedObject,
    MarshalPtrToStructure,
}

/// <summary>Which accessor an <see cref="ReflectionOp.Accessor"/> call selects.</summary>
internal enum ReflectionAccessor : byte { None, Getter, Setter, Adder, Remover, Raiser, All }

/// <summary>One classified reflection call. Each operand field holds the 0-based index of
/// the parameter that carries it (the receiver is not counted), or one of
/// <see cref="Absent"/>, <see cref="Receiver"/>, <see cref="MethodTypeArgument"/>,
/// <see cref="Unknown"/>. An absent operand takes the family default:
/// <see cref="DefaultFlags"/> for the binding flags, <see cref="DefaultInherit"/> for the
/// inherit flag, false for every other Boolean, null for the rest.</summary>
internal readonly record struct ReflectionApi(ReflectionOp Op, ReflectionOpKind Kind)
{
    /// <summary>The overload does not carry the operand.</summary>
    internal const sbyte Absent = -1;
    /// <summary>The operand is the call's receiver.</summary>
    internal const sbyte Receiver = -2;
    /// <summary>The operand is the call's first method type argument.</summary>
    internal const sbyte MethodTypeArgument = -3;
    /// <summary>The member belongs to the family but its parameter list matches no known
    /// overload, so nothing is known about the operand.</summary>
    internal const sbyte Unknown = -4;

    /// <summary>False when the parameter list matched no known overload; every operand
    /// field is then <see cref="Unknown"/>.</summary>
    public bool Recognized { get; init; } = true;
    /// <summary>The reflected subject: the Type, member, delegate, assembly, sequence or
    /// handle the call reads, or the Type a construction allocates.</summary>
    public sbyte Target { get; init; } = Absent;
    /// <summary>A String naming a member, a type (possibly assembly-qualified) or a method.</summary>
    public sbyte Name { get; init; } = Absent;
    /// <summary>The String naming the assembly of a named construction.</summary>
    public sbyte AssemblyName { get; init; } = Absent;
    /// <summary>An Int32: a lookup's generic parameter count, or the rank MakeArrayType makes.</summary>
    public sbyte Arity { get; init; } = Absent;
    public sbyte Flags { get; init; } = Absent;
    public sbyte Binder { get; init; } = Absent;
    public sbyte CallConv { get; init; } = Absent;
    /// <summary>A lone Type a property lookup matches the property type against.</summary>
    public sbyte ReturnType { get; init; } = Absent;
    /// <summary>A Type[]: a lookup's parameter types or the type arguments of an instantiation.</summary>
    public sbyte Types { get; init; } = Absent;
    /// <summary>An Object[]: invocation or constructor arguments, or a property index.</summary>
    public sbyte Args { get; init; } = Absent;
    /// <summary>The instance an invocation, accessor or bound delegate runs against.</summary>
    public sbyte Instance { get; init; } = Absent;
    /// <summary>The value a setter stores, the handler an event accessor receives, or the
    /// default a sequence element read returns.</summary>
    public sbyte Value { get; init; } = Absent;
    public sbyte DelegateType { get; init; } = Absent;
    /// <summary>The attribute type an attribute read filters on.</summary>
    public sbyte AttributeType { get; init; } = Absent;
    public sbyte MemberTypes { get; init; } = Absent;
    public sbyte NonPublic { get; init; } = Absent;
    public sbyte ThrowOnError { get; init; } = Absent;
    public sbyte IgnoreCase { get; init; } = Absent;
    public sbyte Inherit { get; init; } = Absent;
    /// <summary>Type.GetType's assembly resolver; the call is understood only when it is null.</summary>
    public sbyte AssemblyResolver { get; init; } = Absent;
    /// <summary>Type.GetType's type resolver; the call is understood only when it is null.</summary>
    public sbyte TypeResolver { get; init; } = Absent;
    public ReflectionAccessor Accessor { get; init; } = ReflectionAccessor.None;
    /// <summary>The BindingFlags .NET applies when <see cref="Flags"/> is absent. For
    /// InvokeMember it fills in an operand that names no visibility or storage bit.</summary>
    public int DefaultFlags { get; init; }
    public bool DefaultInherit { get; init; }
}

/// <summary>The per-parameter vocabulary of the member-lookup overloads: each parameter
/// type of a lookup overload maps to one slot, and a family admits a fixed slot set.</summary>
internal enum LookupSlot : byte
{
    None, Name, Arity, Flags, Binder, CallConv, ReturnType, Types, Modifiers, Args, Bool, Culture, MemberTypes,
}

internal static class ReflectionApis
{
    internal const int Public = 16, NonPublic = 32, Instance = 4, Static = 8;
    internal const int DeclaredOnly = 2, FlattenHierarchy = 64;
    internal const int DefaultLookup = Public | Instance | Static;
    internal const int PublicInstance = Public | Instance;
    internal const int Everything = Public | NonPublic | Instance | Static;
    internal const int Declared = Everything | DeclaredOnly;
    internal const int TypeInitializerLookup = NonPublic | Static;

    private const string TString = "System.String", TInt32 = "System.Int32", TBool = "System.Boolean";
    private const string TObject = "System.Object", TObjects = "System.Object[]";
    private const string TType = "System.Type", TTypes = "System.Type[]";
    private const string TFlags = "System.Reflection.BindingFlags", TBinder = "System.Reflection.Binder";
    private const string TCulture = "System.Globalization.CultureInfo";
    private const string TModifiers = "System.Reflection.ParameterModifier[]";
    private const string TMethodInfo = "System.Reflection.MethodInfo", TDelegate = "System.Delegate";

    /// <summary>The lookup slot of one parameter type name, or None outside the vocabulary.</summary>
    internal static LookupSlot SlotOf(string typeName) => typeName switch
    {
        TString => LookupSlot.Name,
        TInt32 => LookupSlot.Arity,
        TFlags => LookupSlot.Flags,
        TBinder => LookupSlot.Binder,
        "System.Reflection.CallingConventions" => LookupSlot.CallConv,
        TType => LookupSlot.ReturnType,
        TTypes => LookupSlot.Types,
        TModifiers => LookupSlot.Modifiers,
        TObjects => LookupSlot.Args,
        TBool => LookupSlot.Bool,
        TCulture => LookupSlot.Culture,
        "System.Reflection.MemberTypes" => LookupSlot.MemberTypes,
        _ => LookupSlot.None,
    };

    /// <summary>Whether a member name can name an API <see cref="Classify"/> answers with
    /// a kind other than <see cref="ReflectionOpKind.Transfer"/>. A body whose call
    /// tokens all fail it holds no reflection site.</summary>
    internal static bool IsCandidateName(string name) => name is
        "GetType" or "GetTypeFromHandle" or "GetMethodFromHandle" or "GetFieldFromHandle"
        or "GetMethod" or "GetConstructor" or "GetProperty" or "GetField" or "GetEvent" or "GetMember"
        or "GetNestedType" or "GetInterface" or "get_TypeInitializer"
        or "GetMethods" or "GetConstructors" or "GetProperties" or "GetFields" or "GetEvents" or "GetMembers"
        or "GetNestedTypes" or "GetInterfaces" or "GetDefaultMembers"
        or "GetTypes" or "GetExportedTypes" or "get_ExportedTypes" or "get_DefinedTypes"
        or "GetDeclaredMethod" or "GetDeclaredMethods" or "GetDeclaredProperty" or "GetDeclaredField"
        or "GetDeclaredEvent" or "GetDeclaredNestedType"
        or "get_DeclaredConstructors" or "get_DeclaredMethods" or "get_DeclaredProperties" or "get_DeclaredFields"
        or "get_DeclaredEvents" or "get_DeclaredMembers" or "get_DeclaredNestedTypes" or "get_ImplementedInterfaces"
        or "GetRuntimeMethod" or "GetRuntimeMethods" or "GetRuntimeProperty" or "GetRuntimeProperties"
        or "GetRuntimeField" or "GetRuntimeFields" or "GetRuntimeEvent" or "GetRuntimeEvents" or "GetMethodInfo"
        or "GetGetMethod" or "GetSetMethod" or "get_GetMethod" or "get_SetMethod" or "GetAccessors"
        or "GetAddMethod" or "GetRemoveMethod" or "GetRaiseMethod" or "get_AddMethod" or "get_RemoveMethod"
        or "get_RaiseMethod"
        or "GetParameters" or "get_ParameterType" or "get_FieldType" or "get_PropertyType" or "get_ReturnType"
        or "GetElementType" or "MakeArrayType" or "GetGenericTypeDefinition" or "GetGenericArguments"
        or "get_GenericTypeArguments" or "get_BaseType" or "MakeGenericType" or "MakeGenericMethod"
        or "Invoke" or "GetValue" or "SetValue" or "AddEventHandler" or "RemoveEventHandler" or "InvokeMember"
        or "CreateInstance" or "CreateInstanceDefaultCtor" or "CreateInstanceOfT" or "CreateDelegate" or "DynamicInvoke" or "get_Method"
        or "GetCustomAttributes" or "GetCustomAttribute" or "IsDefined" or "GetCustomAttributesData"
        or "get_CustomAttributes" or "RunClassConstructor" or "GetUninitializedObject"
        or "PtrToStructure";

    /// <summary>Classifies a call to <paramref name="parent"/>::<paramref name="name"/>, or
    /// null when it is no reflection API. A member of a known family whose parameter list
    /// matches no known overload still classifies, with <see cref="ReflectionApi.Recognized"/>
    /// false, so an unfamiliar overload is never mistaken for an ordinary call.</summary>
    internal static ReflectionApi? Classify(string parent, string name, ReadOnlySpan<string> parameters,
        bool hasThis, int methodArity)
    {
        if (IsAttributeName(name) && IsAttributeParent(parent))
            return Attribute(parent, name, parameters, hasThis, methodArity);
        var ps = parameters;
        bool plain = hasThis && methodArity == 0;
        bool onStatic = !hasThis && methodArity == 0;
        return parent switch
        {
            TType => TypeMember(name, ps, hasThis, methodArity),
            "System.Reflection.TypeInfo" => TypeInfoMember(name, ps, plain),
            "System.Reflection.RuntimeReflectionExtensions" => RuntimeExtension(name, ps, onStatic),
            "System.Reflection.IntrospectionExtensions" when name == "GetTypeInfo" =>
                Shape(OnOperand(ReflectionOp.TypeInfoView, ReflectionOpKind.Transfer, 0), onStatic && Sig(ps, TType)),
            "System.Activator" when name == "CreateInstance" => Activator(ps, hasThis, methodArity),
            "System.RuntimeType" when name == "CreateInstanceOfT" =>
                Shape(OnOperand(ReflectionOp.RuntimeTypeCreateInstanceOfT, ReflectionOpKind.Construct, ReflectionApi.Receiver)
                    with { DefaultFlags = PublicInstance }, plain && ps.Length == 0),
            "System.RuntimeType" when name == "CreateInstanceDefaultCtor" =>
                Shape(OnOperand(ReflectionOp.RuntimeTypeDefaultCtor, ReflectionOpKind.Construct, ReflectionApi.Receiver)
                    with { DefaultFlags = PublicInstance }, plain && Sig(ps, TBool, TBool)),
            "System.Reflection.Assembly" => AssemblyMember(name, ps, plain, module: false),
            "System.Reflection.Module" => AssemblyMember(name, ps, plain, module: true),
            TDelegate => DelegateMember(name, ps, hasThis, methodArity),
            TMethodInfo => MethodInfoMember(name, ps, hasThis, methodArity),
            "System.Reflection.MethodBase" => MethodBaseMember(name, ps, hasThis, methodArity),
            "System.Reflection.ConstructorInfo" when name == "Invoke" => CtorInvoke(ps, plain),
            "System.Reflection.PropertyInfo" => PropertyMember(name, ps, plain),
            "System.Reflection.FieldInfo" => FieldMember(name, ps, hasThis, methodArity),
            "System.Reflection.EventInfo" => EventMember(name, ps, plain),
            "System.Reflection.ParameterInfo" when name == "get_ParameterType" =>
                OnReceiver(ReflectionOp.ParameterType, ReflectionOpKind.TypeIdentity, ps, plain),
            "System.Runtime.CompilerServices.RuntimeHelpers" when name == "RunClassConstructor" =>
                Shape(OnOperand(ReflectionOp.RunClassConstructor, ReflectionOpKind.Execute, 0),
                    onStatic && Sig(ps, "System.RuntimeTypeHandle")),
            // Allocates the operand type and runs its initializer, but no constructor.
            "System.Runtime.CompilerServices.RuntimeHelpers" when name == "GetUninitializedObject" =>
                Shape(OnOperand(ReflectionOp.GetUninitializedObject, ReflectionOpKind.Construct, 0), onStatic && Sig(ps, TType)),
            "System.Runtime.InteropServices.Marshal" when name == "PtrToStructure"
                && onStatic && Sig(ps, "System.IntPtr", TType) =>
                OnOperand(ReflectionOp.MarshalPtrToStructure, ReflectionOpKind.Construct, 1),
            TObject when name == "GetType" => OnReceiver(ReflectionOp.ObjectGetType, ReflectionOpKind.TypeIdentity, ps, plain),
            "System.Linq.Enumerable" => Enumerable(name, ps, hasThis, methodArity),
            "System.Collections.Generic.IEnumerable`1" or "System.Collections.IEnumerable"
                or "System.Collections.Generic.List`1" when name == "GetEnumerator" =>
                OnReceiver(ReflectionOp.SequenceElement, ReflectionOpKind.Transfer, ps, plain),
            "System.Collections.Generic.IEnumerator`1" or "System.Collections.IEnumerator"
                or "System.Collections.Generic.List`1+Enumerator" when name == "get_Current" =>
                OnReceiver(ReflectionOp.SequenceElement, ReflectionOpKind.Transfer, ps, plain),
            _ => null,
        };
    }

    private static ReflectionApi? TypeMember(string name, ReadOnlySpan<string> ps, bool hasThis, int arity)
    {
        const ReflectionOpKind L = ReflectionOpKind.Lookup, E = ReflectionOpKind.Enumerate;
        const ReflectionOpKind T = ReflectionOpKind.TypeIdentity;
        bool plain = hasThis && arity == 0;
        switch (name)
        {
            case "GetMethod":
                return Lookup(ReflectionOp.GetMethod, L, ps, plain, Slots(LookupSlot.Name, LookupSlot.Arity,
                    LookupSlot.Flags, LookupSlot.Binder, LookupSlot.CallConv, LookupSlot.Types, LookupSlot.Modifiers),
                    LookupSlot.Name, DefaultLookup);
            case "GetConstructor":
                return Lookup(ReflectionOp.GetConstructor, L, ps, plain, Slots(LookupSlot.Flags, LookupSlot.Binder,
                    LookupSlot.CallConv, LookupSlot.Types, LookupSlot.Modifiers), LookupSlot.Types, PublicInstance);
            case "GetProperty":
                return Lookup(ReflectionOp.GetProperty, L, ps, plain, Slots(LookupSlot.Name, LookupSlot.Flags,
                    LookupSlot.Binder, LookupSlot.CallConv, LookupSlot.ReturnType, LookupSlot.Types,
                    LookupSlot.Modifiers), LookupSlot.Name, DefaultLookup);
            case "GetField":
                return Lookup(ReflectionOp.GetField, L, ps, plain, Slots(LookupSlot.Name, LookupSlot.Flags),
                    LookupSlot.Name, DefaultLookup);
            case "GetEvent":
                return Lookup(ReflectionOp.GetEvent, L, ps, plain, Slots(LookupSlot.Name, LookupSlot.Flags),
                    LookupSlot.Name, DefaultLookup);
            case "GetNestedType":
                return Lookup(ReflectionOp.GetNestedType, L, ps, plain, Slots(LookupSlot.Name, LookupSlot.Flags),
                    LookupSlot.Name, DefaultLookup);
            case "GetMember":
                return Lookup(ReflectionOp.GetMember, L, ps, plain, Slots(LookupSlot.Name, LookupSlot.MemberTypes,
                    LookupSlot.Flags), LookupSlot.Name, DefaultLookup);
            case "GetInterface":
                return Lookup(ReflectionOp.GetInterface, L, ps, plain, Slots(LookupSlot.Name, LookupSlot.Bool),
                    LookupSlot.Name, 0);
            case "get_TypeInitializer":
                return OnReceiver(ReflectionOp.TypeInitializer, L, ps, plain, TypeInitializerLookup);
            case "GetMethods": return Plural(ReflectionOp.GetMethods, ps, plain, DefaultLookup);
            case "GetConstructors": return Plural(ReflectionOp.GetConstructors, ps, plain, PublicInstance);
            case "GetProperties": return Plural(ReflectionOp.GetProperties, ps, plain, DefaultLookup);
            case "GetFields": return Plural(ReflectionOp.GetFields, ps, plain, DefaultLookup);
            case "GetEvents": return Plural(ReflectionOp.GetEvents, ps, plain, DefaultLookup);
            case "GetNestedTypes": return Plural(ReflectionOp.GetNestedTypes, ps, plain, DefaultLookup);
            case "GetMembers": return Plural(ReflectionOp.GetMembers, ps, plain, DefaultLookup);
            case "GetInterfaces": return OnReceiver(ReflectionOp.GetInterfaces, E, ps, plain);
            // The default member name is looked up as GetMember(name) looks it up.
            case "GetDefaultMembers": return OnReceiver(ReflectionOp.GetDefaultMembers, E, ps, plain, DefaultLookup);
            case "GetElementType": return OnReceiver(ReflectionOp.GetElementType, T, ps, plain);
            // A RuntimeTypeHandle stands for the Type it carries.
            case "get_TypeHandle": return OnReceiver(ReflectionOp.TypeHandle, ReflectionOpKind.Transfer, ps, plain);
            case "GetGenericTypeDefinition": return OnReceiver(ReflectionOp.GetGenericTypeDefinition, T, ps, plain);
            case "GetGenericArguments" or "get_GenericTypeArguments":
                return OnReceiver(ReflectionOp.GetGenericArguments, T, ps, plain);
            case "get_BaseType": return OnReceiver(ReflectionOp.BaseType, T, ps, plain);
            case "MakeArrayType":
                if (plain && Sig(ps, TInt32))
                    return OnReceiver(ReflectionOp.MakeArrayType, T) with { Arity = 0 };
                return OnReceiver(ReflectionOp.MakeArrayType, T, ps, plain);
            case "MakeGenericType":
                return Shape(OnReceiver(ReflectionOp.MakeGenericType, ReflectionOpKind.Instantiate) with { Types = 0 },
                    plain && Sig(ps, TTypes));
            case "GetTypeFromHandle":
                return Shape(OnOperand(ReflectionOp.GetTypeFromHandle, T, 0),
                    !hasThis && arity == 0 && Sig(ps, "System.RuntimeTypeHandle"));
            case "GetType" when hasThis:
                return OnReceiver(ReflectionOp.ObjectGetType, T, ps, plain);
            case "GetType":
                return TypeGetType(ps, arity);
            case "InvokeMember":
                return InvokeMember(ps, plain);
        }
        return null;
    }

    private static ReflectionApi TypeGetType(ReadOnlySpan<string> ps, int arity)
    {
        var api = new ReflectionApi(ReflectionOp.TypeGetType, ReflectionOpKind.TypeIdentity) { Name = 0 };
        if (arity != 0 || ps.Length == 0 || ps[0] != TString)
            return Unrecognized(api);
        if (ps.Length == 1)
            return api;
        if (ps[1] == TBool)
            return ps.Length switch
            {
                2 => api with { ThrowOnError = 1 },
                3 when ps[2] == TBool => api with { ThrowOnError = 1, IgnoreCase = 2 },
                _ => Unrecognized(api),
            };
        if (ps.Length < 3 || !ps[1].StartsWith("System.Func`2<", StringComparison.Ordinal)
            || !ps[2].StartsWith("System.Func`4<", StringComparison.Ordinal))
            return Unrecognized(api);
        api = api with { AssemblyResolver = 1, TypeResolver = 2 };
        return ps.Length switch
        {
            3 => api,
            4 when ps[3] == TBool => api with { ThrowOnError = 3 },
            5 when ps[3] == TBool && ps[4] == TBool => api with { ThrowOnError = 3, IgnoreCase = 4 },
            _ => Unrecognized(api),
        };
    }

    private static ReflectionApi InvokeMember(ReadOnlySpan<string> ps, bool plain)
    {
        var api = new ReflectionApi(ReflectionOp.InvokeMember, ReflectionOpKind.Execute)
        {
            Target = ReflectionApi.Receiver, Name = 0, Flags = 1, Binder = 2, Instance = 3, Args = 4,
            DefaultFlags = DefaultLookup,
        };
        if (plain && Sig(ps, TString, TFlags, TBinder, TObject, TObjects))
            return api;
        if (plain && Sig(ps, TString, TFlags, TBinder, TObject, TObjects, TCulture))
            return api;
        if (plain && Sig(ps, TString, TFlags, TBinder, TObject, TObjects, TModifiers, TCulture, "System.String[]"))
            return api;
        return Unrecognized(api);
    }

    private static ReflectionApi? TypeInfoMember(string name, ReadOnlySpan<string> ps, bool plain)
    {
        ReflectionOp? named = name switch
        {
            "GetDeclaredMethod" => ReflectionOp.GetMethod,
            "GetDeclaredMethods" => ReflectionOp.GetMethods,
            "GetDeclaredProperty" => ReflectionOp.GetProperty,
            "GetDeclaredField" => ReflectionOp.GetField,
            "GetDeclaredEvent" => ReflectionOp.GetEvent,
            "GetDeclaredNestedType" => ReflectionOp.GetNestedType,
            _ => null,
        };
        if (named is { } op)
            return Shape(OnReceiver(op, op == ReflectionOp.GetMethods ? ReflectionOpKind.Enumerate : ReflectionOpKind.Lookup)
                with { Name = 0, DefaultFlags = Declared }, plain && Sig(ps, TString));
        ReflectionOp? listed = name switch
        {
            "get_DeclaredConstructors" => ReflectionOp.GetConstructors,
            "get_DeclaredMethods" => ReflectionOp.GetMethods,
            "get_DeclaredProperties" => ReflectionOp.GetProperties,
            "get_DeclaredFields" => ReflectionOp.GetFields,
            "get_DeclaredEvents" => ReflectionOp.GetEvents,
            "get_DeclaredMembers" => ReflectionOp.GetMembers,
            "get_DeclaredNestedTypes" => ReflectionOp.GetNestedTypes,
            "get_ImplementedInterfaces" => ReflectionOp.GetInterfaces,
            _ => null,
        };
        if (listed is { } list)
            return OnReceiver(list, ReflectionOpKind.Enumerate, ps, plain, list == ReflectionOp.GetInterfaces ? 0 : Declared);
        if (name == "AsType")
            return OnReceiver(ReflectionOp.TypeInfoView, ReflectionOpKind.Transfer, ps, plain);
        return null;
    }

    // The extensions take the searched Type first; the named forms use the default
    // lookup and the plural forms every visibility.
    private static ReflectionApi? RuntimeExtension(string name, ReadOnlySpan<string> ps, bool onStatic)
    {
        const ReflectionOpKind L = ReflectionOpKind.Lookup;
        bool named = onStatic && Sig(ps, TType, TString);
        bool listed = onStatic && Sig(ps, TType);
        ReflectionOp op;
        switch (name)
        {
            case "GetRuntimeMethod":
                return Shape(OnOperand(ReflectionOp.GetMethod, L, 0) with { Name = 1, Types = 2, DefaultFlags = DefaultLookup },
                    onStatic && Sig(ps, TType, TString, TTypes));
            case "GetRuntimeProperty": op = ReflectionOp.GetProperty; break;
            case "GetRuntimeField": op = ReflectionOp.GetField; break;
            case "GetRuntimeEvent": op = ReflectionOp.GetEvent; break;
            case "GetRuntimeMethods": return Shape(Everywhere(ReflectionOp.GetMethods), listed);
            case "GetRuntimeProperties": return Shape(Everywhere(ReflectionOp.GetProperties), listed);
            case "GetRuntimeFields": return Shape(Everywhere(ReflectionOp.GetFields), listed);
            case "GetRuntimeEvents": return Shape(Everywhere(ReflectionOp.GetEvents), listed);
            case "GetMethodInfo":
                return Shape(OnOperand(ReflectionOp.DelegateMethod, L, 0), onStatic && Sig(ps, TDelegate));
            default: return null;
        }
        return Shape(OnOperand(op, L, 0) with { Name = 1, DefaultFlags = DefaultLookup }, named);

        static ReflectionApi Everywhere(ReflectionOp op) =>
            OnOperand(op, ReflectionOpKind.Enumerate, 0) with { DefaultFlags = Everything };
    }

    private static ReflectionApi Activator(ReadOnlySpan<string> ps, bool hasThis, int arity)
    {
        const ReflectionOpKind C = ReflectionOpKind.Construct;
        if (arity == 1)
            return Shape(OnOperand(ReflectionOp.ActivatorCreateGeneric, C, ReflectionApi.MethodTypeArgument)
                with { DefaultFlags = PublicInstance }, !hasThis && ps.Length == 0);
        if (ps.Length >= 2 && ps[0] == TString && ps[1] == TString)
        {
            var named = new ReflectionApi(ReflectionOp.ActivatorCreateNamed, C)
                { AssemblyName = 0, Name = 1, DefaultFlags = PublicInstance };
            if (hasThis || arity != 0)
                return Unrecognized(named);
            if (Sig(ps, TString, TString) || Sig(ps, TString, TString, TObjects))
                return named;
            if (Sig(ps, TString, TString, TBool, TFlags, TBinder, TObjects, TCulture, TObjects))
                return named with { IgnoreCase = 2, Flags = 3, Binder = 4, Args = 5 };
            return Unrecognized(named);
        }
        var api = OnOperand(ReflectionOp.ActivatorCreate, C, 0) with { DefaultFlags = PublicInstance };
        if (hasThis || arity != 0)
            return Unrecognized(api);
        if (Sig(ps, TType))
            return api;
        if (Sig(ps, TType, TBool) || Sig(ps, TType, TBool, TBool))
            return api with { NonPublic = 1 };
        if (Sig(ps, TType, TObjects) || Sig(ps, TType, TObjects, TObjects))
            return api with { Args = 1 };
        if (Sig(ps, TType, TFlags, TBinder, TObjects, TCulture)
            || Sig(ps, TType, TFlags, TBinder, TObjects, TCulture, TObjects))
            return api with { Flags = 1, Binder = 2, Args = 3 };
        return Unrecognized(api);
    }

    // Module.GetType(string, bool) takes ignoreCase where Assembly.GetType takes throwOnError.
    private static ReflectionApi? AssemblyMember(string name, ReadOnlySpan<string> ps, bool plain, bool module)
    {
        const ReflectionOpKind E = ReflectionOpKind.Enumerate;
        switch (name)
        {
            case "GetType":
            {
                var api = OnReceiver(ReflectionOp.AssemblyGetType, ReflectionOpKind.TypeIdentity) with { Name = 0 };
                if (!plain || ps.Length == 0 || ps[0] != TString)
                    return Unrecognized(api);
                return ps.Length switch
                {
                    1 => api,
                    2 when ps[1] == TBool => module ? api with { IgnoreCase = 1 } : api with { ThrowOnError = 1 },
                    3 when ps[1] == TBool && ps[2] == TBool => api with { ThrowOnError = 1, IgnoreCase = 2 },
                    _ => Unrecognized(api),
                };
            }
            case "GetTypes":
                return OnReceiver(ReflectionOp.AssemblyGetTypes, E, ps, plain, Public | NonPublic);
            case "GetExportedTypes" or "get_ExportedTypes" when !module:
                return OnReceiver(ReflectionOp.AssemblyGetTypes, E, ps, plain, Public);
            case "get_DefinedTypes" when !module:
                return OnReceiver(ReflectionOp.AssemblyGetTypes, E, ps, plain, Public | NonPublic);
            case "CreateInstance" when !module:
            {
                var api = OnReceiver(ReflectionOp.AssemblyCreateInstance, ReflectionOpKind.Construct)
                    with { Name = 0, DefaultFlags = PublicInstance };
                if (plain && Sig(ps, TString))
                    return api;
                if (plain && Sig(ps, TString, TBool))
                    return api with { IgnoreCase = 1 };
                if (plain && Sig(ps, TString, TBool, TFlags, TBinder, TObjects, TCulture, TObjects))
                    return api with { IgnoreCase = 1, Flags = 2, Binder = 3, Args = 4 };
                return Unrecognized(api);
            }
        }
        return null;
    }

    private static ReflectionApi? DelegateMember(string name, ReadOnlySpan<string> ps, bool hasThis, int arity)
    {
        bool plain = hasThis && arity == 0;
        switch (name)
        {
            case "DynamicInvoke":
                return Shape(OnReceiver(ReflectionOp.DynamicInvoke, ReflectionOpKind.Execute) with { Args = 0 },
                    plain && Sig(ps, TObjects));
            case "get_Method":
                return OnReceiver(ReflectionOp.DelegateMethod, ReflectionOpKind.Lookup, ps, plain);
            case "CreateDelegate":
                return DelegateCreate(ps, hasThis, arity);
        }
        return null;
    }

    // A by-name binding searches the target type and every base type for a method of any
    // visibility, inherited private ones included, so it does not follow ReflectionLookupRules.
    private static ReflectionApi DelegateCreate(ReadOnlySpan<string> ps, bool hasThis, int arity)
    {
        const ReflectionOpKind B = ReflectionOpKind.Bind;
        var bound = new ReflectionApi(ReflectionOp.CreateDelegate, B) { DelegateType = 0 };
        if (hasThis || arity != 0 || ps.Length < 2 || ps[0] != TType)
            return Unrecognized(bound);
        if (Sig(ps, TType, TMethodInfo))
            return bound with { Target = 1 };
        if (Sig(ps, TType, TMethodInfo, TBool))
            return bound with { Target = 1, ThrowOnError = 2 };
        if (Sig(ps, TType, TObject, TMethodInfo))
            return bound with { Target = 2, Instance = 1 };
        if (Sig(ps, TType, TObject, TMethodInfo, TBool))
            return bound with { Target = 2, Instance = 1, ThrowOnError = 3 };
        if (ps.Length < 3 || ps[2] != TString || ps[1] is not (TObject or TType))
            return Unrecognized(bound);
        bool onInstance = ps[1] == TObject;
        var named = new ReflectionApi(ReflectionOp.CreateDelegateNamed, B)
        {
            DelegateType = 0, Target = 1, Name = 2, Instance = onInstance ? (sbyte)1 : ReflectionApi.Absent,
            DefaultFlags = Public | NonPublic | (onInstance ? Instance : Static),
        };
        return ps.Length switch
        {
            3 => named,
            4 when ps[3] == TBool => named with { IgnoreCase = 3 },
            5 when ps[3] == TBool && ps[4] == TBool => named with { IgnoreCase = 3, ThrowOnError = 4 },
            _ => Unrecognized(named),
        };
    }

    private static ReflectionApi? MethodInfoMember(string name, ReadOnlySpan<string> ps, bool hasThis, int arity)
    {
        bool plain = hasThis && arity == 0;
        switch (name)
        {
            case "Invoke":
                return MethodInvoke(ps, plain);
            case "CreateDelegate":
            {
                var api = OnReceiver(ReflectionOp.CreateDelegate, ReflectionOpKind.Bind);
                if (!hasThis)
                    return Unrecognized(api);
                if (arity == 1 && ps.Length == 0)
                    return api with { DelegateType = ReflectionApi.MethodTypeArgument };
                if (arity == 1 && Sig(ps, TObject))
                    return api with { DelegateType = ReflectionApi.MethodTypeArgument, Instance = 0 };
                if (arity == 0 && Sig(ps, TType))
                    return api with { DelegateType = 0 };
                if (arity == 0 && Sig(ps, TType, TObject))
                    return api with { DelegateType = 0, Instance = 1 };
                return Unrecognized(api);
            }
            case "MakeGenericMethod":
                return Shape(OnReceiver(ReflectionOp.MakeGenericMethod, ReflectionOpKind.Instantiate) with { Types = 0 },
                    plain && Sig(ps, TTypes));
            case "get_ReturnType":
                return OnReceiver(ReflectionOp.ReturnType, ReflectionOpKind.TypeIdentity, ps, plain);
        }
        return null;
    }

    private static ReflectionApi? MethodBaseMember(string name, ReadOnlySpan<string> ps, bool hasThis, int arity)
    {
        switch (name)
        {
            case "Invoke":
                return MethodInvoke(ps, hasThis && arity == 0);
            case "GetParameters":
                return OnReceiver(ReflectionOp.GetParameters, ReflectionOpKind.Enumerate, ps, hasThis && arity == 0);
            case "GetMethodFromHandle":
                return Shape(OnOperand(ReflectionOp.MemberFromHandle, ReflectionOpKind.TypeIdentity, 0),
                    !hasThis && arity == 0 && (Sig(ps, "System.RuntimeMethodHandle")
                        || Sig(ps, "System.RuntimeMethodHandle", "System.RuntimeTypeHandle")));
        }
        return null;
    }

    private static ReflectionApi MethodInvoke(ReadOnlySpan<string> ps, bool plain)
    {
        var api = OnReceiver(ReflectionOp.MethodInvoke, ReflectionOpKind.Execute);
        if (plain && Sig(ps, TObject, TObjects))
            return api with { Instance = 0, Args = 1 };
        if (plain && Sig(ps, TObject, TFlags, TBinder, TObjects, TCulture))
            return api with { Instance = 0, Flags = 1, Binder = 2, Args = 3 };
        return Unrecognized(api);
    }

    private static ReflectionApi CtorInvoke(ReadOnlySpan<string> ps, bool plain)
    {
        var api = OnReceiver(ReflectionOp.CtorInvoke, ReflectionOpKind.Construct);
        if (plain && Sig(ps, TObjects))
            return api with { Args = 0 };
        if (plain && Sig(ps, TFlags, TBinder, TObjects, TCulture))
            return api with { Flags = 0, Binder = 1, Args = 2 };
        return Unrecognized(api);
    }

    private static ReflectionApi? PropertyMember(string name, ReadOnlySpan<string> ps, bool plain)
    {
        switch (name)
        {
            case "GetValue":
            {
                var api = OnReceiver(ReflectionOp.PropertyGet, ReflectionOpKind.Execute) with { Instance = 0 };
                if (plain && Sig(ps, TObject))
                    return api;
                if (plain && Sig(ps, TObject, TObjects))
                    return api with { Args = 1 };
                if (plain && Sig(ps, TObject, TFlags, TBinder, TObjects, TCulture))
                    return api with { Flags = 1, Binder = 2, Args = 3 };
                return Unrecognized(api);
            }
            case "SetValue":
            {
                var api = OnReceiver(ReflectionOp.PropertySet, ReflectionOpKind.Execute) with { Instance = 0, Value = 1 };
                if (plain && Sig(ps, TObject, TObject))
                    return api;
                if (plain && Sig(ps, TObject, TObject, TObjects))
                    return api with { Args = 2 };
                if (plain && Sig(ps, TObject, TObject, TFlags, TBinder, TObjects, TCulture))
                    return api with { Flags = 2, Binder = 3, Args = 4 };
                return Unrecognized(api);
            }
            case "GetGetMethod" or "get_GetMethod":
                return AccessorOf(ReflectionAccessor.Getter, name[0] == 'G', ps, plain);
            case "GetSetMethod" or "get_SetMethod":
                return AccessorOf(ReflectionAccessor.Setter, name[0] == 'G', ps, plain);
            case "GetAccessors":
                return AccessorOf(ReflectionAccessor.All, true, ps, plain);
            case "get_PropertyType":
                return OnReceiver(ReflectionOp.PropertyType, ReflectionOpKind.TypeIdentity, ps, plain);
        }
        return null;
    }

    private static ReflectionApi? FieldMember(string name, ReadOnlySpan<string> ps, bool hasThis, int arity)
    {
        bool plain = hasThis && arity == 0;
        switch (name)
        {
            case "GetValue":
                return Shape(OnReceiver(ReflectionOp.FieldGet, ReflectionOpKind.Execute) with { Instance = 0 },
                    plain && Sig(ps, TObject));
            case "SetValue":
            {
                var api = OnReceiver(ReflectionOp.FieldSet, ReflectionOpKind.Execute) with { Instance = 0, Value = 1 };
                if (plain && Sig(ps, TObject, TObject))
                    return api;
                if (plain && Sig(ps, TObject, TObject, TFlags, TBinder, TCulture))
                    return api with { Flags = 2, Binder = 3 };
                return Unrecognized(api);
            }
            case "get_FieldType":
                return OnReceiver(ReflectionOp.FieldType, ReflectionOpKind.TypeIdentity, ps, plain);
            case "GetFieldFromHandle":
                return Shape(OnOperand(ReflectionOp.MemberFromHandle, ReflectionOpKind.TypeIdentity, 0),
                    !hasThis && arity == 0 && (Sig(ps, "System.RuntimeFieldHandle")
                        || Sig(ps, "System.RuntimeFieldHandle", "System.RuntimeTypeHandle")));
        }
        return null;
    }

    private static ReflectionApi? EventMember(string name, ReadOnlySpan<string> ps, bool plain)
    {
        switch (name)
        {
            case "AddEventHandler" or "RemoveEventHandler":
                var op = name[0] == 'A' ? ReflectionOp.EventAdd : ReflectionOp.EventRemove;
                return Shape(OnReceiver(op, ReflectionOpKind.Execute) with { Instance = 0, Value = 1 },
                    plain && Sig(ps, TObject, TDelegate));
            case "GetAddMethod" or "get_AddMethod":
                return AccessorOf(ReflectionAccessor.Adder, name[0] == 'G', ps, plain);
            case "GetRemoveMethod" or "get_RemoveMethod":
                return AccessorOf(ReflectionAccessor.Remover, name[0] == 'G', ps, plain);
            case "GetRaiseMethod" or "get_RaiseMethod":
                return AccessorOf(ReflectionAccessor.Raiser, name[0] == 'G', ps, plain);
        }
        return null;
    }

    // GetXxxMethod() and GetXxxMethod(bool nonPublic) are the Get forms; the get_Xxx
    // properties take no argument and return the accessor of any visibility.
    private static ReflectionApi AccessorOf(ReflectionAccessor which, bool getForm, ReadOnlySpan<string> ps, bool plain)
    {
        var api = OnReceiver(ReflectionOp.Accessor, ReflectionOpKind.Lookup)
            with { Accessor = which, DefaultFlags = getForm ? Public : Public | NonPublic };
        if (plain && ps.Length == 0)
            return api;
        if (plain && getForm && Sig(ps, TBool))
            return api with { NonPublic = 0 };
        return Unrecognized(api);
    }

    // Each member keeps the elements of its source sequence, so the result carries a
    // superset of what the call returns. An OrDefault overload whose last parameter is the
    // element type returns that operand for an empty or unmatched sequence.
    private static ReflectionApi? Enumerable(string name, ReadOnlySpan<string> ps, bool hasThis, int arity) =>
        name switch
        {
            "FirstOrDefault" or "SingleOrDefault" or "LastOrDefault" =>
                Shape(OnOperand(ReflectionOp.SequenceElement, ReflectionOpKind.Transfer, 0) with
                {
                    Value = ps.Length is 2 or 3 && ps[ps.Length - 1] == "!!0" ? (sbyte)(ps.Length - 1) : ReflectionApi.Absent,
                }, !hasThis && arity == 1 && ps.Length >= 1),
            "First" or "Single" or "Last" or "ElementAt" or "ElementAtOrDefault" or "Where" or "ToArray" or "ToList"
                or "OfType" or "Cast" or "AsEnumerable" =>
                Shape(OnOperand(ReflectionOp.SequenceElement, ReflectionOpKind.Transfer, 0),
                    !hasThis && arity == 1 && ps.Length >= 1),
            _ => null,
        };

    private static bool IsAttributeName(string name) => name is
        "GetCustomAttributes" or "GetCustomAttribute" or "IsDefined" or "GetCustomAttributesData"
        or "get_CustomAttributes";

    private static bool IsAttributeParent(string parent) => parent is
        "System.Reflection.MemberInfo" or "System.Reflection.ParameterInfo" or "System.Attribute"
        or "System.Reflection.CustomAttributeExtensions" or "System.Reflection.CustomAttributeData"
        or TType or "System.Reflection.ICustomAttributeProvider" or "System.Reflection.Module"
        or "System.Reflection.ConstructorInfo" or "System.Reflection.MethodBase" or TMethodInfo
        or "System.Reflection.FieldInfo" or "System.Reflection.PropertyInfo" or "System.Reflection.EventInfo"
        or "System.Reflection.Assembly";

    private static bool IsAttributeElement(string type) => type is
        "System.Reflection.MemberInfo" or "System.Reflection.ParameterInfo" or "System.Reflection.Module"
        or "System.Reflection.Assembly";

    // Instance readers take (bool inherit), (Type, bool inherit) or nothing; the static
    // Attribute, CustomAttributeExtensions and CustomAttributeData readers take the element
    // first and default inherit to true.
    private static ReflectionApi Attribute(string parent, string name, ReadOnlySpan<string> ps, bool hasThis, int arity)
    {
        bool data = name is "GetCustomAttributesData" or "get_CustomAttributes"
            || parent == "System.Reflection.CustomAttributeData";
        var op = data ? ReflectionOp.GetCustomAttributesData
            : name == "IsDefined" ? ReflectionOp.IsDefined : ReflectionOp.GetCustomAttributes;
        var kind = op == ReflectionOp.GetCustomAttributes ? ReflectionOpKind.AttributeInstances : ReflectionOpKind.AttributeData;
        if (hasThis)
        {
            var api = OnReceiver(op, kind);
            if (arity != 0 || parent is "System.Attribute" or "System.Reflection.CustomAttributeExtensions"
                or "System.Reflection.CustomAttributeData")
                return Unrecognized(api);
            if (data)
                return ps.Length == 0 ? api : Unrecognized(api);
            if (op == ReflectionOp.GetCustomAttributes && Sig(ps, TBool))
                return api with { Inherit = 0 };
            if (Sig(ps, TType, TBool))
                return api with { AttributeType = 0, Inherit = 1 };
            return Unrecognized(api);
        }
        var element = OnOperand(op, kind, 0) with { DefaultInherit = !data };
        return StaticAttribute(element, parent, name, ps, arity, data);
    }

    private static ReflectionApi StaticAttribute(ReflectionApi api, string parent, string name, ReadOnlySpan<string> ps,
        int arity, bool data)
    {
        var op = api.Op;
        if (ps.Length == 0 || !IsAttributeElement(ps[0])
            || parent is not ("System.Attribute" or "System.Reflection.CustomAttributeExtensions"
                or "System.Reflection.CustomAttributeData"))
            return Unrecognized(api);
        if (data)
            return parent == "System.Reflection.CustomAttributeData" && name == "GetCustomAttributes"
                && arity == 0 && ps.Length == 1 ? api : Unrecognized(api);
        bool generic = arity == 1 && parent == "System.Reflection.CustomAttributeExtensions"
            && op == ReflectionOp.GetCustomAttributes;
        if (arity != 0 && !generic)
            return Unrecognized(api);
        if (generic)
            api = api with { AttributeType = ReflectionApi.MethodTypeArgument };
        bool many = name == "GetCustomAttributes";
        switch (ps.Length)
        {
            case 1 when generic || many:
                return api;
            case 2 when ps[1] == TType && !generic:
                return api with { AttributeType = 1 };
            case 2 when ps[1] == TBool && (generic || many):
                return api with { Inherit = 1 };
            case 3 when ps[1] == TType && ps[2] == TBool && !generic:
                return api with { AttributeType = 1, Inherit = 2 };
        }
        return Unrecognized(api);
    }

    // A vocabulary-matched lookup: every parameter maps to an admitted slot, no slot
    // repeats, and the required slot is present. The receiver is the Type searched.
    private static ReflectionApi Lookup(ReflectionOp op, ReflectionOpKind kind, ReadOnlySpan<string> ps,
        bool plain, int admitted, LookupSlot required, int defaultFlags)
    {
        var api = OnReceiver(op, kind) with { DefaultFlags = defaultFlags };
        if (!plain)
            return Unrecognized(api);
        int seen = 0;
        for (int i = 0; i < ps.Length; i++)
        {
            var slot = SlotOf(ps[i]);
            int bit = 1 << (int)slot;
            if (slot == LookupSlot.None || (admitted & bit) == 0 || (seen & bit) != 0)
                return Unrecognized(api);
            seen |= bit;
            sbyte at = (sbyte)i;
            api = slot switch
            {
                LookupSlot.Name => api with { Name = at },
                LookupSlot.Arity => api with { Arity = at },
                LookupSlot.Flags => api with { Flags = at },
                LookupSlot.Binder => api with { Binder = at },
                LookupSlot.CallConv => api with { CallConv = at },
                LookupSlot.ReturnType => api with { ReturnType = at },
                LookupSlot.Types => api with { Types = at },
                LookupSlot.MemberTypes => api with { MemberTypes = at },
                LookupSlot.Bool => api with { IgnoreCase = at },
                _ => api,
            };
        }
        if ((seen & (1 << (int)required)) == 0)
            return Unrecognized(api);
        return api;
    }

    private static ReflectionApi Plural(ReflectionOp op, ReadOnlySpan<string> ps, bool plain, int defaultFlags)
    {
        var api = OnReceiver(op, ReflectionOpKind.Enumerate) with { DefaultFlags = defaultFlags };
        if (plain && ps.Length == 0)
            return api;
        if (plain && Sig(ps, TFlags))
            return api with { Flags = 0 };
        return Unrecognized(api);
    }

    private static ReflectionApi OnReceiver(ReflectionOp op, ReflectionOpKind kind) =>
        new(op, kind) { Target = ReflectionApi.Receiver };

    private static ReflectionApi OnOperand(ReflectionOp op, ReflectionOpKind kind, sbyte target) =>
        new(op, kind) { Target = target };

    // A parameterless instance member whose receiver is the subject.
    private static ReflectionApi OnReceiver(ReflectionOp op, ReflectionOpKind kind, ReadOnlySpan<string> ps, bool plain,
        int defaultFlags = 0) =>
        Shape(OnReceiver(op, kind) with { DefaultFlags = defaultFlags }, plain && ps.Length == 0);

    private static int Slots(LookupSlot a, LookupSlot b) => (1 << (int)a) | (1 << (int)b);
    private static int Slots(LookupSlot a, LookupSlot b, LookupSlot c) => Slots(a, b) | (1 << (int)c);
    private static int Slots(LookupSlot a, LookupSlot b, LookupSlot c, LookupSlot d, LookupSlot e) =>
        Slots(a, b, c) | Slots(d, e);
    private static int Slots(LookupSlot a, LookupSlot b, LookupSlot c, LookupSlot d, LookupSlot e,
        LookupSlot f, LookupSlot g) => Slots(a, b, c, d, e) | Slots(f, g);

    // Unrecognized runs after every operand is set, so no operand of an unmatched
    // overload survives as a parameter index.
    private static ReflectionApi Shape(ReflectionApi api, bool recognized) => recognized ? api : Unrecognized(api);

    private static ReflectionApi Unrecognized(ReflectionApi api) => api with
    {
        Recognized = false,
        Target = ReflectionApi.Unknown, Name = ReflectionApi.Unknown, AssemblyName = ReflectionApi.Unknown,
        Arity = ReflectionApi.Unknown, Flags = ReflectionApi.Unknown, Binder = ReflectionApi.Unknown,
        CallConv = ReflectionApi.Unknown, ReturnType = ReflectionApi.Unknown, Types = ReflectionApi.Unknown,
        Args = ReflectionApi.Unknown, Instance = ReflectionApi.Unknown, Value = ReflectionApi.Unknown,
        DelegateType = ReflectionApi.Unknown, AttributeType = ReflectionApi.Unknown,
        MemberTypes = ReflectionApi.Unknown, NonPublic = ReflectionApi.Unknown,
        ThrowOnError = ReflectionApi.Unknown, IgnoreCase = ReflectionApi.Unknown, Inherit = ReflectionApi.Unknown,
        AssemblyResolver = ReflectionApi.Unknown, TypeResolver = ReflectionApi.Unknown,
    };

    private static bool Sig(ReadOnlySpan<string> p, string a) => p.Length == 1 && p[0] == a;
    private static bool Sig(ReadOnlySpan<string> p, string a, string b) => p.Length == 2 && p[0] == a && p[1] == b;
    private static bool Sig(ReadOnlySpan<string> p, string a, string b, string c) =>
        p.Length == 3 && p[0] == a && p[1] == b && p[2] == c;
    private static bool Sig(ReadOnlySpan<string> p, string a, string b, string c, string d) =>
        p.Length == 4 && p[0] == a && p[1] == b && p[2] == c && p[3] == d;
    private static bool Sig(ReadOnlySpan<string> p, string a, string b, string c, string d, string e) =>
        p.Length == 5 && p[0] == a && p[1] == b && p[2] == c && p[3] == d && p[4] == e;
    private static bool Sig(ReadOnlySpan<string> p, string a, string b, string c, string d, string e, string f) =>
        p.Length == 6 && p[0] == a && p[1] == b && p[2] == c && p[3] == d && p[4] == e && p[5] == f;
    private static bool Sig(ReadOnlySpan<string> p, string a, string b, string c, string d, string e, string f,
        string g) => p.Length == 7 && Sig(p.Slice(0, 6), a, b, c, d, e, f) && p[6] == g;
    private static bool Sig(ReadOnlySpan<string> p, string a, string b, string c, string d, string e, string f,
        string g, string h) => p.Length == 8 && Sig(p.Slice(0, 6), a, b, c, d, e, f) && p[6] == g && p[7] == h;
}

/// <summary>The op sets behind the transpiler's reflection emission switches and ILDiet's
/// whole-surface keep routes, shared so ILDiet keeps a superset of what a classified site
/// can select.</summary>
internal static class ReflectionRoutes
{
    /// <summary>Runs a reflected method body: MethodBase.Invoke, PropertyInfo.GetValue and
    /// SetValue, EventInfo.AddEventHandler and RemoveEventHandler, Type.InvokeMember.</summary>
    internal static bool RunsReflectedMethod(ReflectionOp op) =>
        op is ReflectionOp.MethodInvoke or ReflectionOp.PropertyGet or ReflectionOp.PropertySet or ReflectionOp.InvokeMember
        || RunsReflectedEventHandler(op);

    internal static bool RunsReflectedEventHandler(ReflectionOp op) =>
        op is ReflectionOp.EventAdd or ReflectionOp.EventRemove;

    /// <summary>A copied assembly arms invocation through its own event wrappers only when
    /// it is user code; a user binding runs its target like an invocation.</summary>
    internal static bool RunsCopiedReflectedMethod(ReflectionOp op, bool userAssembly) =>
        RunsReflectedMethod(op) && (userAssembly || !RunsReflectedEventHandler(op))
        || userAssembly && BindsReflectedMethod(op);

    /// <summary>Delegate.CreateDelegate and MethodInfo.CreateDelegate.</summary>
    internal static bool BindsReflectedMethod(ReflectionOp op) =>
        op is ReflectionOp.CreateDelegate or ReflectionOp.CreateDelegateNamed;

    /// <summary>FieldInfo.GetValue and Type.InvokeMember, which can box a user value type
    /// and run its virtual dispatch without an IL box or a reflective invocation.</summary>
    internal static bool ReadsReflectedField(ReflectionOp op) => op is ReflectionOp.FieldGet or ReflectionOp.InvokeMember;

    /// <summary>Activator.CreateInstance, Assembly.CreateInstance, ConstructorInfo.Invoke and
    /// Type.InvokeMember construct a type chosen at run time; RuntimeHelpers.GetUninitializedObject
    /// allocates one without a constructor.</summary>
    internal static bool ConstructsFromRuntimeType(ReflectionOp op) =>
        op is ReflectionOp.RuntimeTypeCreateInstanceOfT or ReflectionOp.RuntimeTypeDefaultCtor or ReflectionOp.ActivatorCreate or ReflectionOp.ActivatorCreateGeneric
            or ReflectionOp.ActivatorCreateNamed or ReflectionOp.AssemblyCreateInstance or ReflectionOp.CtorInvoke
            or ReflectionOp.InvokeMember or ReflectionOp.GetUninitializedObject;

    /// <summary>Reads attributes: the route materializes constructors and named-property setters.</summary>
    internal static bool ReadsReflectedAttributes(ReflectionOpKind kind) =>
        kind is ReflectionOpKind.AttributeInstances or ReflectionOpKind.AttributeData;
}

/// <summary>The member kinds a lookup row can describe.</summary>
internal enum LookupMemberKind : byte { Method, Constructor, Field, Property, Event, NestedType }

/// <summary>A property or event is Public when any accessor is public and Private when
/// every accessor is private.</summary>
internal enum LookupVisibility : byte { Public, NonPublic, Private }

/// <summary>One candidate member of a reflection lookup, built by each side from its own model.</summary>
internal readonly record struct LookupRow(string Name, LookupMemberKind Kind, LookupVisibility Visibility, bool IsStatic);

/// <summary>The BindingFlags filter RuntimeType applies to one candidate. A walk visits the
/// reflected type and then its base chain (an interface visits only itself), and keeps
/// the most derived of the rows that share a name and signature.</summary>
internal static class ReflectionLookupRules
{
    /// <summary>Whether <paramref name="row"/>, declared on the reflected type or inherited
    /// from a base, passes <paramref name="flags"/>. Nested types and constructors are
    /// never inherited, nested types ignore Instance and Static, inherited statics need
    /// FlattenHierarchy, and an inherited private member is never returned.</summary>
    internal static bool Admits(in LookupRow row, int flags, bool declaredOnReflectedType)
    {
        bool isPublic = row.Visibility == LookupVisibility.Public;
        if ((flags & (isPublic ? ReflectionApis.Public : ReflectionApis.NonPublic)) == 0)
            return false;
        if (row.Kind == LookupMemberKind.NestedType)
            return declaredOnReflectedType;
        if ((flags & (row.IsStatic ? ReflectionApis.Static : ReflectionApis.Instance)) == 0)
            return false;
        if (declaredOnReflectedType)
            return true;
        if ((flags & ReflectionApis.DeclaredOnly) != 0 || row.Kind == LookupMemberKind.Constructor
            || row.Visibility == LookupVisibility.Private)
            return false;
        return !row.IsStatic || (flags & ReflectionApis.FlattenHierarchy) != 0;
    }

    /// <summary>The name comparison a lookup applies: ordinal, or ordinal ignoring case
    /// under BindingFlags.IgnoreCase; a <paramref name="prefix"/> name matches every row
    /// name it starts.</summary>
    internal static bool NameMatches(string rowName, string name, int flags, bool prefix = false)
    {
        var comparison = (flags & 1) != 0 ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return prefix ? rowName.StartsWith(name, comparison) : string.Equals(rowName, name, comparison);
    }

    /// <summary>The name a lookup matches. GetMember, GetDefaultMembers and InvokeMember
    /// (<paramref name="allowPrefix"/>) read one trailing '*' as "starts with the rest",
    /// so a lone "*" matches every name; other lookups match the whole name.</summary>
    internal static string LookupName(string name, bool allowPrefix, out bool prefix)
    {
        prefix = allowPrefix && name.Length > 0 && name[name.Length - 1] == '*';
        return prefix ? name.Substring(0, name.Length - 1) : name;
    }
}

/// <summary>A parsed reflection type name: an optionally assembly-qualified name with
/// '+' nesting, a generic argument list and array, by-ref and pointer modifiers.</summary>
internal sealed class ReflectionTypeName
{
    /// <summary>A vector "[]".</summary>
    internal const int SZArray = -1;
    /// <summary>A by-ref "&amp;".</summary>
    internal const int ByRef = -2;
    /// <summary>A pointer "*".</summary>
    internal const int Pointer = -3;

    /// <summary>The namespace-qualified name of the outermost type, unescaped.</summary>
    public string TopLevelName = "";
    /// <summary>The names after each '+', outermost first, unescaped.</summary>
    public string[] NestedNames = Array.Empty<string>();
    public ReflectionTypeName[] GenericArguments = Array.Empty<ReflectionTypeName>();
    /// <summary>Applied left to right: <see cref="SZArray"/>, <see cref="ByRef"/>,
    /// <see cref="Pointer"/>, or the rank of a multidimensional array ("[*]" is rank 1).</summary>
    public int[] Modifiers = Array.Empty<int>();
    /// <summary>The assembly display name after the qualifying comma, trimmed; null when unqualified.</summary>
    public string? AssemblyName;

    /// <summary>The simple name of <see cref="AssemblyName"/>.</summary>
    public string? AssemblySimpleName
    {
        get
        {
            if (AssemblyName is null)
                return null;
            int comma = AssemblyName.IndexOf(',');
            return (comma < 0 ? AssemblyName : AssemblyName.Substring(0, comma)).Trim();
        }
    }

    /// <summary>The definition name: <see cref="TopLevelName"/> and the nested names joined by '+'.</summary>
    public string DefinitionName
    {
        get
        {
            string name = TopLevelName;
            for (int i = 0; i < NestedNames.Length; i++)
                name = name + "+" + NestedNames[i];
            return name;
        }
    }

    /// <summary>Parses a name in the grammar Type.GetType accepts, or returns null for a
    /// malformed one. Never throws.</summary>
    public static ReflectionTypeName? Parse(string text)
    {
        int at = 0;
        var parsed = ParseName(text, ref at, NameEnd.Text, 0);
        if (parsed is null || at != text.Length)
            return null;
        return parsed;
    }

    // Where a name ends decides what a comma after it means: the whole text and a
    // bracketed generic argument take an assembly name, a bare generic argument ends
    // at the comma that separates it from the next argument.
    private enum NameEnd : byte { Text, Bracket, Bare }

    private const int MaxDepth = 32;

    private static ReflectionTypeName? ParseName(string s, ref int at, NameEnd end, int depth)
    {
        if (depth > MaxDepth)
            return null;
        SkipSpaces(s, ref at);
        var result = new ReflectionTypeName();
        var names = new List<string>();
        while (true)
        {
            string? part = ParseIdentifier(s, ref at);
            if (part is null)
                return null;
            names.Add(part);
            if (at >= s.Length || s[at] != '+')
                break;
            at++;
        }
        result.TopLevelName = names[0];
        if (names.Count > 1)
        {
            result.NestedNames = new string[names.Count - 1];
            for (int i = 1; i < names.Count; i++)
                result.NestedNames[i - 1] = names[i];
        }
        if (at < s.Length && s[at] == '[' && IsGenericOpen(s, at))
        {
            at++;
            var arguments = ParseArguments(s, ref at, depth);
            if (arguments is null)
                return null;
            result.GenericArguments = arguments;
        }
        if (!ParseModifiers(s, ref at, result))
            return null;
        if (end == NameEnd.Bare || at >= s.Length || s[at] != ',')
            return result;
        int start = at + 1;
        int stop = end == NameEnd.Text ? s.Length : s.IndexOf(']', start);
        if (stop < 0)
            return null;
        string assembly = s.Substring(start, stop - start).Trim();
        if (!IsAssemblyDisplayName(assembly))
            return null;
        result.AssemblyName = assembly;
        at = stop;
        return result;
    }

    private static ReflectionTypeName[]? ParseArguments(string s, ref int at, int depth)
    {
        var arguments = new List<ReflectionTypeName>();
        while (true)
        {
            SkipSpaces(s, ref at);
            ReflectionTypeName? argument;
            if (at < s.Length && s[at] == '[')
            {
                at++;
                argument = ParseName(s, ref at, NameEnd.Bracket, depth + 1);
                if (argument is null || at >= s.Length || s[at] != ']')
                    return null;
                at++;
            }
            else
            {
                argument = ParseName(s, ref at, NameEnd.Bare, depth + 1);
                if (argument is null)
                    return null;
            }
            arguments.Add(argument);
            SkipSpaces(s, ref at);
            if (at >= s.Length)
                return null;
            if (s[at] == ']')
            {
                at++;
                return arguments.ToArray();
            }
            if (s[at] != ',')
                return null;
            at++;
        }
    }

    private static bool ParseModifiers(string s, ref int at, ReflectionTypeName result)
    {
        var modifiers = new List<int>();
        while (at < s.Length)
        {
            char c = s[at];
            if (c == '&')
                modifiers.Add(ByRef);
            else if (c == '*')
                modifiers.Add(Pointer);
            else if (c == '[')
            {
                int rank = ParseArraySuffix(s, ref at);
                if (rank == 0)
                    return false;
                modifiers.Add(rank);
                continue;
            }
            else
                break;
            at++;
        }
        if (modifiers.Count > 0)
            result.Modifiers = modifiers.ToArray();
        return true;
    }

    // "[" opens a generic argument list unless it is an array suffix: "[]", "[,…]" or "[*]".
    private static bool IsGenericOpen(string s, int at)
    {
        int next = at + 1;
        SkipSpaces(s, ref next);
        return next < s.Length && s[next] is not (']' or ',' or '*');
    }

    // Every component after the simple name is a key=value pair.
    private static bool IsAssemblyDisplayName(string assembly)
    {
        int comma = assembly.IndexOf(',');
        string simple = (comma < 0 ? assembly : assembly.Substring(0, comma)).Trim();
        if (simple.Length == 0 || simple.IndexOf('=') >= 0)
            return false;
        while (comma >= 0)
        {
            int next = assembly.IndexOf(',', comma + 1);
            string part = next < 0 ? assembly.Substring(comma + 1) : assembly.Substring(comma + 1, next - comma - 1);
            if (part.IndexOf('=') <= 0)
                return false;
            comma = next;
        }
        return true;
    }

    // Returns SZArray, the rank of a multidimensional suffix, or 0 when malformed.
    private static int ParseArraySuffix(string s, ref int at)
    {
        int i = at + 1;
        SkipSpaces(s, ref i);
        if (i < s.Length && s[i] == '*')
        {
            i++;
            SkipSpaces(s, ref i);
            if (i >= s.Length || s[i] != ']')
                return 0;
            at = i + 1;
            return 1;
        }
        int rank = 1;
        while (i < s.Length && s[i] == ',')
        {
            rank++;
            i++;
            SkipSpaces(s, ref i);
        }
        if (i >= s.Length || s[i] != ']')
            return 0;
        at = i + 1;
        return rank == 1 ? SZArray : rank;
    }

    private static string? ParseIdentifier(string s, ref int at)
    {
        var name = new System.Text.StringBuilder();
        while (at < s.Length)
        {
            char c = s[at];
            if (c == '\\')
            {
                if (at + 1 >= s.Length)
                    return null;
                name.Append(s[at + 1]);
                at += 2;
                continue;
            }
            if (c is ',' or '+' or '&' or '*' or '[' or ']')
                break;
            name.Append(c);
            at++;
        }
        if (name.Length == 0)
            return null;
        return name.ToString();
    }

    private static void SkipSpaces(string s, ref int at)
    {
        while (at < s.Length && s[at] == ' ')
            at++;
    }
}
