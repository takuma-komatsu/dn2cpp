// A class generic virtual dispatched through a base whose slot a subclass hides.
// `new virtual` opens a fresh slot: an override below the hider overrides the
// hider's slot, so a base-typed call must still land on the base body. A
// non-virtual `new` never takes the slot either.
//
// RunSubstitutionCollisions: where substitution makes generic overloads alike
// (X = object closes Convert<U>(X) to Convert<U>(object)), the generic
// definitions decide which template an override takes the slot of and which one
// implements an interface method, whether the class, a base or a derived
// interface lists it or an explicit body names it. A body shared over a
// placeholder that a concrete receiver or TSelf implements only at its real
// instantiation binds per instantiation, and a generic struct first boxed as an
// interface is a dispatch case.
//
// RunCrossLevelCollisions: the definitions decide on every level, not only between
// alike templates of one level. An override passes a nearer template alike only
// once T closes and takes the slot of the base template its definition repeats,
// and a listing level's own alike method leaves the interface method to the base
// body whose definition implements it.
//
// RunVariantInterfaces: an interface generic virtual called through an instantiation
// the receiver implements only through variance (IVariantIn<string> served by
// IVariantIn<object>, IVariantOut<object> by IVariantOut<string>) runs the body the
// implemented instantiation binds, or that instantiation's default body, whether the
// call is constrained on a struct or a class, goes through a box or a bound delegate.
// An exact instantiation outranks a variant one; among several variant ones the first
// in the receiver's interface list wins, most derived level first. A non-generic slot
// a constrained call reaches binds the same way, default body included, past an
// instantiation variance cannot convert (IVariantPlainIn<int> for IVariantPlainIn<string>).
using System;

namespace GvmHiderSubset
{
    internal class GvmBase
    {
        public virtual string Tag<T>() => "base:" + typeof(T).Name;
    }

    internal class GvmHider : GvmBase
    {
        public new virtual string Tag<T>() => "hider:" + typeof(T).Name;
    }

    internal sealed class GvmLeaf : GvmHider
    {
        public override string Tag<T>() => "leaf:" + typeof(T).Name;
    }

    internal class GvmMid : GvmBase
    {
        public override string Tag<T>() => "mid:" + typeof(T).Name;
    }

    internal class GvmMidHider : GvmMid
    {
        public new virtual string Tag<T>() => "midhider:" + typeof(T).Name;
    }

    internal sealed class GvmMidLeaf : GvmMidHider
    {
        public override string Tag<T>() => "midleaf:" + typeof(T).Name;
    }

    internal class GvmPlainHider : GvmBase
    {
        public new string Tag<T>() => "plain:" + typeof(T).Name;
    }

    internal sealed class GvmPlainLeaf : GvmPlainHider
    {
    }

    internal class GvmOverloadBase
    {
        public virtual string Tag<T>(T value) => "generic";
        public virtual string Tag<T>(object value) => "object";
    }

    internal sealed class GvmOverloadLeaf : GvmOverloadBase
    {
        public override string Tag<T>(object value) => "derived object";
    }

    internal class GvmCovariantBase
    {
        public virtual GvmCovariantBase Tag<T>() => new GvmCovariantBase();
    }

    internal sealed class GvmCovariantLeaf : GvmCovariantBase
    {
        public override GvmCovariantLeaf Tag<T>() => this;
    }

    internal class PickBase<T>
    {
        public virtual string Pick<U>(object value, U tag) => "base object";
        public virtual string Pick<U>(T value, U tag) => "base T";
    }

    internal sealed class PickLeaf<T> : PickBase<T>
    {
        public override string Pick<U>(T value, U tag) => "leaf T";
    }

    internal class PickMid<T> : PickBase<T>
    {
    }

    internal sealed class PickDeep<T> : PickMid<T>
    {
        public override string Pick<U>(T value, U tag) => "deep T";
    }

    internal interface IConvert<X>
    {
        string Convert<U>(X value);
    }

    internal sealed class PlainConverter<X> : IConvert<X>
    {
        public string Convert<U>(object value) => "plain object";
        public string Convert<U>(X value) => "plain X";
    }

    internal class VirtualConverter<X> : IConvert<X>
    {
        public virtual string Convert<U>(object value) => "virtual object";
        public virtual string Convert<U>(X value) => "virtual X";
    }

    internal class ConverterBase<X>
    {
        public virtual string Convert<U>(object value) => "inherited object";
        public virtual string Convert<U>(X value) => "inherited X";
    }

    internal sealed class InheritingConverter<X> : ConverterBase<X>, IConvert<X>
    {
    }

    internal interface IDerivedConvert<X> : IConvert<X>
    {
    }

    internal class DerivedConverter<X> : IDerivedConvert<X>
    {
        public virtual string Convert<U>(object value) => "derived object";
        public virtual string Convert<U>(X value) => "derived X";
    }

