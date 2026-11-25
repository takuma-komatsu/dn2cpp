#nullable disable
// (practical reflection): Nullable.GetUnderlyingType(Type) + typeof(T?). A serializer
// / data-binding layer detects a nullable property type with Nullable.GetUnderlyingType. The
// carve-out was that typeof(int?) emitted a reference to an un-emitted Nullable<Int32>
// type-info (a closed generic value type otherwise unreached). Now typeof(T?) notes the
// closed Nullable<T> for emission, so its type-info carries genericDef=Nullable`1 +
// genericArgs=[T] (the closed-generic metadata); Nullable.GetUnderlyingType reads that
// arg back, and IsGenericType / GetGenericArguments work too. Nullable<T> *values* (int?
// locals, lifted operators, the special box/unbox) are pinned separately by
// NullableValueSubset. Diffed exact vs real.NET.
using System;
using System.Reflection;

namespace ReflectNullableSubset;

public static class Program
{
    static string U(Type t) => Nullable.GetUnderlyingType(t)?.Name ?? "null";

    internal static void Run()
    {
        Console.WriteLine(U(typeof(int?)));
        Console.WriteLine(U(typeof(double?)));
        Console.WriteLine(U(typeof(bool?)));
        Console.WriteLine(U(typeof(int)));     // null — not nullable
        Console.WriteLine(U(typeof(string)));  // null — reference type

        // IsGenericType / GetGenericArguments on a Nullable type
        Type nt = typeof(long?);
        Console.WriteLine(nt.IsGenericType);
        Console.WriteLine(nt.GetGenericArguments()[0].Name);

        // serializer-style: use the underlying type when nullable, else the type itself
        Type prop = typeof(int?);
        Type effective = Nullable.GetUnderlyingType(prop) ?? prop;
        Console.WriteLine("effective=" + effective.Name);
    }

    sealed class StructConstraint<T> where T : struct { }

    sealed class NullableOverride : TypeDelegator
    {
        readonly bool _definition;
        internal NullableOverride(bool definition = false) : base(typeof(int?)) { _definition = definition; }
        public override bool IsGenericTypeDefinition => _definition;
        internal Type FromBase() => base.GetNullableUnderlyingType();
        public override Type GetNullableUnderlyingType() => typeof(byte);
    }

    static Type Underlying(Type type) => type.GetNullableUnderlyingType();

    internal static void RunTypeQuery()
    {
        Console.WriteLine("== Type.GetNullableUnderlyingType ==");
        Type definition = typeof(Nullable<>);
        Type formal = definition.GetGenericArguments()[0];
        Console.WriteLine("closed=" + (Underlying(typeof(int?)) == typeof(int))
            + " plain=" + (Underlying(typeof(int)) == null)
            + " reference=" + (Underlying(typeof(string)) == null));
        Console.WriteLine("open formal=" + ReferenceEquals(Underlying(definition), formal)
            + " static=" + (Nullable.GetUnderlyingType(definition) == null));
        Type otherFormal = typeof(StructConstraint<>).GetGenericArguments()[0];
        Type partial = definition.MakeGenericType(otherFormal);
        Console.WriteLine("open constructed=" + ReferenceEquals(Underlying(partial), otherFormal));
        Type delegator = new TypeDelegator(typeof(long?));
        var custom = new NullableOverride();
        Func<Type> bound = custom.GetNullableUnderlyingType;
        Console.WriteLine("delegator=" + (Underlying(delegator) == typeof(long))
            + " override=" + (bound() == typeof(byte))
            + " base=" + (custom.FromBase() == typeof(int))
            + " static=" + (Nullable.GetUnderlyingType(custom) == typeof(byte))
            + " skip definition=" + (Nullable.GetUnderlyingType(new NullableOverride(true)) == null));
        const string name = nameof(Type.GetNullableUnderlyingType);
        var method = typeof(Type).GetMethod(name);
        Type runtime = Type.GetType("System.RuntimeType", true);
        Func<Type> native = typeof(int?).GetNullableUnderlyingType;
        var declared = runtime.GetMethod(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Console.WriteLine("reflected=" + ((Type)method.Invoke(typeof(long?), null) == typeof(long))
            + "/" + ((Type)method.Invoke(custom, null) == typeof(byte))
            + " runtime=" + (runtime.GetMethod(name)?.DeclaringType == runtime)
            + "/" + (declared?.DeclaringType == runtime) + "/" + (native.Method.DeclaringType == runtime));
        try { Nullable.GetUnderlyingType(null); }
        catch (ArgumentNullException e) { Console.WriteLine("static null=" + e.GetType().Name + "/" + e.ParamName); }
        try { Underlying(null); }
        catch (NullReferenceException) { Console.WriteLine("null receiver=NullReferenceException"); }

        Console.WriteLine("== lowered custom Type ==");
        try { Console.WriteLine("equals=" + delegator.Equals(typeof(long?))); }
        catch (PlatformNotSupportedException) { Console.WriteLine("equals=PlatformNotSupportedException"); }
        try { Console.WriteLine("equals Type argument=" + typeof(long?).Equals(delegator)); }
        catch (PlatformNotSupportedException) { Console.WriteLine("equals Type argument=PlatformNotSupportedException"); }
        Console.WriteLine("equals object argument=" + typeof(long?).Equals((object)delegator));
        try { Console.WriteLine("handle=" + delegator.TypeHandle.Equals(typeof(long?).TypeHandle)); }
        catch (PlatformNotSupportedException) { Console.WriteLine("handle=PlatformNotSupportedException"); }
        try { Console.WriteLine("subclass argument=" + typeof(string).IsSubclassOf(delegator)); }
        catch (PlatformNotSupportedException) { Console.WriteLine("subclass argument=PlatformNotSupportedException"); }
    }
}
