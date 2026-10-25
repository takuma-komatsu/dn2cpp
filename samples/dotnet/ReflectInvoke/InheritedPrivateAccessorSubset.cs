using System;
using System.Reflection;

namespace InheritedPrivateAccessorSubset
{
    class Base
    {
        private int _indexed = 30;
        private int _named = 40;

        public int PrivateSet { get; private set; } = 10;
        public int PrivateGet { private get; set; } = 20;
        public int this[int index]
        {
            get => _indexed + index;
            private set => _indexed = value - index;
        }
        public int this[string index]
        {
            private get => _named;
            set => _named = value;
        }

        public void AdmitBodies()
        {
            PrivateSet = 11;
            _ = PrivateSet;
            _ = PrivateGet;
            PrivateGet = 20;
            this[1] = 31;
            _ = this[1];
            _ = this["x"];
            this["x"] = 40;
        }
    }

    class Derived : Base { }

    class ColdBase
    {
        public int PrivateSet { get => 50; private set => throw new InvalidOperationException(); }
        public int PrivateGet { private get => throw new InvalidOperationException(); set { } }
    }

    class ColdDerived : ColdBase { }

    static class Program
    {
        private static string Read(Func<object> action)
        {
            try { return "value:" + action(); }
            catch (Exception ex) { return ex.GetType().Name; }
        }

        private static string Write(Action action)
        {
            try { action(); return "ok"; }
            catch (Exception ex) { return ex.GetType().Name; }
        }

        private static void Probe(string name, PropertyInfo property, object target, object[] index)
        {
            Console.WriteLine(name + " capabilities=" + property.CanRead + "/" + property.CanWrite
                + " methods=" + (property.GetGetMethod(true) is not null) + "/"
                + (property.GetSetMethod(true) is not null));
            if (index is null)
            {
                Console.WriteLine(name + " plain=" + Read(() => property.GetValue(target)) + "/"
                    + Write(() => property.SetValue(target, 70)));
            }
            Console.WriteLine(name + " indexed=" + Read(() => property.GetValue(target, index)) + "/"
                + Write(() => property.SetValue(target, 71, index)));
            Console.WriteLine(name + " overload=" + Read(() => property.GetValue(target,
                    BindingFlags.Default, null, index, null)) + "/"
                + Write(() => property.SetValue(target, 72, BindingFlags.Default, null, index, null)));
        }

        internal static void Run()
        {
            Console.WriteLine("== inherited private property accessors ==");
            var warm = new Derived();
            warm.AdmitBodies();
            Probe("declared setter", typeof(Base).GetProperty("PrivateSet"), warm, null);
            Probe("inherited setter", typeof(Derived).GetProperty("PrivateSet"), warm, null);
            Probe("declared getter", typeof(Base).GetProperty("PrivateGet"), warm, null);
            Probe("inherited getter", typeof(Derived).GetProperty("PrivateGet"), warm, null);
            Probe("declared index setter", typeof(Base).GetProperty("Item", new[] { typeof(int) }), warm,
                new object[] { 2 });
            Probe("inherited index setter", typeof(Derived).GetProperty("Item", new[] { typeof(int) }), warm,
                new object[] { 2 });
            Probe("declared index getter", typeof(Base).GetProperty("Item", new[] { typeof(string) }), warm,
                new object[] { "x" });
            Probe("inherited index getter", typeof(Derived).GetProperty("Item", new[] { typeof(string) }), warm,
                new object[] { "x" });
            var cold = new ColdDerived();
            _ = cold.PrivateSet;
            cold.PrivateGet = 60;
            Probe("cold inherited setter", typeof(ColdDerived).GetProperty("PrivateSet"), cold, null);
            Probe("cold inherited getter", typeof(ColdDerived).GetProperty("PrivateGet"), cold, null);
            Console.WriteLine("inherited private property accessors end");
        }
    }
}
