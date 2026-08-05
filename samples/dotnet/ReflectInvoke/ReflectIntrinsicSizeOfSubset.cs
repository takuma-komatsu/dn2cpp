#nullable enable
// SUBJECT: reflection over an INTRINSIC type's metadata-answerable member —
// Unsafe.SizeOf<T> reached as GetMethod("SizeOf").MakeGenericMethod(t).Invoke.
// An intrinsic type carries no reflection method rows (its members are lowered
// inline), so the runtime synthesizes the row and answers from the type
// argument's metadata. Every line diffs exact against real .NET, including the
// two traps: a reference type's SizeOf is pointer width, and a primitive's or
// enum's is the field-layout width, not a box payload width.
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ReflectIntrinsicSizeOfSubset;

struct Bytes3 { public byte A, B, C; }

struct Pair<T> { public T V; public int N; }

struct Blank { }

enum Small : short { A }

enum Plain { A }

enum Wide : long { A }

class Boxy { public int X; }

interface IMarker { }

static class Program
{
    // The definition is fetched once, closed over a type known only at run time,
    // and invoked. No static call site names any of these instantiations.
    static int SizeOf(Type t) =>
        (int)typeof(Unsafe).GetMethod("SizeOf")!.MakeGenericMethod(t).Invoke(null, null)!;

    static void Try(string label, Action a)
    {
        try { a(); Console.WriteLine($"{label}: OK"); }
        catch (Exception e) { Console.WriteLine($"{label}: {e.GetType().Name}"); }
    }

