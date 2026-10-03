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
    }
}
