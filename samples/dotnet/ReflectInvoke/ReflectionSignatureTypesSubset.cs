using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ReflectionSignatureTypesSubset;

public unsafe class Shapes
{
    public static ref int Ref(ref int value) { value++; return ref value; }
    public static int** Pointer(int** value) => value;
    public static string* Text(string* value) => value;
    public static ref int[] Array(ref int[] value) => ref value;
    public static ref List<int> Generic(ref List<int> value) => ref value;
    public static int*[] PointerArray(int*[] value) => value;
    public static int*[,] PointerMdArray(int*[,] value) => value;
    public static delegate*<ref int, int*, void> Function(delegate*<ref int, int*, void> value) => value;
    public static delegate* unmanaged[Cdecl]<delegate*<int>, int*> Unmanaged(delegate* unmanaged[Cdecl]<delegate*<int>, int*> value) => value;
    public static delegate*<int> Different(delegate*<TypeCode> value) => null;
    public struct Marker { public int Value; }
    public static int******* DeepPointer(int******* value) => value;
    public static Marker* StructurePointer(Marker* value) => value;
    public static delegate*<int>* FunctionPointerPointer(delegate*<int>* value) => value;
    public static int** PointerField;
    public static int******* DeepPointerField;
    public static Marker* StructurePointerField;
    public static delegate*<int>* FunctionPointerField;
    public static delegate*<Hidden, Hidden> HiddenFunction(delegate*<Hidden, Hidden> value) => value;
    public static void Select(int value) { }
    public static void Select(ref int value) { }
    public static void Select(int* value) { }
    public static void Select(delegate*<int> value) { }
    public Shapes(ref int value) { }
    public Shapes(int* value) { }
    public class Hidden { }
    private class PrivateLeaf { }
    private static PrivateLeaf* HiddenPointer(PrivateLeaf* value) => value;
    private static PrivateLeaf*[] HiddenPointerArray(PrivateLeaf*[] value) => value;
    private static delegate*<PrivateLeaf, void> PrivateFunction(delegate*<PrivateLeaf, void> value) => value;
}

public unsafe class GenericFunction<T>
{
    public static delegate*<T, T> Identity(delegate*<T, T> value) => value;
}

public class ConstructorBase
{
    public ConstructorBase(int value) { }
}

public unsafe class DeclaredConstructors : ConstructorBase
{
    public DeclaredConstructors(ref int value) : base(value) { }
    private DeclaredConstructors(int* value) : base(0) { }
}

public static unsafe class Program
{
    private static string Text(string value) => value is null ? "<null>" : "[" + value + "]";
    private static void Describe(string label, Type type)
    {
        Console.WriteLine(label + " shape: " + type + "/" + type.IsByRef + "/" + type.IsPointer + "/"
            + type.IsFunctionPointer + "/" + type.IsUnmanagedFunctionPointer + "/" + type.HasElementType + "/" + type.GetElementType());
        Console.WriteLine(label + " names: " + Text(type.Name) + "/" + Text(type.FullName) + "/" + Text(type.Namespace));
        Console.WriteLine(label + " qualified: " + Text(type.AssemblyQualifiedName));
        Console.WriteLine(label + " flags: " + type.IsClass + "/" + type.IsValueType + "/" + type.IsPrimitive + "/"
            + type.IsSealed + "/" + type.IsPublic + "/" + type.IsVisible + "/" + (int)type.Attributes + "/"
            + (type.BaseType is null) + "/" + typeof(object).IsAssignableFrom(type) + "/" + type.IsSubclassOf(typeof(object)));
        Console.WriteLine(label + " handle: " + ReferenceEquals(type, Type.GetTypeFromHandle(type.TypeHandle)) + "/"
            + ReferenceEquals(type, type.UnderlyingSystemType) + "/" + type.Equals(Type.GetTypeFromHandle(type.TypeHandle)) + "/"
            + (type.GetHashCode() == Type.GetTypeFromHandle(type.TypeHandle).GetHashCode()));
        if (type.IsFunctionPointer)
        {
            var parameters = type.GetFunctionPointerParameterTypes();
            Console.WriteLine(label + " function: " + type.GetFunctionPointerReturnType() + "/" + parameters.Length + "/"
                + type.GetFunctionPointerCallingConventions().Length + "/" + parameters.GetType().Name);
            foreach (var parameter in parameters) Console.WriteLine(label + " argument: " + parameter);
        }
    }

