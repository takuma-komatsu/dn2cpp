#nullable enable
// A *direct* `enumValue.ToString()` (no explicit box)
// reaches `object::ToString()` through a `constrained.` callvirt on the unboxed
// value. The pre-existing EnumToStringSubset only covers the already-boxed form
// (`object o = e; o.ToString()`); the constrained form on an enum field/local was
// emitted as `dn2cpp_object_tostring(&field)` — the interior pointer to the
// embedded enum was handed to the runtime UNBOXED, which then dereferenced
// garbage as a type-info and SIGSEGV'd. This is exactly what aborted native
// dn2cpp mid-transpile: TypeDesc.ToString does `Primitive.ToString()` on a
// byte-underlying external enum field. Covers a field receiver (ldflda), a local
// receiver (ldloca), int- and byte-underlying enums, a [Flags] enum, and an
// undefined value (-> number). Real System.Private.CoreLib (-r) -> run vs .NET.
using System;

namespace EnumValueToStringSubset
{
    internal enum Kind { Alpha, Beta, Gamma }      // int underlying

    internal enum Code : byte { Zero, One, Two }   // byte underlying (like PrimitiveTypeCode)

    [Flags]
    internal enum Access { None = 0, Read = 1, Write = 2 }

    internal sealed class Holder
    {
        public Kind K;
        public Code C;

        public Holder(Kind k, Code c)
        {
            K = k;
            C = c;
        }

        // Field receivers: `K.ToString()` / `C.ToString()` lower to
        // `ldflda; constrained. <enum>; callvirt object::ToString()`.
        public string Describe()
        {
            return string.Concat(K.ToString(), "/", C.ToString());
        }
    }

    internal static class Program
    {
        internal static int __GateEntry()
        {
            Holder h = new Holder(Kind.Beta, Code.Two);
            Console.WriteLine(h.Describe());        // Beta/Two

            // Local receivers: `local.ToString()` -> ldloca; constrained; callvirt.
            Kind k = Kind.Gamma;
            Console.WriteLine(k.ToString());        // Gamma

            Code c = Code.One;
            Console.WriteLine(c.ToString());        // One

            Access a = Access.Read | Access.Write;
            Console.WriteLine(a.ToString());        // Read, Write

            Kind undefined = (Kind)42;
            Console.WriteLine(undefined.ToString()); // 42

            // Concatenation form ("x=" + e) also goes through the constrained
            // ToString when the value is statically the enum type.
            Console.WriteLine("k=" + k.ToString());  // k=Gamma
            return 0;
        }
    }
}
