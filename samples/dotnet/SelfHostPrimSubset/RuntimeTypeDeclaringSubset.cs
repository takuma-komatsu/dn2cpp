using System;
using System.Globalization;
using System.Reflection;

namespace RuntimeTypeDeclaringSubset;

public class Container<T>
{
    public class Nested<U> { }
    public class Plain { }
    public enum Choice { One }
    public static void GenericMethod<U>() { }
}
public class OrdinaryOwner { public class Nested { } public enum Choice { One } private class Hidden { } public static Type HiddenType() => typeof(Hidden); }
public static unsafe class DeclaringChecks
{
    const string Missing = "RuntimeType.GetDeclaringType: enclosing type identity is not retained";
    public delegate Type Caller(Type type);
    static Type Direct(Type type) => type.DeclaringType;
    static Type Pointer(Type type) => type.DeclaringType;
    static Caller Address() => type => type.DeclaringType;
    static Type Bound(Type type) => Address()(type);
    static Type Reflected(Type type) => (Type)Address().Method.Invoke(null, new object[] { type });
    // The fixture creates genuine managed RuntimeTypeCache storage, never a fake RuntimeType payload.
    static bool Cache(Type type) => throw new InvalidOperationException("Unrewritten cache fixture");
    static Type[] Supported() => new Type[] {
        typeof(int), typeof(Container<>), typeof(Container<int>),
        typeof(Container<>).GetGenericArguments()[0],
        typeof(Container<>).GetMethod("GenericMethod").GetGenericArguments()[0],
        typeof(Container<int>).GetMethod("GenericMethod").GetGenericArguments()[0],
        typeof(Container<int>.Plain[]), typeof(Container<int>.Plain[,]), Array.CreateInstance(typeof(Container<int>.Plain), new[] { 1 }, new[] { 1 }).GetType(),
        typeof(Container<int>.Plain).MakePointerType(), typeof(Container<int>.Plain).MakeByRefType(), typeof(delegate* managed<int, void>) };
    static Type[] Nested() => new Type[] { typeof(OrdinaryOwner.Nested), typeof(OrdinaryOwner.Choice),
        typeof(Container<>.Nested<>), typeof(Container<int>.Nested<string>), typeof(Container<int>.Plain), typeof(Container<int>.Choice), OrdinaryOwner.HiddenType(),
        typeof(Container<>.Nested<>).MakeGenericType(typeof(long), typeof(byte)),
        typeof(System.Runtime.CompilerServices.ConfiguredTaskAwaitable<int>.ConfiguredTaskAwaiter),
        typeof(System.Runtime.CompilerServices.ConfiguredTaskAwaitable<int>.ConfiguredTaskAwaiter).GetGenericTypeDefinition() };
    static void Culture() { CultureInfo.CurrentCulture=CultureInfo.InvariantCulture; CultureInfo.CurrentUICulture=CultureInfo.InvariantCulture; }
    static void Check(string label, bool value) { Console.WriteLine("declaring-type-"+label+":"+value); if(!value)throw new InvalidOperationException(label); }
    public static void Run()
    {
        Culture(); Console.WriteLine("== RuntimeType declaring owner =="); int i=0;
        foreach(Type type in Supported()) Console.WriteLine("declaring-type-public-"+(i++)+":"+(type.DeclaringType?.ToString()??"<null>"));
        Console.WriteLine("RuntimeType declaring owner end");
    }
    public static void RunNestedOracle()
    {
        Culture(); int i=0;
        foreach(Type type in Nested())
        {
            Check("clr-nested-"+i,type.DeclaringType==(i is 0 or 1 or 6 ? typeof(OrdinaryOwner) : i >= 8 ? typeof(System.Runtime.CompilerServices.ConfiguredTaskAwaitable<>) : typeof(Container<>)));
            Console.WriteLine("declaring-type-clr-owner-"+i+":"+type.DeclaringType); i++;
        }
    }
    static void Routes(string name, Caller route)
    {
        int i=0;
        foreach(Type type in Supported())
        {
            Type expected = i is >=3 and <=5 ? typeof(Container<>) : null;
            Check(name+"-"+i, route(type)==expected); i++;
        }
        bool nullRefused=false;
        try { route(null); } catch(NullReferenceException) when(name!="reflection") { nullRefused=true; }
        catch(TargetInvocationException ex) when(name=="reflection") { nullRefused=ex.InnerException is NullReferenceException; }
        Check(name+"-null",nullRefused);
        i=0;
        foreach(Type type in Nested())
        {
            bool refused=false;
            try { route(type); } catch(PlatformNotSupportedException ex) when(name!="reflection") { refused=ex.Message==Missing; }
            catch(TargetInvocationException ex) when(name=="reflection") { refused=ex.InnerException is PlatformNotSupportedException && ex.InnerException.Message==Missing; }
            Check(name+"-nested-"+(i++),refused);
        }
    }
    public static void RunArtifact()
    {
        Run(); Routes("direct",Direct); Routes("calli",Pointer); Routes("delegate",Bound); Routes("reflection",Reflected);
        Check("cache-void-sentinel",Cache(typeof(int)));
    }
    public static void RunReflectionArtifact() { Run(); Routes("reflection",Reflected); }
}
