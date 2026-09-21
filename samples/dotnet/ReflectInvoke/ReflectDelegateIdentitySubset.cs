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
            Console.WriteLine("delegate-method-end");
        }
    }
}
