using System;
using System.Globalization;
using System.Reflection;

namespace RuntimeTypeTypeCodeSubset;

internal static unsafe class TypeCodeChecks
{
    private enum CodeSByte : sbyte { Value = -1 }
    private enum CodeByte : byte { Value = 1 }
    private enum CodeShort : short { Value = -1 }
    private enum CodeUShort : ushort { Value = 1 }
    private enum CodeInt : int { Value = -1 }
    private enum CodeUInt : uint { Value = 1 }
    private enum CodeLong : long { Value = -1 }
    private enum CodeULong : ulong { Value = 1 }
    private class CodeOwner<T> { public static void Method<U>() { } public enum Nested : short { Value = -1 } }
    private struct CodeStruct { }
    private interface ICodeInterface { }
    private sealed class CustomCodeType : TypeDelegator
    {
        internal CustomCodeType() : base(typeof(int)) { }
        protected override TypeCode GetTypeCodeImpl() => TypeCode.Decimal;
    }

    private static void Check(string name, bool success)
    {
        Console.WriteLine("type-code-" + name + ":" + success);
        if (!success) throw new InvalidOperationException(name);
    }
    private static Exception Error(Action action)
    {
        try { action(); }
        catch (TargetInvocationException error) { return error.InnerException; }
        catch (Exception error) { return error; }
        return null;
    }

    // The retained instance method requires a receiver; the public static API does not.
    // Its managed cache getter's initial field load is verified by the fixture.
    private static TypeCode Public(Type type)
    {
        if (type is null) throw new NullReferenceException();
        return Type.GetTypeCode(type);
    }
    private static TypeCode Direct(Type type) => Public(type);
    private static TypeCode Pointer(Type type) => Public(type);
    private static TypeCode Virtual(Type type) => Public(type);
    private static TypeCode VirtualPointer(Type type) => Public(type);
    private static TypeCode Constrained(Type type) => Public(type);
    private static Func<TypeCode> Address(Type type)
    {
        if (type is null) throw new ArgumentException();
        return () => Public(type);
    }
    private static Func<TypeCode> VirtualAddress(Type type)
    {
        if (type is null) throw new NullReferenceException();
        return () => Public(type);
    }
    private static Func<TypeCode> FlowAddress(Type type, bool virt) => virt ? VirtualAddress(type) : Address(type);
    private static TypeCode AddressQuery(Type type) => Address(type)();
    private static TypeCode VirtualAddressQuery(Type type) => VirtualAddress(type)();
    private static TypeCode FlowDirect(Type type) => FlowAddress(type, false)();
    private static TypeCode FlowVirtual(Type type) => FlowAddress(type, true)();
    private static TypeCode ReflectionLeaf(Type type) => Public(type);
    private static MethodInfo ReflectionMethod() => typeof(TypeCodeChecks)
        .GetMethod(nameof(ReflectionLeaf), BindingFlags.Static | BindingFlags.NonPublic);
    private static TypeCode Reflection(Type type) => (TypeCode)typeof(TypeCodeChecks)
        .GetMethod(nameof(ReflectionLeaf), BindingFlags.Static | BindingFlags.NonPublic)
        .Invoke(null, new object[] { type });
    private static TypeCode Generic<T>() => Type.GetTypeCode(typeof(T));

