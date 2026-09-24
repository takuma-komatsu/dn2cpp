using System;
using System.Reflection;

namespace ReflectDelegateIdentitySubset
{
    interface IRuntimeBaseDefault
    {
        string Pick() => "base";
    }

    interface IRuntimeDerivedDefault : IRuntimeBaseDefault
    {
        string IRuntimeBaseDefault.Pick() => "derived";
    }

    // A top-level definition only typeof names: a MakeGenericType instance has no
    // closed instantiation in the image.
    class RuntimeDerivedDefault<T> : IRuntimeDerivedDefault { }

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
        interface IRowGeneric { string Pick<T>(); }
        class PlainFirstGeneric : IRowGeneric
        {
            public string Pick<T>() => "plain";
            string IRowGeneric.Pick<T>() => "explicit";
        }
        class ExplicitFirstGeneric : IRowGeneric
        {
            string IRowGeneric.Pick<T>() => "explicit";
            public string Pick<T>() => "plain";
        }
        interface IBaseDefault
        {
            string Pick() => "base";
        }
        interface IDerivedDefault : IBaseDefault
        {
            string IBaseDefault.Pick() => "derived";
        }
        class DerivedDefault : IDerivedDefault { }
        struct StructDerivedDefault : IDerivedDefault { }
        class InheritedDerivedDefault : DerivedDefault { }
        interface IBaseGenericDefault
        {
            string Pick<T>() => "base";
        }
        interface IDerivedGenericDefault : IBaseGenericDefault
        {
            string IBaseGenericDefault.Pick<T>() => "derived";
        }
        class DerivedGenericDefault : IDerivedGenericDefault { }
        interface IBaseTyped<T>
        {
            string Tag() => "base";
        }
        interface IDerivedTyped<T> : IBaseTyped<T>
        {
            string IBaseTyped<T>.Tag() => "derived";
        }
        class DerivedTyped : IDerivedTyped<int> { }
        interface IBaseSame
        {
            string Tag() => "same";
        }
        interface IDerivedSame : IBaseSame
        {
            string IBaseSame.Tag() => "same";
        }
        class DerivedSame : IDerivedSame { }
        interface IOverloadedGeneric
        {
            string Pick<T>(T generic);
            string Pick<T>(int number);
        }
        class OverloadedGeneric : IOverloadedGeneric
        {
            string IOverloadedGeneric.Pick<T>(T generic) => "generic";
            string IOverloadedGeneric.Pick<T>(int number) => "integer";
        }
        // Declared through a closed generic interface, the MethodImpl rows name
        // MemberRefs whose open signatures must tell the overloads apart.
        interface IOverloadedGenericOf<T>
        {
            string Pick<U>(U generic);
            string Pick<U>(int number);
        }
        class OverloadedGenericOf : IOverloadedGenericOf<string>
        {
            string IOverloadedGenericOf<string>.Pick<U>(U generic) => "generic";
            string IOverloadedGenericOf<string>.Pick<U>(int number) => "integer";
        }
        interface IPlainOverload
        {
            string Pick<T>(T generic);
            string Pick<T>(int number);
        }
        class PlainOverload : IPlainOverload
        {
            public string Pick<T>(T generic) => "plain";
            string IPlainOverload.Pick<T>(int number) => "int-explicit";
        }
        interface IPrefix { string Pick<T>(); }
        interface IPrefixLonger { string Pick<T>(); }
        class PrefixQualified : IPrefix, IPrefixLonger
        {
            public string Pick<T>() => "plain";
            string IPrefixLonger.Pick<T>() => "longer";
        }
        interface IArityPick { string Pick<U>(); }
        interface IArityPick<T> { string Pick<U>(); }
        class ArityQualified : IArityPick, IArityPick<int>
        {
            public string Pick<U>() => "plain";
            string IArityPick<int>.Pick<U>() => "explicit-generic";
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
        class CovariantBase
        {
            public virtual CovariantBase Tag<T>() => new CovariantBase();
        }
        class CovariantLeaf : CovariantBase
        {
            public override CovariantLeaf Tag<T>() => this;
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
            CovariantBase covariantReceiver = new CovariantLeaf();
            Func<CovariantBase> covariantTag = covariantReceiver.Tag<int>;
            Console.WriteLine("delegate-method-generic-covariant=" + covariantTag.Method.DeclaringType.Name
                + "/" + covariantTag.Method.Invoke(covariantReceiver, null)!.GetType().Name
                + "/" + covariantTag().GetType().Name);
            Console.WriteLine("delegate-method-end");
            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_INTERFACE_SELECTION") == "1")
                return;
            RunInterfaceMethod();
            RunInterfaceGenericDispatch();
        }