    internal class ClosedConverter<X> : IConvert<string>
    {
        public virtual string Convert<U>(X value) => "closed X";
        public virtual string Convert<U>(string value) => "closed string";
    }

    internal class IntConverterBase
    {
        public virtual string Convert<U>(int value) => "int base";
    }

    // Its own overload has another signature, so the inherited body implements the
    // interface method.
    internal class OverloadConverter : IntConverterBase, IConvert<int>
    {
        public virtual string Convert<U>(string value) => "overload string";
    }

    internal interface IPair<X>
    {
        string Get<U>(X value);
        string Get<U>(object value);
    }

    // The explicit bodies come in the other order than the interface declares them.
    internal sealed class ExplicitPair<X> : IPair<X>
    {
        string IPair<X>.Get<U>(object value) => "explicit object";
        string IPair<X>.Get<U>(X value) => "explicit X";
    }

    internal struct StructConverter<X> : IConvert<X>
    {
        public string Convert<U>(object value) => "struct object";
        public string Convert<U>(X value) => "struct X";
    }

    internal struct IntConverter : IConvert<int>
    {
        public string Convert<U>(int value) => "int " + value;
    }

    internal interface IParse<TSelf, X> where TSelf : IParse<TSelf, X>
    {
        static abstract string Parse<U>(X value);
    }

    internal struct IntParser : IParse<IntParser, int>
    {
        public static string Parse<U>(U value) => "parse U";
        public static string Parse<U>(int value) => "parse int " + value;
    }

    internal class LevelRoot<T>
    {
        public virtual string Pick<U>(T value, U tag) => "root T";
    }

    internal class LevelMid<T> : LevelRoot<T>
    {
        public virtual string Pick<U>(object value, U tag) => "mid object";
    }

    internal sealed class LevelLeaf<T> : LevelMid<T>
    {
        public override string Pick<U>(T value, U tag) => "leaf T";
    }

    internal class ObjectLevelRoot<T>
    {
        public virtual string Pick<U>(object value, U tag) => "root object";
    }

    internal class ObjectLevelMid<T> : ObjectLevelRoot<T>
    {
        public virtual string Pick<U>(T value, U tag) => "mid T";
    }

    internal sealed class ObjectLevelLeaf<T> : ObjectLevelMid<T>
    {
        public override string Pick<U>(object value, U tag) => "leaf object";
    }

    internal class LevelConverterBase<X>
    {
        public virtual string Convert<U>(X value) => "base X";
    }

    internal class LevelConverter<X> : LevelConverterBase<X>, IConvert<X>
    {
        public virtual string Convert<U>(object value) => "own object";
    }

    internal interface IVariantIn<in X>
    {
        string M<T>(X x);
    }

    internal interface IVariantOut<out X>
    {
        X Get<T>();
    }

    internal interface IVariantDefault<in X>
    {
        string D<T>(X x) => "default:" + typeof(X).Name + ":" + typeof(T).Name + ":" + x;
    }

    internal struct VariantInStruct : IVariantIn<object>
    {
        public int K;
        public string M<T>(object o) => "struct" + K + ":" + typeof(T).Name + ":" + o;
    }

    internal sealed class VariantInClass : IVariantIn<object>
    {
        public string M<T>(object o) => "class:" + typeof(T).Name + ":" + o;
    }

    internal sealed class VariantInExplicit : IVariantIn<object>
    {
        string IVariantIn<object>.M<T>(object o) => "explicit:" + typeof(T).Name + ":" + o;
    }

    internal class VariantInBase : IVariantIn<object>
    {
        public virtual string M<T>(object o) => "base:" + typeof(T).Name + ":" + o;
    }

    internal sealed class VariantInDerived : VariantInBase
    {
        public override string M<T>(object o) => "derived:" + typeof(T).Name + ":" + o;
    }

    internal sealed class VariantInInherited : VariantInBase
    {
    }

    internal sealed class VariantInExact : IVariantIn<object>, IVariantIn<string>
    {
        string IVariantIn<object>.M<T>(object o) => "exact object:" + typeof(T).Name + ":" + o;
        string IVariantIn<string>.M<T>(string s) => "exact string:" + typeof(T).Name + ":" + s;
    }

    internal struct VariantOutStruct : IVariantOut<string>
    {
        public int K;
        public string Get<T>() => "struct" + K + ":" + typeof(T).Name;
    }

    internal sealed class VariantOutClass : IVariantOut<string>
    {
        public string Get<T>() => "class:" + typeof(T).Name;
    }

    internal sealed class VariantOutPair : IVariantOut<string>, IVariantOut<Version>
    {
        string IVariantOut<string>.Get<T>() => "pair string:" + typeof(T).Name;
        Version IVariantOut<Version>.Get<T>() => new Version(1, 2);
    }

