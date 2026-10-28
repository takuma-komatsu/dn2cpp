using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace RuntimeTypeHandleNameSubset;

public static unsafe class NameChecks
{
    const string Diagnostic = "RuntimeTypeHandle.ConstructName supports name, display, full and assembly-qualified modes only in the native type-name domain.";
    public class Outer<T> { public class Inner<U> { } }
    public delegate string Bound(int flags);
    delegate string Route(ref RuntimeTypeHandle handle, int flags);
    static string Public(ref RuntimeTypeHandle handle, int flags)
    {
        Type type = Type.GetTypeFromHandle(handle);
        if (type == null) throw new ArgumentNullException(null, "Invalid handle.");
        return flags == 0 ? type.Name : flags == 1 ? type.ToString() : flags == 3 ? type.FullName : type.AssemblyQualifiedName;
    }
    // Native-only copies replace these bodies with exact retained method routes.
    static string Direct(ref RuntimeTypeHandle handle, int flags) => Public(ref handle, flags);
    static string Pointer(ref RuntimeTypeHandle handle, int flags) => Public(ref handle, flags);
    static Bound Address(RuntimeTypeHandle handle) => flags => Public(ref handle, flags);
    static string BoundRoute(ref RuntimeTypeHandle handle, int flags) => Address(handle)(flags);
    static string ReflectionRoute(ref RuntimeTypeHandle handle, int flags)
    {
        MethodInfo method = Address(handle).Method;
        object enumValue = Enum.ToObject(method.GetParameters()[0].ParameterType, flags);
        return (string)method.Invoke(handle, new object[] { enumValue });
    }
    static Type[] Supported() => new Type[] { typeof(int), typeof(void), typeof(Outer<>), typeof(Outer<int>.Inner<string>),
        typeof(int[]), typeof(int[,]), typeof(int*), typeof(int).MakeByRefType() };
    static Type[] PublicNullTypes()
    {
        Type formal = typeof(Outer<>).GetGenericArguments()[0];
        return new[] { formal, typeof(Dictionary<,>).MakeGenericType(typeof(string), formal), typeof(delegate* managed<int, void>) };
    }
    static void Check(string label, bool value)
    {
        Console.WriteLine("type-name-" + label + ":" + value);
        if (!value) throw new InvalidOperationException(label);
    }
    static void Culture()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }
    public static void Run()
    {
        Culture(); Console.WriteLine("== RuntimeTypeHandle names ==");
        int index = 0;
        foreach (Type type in Supported())
        {
            RuntimeTypeHandle handle = type.TypeHandle;
            foreach (int flags in new int[] { 0, 1, 3, 7 })
                Console.WriteLine("type-name-public-" + index + "-" + flags + ":" + Public(ref handle, flags));
            index++;
        }
        index = 0;
        foreach (Type type in PublicNullTypes())
        {
            Console.WriteLine("type-name-public-domain-" + index + "-name:" + type.Name);
            Console.WriteLine("type-name-public-domain-" + index + "-display:" + type.ToString());
            Console.WriteLine("type-name-public-domain-" + index + "-null:" + (type.FullName == null && type.AssemblyQualifiedName == null));
            index++;
        }
        Console.WriteLine("RuntimeTypeHandle names end");
    }
    static void Routes(string name, Route route)
    {
        int count = 0;
        foreach (Type type in Supported())
        {
            RuntimeTypeHandle handle = type.TypeHandle;
            foreach (int flags in new int[] { 0, 1, 3, 7 })
            {
                if (route(ref handle, flags) != Public(ref handle, flags)) throw new InvalidOperationException(name + ":" + type + ":" + flags);
                count++;
            }
        }
        Check(name, count == 32);
    }
    public static void RunArtifact(string[] args)
    {
        Culture();
        if (args.Length != 0) { Policy(); return; }
        Run(); Routes("direct", Direct); Routes("calli", Pointer); Routes("delegate", BoundRoute); Routes("reflection", ReflectionRoute);
    }
    public static void RunReflectionArtifact() { Run(); Routes("reflection", ReflectionRoute); }
    static void Policy()
    {
        RuntimeTypeHandle empty = default;
        foreach (int flags in new int[] { 0, 1, 3, 7, 64 })
        {
            bool ordered = false;
            try { Direct(ref empty, flags); }
            catch (ArgumentNullException ex) { ordered = ex.ParamName == null && ex.Message == "Invalid handle." && ex.HResult == unchecked((int)0x80004003); }
            Check("default-before-flags-" + flags, ordered);
        }
        foreach (int flags in new int[] { 0, 64 })
        {
            bool refused = false;
            try { Direct(ref Unsafe.NullRef<RuntimeTypeHandle>(), flags); }
            catch (NullReferenceException) { refused = true; }
            Check("null-storage-" + flags, refused);
        }
        RuntimeTypeHandle handle = typeof(int).TypeHandle;
        foreach (int flags in new int[] { 2, 4, 8, 64, 128, 256, -1 })
        {
            bool refused = false;
            try { Direct(ref handle, flags); } catch (PlatformNotSupportedException ex) { refused = ex.Message == Diagnostic; }
            Check("unsupported-" + flags, refused);
        }
        int index = 0;
        foreach (Type type in PublicNullTypes())
        {
            Check("public-null-" + index, type.FullName == null && type.AssemblyQualifiedName == null);
            handle = type.TypeHandle;
            foreach (int flags in new int[] { 3, 7 })
            {
                bool refused = false;
                try { Direct(ref handle, flags); } catch (PlatformNotSupportedException ex) { refused = ex.Message == Diagnostic; }
                Check("domain-" + index + "-" + flags, refused);
            }
            Check("basic-" + index, Direct(ref handle, 0) == type.Name && Direct(ref handle, 1) == type.ToString());
            index++;
        }
    }
}
