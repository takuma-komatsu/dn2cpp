#nullable enable
using System;
using System.Reflection;

namespace ReflectGenericMethodSubset
{
    // SUBJECT: the generic-method reflection surface — MakeGenericMethod resolves
    // within the instantiations the program reaches STATICALLY (each explicit call
    // below is what puts the closed row in the image), GetGenericMethodDefinition
    // round-trips by definition identity, GetBaseDefinition walks virtual slots.
    // The intentional AOT divergences are frozen in the reflect-types bucket.
    static class Helper
    {
        public static string Describe<T>(T value) => typeof(T).Name + ":" + value;
        public static int TwoArg<T1, T2>(int seed) => seed + typeof(T1).Name.Length + typeof(T2).Name.Length;
        public static int Plain(int x) => x + 1;
    }

    class Box
    {
        public string Tag<T>(T v) => typeof(T).Name + "=" + v;
    }

    class BaseV
    {
        public virtual string V() => "BaseV.V";
        public virtual string W() => "BaseV.W";
        public string NV() => "BaseV.NV";
    }

    class MidV : BaseV
    {
        public override string V() => "MidV.V";
    }

    class DerV : MidV
    {
        public override string V() => "DerV.V";
        public new virtual string W() => "DerV.W";
    }

    static class Program
    {
        internal static void Run()
        {
            // Put the closed instantiations in the image (the AOT resolution set).
            Console.WriteLine($"direct => {Helper.Describe<int>(42)}");
            Console.WriteLine($"direct2 => {Helper.TwoArg<int, string>(10)}");
            Console.WriteLine($"direct3 => {new Box().Tag<double>(1.5)}");

            MethodInfo describe = typeof(Helper).GetMethod("Describe")!;
            Console.WriteLine($"gm-isgeneric => {describe.IsGenericMethod}");

            // MakeGenericMethod over an in-image instantiation, then invoke it.
            MethodInfo closed = describe.MakeGenericMethod(typeof(int));
            Console.WriteLine($"gm-make => {closed.Invoke(null, new object[] { 7 })}");
            Console.WriteLine($"gm-make-args => {closed.GetGenericArguments()[0].Name}");
            Console.WriteLine($"gm-make-isdef => {closed.IsGenericMethodDefinition}");

            // Definition round-trip: the definitions reached from two different
            // handles of the same method compare equal.
            MethodInfo def1 = describe.GetGenericMethodDefinition();
            MethodInfo def2 = closed.GetGenericMethodDefinition();
            Console.WriteLine($"gm-def-eq => {def1 == def2}");
            Console.WriteLine($"gm-def-isdef => {def1.IsGenericMethodDefinition}");
            Console.WriteLine($"gm-def-name => {def1.Name}");

            // MakeGenericMethod on the definition view re-resolves in-image.
            MethodInfo remade = def1.MakeGenericMethod(typeof(int));
            Console.WriteLine($"gm-remake => {remade.Invoke(null, new object[] { 9 })}");

            // Two-parameter arity + wrong-arity ArgumentException.
            MethodInfo two = typeof(Helper).GetMethod("TwoArg")!;
            MethodInfo twoClosed = two.MakeGenericMethod(typeof(int), typeof(string));
            Console.WriteLine($"gm-two => {twoClosed.Invoke(null, new object[] { 100 })}");
            try
            {
                two.MakeGenericMethod(typeof(int));
                Console.WriteLine("gm-arity => no exception");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("gm-arity => ArgumentException");
            }

            // Non-generic receivers reject the generic-method surface.
            MethodInfo plain = typeof(Helper).GetMethod("Plain")!;
            Console.WriteLine($"gm-plain-isgeneric => {plain.IsGenericMethod}");
            try
            {
                plain.MakeGenericMethod(typeof(int));
                Console.WriteLine("gm-plain-make => no exception");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("gm-plain-make => InvalidOperationException");
            }
            try
            {
                plain.GetGenericMethodDefinition();
                Console.WriteLine("gm-plain-def => no exception");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("gm-plain-def => InvalidOperationException");
            }
            Console.WriteLine($"gm-plain-args => {plain.GetGenericArguments().Length}");

            // Instance generic method through the same route.
            MethodInfo tag = typeof(Box).GetMethod("Tag")!.MakeGenericMethod(typeof(double));
            Console.WriteLine($"gm-instance => {tag.Invoke(new Box(), new object[] { 2.5 })}");

            // GetBaseDefinition: override chains resolve to the root declaration,
            // a `new` slot roots at its own declaration, non-virtuals are their
            // own base definition.
            MethodInfo derV = typeof(DerV).GetMethod("V")!;
            Console.WriteLine($"basedef-override => {derV.GetBaseDefinition().DeclaringType!.Name}");
            MethodInfo midV = typeof(MidV).GetMethod("V")!;
            Console.WriteLine($"basedef-mid => {midV.GetBaseDefinition().DeclaringType!.Name}");
            Console.WriteLine($"basedef-eq => {derV.GetBaseDefinition() == typeof(BaseV).GetMethod("V")}");
            MethodInfo derW = typeof(DerV).GetMethod("W",
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;
            Console.WriteLine($"basedef-new => {derW.GetBaseDefinition().DeclaringType!.Name}");
            MethodInfo baseW = typeof(BaseV).GetMethod("W")!;
            Console.WriteLine($"basedef-root => {baseW.GetBaseDefinition().DeclaringType!.Name}");
            MethodInfo nv = typeof(DerV).GetMethod("NV")!;
            Console.WriteLine($"basedef-nonvirtual => {nv.GetBaseDefinition().DeclaringType!.Name}");
        }
    }
}