    internal class VariantOutBase : IVariantOut<string>
    {
        string IVariantOut<string>.Get<T>() => "base string:" + typeof(T).Name;
    }

    internal sealed class VariantOutDerived : VariantOutBase, IVariantOut<Version>
    {
        Version IVariantOut<Version>.Get<T>() => new Version(3, 4);
    }

    internal sealed class VariantDefaultClass : IVariantDefault<object>
    {
    }

    internal struct VariantDefaultStruct : IVariantDefault<object>
    {
    }

    internal interface IVariantPlainIn<in X>
    {
        string M(X x);
    }

    internal interface IVariantPlainOut<out X>
    {
        X Get();
    }

    internal struct VariantPlainMixed : IVariantPlainIn<object>, IVariantPlainIn<int>
    {
        public string M(object o) => "object:" + o;
        public string M(int i) => "int:" + i;
    }

    internal struct VariantPlainPair : IVariantPlainOut<string>, IVariantPlainOut<Version>
    {
        string IVariantPlainOut<string>.Get() => "pair string";
        Version IVariantPlainOut<Version>.Get() => new Version(5, 6);
    }

    internal interface IVariantPlainDefault<in X>
    {
        string D(X x) => "plain default:" + typeof(X).Name + ":" + x;
    }

    internal struct VariantPlainDefaultStruct : IVariantPlainDefault<object>
    {
    }

    internal static class Program
    {
        private static string ViaBase<T>(PickBase<T> target, T value) => target.Pick<int>(value, 1);

        private static string ViaLevelRoot<T>(LevelRoot<T> target, T value) => target.Pick<int>(value, 1);

        private static string ViaConstrained<T>(T target) where T : IConvert<object> => target.Convert<int>(null);

        private static string ViaConverter<T, X>(T target, X value) where T : IConvert<X> => target.Convert<int>(value);

        private static string ViaParser<P, X>(X value) where P : IParse<P, X> => P.Parse<string>(value);

        private static string ViaPair<X>(IPair<X> pair, X value) => pair.Get<int>(value);

        private static string ViaPairObject<X>(IPair<X> pair) => pair.Get<int>((object)null);

        internal static void RunSubstitutionCollisions()
        {
            PickBase<object> leaf = new PickLeaf<object>();
            PickBase<object> deep = new PickDeep<object>();
            Console.WriteLine("gvm collision class=" + ViaBase(leaf, null) + "/" + leaf.Pick<int>((object)null, 1)
                + "/" + ViaBase(deep, null) + "/" + deep.Pick<int>((object)null, 1));
            Console.WriteLine("gvm collision interface="
                + ((IConvert<object>)new PlainConverter<object>()).Convert<int>(null)
                + "/" + ((IConvert<object>)new VirtualConverter<object>()).Convert<int>(null)
                + "/" + ((IConvert<object>)new InheritingConverter<object>()).Convert<int>(null)
                + "/" + ((IConvert<object>)new DerivedConverter<object>()).Convert<int>(null)
                + "/" + ((IConvert<string>)new ClosedConverter<string>()).Convert<int>(null)
                + "/" + ((IConvert<int>)new OverloadConverter()).Convert<int>(1));
            var pair = new ExplicitPair<object>();
            Console.WriteLine("gvm collision explicit=" + ViaPair<object>(pair, null) + "/" + ViaPairObject<object>(pair));
            Console.WriteLine("gvm collision struct=" + ((IConvert<object>)new StructConverter<object>()).Convert<int>(null)
                + "/" + ViaConstrained(new StructConverter<object>()));
            Console.WriteLine("gvm collision shared=" + ViaConverter(new IntConverter(), 5)
                + "/" + ViaParser<IntParser, int>(6));
        }

        internal static void RunCrossLevelCollisions()
        {
            var leaf = new LevelLeaf<object>();
            Console.WriteLine("gvm cross level class=" + ViaLevelRoot<object>(leaf, null)
                + "/" + ((LevelMid<object>)leaf).Pick<int>((object)null, 1)
                + "/" + ViaLevelRoot(new LevelLeaf<string>(), "s"));
            var objectLeaf = new ObjectLevelLeaf<object>();
            Console.WriteLine("gvm cross level mirrored=" + ((ObjectLevelRoot<object>)objectLeaf).Pick<int>(null, 1)
                + "/" + ((ObjectLevelMid<object>)objectLeaf).Pick<int>(null, 1));
            Console.WriteLine("gvm cross level interface=" + ((IConvert<object>)new LevelConverter<object>()).Convert<int>(null)
                + "/" + ((IConvert<string>)new LevelConverter<string>()).Convert<int>(null));
        }

