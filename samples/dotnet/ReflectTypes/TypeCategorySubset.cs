using System;
using System.Collections.Generic;

namespace TypeCategorySubset
{
    interface IAnimal { }

    sealed class Dog : IAnimal { }

    class Beast { }                // open (non-sealed) reference class

    abstract class Shape { }       // abstract, non-sealed

    enum Color { Red, Green, Blue }

    enum ByteColor : byte { X }    // enum over byte -> GetTypeCode == Byte

    enum LongFlag : long { X }     // enum over long -> GetTypeCode == Int64

    struct Point { public int X; }

    ref struct RefS { public int X; }   // ref struct -> IsByRefLike

    static class Program
    {
        // IsValueType / IsClass / IsPrimitive as a single "VT C P" line.
        static void Cat(Type t) =>
            Console.WriteLine($"{t.IsValueType} {t.IsClass} {t.IsPrimitive}");

        // IsSealed on its own line. Value types/enums/sealed classes/static classes
        // and arrays are sealed; object/open/abstract classes and interfaces are not.
        static void Seal(Type t) =>
            Console.WriteLine(t.IsSealed);

        // Type.GetTypeCode as its numeric System.TypeCode value.
        static void Tc(Type t) =>
            Console.WriteLine((int)Type.GetTypeCode(t));

