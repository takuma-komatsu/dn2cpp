#nullable disable
using System;

namespace InterfaceElementArraySubset
{
    // An interface-element array's TYPE-INFO PRECISION, not element access: the subject is
    // the emitter (CppEmitter.FieldTypeInfoExpr), which must not degrade a cross-assembly
    // element to System.Object — an IDisposable[] indistinguishable from object[] is
    // silent at every elementType reader. Asserted here: GetElementType(), typeof identity
    // both ways, isinst/IsAssignableFrom array covariance (the discriminating direction is
    // object[] -> IDisposable[] reading FALSE), and Array.Copy's pair verdict — int[] ->
    // IDisposable[] must refuse, and the reverse-assignable object[] -> IDisposable[] pair
    // must cast-check per element and fault mid-copy. The delegate line pins the same
    // precision for a cross-assembly CLASS element.
    internal static class Program
    {
        private sealed class Disp : IDisposable
        {
            public void Dispose() { }

            public override string ToString() => "Disp";
        }

        private static void Try(string label, Action a)
        {
            try { a(); }
            catch (Exception e) { Console.WriteLine(label + ": throws " + e.GetType().Name); }
        }

        public static void Run()
        {
            Console.WriteLine("-- itfelem: identity --");
            IDisposable[] a = new IDisposable[2];
            Console.WriteLine("type: " + a.GetType());
            Console.WriteLine("elem: " + a.GetType().GetElementType());
            Console.WriteLine("elem==typeof(IDisposable): " + (a.GetType().GetElementType() == typeof(IDisposable)));
            Console.WriteLine("type==typeof(IDisposable[]): " + (a.GetType() == typeof(IDisposable[])));
            Console.WriteLine("type==typeof(object[]): " + (a.GetType() == typeof(object[])));
            Console.WriteLine("delegate elem==typeof(Action): " + (new Action[1].GetType().GetElementType() == typeof(Action)));

            Console.WriteLine("-- itfelem: covariance --");
            object[] oa = new object[1];
            Disp[] da = new Disp[] { new Disp() };
            Console.WriteLine("object[] is IDisposable[]: " + (((object)oa) is IDisposable[]));
            Console.WriteLine("Disp[] is IDisposable[]: " + (((object)da) is IDisposable[]));
            Console.WriteLine("IDisposable[] is object[]: " + (((object)a) is object[]));
            Console.WriteLine("IsAssignableFrom(Disp[]): " + typeof(IDisposable[]).IsAssignableFrom(typeof(Disp[])));
            Console.WriteLine("IsAssignableFrom(object[]): " + typeof(IDisposable[]).IsAssignableFrom(typeof(object[])));

            Console.WriteLine("-- itfelem: Array.Copy verdicts --");
            Try("int->IDisposable", () => { Array s = new int[] { 1 }; Array.Copy(s, a, 1); });
            Try("zero-len int->IDisposable", () => { Array s = new int[1]; Array.Copy(s, new IDisposable[1], 0); });
            Try("Disp->IDisposable", () => { Array.Copy(da, a, 1); Console.WriteLine("Disp->IDisposable: ok " + a[0]); });
            IDisposable[] a2 = new IDisposable[2];
            object[] mixed = new object[] { new Disp(), "no" };
            Try("mixed object->IDisposable", () => Array.Copy(mixed, a2, 2));
            Console.WriteLine("partial: [" + (a2[0] is null ? "null" : a2[0].ToString()) + " " + (a2[1] is null ? "null" : a2[1].ToString()) + "]");
        }
    }
}