        internal static void Run()
        {
            var leaf = new GvmLeaf();
            GvmBase b = leaf;
            Console.WriteLine("gvm hider base=" + b.Tag<int>());
            Console.WriteLine("gvm hider hider=" + ((GvmHider)leaf).Tag<int>());
            GvmBase mid = new GvmMidLeaf();
            Console.WriteLine("gvm hider mid=" + mid.Tag<string>());
            Console.WriteLine("gvm hider midhider=" + ((GvmMidHider)mid).Tag<string>());
            GvmBase plain = new GvmPlainLeaf();
            Console.WriteLine("gvm hider plain=" + plain.Tag<int>());
            Console.WriteLine("gvm hider plain direct=" + ((GvmPlainHider)plain).Tag<int>());
            GvmOverloadBase overloaded = new GvmOverloadLeaf();
            Console.WriteLine("gvm hider overload generic=" + overloaded.Tag<int>(1));
            Console.WriteLine("gvm hider overload object=" + overloaded.Tag<int>((object)1));
            GvmCovariantBase covariant = new GvmCovariantLeaf();
            Console.WriteLine("gvm hider covariant=" + (covariant.Tag<int>() is GvmCovariantLeaf));
        }

        private static string ConstrainedIn<TS>(TS s) where TS : IVariantIn<string> => s.M<int>("x");

        private static string ConstrainedInAt<TS, U>(TS s) where TS : IVariantIn<string> => s.M<U>("g");

        private static object ConstrainedOut<TS>(TS s) where TS : IVariantOut<object> => s.Get<int>();

        private static string ConstrainedDefault<TS>(TS s) where TS : IVariantDefault<string> => s.D<int>("z");

        private static string ConstrainedPlainIn<TS>(TS s) where TS : IVariantPlainIn<string> => s.M("p");

        private static object ConstrainedPlainOut<TS>(TS s) where TS : IVariantPlainOut<object> => s.Get();

        private static string ConstrainedPlainDefault<TS>(TS s) where TS : IVariantPlainDefault<string> => s.D("d");

        internal static void RunVariantInterfaces()
        {
            Console.WriteLine("gvm variant constrained struct in=" + ConstrainedIn(new VariantInStruct { K = 4 }));
            Console.WriteLine("gvm variant constrained class in=" + ConstrainedIn(new VariantInClass()));
            Console.WriteLine("gvm variant constrained explicit in=" + ConstrainedIn(new VariantInExplicit()));
            Console.WriteLine("gvm variant constrained derived in=" + ConstrainedIn(new VariantInDerived()));
            Console.WriteLine("gvm variant constrained inherited in=" + ConstrainedIn(new VariantInInherited()));
            Console.WriteLine("gvm variant constrained method arg in="
                + ConstrainedInAt<VariantInStruct, string>(new VariantInStruct { K = 7 }));
            IVariantIn<string> boxed = new VariantInStruct { K = 5 };
            Console.WriteLine("gvm variant boxed struct in=" + boxed.M<long>("y"));
            IVariantIn<string> instance = new VariantInClass();
            Console.WriteLine("gvm variant class in=" + instance.M<long>("y"));
            Func<string, string> bound = boxed.M<int>;
            Console.WriteLine("gvm variant delegate in=" + bound("q"));
            Console.WriteLine("gvm variant constrained struct out=" + ConstrainedOut(new VariantOutStruct { K = 6 }));
            Console.WriteLine("gvm variant constrained class out=" + ConstrainedOut(new VariantOutClass()));
            IVariantOut<object> boxedOut = new VariantOutStruct { K = 8 };
            Console.WriteLine("gvm variant boxed struct out=" + boxedOut.Get<byte>());
            Console.WriteLine("gvm variant constrained class default=" + ConstrainedDefault(new VariantDefaultClass()));
            Console.WriteLine("gvm variant constrained struct default=" + ConstrainedDefault(new VariantDefaultStruct()));
            IVariantDefault<string> withDefault = new VariantDefaultClass();
            Console.WriteLine("gvm variant class default=" + withDefault.D<short>("w"));
            Console.WriteLine("gvm variant constrained exact=" + ConstrainedIn(new VariantInExact()));
            Console.WriteLine("gvm variant class exact=" + ((IVariantIn<string>)new VariantInExact()).M<int>("x"));
            Console.WriteLine("gvm variant first of two=" + ((IVariantOut<object>)new VariantOutPair()).Get<int>());
            Console.WriteLine("gvm variant derived level first=" + ((IVariantOut<object>)new VariantOutDerived()).Get<int>());
            Console.WriteLine("plain variant constrained mixed in=" + ConstrainedPlainIn(new VariantPlainMixed()));
            Console.WriteLine("plain variant constrained first of two=" + ConstrainedPlainOut(new VariantPlainPair()));
            Console.WriteLine("plain variant constrained struct default=" + ConstrainedPlainDefault(new VariantPlainDefaultStruct()));
        }
    }
}
