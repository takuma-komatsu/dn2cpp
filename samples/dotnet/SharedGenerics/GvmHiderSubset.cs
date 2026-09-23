// A class generic virtual dispatched through a base whose slot a subclass hides.
// `new virtual` opens a fresh slot: an override below the hider overrides the
// hider's slot, so a base-typed call must still land on the base body. A
// non-virtual `new` never takes the slot either.
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

    internal class GvmCovariantBase
    {
        public virtual GvmCovariantBase Tag<T>() => new GvmCovariantBase();
    }

    internal sealed class GvmCovariantLeaf : GvmCovariantBase
    {
        public override GvmCovariantLeaf Tag<T>() => this;
    }

    internal static class Program
    {
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
            GvmCovariantBase covariant = new GvmCovariantLeaf();
            Console.WriteLine("gvm hider covariant=" + (covariant.Tag<int>() is GvmCovariantLeaf));
        }
    }
}