        // Delegate.Method for the body an interface binding selects: explicit over
        // plain generic bodies, and derived-interface overrides of a default.
        static void RunInterfaceMethod()
        {
            Console.WriteLine("delegate-method-interface-begin");
            IRowGeneric plainFirstReceiver = new PlainFirstGeneric();
            IRowGeneric explicitFirstReceiver = new ExplicitFirstGeneric();
            Func<string> plainFirstPick = plainFirstReceiver.Pick<int>;
            Func<string> explicitFirstPick = explicitFirstReceiver.Pick<int>;
            Console.WriteLine("delegate-method-generic-explicit-order=" + plainFirstPick() + "/" + plainFirstPick.Method.DeclaringType.Name
                + "/" + plainFirstPick.Method.Name.EndsWith(".Pick") + "/" + explicitFirstPick()
                + "/" + explicitFirstPick.Method.DeclaringType.Name + "/" + explicitFirstPick.Method.Name.EndsWith(".Pick"));
            IBaseDefault derivedDefaultReceiver = new DerivedDefault();
            Func<string> derivedDefaultPick = derivedDefaultReceiver.Pick;
            Console.WriteLine("delegate-method-derived-default=" + derivedDefaultPick() + "/" + derivedDefaultPick.Method.DeclaringType.Name
                + "/" + derivedDefaultPick.Method.Name.EndsWith(".Pick"));
            IBaseGenericDefault genericDefaultReceiver = new DerivedGenericDefault();
            Func<string> genericDefaultPick = genericDefaultReceiver.Pick<int>;
            Console.WriteLine("delegate-method-derived-generic=" + genericDefaultPick() + "/"
                + genericDefaultPick.Method.DeclaringType.Name + "/"
                + genericDefaultPick.Method.Name.EndsWith(".Pick"));
            IBaseDefault structDefaultReceiver = new StructDerivedDefault();
            Func<string> structDefaultPick = structDefaultReceiver.Pick;
            Console.WriteLine("delegate-method-derived-struct=" + structDefaultPick() + "/"
                + structDefaultPick.Method.DeclaringType.Name);
            IBaseDefault inheritedDefaultReceiver = new InheritedDerivedDefault();
            Func<string> inheritedDefaultPick = inheritedDefaultReceiver.Pick;
            Console.WriteLine("delegate-method-derived-inherited=" + inheritedDefaultPick() + "/"
                + inheritedDefaultPick.Method.DeclaringType.Name);
            IBaseTyped<int> typedDefaultReceiver = new DerivedTyped();
            Func<string> typedDefaultTag = typedDefaultReceiver.Tag;
            Console.WriteLine("delegate-method-derived-typed=" + typedDefaultTag() + "/"
                + (typedDefaultTag.Method.DeclaringType == typeof(IDerivedTyped<int>)));
            IBaseSame sameReceiver = new DerivedSame();
            Func<string> sameTag = sameReceiver.Tag;
            Console.WriteLine("delegate-method-derived-same=" + sameTag() + "/"
                + sameTag.Method.DeclaringType.Name);
            var runtimeReceiver = (IRuntimeBaseDefault)Activator.CreateInstance(
                typeof(RuntimeDerivedDefault<>).MakeGenericType(typeof(string)));
            Func<string> runtimePick = runtimeReceiver.Pick;
            Console.WriteLine("delegate-method-derived-runtime-type=" + runtimePick() + "/"
                + runtimePick.Method.DeclaringType.Name + "/" + runtimePick.Method.Name.EndsWith(".Pick"));
            Console.WriteLine("delegate-method-interface-end");
        }

        // The body an interface generic method call binds among overloads and
        // explicit bodies for other interfaces, with the delegate's Method alongside.
        static void RunInterfaceGenericDispatch()
        {
            Console.WriteLine("interface-gvm-dispatch-begin");
            IOverloadedGeneric overloaded = new OverloadedGeneric();
            Console.WriteLine("interface-gvm-explicit-overloads=" + overloaded.Pick<int>(generic: 5)
                + "/" + overloaded.Pick<int>(number: 5));
            IOverloadedGenericOf<string> overloadedOf = new OverloadedGenericOf();
            Console.WriteLine("interface-gvm-explicit-overloads-generic-interface=" + overloadedOf.Pick<int>(generic: 5)
                + "/" + overloadedOf.Pick<int>(number: 5));
            IPlainOverload plainOverload = new PlainOverload();
            Func<string, string> plainOverloadPick = plainOverload.Pick<string>;
            Func<int, string> explicitOverloadPick = plainOverload.Pick<string>;
            Console.WriteLine("interface-gvm-plain-and-explicit-overload=" + plainOverload.Pick<string>("s")
                + "/" + plainOverload.Pick<string>(5) + "/" + plainOverloadPick.Method.Name
                + "/" + explicitOverloadPick.Method.Name.EndsWith(".Pick"));
            var prefix = new PrefixQualified();
            Func<string> prefixPick = ((IPrefix)prefix).Pick<int>;
            Func<string> longerPick = ((IPrefixLonger)prefix).Pick<int>;
            Console.WriteLine("interface-gvm-qualifier-prefix=" + ((IPrefix)prefix).Pick<int>()
                + "/" + ((IPrefixLonger)prefix).Pick<int>() + "/" + prefixPick.Method.Name
                + "/" + longerPick.Method.Name.EndsWith(".Pick"));
            var arity = new ArityQualified();
            Func<string> arityPick = ((IArityPick)arity).Pick<int>;
            Func<string> genericArityPick = ((IArityPick<int>)arity).Pick<int>;
            Console.WriteLine("interface-gvm-qualifier-arity=" + ((IArityPick)arity).Pick<int>()
                + "/" + ((IArityPick<int>)arity).Pick<int>() + "/" + arityPick.Method.Name
                + "/" + genericArityPick.Method.Name.EndsWith(".Pick"));
            Console.WriteLine("interface-gvm-dispatch-end");
        }
    }
}
