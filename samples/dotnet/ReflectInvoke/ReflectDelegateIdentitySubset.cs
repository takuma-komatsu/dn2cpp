using System;
using System.Reflection;

namespace ReflectDelegateIdentitySubset
{
    static class Extensions
    {
        public static string Decorate(this string prefix, string value) => prefix + value;
    }

    static class Program
    {
        interface IProbe { int Value(int value); }
        class Probe : IProbe
        {
            public virtual int Value(int value) => value + 10;
            public virtual T GenericValue<T>(T value) => value;
            public int Plain(int value) => value + 3;
        }
        class Derived : Probe
        {
            public override int Value(int value) => value + 20;
            public override T GenericValue<T>(T value) => value;
        }
        struct StructProbe : IProbe
        {
            public int Value(int value) => value + 30;
        }
        class ExplicitProbe : IProbe
        {
            int IProbe.Value(int value) => value + 40;
        }
        interface IDefaultProbe
        {
            int Default(int value) => value + 100;
        }
        class DefaultProbe : IDefaultProbe { }
        interface IGenericProbe { T Pick<T>(T value); }
        class ImplicitGeneric : IGenericProbe
        {
            public T Pick<T>(T value) => value;
        }
        class ExplicitGeneric : IGenericProbe
        {
            T IGenericProbe.Pick<T>(T value) => value;
        }
        // A new-slot hider detaches the leaf's override from the base's slot.
        class GvmBase
        {
            public virtual string Tag<T>() => "base";
        }
        class GvmHider : GvmBase
        {
            public new virtual string Tag<T>() => "hider";
        }
        class GvmLeaf : GvmHider
        {
            public override string Tag<T>() => "leaf";
        }
        static class IdentityOwner<T>
        {
            public static T Echo(T value) => value;
            public static void Empty() { }
        }
        static T Identity<T>(T value) => value;
        static int Add(int value) => value + 1;
        static string Prefix(string prefix, string value) => prefix + value;
        static void First() { }
        static void Last() { }
        static Func<T, T> Factory<T>() => IdentityOwner<T>.Echo;
        static Action ActionFactory<T>() => IdentityOwner<T>.Empty;

