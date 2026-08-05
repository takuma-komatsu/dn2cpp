#nullable enable
// SUBJECT: a NULL reflected handle reaching an instance member must raise the
// catchable NullReferenceException real .NET raises — never a SIGSEGV, and never
// a default answer. A null handle is a NORMAL value here: GetMethod /
// GetProperty / GetField / GetConstructor / Type.GetType all answer null on a
// miss, and library code routinely writes `t.GetMethod(n)!`. The two failure
// shapes guarded against are the dereference crash and the silent wrong answer
// (an EMPTY member array, a null Name, MemberType 0), which is indistinguishable
// at the call site from a real answer. A null ARGUMENT is a separate contract and
// gets its own tail section.
using System;
using System.Reflection;

namespace ReflectNullHandleSubset;

class Subject
{
    public int Field;
    public int Prop { get; set; }
    public Subject() { }
    public void Method(int a) { }
}

static class Program
{
    // Read through a static field so nothing folds the null away at the call site.
    static readonly ParameterInfo? NullParam = null;
    static readonly MemberInfo? NullMember = null;

    static void Try(string label, Action a)
    {
        try { a(); Console.WriteLine($"{label}: no throw"); }
        catch (Exception e) { Console.WriteLine($"{label}: {e.GetType().Name}"); }
    }

