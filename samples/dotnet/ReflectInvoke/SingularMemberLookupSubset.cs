using System;
using System.Reflection;

namespace SingularMemberLookupSubset
{
    internal class Base
    {
        public int Shade;
        public int OnlyBase;
        public void Step() { }
        public int Tone => 1;
    }

    internal sealed class Derived : Base
    {
        public int shade;
        public void step() { }
        public int tone => 2;
    }

    internal class VirtualBase
    {
        public virtual int Tone => 3;
    }

    internal class VirtualDerived : VirtualBase
    {
        public override int Tone => 4;
    }

    internal sealed class VirtualLeaf : VirtualDerived
    {
        public int tone => 5;
    }

    internal class SlotBase
    {
        public virtual int Tone => 6;
    }

    internal class SlotNew : SlotBase
    {
        public new virtual int Tone => 7;
    }

    internal sealed class SlotOverride : SlotNew
    {
        public override int Tone => 8;
    }

    internal class DuplicateAccessor
    {
        public virtual int Tone => 9;
    }

    internal interface ISharedAccessor
    {
        int Tone { get; }
    }

    internal class ProtectedBase
    {
        protected virtual int Tone => 10;
    }

    internal sealed class ProtectedOverride : ProtectedBase
    {
        protected override int Tone => 11;
    }

    internal class AccessorBase
    {
        private int _tone;
        public virtual int Tone { get => _tone; set => _tone = value; }
    }

    internal sealed class AccessorOverride : AccessorBase
    {
        public override int Tone { get => base.Tone; set => base.Tone = value; }
    }

    internal sealed class Collision
    {
        public int Alpha;
        public int alpha;
        public void Beta() { }
        public void beta() { }
        public int Gamma => 1;
        public int gamma => 2;
        private int Hidden;
    }

    internal sealed class Overload
    {
        public void Echo(int value) { }
        public void echo(string value) { }
        public int Entry => 1;
        public string entry => "e";
    }

    internal static class Program
    {
        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

        private static string Lookup(Func<MemberInfo> get)
        {
            try
            {
                MemberInfo member = get();
                return member is null ? "null" : member.DeclaringType.Name + "." + member.Name;
            }
            catch (AmbiguousMatchException)
            {
                return nameof(AmbiguousMatchException);
            }
        }

        public static void Run()
        {
            Console.WriteLine("== singular member IgnoreCase ==");
            Type collision = typeof(Collision);
            Type derived = typeof(Derived);
            Type overload = typeof(Overload);
            BindingFlags folded = PublicInstance | BindingFlags.IgnoreCase;

            Console.WriteLine("field exact=" + Lookup(() => collision.GetField("Alpha", PublicInstance)));
            Console.WriteLine("field case miss=" + Lookup(() => collision.GetField("ALPHA", PublicInstance)));
            Console.WriteLine("field folded collision=" + Lookup(() => collision.GetField("ALPHA", folded)));
            Console.WriteLine("field inherited=" + Lookup(() => derived.GetField("oNlYbAsE", folded)));
            Console.WriteLine("field hidden=" + Lookup(() => derived.GetField("SHADE", folded)));
            Console.WriteLine("field private=" + Lookup(() => collision.GetField("hIdDeN",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase)));
            Console.WriteLine("field wildcard=" + Lookup(() => collision.GetField("Alpha*", folded)));

            Console.WriteLine("method exact=" + Lookup(() => collision.GetMethod("Beta", PublicInstance)));
            Console.WriteLine("method case miss=" + Lookup(() => collision.GetMethod("BETA", PublicInstance)));
            Console.WriteLine("method folded collision=" + Lookup(() => collision.GetMethod("BETA", folded)));
            Console.WriteLine("method inherited=" + Lookup(() => derived.GetMethod("sTeP", folded)));
            Console.WriteLine("method overload int=" + Lookup(() => overload.GetMethod("ECHO", folded,
                null, new[] { typeof(int) }, null)));
            Console.WriteLine("method overload string=" + Lookup(() => overload.GetMethod("ECHO", folded,
                null, new[] { typeof(string) }, null)));
            Console.WriteLine("method wildcard=" + Lookup(() => collision.GetMethod("Beta*", folded)));

            Console.WriteLine("property exact=" + Lookup(() => collision.GetProperty("Gamma", PublicInstance)));
            Console.WriteLine("property case miss=" + Lookup(() => collision.GetProperty("GAMMA", PublicInstance)));
            Console.WriteLine("property folded collision=" + Lookup(() => collision.GetProperty("GAMMA", folded)));
            Console.WriteLine("property inherited=" + Lookup(() => derived.GetProperty("tOnE", folded)));
            Console.WriteLine("property overridden alias=" + Lookup(() =>
                typeof(VirtualDerived).GetProperty("tOnE", folded)));
            Console.WriteLine("property overridden exact base=" + Lookup(() =>
                typeof(VirtualDerived).GetProperty("Tone", PublicInstance)));
            Console.WriteLine("property overridden exact derived=" + Lookup(() =>
                typeof(VirtualDerived).GetProperty("tone", PublicInstance)));
            Console.WriteLine("property three-level folded=" + Lookup(() =>
                typeof(VirtualLeaf).GetProperty("tOnE", folded)));
            Console.WriteLine("property three-level exact base=" + Lookup(() =>
                typeof(VirtualLeaf).GetProperty("Tone", PublicInstance)));
            Console.WriteLine("property new-slot folded=" + Lookup(() =>
                typeof(SlotOverride).GetProperty("tOnE", folded)));
            Console.WriteLine("property new-slot exact=" + Lookup(() =>
                typeof(SlotOverride).GetProperty("Tone", PublicInstance)));
            Console.WriteLine("property shared accessor=" + Lookup(() =>
                typeof(DuplicateAccessor).GetProperty("tOnE", folded)));
            Console.WriteLine("property interface shared accessor=" + Lookup(() =>
                typeof(ISharedAccessor).GetProperty("tOnE", folded)));
            Console.WriteLine("property protected override=" + Lookup(() =>
                typeof(ProtectedOverride).GetProperty("tOnE",
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase)));
            Console.WriteLine("property setter-only override=" + Lookup(() =>
                typeof(AccessorOverride).GetProperty("tOnE", folded)));
            Console.WriteLine("property overload int=" + Lookup(() => overload.GetProperty("ENTRY", folded,
                null, typeof(int), Type.EmptyTypes, null)));
            Console.WriteLine("property overload string=" + Lookup(() => overload.GetProperty("ENTRY", folded,
                null, typeof(string), Type.EmptyTypes, null)));
            Console.WriteLine("property wildcard=" + Lookup(() => collision.GetProperty("Gamma*", folded)));
            Console.WriteLine("singular member IgnoreCase end");
        }
    }
}