        public static void Run()
        {
            Console.WriteLine("delegate-method-begin");
            Func<int, int> direct = Add;
            Console.WriteLine("delegate-method-static=" + direct.Method.Name + "/" + direct.Method.IsStatic + "/" + direct.Method.Invoke(null, new object[] { 4 }));
            Probe receiver = new Derived();
            Func<int, int> instance = receiver.Plain;
            Func<int, int> virtualMethod = receiver.Value;
            IProbe iface = receiver;
            Func<int, int> interfaceMethod = iface.Value;
            Console.WriteLine("delegate-method-instance=" + instance.Method.Name + "/" + ReferenceEquals(instance.Target, receiver));
            Console.WriteLine("delegate-method-virtual=" + virtualMethod.Method.DeclaringType.Name + "/" + virtualMethod.Method.Invoke(receiver, new object[] { 2 }));
            Console.WriteLine("delegate-method-interface=" + interfaceMethod.Method.DeclaringType.Name + "/" + interfaceMethod.Method.Name);
            Func<string, string> genericVirtual = receiver.GenericValue<string>;
            Console.WriteLine("delegate-method-generic-virtual=" + genericVirtual.Method.DeclaringType.Name + "/" + genericVirtual.Method.GetGenericArguments()[0].Name);
            Func<int, int> derivedMethod = ((Derived)receiver).Value;
            Console.WriteLine("delegate-method-virtual-equality=" + (virtualMethod == derivedMethod) + "/" + (virtualMethod.GetHashCode() == derivedMethod.GetHashCode()));
            var text = Factory<string>();
            var obj = Factory<object>();
            Console.WriteLine("delegate-method-shared=" + (text.Method.DeclaringType == typeof(IdentityOwner<string>)) + "/" + (obj.Method.DeclaringType == typeof(IdentityOwner<object>)));
            Func<int, int> genericInt = Identity<int>;
            Func<string, string> genericText = Identity<string>;
            Console.WriteLine("delegate-method-generic=" + genericInt.Method.GetGenericArguments()[0].Name + "/" + genericText.Method.GetGenericArguments()[0].Name);
            Action first = First;
            Action last = Last;
            Action multicast = first + last;
            Console.WriteLine("delegate-method-multicast=" + multicast.Method.Name + "/" + (multicast - last).Method.Name);
            var sharedA = ActionFactory<string>();
            var sharedB = ActionFactory<object>();
            Console.WriteLine("delegate-method-generic-equality=" + (sharedA == sharedB) + "/" + (sharedA == ActionFactory<string>()));
            Console.WriteLine("delegate-method-generic-remove=" + ((sharedA + sharedB - sharedA).Method.DeclaringType == typeof(IdentityOwner<object>)));
            var open = (Func<Probe, int, int>)typeof(Probe).GetMethod("Plain").CreateDelegate(typeof(Func<Probe, int, int>));
            var closed = (Func<string, string>)typeof(Program).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic).CreateDelegate(typeof(Func<string, string>), "pre:");
            Console.WriteLine("delegate-method-reflection=" + open.Method.Name + "/" + closed.Method.Name + "/" + closed("x"));
            Func<string, string> closedIl = "il:".Decorate;
            Console.WriteLine("delegate-method-closed-static=" + closedIl.Method.Name + "/" + closedIl.Method.IsStatic + "/" + closedIl("x"));
            // A runtime-owned declaring type may answer null; the name must not lie.
            Func<string, bool> empty = string.IsNullOrEmpty;
            Func<string> text2 = receiver.ToString;
            Console.WriteLine("delegate-method-runtime-owned=" + (empty.Method is null || empty.Method.Name == "IsNullOrEmpty")
                + "/" + (text2.Method is null || text2.Method.Name == "ToString"));
            IProbe boxed = new StructProbe();
            Func<int, int> structMethod = boxed.Value;
            Console.WriteLine("delegate-method-struct-interface=" + structMethod.Method.DeclaringType.Name + "/" + structMethod.Method.Name
                + "/" + structMethod(1) + "/" + structMethod.Method.Invoke(boxed, new object[] { 1 }));
            IProbe explicitReceiver = new ExplicitProbe();
            Func<int, int> explicitMethod = explicitReceiver.Value;
            Console.WriteLine("delegate-method-explicit-interface=" + explicitMethod.Method.DeclaringType.Name + "/" + explicitMethod.Method.Name.EndsWith(".Value")
                + "/" + explicitMethod(1) + "/" + explicitMethod.Method.Invoke(explicitReceiver, new object[] { 1 }));
            IDefaultProbe defaultReceiver = new DefaultProbe();
            Func<int, int> defaultMethod = defaultReceiver.Default;
            Console.WriteLine("delegate-method-default-interface=" + defaultMethod.Method.DeclaringType.Name + "/" + defaultMethod.Method.Name
                + "/" + defaultMethod(1));
            IGenericProbe implicitGeneric = new ImplicitGeneric();
            IGenericProbe explicitGeneric = new ExplicitGeneric();
            Func<string, string> implicitPick = implicitGeneric.Pick<string>;
            Func<int, int> explicitPick = explicitGeneric.Pick<int>;
            Console.WriteLine("delegate-method-interface-generic=" + implicitPick.Method.DeclaringType.Name + "/" + implicitPick.Method.GetGenericArguments()[0].Name
                + "/" + explicitPick.Method.DeclaringType.Name + "/" + explicitPick.Method.Name.EndsWith(".Pick") + "/" + explicitPick.Method.GetGenericArguments()[0].Name
                + "/" + implicitPick("p") + explicitPick(5));
            Func<int[], int[]> intArray = Identity<int[]>;
            Func<string[], string[]> textArray = Identity<string[]>;
            Console.WriteLine("delegate-method-array-generic=" + intArray.Method.GetGenericArguments()[0].Name + "/" + textArray.Method.GetGenericArguments()[0].Name);
            var leaf = new GvmLeaf();
            GvmBase gvmBase = leaf;
            Func<string> baseTag = gvmBase.Tag<int>;
            Func<string> hiderTag = ((GvmHider)leaf).Tag<int>;
            Console.WriteLine("delegate-method-generic-hider=" + baseTag.Method.DeclaringType.Name + "/" + baseTag()
                + "/" + hiderTag.Method.DeclaringType.Name + "/" + hiderTag());
            Console.WriteLine("delegate-method-end");
        }
    }
}