    private static void Route(string route, Func<Type, TypeCode> query)
    {
        Type[] known = { typeof(bool), typeof(char), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort),
            typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal),
            typeof(DateTime), typeof(DBNull), typeof(string), typeof(CodeSByte), typeof(CodeByte), typeof(CodeShort),
            typeof(CodeUShort), typeof(CodeInt), typeof(CodeUInt), typeof(CodeLong), typeof(CodeULong),
            typeof(TypeCode), typeof(CodeOwner<int>.Nested) };
        TypeCode[] expected = { TypeCode.Boolean, TypeCode.Char, TypeCode.SByte, TypeCode.Byte, TypeCode.Int16,
            TypeCode.UInt16, TypeCode.Int32, TypeCode.UInt32, TypeCode.Int64, TypeCode.UInt64, TypeCode.Single,
            TypeCode.Double, TypeCode.Decimal, TypeCode.DateTime, TypeCode.DBNull, TypeCode.String,
            TypeCode.SByte, TypeCode.Byte, TypeCode.Int16, TypeCode.UInt16, TypeCode.Int32, TypeCode.UInt32,
            TypeCode.Int64, TypeCode.UInt64, TypeCode.Int32, TypeCode.Int16 };
        string[] names = { "bool", "char", "sbyte", "byte", "short", "ushort", "int", "uint", "long", "ulong",
            "float", "double", "decimal", "datetime", "dbnull", "string", "enum-sbyte", "enum-byte", "enum-short",
            "enum-ushort", "enum-int", "enum-uint", "enum-long", "enum-ulong", "typecode-enum", "nested-closed-enum" };
        for (int i = 0; i < known.Length; i++) Check(route + "-" + names[i], query(known[i]) == expected[i]);
        Type formal = typeof(CodeOwner<>).GetGenericArguments()[0];
        Type method = typeof(CodeOwner<>).GetMethod("Method").GetGenericArguments()[0];
        Type[] others = { typeof(object), typeof(void), typeof(nint), typeof(nuint), typeof(Enum), typeof(ValueType),
            typeof(CodeStruct), typeof(ICodeInterface), typeof(CodeOwner<>), typeof(CodeOwner<int>),
            typeof(int?), typeof(CodeInt?), typeof(Nullable<>),
            formal, method, typeof(int[]), typeof(string[,]), typeof(CodeInt[]), formal.MakeArrayType(),
            typeof(int*), typeof(int).MakeByRefType(), formal.MakePointerType(), method.MakeByRefType(),
            typeof(delegate*<int, void>), typeof(delegate* unmanaged<int, void>), typeof(Action), typeof(TypedReference) };
        string[] otherNames = { "object", "void", "nint", "nuint", "enum-base", "value-base", "struct", "interface",
            "definition", "constructed", "nullable-int", "nullable-enum", "nullable-definition",
            "formal", "method-formal", "array", "mdarray", "enum-array", "formal-array", "pointer", "byref",
            "formal-pointer", "method-byref", "function", "unmanaged-function", "delegate", "typed-reference" };
        for (int i = 0; i < others.Length; i++) Check(route + "-" + otherNames[i], query(others[i]) == TypeCode.Object);
        Check(route + "-handle", query(Type.GetTypeFromHandle(typeof(decimal).TypeHandle)) == TypeCode.Decimal);
        Check(route + "-repeat", query(typeof(int)) == TypeCode.Int32 && query(typeof(int)) == TypeCode.Int32);
    }
    private static void Nulls(string route, Func<Type, TypeCode> query)
    {
        Check(route + "-null", Error(() => query(null)) is NullReferenceException);
        Check(route + "-default-handle", Error(() => query(Type.GetTypeFromHandle(default))) is NullReferenceException);
    }
    internal static void Run(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime type code ==");
        Route("public", Public); Nulls("public", Public);
        Check("static-null-empty", Type.GetTypeCode(null) == TypeCode.Empty);
        Check("generic", Generic<int>() == TypeCode.Int32 && Generic<CodeULong>() == TypeCode.UInt64
            && Generic<int?>() == TypeCode.Object);
        Console.WriteLine("Runtime type code end");
    }
    internal static void RunPublicScope(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        // CLR-only scope witnesses, not native custom-Type or open-instantiation support claims.
        Check("public-scope-open-enum", Type.GetTypeCode(typeof(CodeOwner<>.Nested)) == TypeCode.Int16);
        Type method = typeof(CodeOwner<>).GetMethod("Method").GetGenericArguments()[0];
        Check("public-scope-open-constructed", Type.GetTypeCode(typeof(CodeOwner<>).MakeGenericType(method)) == TypeCode.Object);
        Check("public-scope-delegator", Type.GetTypeCode(new TypeDelegator(typeof(int))) == TypeCode.Int32);
        Check("public-scope-override", Type.GetTypeCode(new CustomCodeType()) == TypeCode.Decimal);
    }
    internal static void RunArtifact(string[] args)
    {
        Run(args);
        Route("direct", Direct); Nulls("direct", Direct);
        Route("calli", Pointer); Nulls("calli", Pointer);
        Route("virtual", Virtual); Nulls("virtual", Virtual);
        Route("virtual-calli", VirtualPointer); Nulls("virtual-calli", VirtualPointer);
        Route("constrained", Constrained); Nulls("constrained", Constrained);
        Route("address", AddressQuery);
        Route("virtual-address", VirtualAddressQuery); Nulls("virtual-address", VirtualAddressQuery);
        Route("flow-direct", FlowDirect);
        Route("flow-virtual", FlowVirtual); Nulls("flow-virtual", FlowVirtual);
        Check("address-bind-null", Error(() => Address(null)) is ArgumentException);
        Check("virtual-address-bind-null", Error(() => VirtualAddress(null)) is NullReferenceException);
        Check("flow-direct-bind-null", Error(() => FlowAddress(null, false)) is ArgumentException);
        Check("flow-virtual-bind-null", Error(() => FlowAddress(null, true)) is NullReferenceException);
        RunReflectionArtifact(Array.Empty<string>());
        if (args.Length > 0 && args[0] == "native-policy")
        {
            Type openEnum = typeof(CodeOwner<>.Nested);
            Check("native-open-enum-policy", Type.GetTypeCode(openEnum) == TypeCode.Object
                && Direct(openEnum) == TypeCode.Object && Pointer(openEnum) == TypeCode.Object
                && Virtual(openEnum) == TypeCode.Object && Constrained(openEnum) == TypeCode.Object);
            DelegateMetadata("address", Address(typeof(int)));
            DelegateMetadata("virtual-address", VirtualAddress(typeof(int)));
            DelegateMetadata("flow-direct", FlowAddress(typeof(int), false));
            DelegateMetadata("flow-virtual", FlowAddress(typeof(int), true));
        }
        Console.WriteLine("Runtime type code routes end");
    }
    private static void DelegateMetadata(string route, Func<TypeCode> query)
    {
        MethodInfo method = query.Method;
        Check("native-" + route + "-row", method.Name == "GetTypeCodeImpl" && method.ReturnType == typeof(TypeCode)
            && !method.IsStatic && method.DeclaringType == ((object)typeof(int)).GetType()
            && method.DeclaringType.FullName == "System.RuntimeType");
        object result = method.Invoke(typeof(int), null);
        Check("native-" + route + "-invoke", result.GetType() == typeof(TypeCode) && (TypeCode)result == TypeCode.Int32
            && (TypeCode)method.Invoke(typeof(CodeULong), null) == TypeCode.UInt64
            && (TypeCode)method.Invoke(typeof(object), null) == TypeCode.Object);
    }
    internal static void RunReflectionArtifact(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime type code reflection ==");
        Route("reflection", Reflection); Nulls("reflection", Reflection);
        if (args.Length > 0 && args[0] == "native-policy")
        {
            Type openEnum = typeof(CodeOwner<>.Nested);
            Check("native-open-enum-policy", Type.GetTypeCode(openEnum) == TypeCode.Object
                && Reflection(openEnum) == TypeCode.Object);
            MethodInfo method = ReflectionMethod();
            Check("native-reflection-wrapper-row", method.Name == nameof(ReflectionLeaf)
                && method.DeclaringType == typeof(TypeCodeChecks) && method.IsStatic);
            Check("native-runtime-lookup-unavailable", ((object)typeof(int)).GetType()
                .GetMethod("GetTypeCodeImpl", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly) is null);
        }
        Console.WriteLine("Runtime type code reflection end");
    }
}