    private static string Fault(Action action)
    {
        try { action(); return "none"; }
        catch (Exception ex) { return ex.GetType().Name; }
    }

    private static void Assignment(string label, Type target, Type source)
    {
        Console.WriteLine("signature assignment " + label + ": " + target.IsAssignableFrom(source) + "/"
            + source.IsAssignableTo(target));
    }

    private static string PointerInvoke(string name, Type type)
    {
        try
        {
            object result = typeof(Shapes).GetMethod(name).Invoke(null, new[] { Pointer.Box((void*)0x1230, type) });
            return ((nuint)Pointer.Unbox(result)).ToString();
        }
        catch (Exception ex) { return ex.GetType().Name; }
    }

    private static string PointerField(string name, Type type)
    {
        try
        {
            var field = typeof(Shapes).GetField(name);
            field.SetValue(null, Pointer.Box((void*)0x1230, type));
            return ((nuint)Pointer.Unbox(field.GetValue(null))).ToString();
        }
        catch (Exception ex) { return ex.GetType().Name; }
    }

    public static void Run()
    {
        Console.WriteLine("== reflection signature type handles ==");
        foreach (var name in new[] { "Ref", "Pointer", "Text", "Array", "Generic", "PointerArray", "PointerMdArray", "Function", "Unmanaged", "HiddenFunction" })
        {
            var method = typeof(Shapes).GetMethod(name);
            var type = method.ReturnType;
            Describe(name, type);
            Console.WriteLine(name + " identities: " + ReferenceEquals(type, method.GetParameters()[0].ParameterType) + "/"
                + ReferenceEquals(type, method.ReturnParameter.ParameterType) + "/" + ReferenceEquals(type, typeof(Shapes).GetMethod(name).ReturnType));
        }
        var different = typeof(Shapes).GetMethod("Different");
        Describe("Different return", different.ReturnType);
        Describe("Different parameter", different.GetParameters()[0].ParameterType);
        Console.WriteLine("signature distinct positions: " + (different.ReturnType != different.GetParameters()[0].ParameterType));
        var pointer = typeof(Shapes).GetMethod("Pointer").ReturnType;
        var function = typeof(Shapes).GetMethod("Function").ReturnType;
        Console.WriteLine("signature composition: " + ReferenceEquals(pointer, typeof(int).MakePointerType().MakePointerType()) + "/"
            + ReferenceEquals(pointer, typeof(int**)) + "/" + ReferenceEquals(typeof(int).MakeByRefType(), typeof(Shapes).GetMethod("Ref").ReturnType) + "/"
            + ReferenceEquals(function, typeof(delegate*<ref int, int*, void>)) + "/"
            + ReferenceEquals(function.MakePointerType().GetElementType(), function) + "/"
            + ReferenceEquals(function.MakeByRefType().GetElementType(), function));
        Console.WriteLine("signature negatives: " + (typeof(int*) == typeof(long*)) + "/" + (typeof(int*) == typeof(int).MakeByRefType()) + "/"
            + (typeof(delegate*<int>) == typeof(delegate* unmanaged[Cdecl]<int>)) + "/"
            + (typeof(delegate*<int>) == typeof(delegate*<int, int>)) + "/"
            + (typeof(int*[]) == typeof(int*[,])));
        Console.WriteLine("signature nested array: " + ReferenceEquals(typeof(int*[][]), typeof(int*).MakeArrayType().MakeArrayType()) + "/"
            + ReferenceEquals(typeof(int*[]), typeof(int*[][]).GetElementType()));
        Console.WriteLine("signature conventions: " + (typeof(delegate* unmanaged[Cdecl]<int>) == typeof(delegate* unmanaged[Stdcall]<int>)) + "/"
            + (typeof(delegate* unmanaged[Cdecl]<int>) == typeof(delegate* unmanaged[Cdecl, SuppressGCTransition]<int>)));
        Console.WriteLine("signature static flags: " + typeof(delegate*<int>).IsPointer + "/" + typeof(delegate*<int>).HasElementType + "/"
            + typeof(delegate*<int>).IsFunctionPointer + "/" + typeof(delegate* unmanaged[Cdecl]<int>).IsUnmanagedFunctionPointer);
        Assignment("int-uint pointer", typeof(int*), typeof(uint*));
        Assignment("nested int-uint pointer", typeof(int**), typeof(uint**));
        Assignment("void-int pointer", typeof(void*), typeof(int*));
        Assignment("enum-int pointer", typeof(DayOfWeek*), typeof(int*));
        Assignment("int-uint byref", typeof(int).MakeByRefType(), typeof(uint).MakeByRefType());
        Assignment("object-string pointer", typeof(object*), typeof(string*));
        Assignment("string-object pointer", typeof(string*), typeof(object*));
        Assignment("object-string byref", typeof(object).MakeByRefType(), typeof(string).MakeByRefType());
        Assignment("bool-byte pointer", typeof(bool*), typeof(byte*));
        Assignment("char-ushort pointer", typeof(char*), typeof(ushort*));
        Assignment("float-int pointer", typeof(float*), typeof(int*));
        Assignment("native-long pointer", typeof(nint*), typeof(long*));
        Assignment("native-unsigned pointer", typeof(nint*), typeof(nuint*));
        Assignment("int-uint pointer array", typeof(int*[]), typeof(uint*[]));
        Assignment("object-pointer array", typeof(object[]), typeof(int*[]));
        Assignment("nested object-pointer array", typeof(object[][]), typeof(int*[][]));
        Assignment("array pointer", typeof(object[]*), typeof(string[]*));
        Assignment("nested object-string pointer", typeof(object**), typeof(string**));
        Assignment("function pointer identity", typeof(delegate*<int>), typeof(delegate*<int>));
        Assignment("function pointer signature", typeof(delegate*<int>), typeof(delegate*<uint>));
        Assignment("nested function pointer", typeof(delegate*<int>*), typeof(delegate*<uint>*));
        foreach (var type in new[] { typeof(string), typeof(List<int>), typeof(int[]), typeof(void), typeof(delegate*<void>) })
            Console.WriteLine("signature make: " + type.MakePointerType() + "/" + type.MakeByRefType() + "/"
                + ReferenceEquals(type, type.MakePointerType().GetElementType()) + "/" + ReferenceEquals(type, type.MakeByRefType().GetElementType()));
        var byref = typeof(int).MakeByRefType();
        Console.WriteLine("signature faults: " + Fault(() => byref.MakeByRefType()) + "/" + Fault(() => byref.MakePointerType()) + "/"
            + Fault(() => typeof(int).GetFunctionPointerReturnType()) + "/" + Fault(() => pointer.GetFunctionPointerParameterTypes()) + "/"
            + Fault(() => byref.GetFunctionPointerCallingConventions()) + "/" + Fault(() => ((Type)null).MakePointerType()));
        foreach (var name in new[] { "HiddenPointer", "HiddenPointerArray", "PrivateFunction" })
        {
            var type = typeof(Shapes).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic).ReturnType;
            Console.WriteLine(name + " visibility: " + type.IsPublic + "/" + type.IsVisible);
        }
        foreach (var type in new[] { typeof(int), typeof(int).MakeByRefType(), typeof(int*), typeof(delegate*<int>) })
        {
            var method = typeof(Shapes).GetMethod("Select", new[] { type });
            Console.WriteLine("signature lookup: " + ReferenceEquals(type, method.GetParameters()[0].ParameterType));
        }
        foreach (var type in new[] { typeof(int).MakeByRefType(), typeof(int*) })
            Console.WriteLine("signature constructor: " + ReferenceEquals(type, typeof(Shapes).GetConstructor(new[] { type }).GetParameters()[0].ParameterType));
        var closed = typeof(GenericFunction<int>).GetMethod("Identity").ReturnType;
        Console.WriteLine("signature closed generic: " + ReferenceEquals(closed, typeof(delegate*<int, int>)) + "/"
            + ReferenceEquals(closed.GetFunctionPointerReturnType(), typeof(int)) + "/"
            + ReferenceEquals(closed.GetFunctionPointerParameterTypes()[0], typeof(int)));
        var runtimeOwner = typeof(GenericFunction<>).MakeGenericType(typeof(Guid));
#if SIGNATURE_TYPES_UNSHARED
        if (runtimeOwner != typeof(GenericFunction<Guid>)) throw new InvalidOperationException("AOT owner identity");
#endif
        var runtime = runtimeOwner.GetMethod("Identity").ReturnType;
        Console.WriteLine("signature runtime generic: " + ReferenceEquals(runtime, typeof(delegate*<Guid, Guid>)) + "/"
            + ReferenceEquals(runtime.GetFunctionPointerReturnType(), typeof(Guid)) + "/"
            + ReferenceEquals(runtime.GetFunctionPointerParameterTypes()[0], typeof(Guid)));
        foreach (var type in new[] { typeof(int*), typeof(int).MakeByRefType(), typeof(delegate*<int>) })
            Console.WriteLine("signature allocation guards: " + Fault(() => Activator.CreateInstance(type)) + "/"
                + Fault(() => Activator.CreateInstance(type, new object[] { 1 })) + "/"
                + Fault(() => RuntimeHelpers.GetUninitializedObject(type)) + "/" + Fault(() => typeof(List<>).MakeGenericType(type)));
        Console.WriteLine("signature array query: " + ReferenceEquals(typeof(int*[]), typeof(int*).MakeArrayType()) + "/"
            + ReferenceEquals(typeof(int*[,]), typeof(int*).MakeArrayType(2)) + "/"
            + Fault(() => byref.MakeArrayType()) + "/" + Fault(() => byref.MakeArrayType(1)) + "/"
            + Fault(() => System.Array.CreateInstance(byref, 0)));
        Console.WriteLine("signature array rank: " + (typeof(int*).MakeArrayType(1).GetArrayRank() == 1) + "/"
            + (!typeof(int*).MakeArrayType(1).IsSZArray) + "/"
            + ReferenceEquals(typeof(int*), typeof(int*).MakeArrayType(32).GetElementType()) + "/"
            + Fault(() => typeof(int*).MakeArrayType(0)) + "/" + Fault(() => typeof(int*).MakeArrayType(33)) + "/"
            + Fault(() => typeof(delegate*<int>).MakeArrayType(int.MaxValue)));
        object[] values = { 7 };
        Console.WriteLine("signature Invoke unchanged: " + typeof(Shapes).GetMethod("Ref").Invoke(null, values) + "/" + values[0]);
        Console.WriteLine("signature Pointer.Box Invoke: " + PointerInvoke("Pointer", typeof(int**)) + "/"
            + PointerInvoke("DeepPointer", typeof(int*******)) + "/" + PointerInvoke("StructurePointer", typeof(Shapes.Marker*)) + "/"
            + PointerInvoke("FunctionPointerPointer", typeof(delegate*<int>*)) + "/"
            + PointerInvoke("FunctionPointerPointer", typeof(delegate* unmanaged[Cdecl]<int>*)) + "/"
            + PointerInvoke("DeepPointer", typeof(long*******)));
        Console.WriteLine("signature Pointer.Box fields: " + PointerField("PointerField", typeof(int**)) + "/"
            + PointerField("DeepPointerField", typeof(int*******)) + "/" + PointerField("StructurePointerField", typeof(Shapes.Marker*)) + "/"
            + PointerField("FunctionPointerField", typeof(delegate*<int>*)) + "/"
            + PointerField("FunctionPointerField", typeof(delegate* unmanaged[Cdecl]<int>*)) + "/"
            + PointerField("StructurePointerField", typeof(int*)));
        var constructors = typeof(DeclaredConstructors).GetTypeInfo().DeclaredConstructors;
        Console.WriteLine("signature declared constructor array: " + constructors.GetType().Name);
        int count = 0;
        foreach (var constructor in constructors)
        {
            count++;
            Console.WriteLine("signature declared constructor: " + constructor.IsPublic + "/" + constructor.IsStatic + "/"
                + (constructor.DeclaringType == typeof(DeclaredConstructors)) + "/" + constructor.GetParameters()[0].ParameterType);
        }
        Console.WriteLine("signature declared constructors count: " + count);
        Console.WriteLine("signature declared constructors null: " + Fault(() => ((TypeInfo)null).DeclaredConstructors.GetEnumerator()));
        Console.WriteLine("reflection signature type handles end");
    }
#if SIGNATURE_TYPES_ONLY
    public static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        if (args.Length != 0 && args[0] == "signature-layout-boundaries")
        {
            Console.WriteLine("signature pointer array allocation: " + Fault(() => System.Array.CreateInstance(typeof(int*), 0)));
            Console.WriteLine("signature function array allocation: " + Fault(() => System.Array.CreateInstance(typeof(delegate*<int>), 0)));
            return;
        }
        Run();
    }
#endif
}