        internal static void Run()
        {
            // typeof(...) — covers the primitives, decimal (value type but not
            // primitive), an enum, a struct, a string, a class, and an interface
            // (neither value type nor class). Arrays are exercised via a runtime
            // GetType() below (a typeof(T[]) token has no registered handle).
            Console.WriteLine("== typeof ==");
            Cat(typeof(int));      // True False True
            Cat(typeof(double));   // True False True
            Cat(typeof(char));     // True False True
            Cat(typeof(bool));     // True False True
            Cat(typeof(decimal));  // True False False
            Cat(typeof(Color));    // True False False
            Cat(typeof(Point));    // True False False
            Cat(typeof(string));   // False True False
            Cat(typeof(Dog));      // False True False
            Cat(typeof(IAnimal));  // False False False

            // Runtime GetType() — reads the VALUETYPE/PRIMITIVE flag bits off the
            // same Dn2CppTypeInfo. Boxed primitives, a boxed enum, a boxed struct,
            // a string instance, a class instance and a live array.
            Console.WriteLine("== getType ==");
            object bi = 5;
            object bd = 1.5;
            object bc = 'x';
            object bb = true;
            object be = Color.Green;
            object bp = new Point { X = 1 };
            Cat(bi.GetType());            // True False True
            Cat(bd.GetType());            // True False True
            Cat(bc.GetType());            // True False True
            Cat(bb.GetType());            // True False True
            Cat(be.GetType());            // True False False
            Cat(bp.GetType());            // True False False
            Cat("hi".GetType());          // False True False
            Cat(new Dog().GetType());     // False True False
            Cat(new int[1].GetType());    // False True False

            // IsSealed — typeof folds (static category) and runtime GetType() reads
            // the SEALED flag bit. Reached on the real path from EqualityComparer<T>.
            // GetHashCode's per-type hash mixing.
            Console.WriteLine("== sealed ==");
            Seal(typeof(int));            // True  (value type)
            Seal(typeof(decimal));        // True  (value type)
            Seal(typeof(Color));          // True  (enum)
            Seal(typeof(Point));          // True  (struct)
            Seal(typeof(string));         // True  (sealed class)
            Seal(typeof(Dog));            // True  (sealed class)
            Seal(typeof(Beast));          // False (open class)
            Seal(typeof(Shape));          // False (abstract class)
            Seal(typeof(IAnimal));        // False (interface)
            Seal(typeof(object));         // False
            Seal(typeof(Program));        // True  (static class = abstract+sealed)
            Seal(bi.GetType());           // True  (boxed int)
            Seal(new Dog().GetType());    // True  (sealed class instance)
            Seal(new Beast().GetType());  // False (open class instance)
            Seal(new int[1].GetType());   // True  (array)

            // Type.Assembly identity — op_Equality/op_Inequality on the defining
            // assembly. Reached on the real path from JsonConverter..ctor
            // (IsInternalConverter = GetType().Assembly == typeof(JsonConverter).Assembly).
            Console.WriteLine("== assembly ==");
            Console.WriteLine(typeof(int).Assembly == typeof(string).Assembly);   // True  (both CoreLib)
            Console.WriteLine(typeof(List<int>).Assembly == typeof(int).Assembly);// True  (both CoreLib)
            Console.WriteLine(typeof(Dog).Assembly == typeof(Beast).Assembly);    // True  (both this assembly)
            Console.WriteLine(typeof(int).Assembly == typeof(Dog).Assembly);      // False (CoreLib vs app)
            Console.WriteLine(typeof(Dog).Assembly != typeof(int).Assembly);      // True  (different)
            Console.WriteLine(bi.GetType().Assembly == typeof(string).Assembly);  // True  (boxed int vs string, CoreLib)
            Console.WriteLine(new Dog().GetType().Assembly == typeof(Beast).Assembly); // True (both app)

            // IsPointer / IsByRef / IsByRefLike — the ref-kind Type properties.
            // Reached on the real path from JsonTypeInfo.IsInvalidForSerialization.
            // dn2cpp never materializes a pointer/byref Type, so those are always false;
            // IsByRefLike reads the flag bit (true for a ref struct).
            Console.WriteLine("== refkind ==");
            Console.WriteLine(typeof(int).IsPointer);            // False
            Console.WriteLine(typeof(Dog).IsPointer);            // False
            Console.WriteLine(typeof(int).IsByRef);              // False
            Console.WriteLine(typeof(RefS).IsByRefLike);         // True  (ref struct)
            Console.WriteLine(typeof(Point).IsByRefLike);        // False (normal struct)
            Console.WriteLine(typeof(Dog).IsByRefLike);          // False (class)
            Console.WriteLine(new Dog().GetType().IsByRefLike);  // False (runtime class)

            // Type.GetTypeCode — typeof folds (TypeCodeStatic) and runtime GetType()
            // reads the helper (an enum unwraps to its underlying integer's code).
            // Reached on the real path from ConcurrentDictionary<,>.IsWriteAtomic on
            // STJ's options cache. Primitives map to their code; decimal/DateTime/
            // String to theirs; everything else (IntPtr/UIntPtr, arrays, structs,
            // reference types) is Object. Arrays use runtime GetType() (a typeof(T[])
            // token has no registered handle, like the sealed section). DBNull (code 2)
            // is handled by both fold + helper but not exercised here — dn2cpp emits no
            // DBNull type handle, so typeof(DBNull) is unavailable; it is off any
            // reachable path.
            Console.WriteLine("== typecode ==");
            Tc(typeof(bool));        // 3
            Tc(typeof(char));        // 4
            Tc(typeof(sbyte));       // 5
            Tc(typeof(byte));        // 6
            Tc(typeof(short));       // 7
            Tc(typeof(ushort));      // 8
            Tc(typeof(int));         // 9
            Tc(typeof(uint));        // 10
            Tc(typeof(long));        // 11
            Tc(typeof(ulong));       // 12
            Tc(typeof(float));       // 13
            Tc(typeof(double));      // 14
            Tc(typeof(decimal));     // 15
            Tc(typeof(DateTime));    // 16
            Tc(typeof(string));      // 18
            Tc(typeof(object));      // 1
            Tc(typeof(IntPtr));      // 1  (not a TypeCode -> Object)
            Tc(typeof(UIntPtr));     // 1  (not a TypeCode -> Object)
            Tc(typeof(Color));       // 9  (enum : int -> Int32)
            Tc(typeof(ByteColor));   // 6  (enum : byte -> Byte)
            Tc(typeof(LongFlag));    // 11 (enum : long -> Int64)
            Tc(typeof(Point));       // 1  (struct -> Object)
            Tc(typeof(Dog));         // 1  (class -> Object)
            Tc(typeof(IAnimal));     // 1  (interface -> Object)
            object ti = 7;
            object te = Color.Red;
            object tbe = ByteColor.X;
            object tdt = new DateTime(2020, 1, 1);
            Tc(ti.GetType());           // 9  (boxed int)
            Tc(te.GetType());           // 9  (boxed enum : int)
            Tc(tbe.GetType());          // 6  (boxed enum : byte)
            Tc(tdt.GetType());          // 16 (boxed DateTime)
            Tc("hi".GetType());         // 18 (string instance)
            Tc(new Dog().GetType());    // 1  (class instance)
            Tc(new int[1].GetType());   // 1  (array instance)

            // The one cell where real .NET answers a Type predicate TWO ways in
            // one process, which is why it is asserted here — against a frozen
            // snapshot — and not in the ReflectInvoke diff bucket that carries the
            // rest of this surface (TypePredicateFoldSubset).
            //
            // Measured on .NET 10: `typeof(void).IsPrimitive` written at the call
            // site is TRUE, because RyuJIT expands the property over a `typeof`
            // operand and its CorInfoType classification counts CORINFO_TYPE_VOID
            // as primitive; every non-folded read of the SAME RuntimeType instance
            // — a local, a MethodInfo.ReturnType, Type.GetType("System.Void") — is
            // FALSE. TypedReference, the other exclusion, is FALSE on both arms,
            // and DOTNET_TieredCompilation=0 reproduces the split, so it is not a
            // tiering artefact.
            //
            // dn2cpp answers FALSE on both arms: self-consistent, and equal to
            // .NET's reflection answer. Reproducing the JIT's folded TRUE would
            // mean re-creating an internal disagreement — a fold saying TRUE while
            // dn2cpp_type_is_primitive says FALSE — in order to match a runtime
            // that contradicts itself. Both lines below are dn2cpp
            // deliberately declining to.
            Console.WriteLine("== typeof(void).IsPrimitive: folded, and not ==");
            Type voidFolded = typeof(void);
            Console.WriteLine($"{typeof(void).IsPrimitive} {voidFolded.IsPrimitive}");
        }
    }
}