    internal static void Run()
    {
        Console.WriteLine("== null MethodInfo ==");
        MethodInfo? m = typeof(Subject).GetMethod("NoSuchMethod");
        Console.WriteLine($"lookup missed: {m is null}");
        Try("MakeGenericMethod", () => m!.MakeGenericMethod(typeof(int)));
        Try("Invoke", () => m!.Invoke(null, null));
        Try("Name", () => _ = m!.Name);
        Try("ReturnType", () => _ = m!.ReturnType);
        Try("ReturnParameter", () => _ = m!.ReturnParameter);
        Try("DeclaringType", () => _ = m!.DeclaringType);
        Try("IsStatic", () => _ = m!.IsStatic);
        Try("IsPublic", () => _ = m!.IsPublic);
        Try("IsVirtual", () => _ = m!.IsVirtual);
        Try("IsAbstract", () => _ = m!.IsAbstract);
        Try("IsFinal", () => _ = m!.IsFinal);
        Try("IsConstructor", () => _ = m!.IsConstructor);
        Try("IsSpecialName", () => _ = m!.IsSpecialName);
        Try("IsGenericMethod", () => _ = m!.IsGenericMethod);
        Try("IsGenericMethodDefinition", () => _ = m!.IsGenericMethodDefinition);
        Try("Attributes", () => _ = m!.Attributes);
        Try("MethodImplementationFlags", () => _ = m!.MethodImplementationFlags);
        Try("GetParameters", () => m!.GetParameters());
        Try("GetGenericArguments", () => m!.GetGenericArguments());
        Try("GetGenericMethodDefinition", () => m!.GetGenericMethodDefinition());
        Try("GetBaseDefinition", () => m!.GetBaseDefinition());
        Try("MetadataToken", () => _ = m!.MetadataToken);
        Try("Module", () => _ = m!.Module);
        Try("CustomAttributes", () => _ = m!.CustomAttributes);
        Try("GetCustomAttributes", () => m!.GetCustomAttributes(false));

        Console.WriteLine("== null ConstructorInfo ==");
        ConstructorInfo? c = typeof(Subject).GetConstructor(new[] { typeof(long) });
        Console.WriteLine($"lookup missed: {c is null}");
        Try("ctor Invoke", () => c!.Invoke(new object[] { 1L }));
        Try("ctor Name", () => _ = c!.Name);

        Console.WriteLine("== null PropertyInfo ==");
        PropertyInfo? p = typeof(Subject).GetProperty("NoSuchProperty");
        Console.WriteLine($"lookup missed: {p is null}");
        Try("prop GetValue", () => p!.GetValue(new Subject()));
        Try("prop SetValue", () => p!.SetValue(new Subject(), 1));
        Try("prop indexed GetValue", () => p!.GetValue(new Subject(), new object[] { 0 }));
        Try("prop PropertyType", () => _ = p!.PropertyType);
        Try("prop CanRead", () => _ = p!.CanRead);
        Try("prop CanWrite", () => _ = p!.CanWrite);
        Try("prop GetGetMethod", () => p!.GetGetMethod());
        Try("prop GetIndexParameters", () => p!.GetIndexParameters());
        Try("prop Name", () => _ = p!.Name);

        Console.WriteLine("== null FieldInfo ==");
        FieldInfo? f = typeof(Subject).GetField("NoSuchField");
        Console.WriteLine($"lookup missed: {f is null}");
        Try("field GetValue", () => f!.GetValue(new Subject()));
        Try("field FieldType", () => _ = f!.FieldType);

        Console.WriteLine("== null ParameterInfo ==");
        Try("param ParameterType", () => _ = NullParam!.ParameterType);
        Try("param Position", () => _ = NullParam!.Position);
        Try("param Name", () => _ = NullParam!.Name);
        Try("param Member", () => _ = NullParam!.Member);
        Try("param Attributes", () => _ = NullParam!.Attributes);
        Try("param IsOptional", () => _ = NullParam!.IsOptional);
        Try("param IsOut", () => _ = NullParam!.IsOut);
        Try("param HasDefaultValue", () => _ = NullParam!.HasDefaultValue);

        Console.WriteLine("== null MemberInfo ==");
        Try("member Name", () => _ = NullMember!.Name);
        Try("member DeclaringType", () => _ = NullMember!.DeclaringType);
        Try("member MemberType", () => _ = NullMember!.MemberType);
        Try("member MetadataToken", () => _ = NullMember!.MetadataToken);
        Try("member Module", () => _ = NullMember!.Module);

        Console.WriteLine("== null Type: the member lookups that answered EMPTY ==");
        Type? t = Type.GetType("No.Such.Type.Anywhere");
        Console.WriteLine($"lookup missed: {t is null}");
        Try("GetMethods", () => t!.GetMethods());
        Try("GetMethod", () => t!.GetMethod("X"));
        Try("GetFields", () => t!.GetFields());
        Try("GetField", () => t!.GetField("X"));
        Try("GetProperties", () => t!.GetProperties());
        Try("GetProperty", () => t!.GetProperty("X"));
        Try("GetConstructors", () => t!.GetConstructors());
        Try("GetConstructor", () => t!.GetConstructor(Type.EmptyTypes));
        Try("GetMembers", () => t!.GetMembers());
        Try("GetMember", () => t!.GetMember("X"));
        Try("GetDefaultMembers", () => t!.GetDefaultMembers());

        Console.WriteLine("== null Type: the properties that dereferenced ==");
        Try("Name", () => _ = t!.Name);
        Try("FullName", () => _ = t!.FullName);
        Try("Namespace", () => _ = t!.Namespace);
        Try("IsValueType", () => _ = t!.IsValueType);
        Try("IsClass", () => _ = t!.IsClass);
        Try("IsPrimitive", () => _ = t!.IsPrimitive);
        Try("IsSealed", () => _ = t!.IsSealed);
        Try("IsEnum", () => _ = t!.IsEnum);
        Try("IsArray", () => _ = t!.IsArray);
        Try("IsInterface", () => _ = t!.IsInterface);
        Try("IsAbstract", () => _ = t!.IsAbstract);
        Try("IsNested", () => _ = t!.IsNested);
        Try("IsPointer", () => _ = t!.IsPointer);
        Try("IsByRef", () => _ = t!.IsByRef);
        Try("IsByRefLike", () => _ = t!.IsByRefLike);
        Try("IsGenericType", () => _ = t!.IsGenericType);
        Try("IsConstructedGenericType", () => _ = t!.IsConstructedGenericType);
        Try("IsGenericTypeDefinition", () => _ = t!.IsGenericTypeDefinition);
        Try("ContainsGenericParameters", () => _ = t!.ContainsGenericParameters);
        Try("HasElementType", () => _ = t!.HasElementType);
        Try("Attributes", () => _ = t!.Attributes);
        Try("IsPublic", () => _ = t!.IsPublic);
        Try("IsNotPublic", () => _ = t!.IsNotPublic);
        Try("IsVisible", () => _ = t!.IsVisible);
        Try("IsNestedPublic", () => _ = t!.IsNestedPublic);
        Try("TypeHandle", () => _ = t!.TypeHandle);
        Try("GetArrayRank", () => t!.GetArrayRank());
        Try("BaseType", () => _ = t!.BaseType);
        Try("GetInterfaces", () => t!.GetInterfaces());
        // The virtual ToString on the same null handle. Roslyn emits it against
        // System.Object::ToString, whose DISPATCH route must not share the string-
        // concat formatter, where a null operand folds to string.Empty.
        Try("ToString", () => _ = t!.ToString());

        Console.WriteLine("== a null ARGUMENT is not a null receiver ==");
        // Type.GetTypeCode is static, so its null is an argument.
        Console.WriteLine($"Type.GetTypeCode(null) = {Type.GetTypeCode(t)}");
        // IsAssignableTo is the argument-SWAPPED IsAssignableFrom, so it cannot share
        // that helper: the swap would put a null target in the receiver's position.
        Console.WriteLine($"typeof(int).IsAssignableTo(null) = {typeof(int).IsAssignableTo(t)}");
        Console.WriteLine($"typeof(int).IsAssignableFrom(null) = {typeof(int).IsAssignableFrom(t)}");
        // ...and IsSubclassOf's null argument is neither: .NET rejects it outright.
        Try("IsSubclassOf(null)", () => _ = typeof(int).IsSubclassOf(t!));
        Console.WriteLine($"typeof(int).IsInstanceOfType(null) = {typeof(int).IsInstanceOfType(null)}");
        Try("null receiver IsAssignableTo", () => _ = t!.IsAssignableTo(typeof(int)));
        Try("null receiver IsAssignableFrom", () => _ = t!.IsAssignableFrom(typeof(int)));
    }
}
