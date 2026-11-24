#nullable enable
// SUBJECT: Type.GetInterface(name[, ignoreCase]) over the runtime interface
// table. The name splits at its LAST '.'; ignoreCase folds the SIMPLE-NAME part
// only, so a wrong-cased namespace stays a miss; a closed generic matches by its
// definition's mangled simple name ("IEnumerable`1"); two matching rows throw
// AmbiguousMatchException; there is NO prefix matching, unlike GetMember.
// Generic-result probes print .Name, never .FullName — a closed generic's
// FullName is dn2cpp-mangled while Name normalizes on both sides.
//
// SUBJECT (tail): the single-attribute getters' ambiguity contract. BaseAttr and
// DerivedAttr both sit on one method and on the assembly, so
// GetCustomAttribute(typeof(BaseAttr)) matches two rows and must throw.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

[assembly: GetInterfaceSubset.BaseAttr]
[assembly: GetInterfaceSubset.DerivedAttr]
[module: GetInterfaceSubset.DerivedAttr]

namespace GetInterfaceSubset.NsA
{
    public interface IFoo { }
}

namespace GetInterfaceSubset.NsB
{
    public interface IFoo { }
}

namespace GetInterfaceSubset
{
    class Simple : IDisposable
    {
        public void Dispose() { }
    }

    // The enumerator bodies are never called — the subject is the interface TABLE.
    class Generic : IEnumerable<int>
    {
        public IEnumerator<int> GetEnumerator() { throw new NotSupportedException(); }
        IEnumerator IEnumerable.GetEnumerator() { throw new NotSupportedException(); }
    }

    class Both : IEnumerable<int>, IEnumerable<string>
    {
        public IEnumerator<int> GetEnumerator() { throw new NotSupportedException(); }
        IEnumerator<string> IEnumerable<string>.GetEnumerator() { throw new NotSupportedException(); }
        IEnumerator IEnumerable.GetEnumerator() { throw new NotSupportedException(); }
    }

    class TwoNs : NsA.IFoo, NsB.IFoo { }

    public class BaseAttr : Attribute { }
    public class DerivedAttr : BaseAttr
    {
        public static int Constructions;
        public DerivedAttr() => Constructions++;
    }

    class Attributed
    {
        [BaseAttr]
        [DerivedAttr]
        public void M() { }
    }

    static class Program
    {
        // A null read through a static field so nothing folds it at the call site.
        static readonly string? NullName = null;