    internal static void Run()
    {
        Console.WriteLine("== the definition handle ==");
        MethodInfo? def = typeof(Unsafe).GetMethod("SizeOf");
        Console.WriteLine($"found: {def is not null}");
        Console.WriteLine($"name={def!.Name} ret={def.ReturnType.Name} params={def.GetParameters().Length}");
        Console.WriteLine($"decl={def.DeclaringType!.FullName}");
        Console.WriteLine($"static={def.IsStatic} public={def.IsPublic} generic={def.IsGenericMethod} genericDef={def.IsGenericMethodDefinition}");
        // A late-bound call on an open generic method definition is an error, not
        // a default answer.
        Try("invoke the definition", () => def.Invoke(null, null));

        Console.WriteLine("== handle identity ==");
        MethodInfo? again = typeof(Unsafe).GetMethod("SizeOf");
        Console.WriteLine($"def interned: {ReferenceEquals(def, again)} equals: {def.Equals(again)}");
        MethodInfo c1 = def.MakeGenericMethod(typeof(int));
        MethodInfo c2 = def.MakeGenericMethod(typeof(int));
        Console.WriteLine($"closed interned: {ReferenceEquals(c1, c2)} equals: {c1.Equals(c2)}");
        Console.WriteLine($"closed genericDef={c1.IsGenericMethodDefinition} arg={c1.GetGenericArguments()[0].Name}");
        Console.WriteLine($"closed != other arg: {c1.Equals(def.MakeGenericMethod(typeof(long)))}");

        Console.WriteLine("== value types ==");
        foreach (Type t in new[] { typeof(int), typeof(uint), typeof(byte), typeof(sbyte),
                                   typeof(short), typeof(ushort), typeof(long), typeof(ulong),
                                   typeof(float), typeof(double), typeof(bool), typeof(char),
                                   typeof(IntPtr), typeof(Bytes3), typeof(Blank) })
            Console.WriteLine($"{t.Name} -> {SizeOf(t)}");

        Console.WriteLine("== generic value types ==");
        Console.WriteLine($"Pair<int> -> {SizeOf(typeof(Pair<int>))}");
        Console.WriteLine($"Pair<long> -> {SizeOf(typeof(Pair<long>))}");
        Console.WriteLine($"Pair<byte> -> {SizeOf(typeof(Pair<byte>))}");

        Console.WriteLine("== nullable (the hasValue+value layout, not a shell) ==");
        // No static call site names Nullable<U> beyond these typeof tokens, so the
        // emitter sees a reference-only type and emits an opaque shell struct; its
        // instanceSize must still be the modeled hasValue+value extent.
        Console.WriteLine($"int? -> {SizeOf(typeof(int?))}");
        Console.WriteLine($"long? -> {SizeOf(typeof(long?))}");
        Console.WriteLine($"byte? -> {SizeOf(typeof(byte?))}");
        Console.WriteLine($"Bytes3? -> {SizeOf(typeof(Bytes3?))}");
        Console.WriteLine($"Pair<long>? -> {SizeOf(typeof(Pair<long>?))}");
        // A second reader of the same instanceSize stamp: a reflection-created
        // array's element stride. Too small and the elements overlap, which the
        // null/values round-trip below detects.
        Array na = Array.CreateInstance(typeof(long?), 3);
        na.SetValue(7L, 0);
        na.SetValue(9L, 2);
        Console.WriteLine($"long?[3]: [{na.GetValue(0)}, {(na.GetValue(1) is null ? "null" : "set")}, {na.GetValue(2)}]");

        Console.WriteLine("== enums (the underlying width, not the box width) ==");
        Console.WriteLine($"Small(:short) -> {SizeOf(typeof(Small))}");
        Console.WriteLine($"Plain(:int) -> {SizeOf(typeof(Plain))}");
        Console.WriteLine($"Wide(:long) -> {SizeOf(typeof(Wide))}");

        Console.WriteLine("== reference types (pointer width) ==");
        foreach (Type t in new[] { typeof(object), typeof(string), typeof(Boxy),
                                   typeof(int[]), typeof(Bytes3[]), typeof(Action), typeof(IMarker) })
            Console.WriteLine($"{t.Name} -> {SizeOf(t)}");
        Console.WriteLine($"pointer width matches IntPtr.Size: {SizeOf(typeof(object)) == IntPtr.Size}");

        Console.WriteLine("== lookup filters the synthesized row honours ==");
        Try("wrong arity", () => def.MakeGenericMethod(typeof(int), typeof(long)));
        Console.WriteLine($"arity 2: {typeof(Unsafe).GetMethod("SizeOf", 2, Type.EmptyTypes) is null}");
        Console.WriteLine($"arity 1: {typeof(Unsafe).GetMethod("SizeOf", 1, Type.EmptyTypes) is null}");
        Console.WriteLine($"with a parameter: {typeof(Unsafe).GetMethod("SizeOf", new[] { typeof(int) }) is null}");
        Console.WriteLine($"NonPublic|Instance: {typeof(Unsafe).GetMethod("SizeOf", BindingFlags.NonPublic | BindingFlags.Instance) is null}");
        Console.WriteLine($"Public|Static: {typeof(Unsafe).GetMethod("SizeOf", BindingFlags.Public | BindingFlags.Static) is null}");
        Console.WriteLine($"unmodeled name: {typeof(Unsafe).GetMethod("NoSuchMember") is null}");

        Console.WriteLine("== the Arch registry loop ==");
        // An ECS component registry's shape: a per-type size looked up over a
        // component set assembled at run time.
        Type[] components = { typeof(int), typeof(Bytes3), typeof(Pair<long>), typeof(Small), typeof(Boxy) };
        int total = 0;
        foreach (Type t in components)
            total += SizeOf(t);
        Console.WriteLine($"total component size: {total}");

        Console.WriteLine("== every size reader agrees, per type ==");
        // An intrinsic-represented value type (the date/decimal family) is lowered
        // to a hand-written C++ struct, so its instanceSize is that struct's sizeof.
        // INVARIANT: all three readers of that number agree — the statically lowered
        // Unsafe.SizeOf<T>, the same method reached through reflection, and the byte
        // stride pointer arithmetic actually steps. A fix that makes the reflected
        // size match .NET at one reader alone leaves callers sizing buffers from a
        // number the stride disagrees with; repack the representation instead.
        SizeReadersAgree<DateTime>("DateTime");
        SizeReadersAgree<TimeSpan>("TimeSpan");
        SizeReadersAgree<DateTimeOffset>("DateTimeOffset");
        SizeReadersAgree<DateOnly>("DateOnly");
        SizeReadersAgree<TimeOnly>("TimeOnly");
        SizeReadersAgree<decimal>("Decimal");
        SizeReadersAgree<Guid>("Guid");
        SizeReadersAgree<Bytes3>("Bytes3");
        SizeReadersAgree<Pair<long>>("Pair<long>");
    }

    // Printed as booleans, not sizes: the agreement is the assertion, and the
    // number itself may legitimately differ from .NET's for an intrinsic type.
    static void SizeReadersAgree<T>(string label)
    {
        int stat = Unsafe.SizeOf<T>();
        int reflected = SizeOf(typeof(T));
        T[] a = new T[2];
        int stride = (int)(long)Unsafe.ByteOffset(ref a[0], ref a[1]);
        Console.WriteLine($"{label}: static==reflected {stat == reflected}, "
            + $"static==stride {stat == stride}, positive {stat > 0}");
    }
}