        static void PF(string label, Func<Type?> f)
        {
            try
            {
                Type? t = f();
                Console.WriteLine($"{label}: {(t is null ? "null" : t.FullName)}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"{label}: {e.GetType().Name}");
            }
        }

        static void PN(string label, Func<Type?> f)
        {
            try
            {
                Type? t = f();
                Console.WriteLine($"{label}: {(t is null ? "null" : t.Name)}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"{label}: {e.GetType().Name}");
            }
        }

        internal static void Run()
        {
            Console.WriteLine("== Type.GetInterface ==");
            Type s = typeof(Simple);
            PF("simple", () => s.GetInterface("IDisposable"));
            PF("qualified", () => s.GetInterface("System.IDisposable"));
            PF("partial-ns", () => s.GetInterface("m.IDisposable"));
            PF("wrong-case", () => s.GetInterface("idisposable"));
            PF("wrong-case ic", () => s.GetInterface("idisposable", true));
            PF("wrong-ns-case ic", () => s.GetInterface("system.idisposable", true));
            PF("ns-ok folded ic", () => s.GetInterface("System.IDISPOSABLE", true));
            PF("no-match", () => s.GetInterface("IComparable"));
            PF("empty", () => s.GetInterface(""));
            PF("prefix-star", () => s.GetInterface("IDisp*"));
            PF("null-name", () => s.GetInterface(NullName!));

            Type g = typeof(Generic);
            PN("generic mangled", () => g.GetInterface("IEnumerable`1"));
            PN("generic qualified", () => g.GetInterface("System.Collections.Generic.IEnumerable`1"));
            PN("generic folded ic", () => g.GetInterface("ienumerable`1", true));
            PF("nongeneric via closure", () => g.GetInterface("IEnumerable"));

            Type b = typeof(Both);
            PN("two instantiations", () => b.GetInterface("IEnumerable`1"));
            PN("two instantiations qualified", () => b.GetInterface("System.Collections.Generic.IEnumerable`1"));
            PF("nongeneric on Both", () => b.GetInterface("IEnumerable"));

            Type t2 = typeof(TwoNs);
            PF("two namespaces", () => t2.GetInterface("IFoo"));
            PF("two namespaces qualified", () => t2.GetInterface("GetInterfaceSubset.NsA.IFoo"));
            PF("two namespaces wrong-ns-case ic", () => t2.GetInterface("getinterfacesubset.nsa.IFoo", true));

            Console.WriteLine("== single-attribute ambiguity ==");
            MethodInfo mi = typeof(Attributed).GetMethod("M")!;
            PA("member static form", () => Attribute.GetCustomAttribute(mi, typeof(BaseAttr)));
            PA("member extension form", () => mi.GetCustomAttribute(typeof(BaseAttr)));
            PA("member derived control", () => mi.GetCustomAttribute(typeof(DerivedAttr)));
            Assembly asm = typeof(Attributed).Assembly;
            PA("assembly form", () => asm.GetCustomAttribute(typeof(BaseAttr)));
            PA("assembly derived control", () => asm.GetCustomAttribute(typeof(DerivedAttr)));
        }

        // Catches the EXACT ambiguity type, so a stand-in exception escapes and
        // fails the run rather than merely the diff.
        static void PA(string label, Func<object?> f)
        {
            try
            {
                object? a = f();
                Console.WriteLine($"{label}: {(a is null ? "null" : a.GetType().Name)}");
            }
            catch (AmbiguousMatchException e)
            {
                Console.WriteLine($"{label}: {e.GetType().Name}");
            }
        }

        private sealed class CustomModule : Module
        {
            internal int Calls;
            public override IList<CustomAttributeData> GetCustomAttributesData()
            {
                Calls++;
                return Array.Empty<CustomAttributeData>();
            }

            internal IList<CustomAttributeData> BaseData() => base.GetCustomAttributesData();
        }

        private static int Markers(IEnumerable<CustomAttributeData> rows)
        {
            int mask = 0;
            foreach (var row in rows)
            {
                if (row.AttributeType == typeof(BaseAttr)) mask |= 1;
                if (row.AttributeType == typeof(DerivedAttr)) mask |= 2;
            }
            return mask;
        }

        internal static void RunModuleData()
        {
            var module = typeof(Program).Module;
            int before = DerivedAttr.Constructions;
            var data = module.GetCustomAttributesData();
            Console.WriteLine("module attribute masks=" + Markers(data) + "/" + Markers(module.CustomAttributes)
                + "/" + Markers(CustomAttributeData.GetCustomAttributes(module)) + "/"
                + Markers(typeof(Program).Assembly.GetCustomAttributesData()) + " constructed="
                + (DerivedAttr.Constructions - before));
            try { data[0] = data[0]; }
            catch (NotSupportedException) { Console.WriteLine("module attribute data read-only"); }

            var custom = new CustomModule();
            Module owner = custom;
            _ = owner.CustomAttributes;
            _ = CustomAttributeData.GetCustomAttributes(owner);
            try { custom.BaseData(); }
            catch (NotImplementedException) { Console.WriteLine("custom module overrides=" + custom.Calls + " base=NotImplementedException"); }
            try { ((Module)null!).GetCustomAttributesData(); }
            catch (NullReferenceException) { Console.WriteLine("null module=NullReferenceException"); }
            try { CustomAttributeData.GetCustomAttributes((Module)null!); }
            catch (ArgumentNullException e) { Console.WriteLine("null module argument=" + e.ParamName); }

            Func<IList<CustomAttributeData>> bound = module.GetCustomAttributesData;
            var method = typeof(Module).GetMethod(nameof(Module.GetCustomAttributesData))!;
            Console.WriteLine("module method=" + (bound.Method.DeclaringType == module.GetType()) + "/"
                + (method.DeclaringType == typeof(Module)) + "/"
                + Markers((IList<CustomAttributeData>)bound.Method.Invoke(module, null)!) + "/"
                + Markers((IList<CustomAttributeData>)method.Invoke(module, null)!));
        }
    }
}
