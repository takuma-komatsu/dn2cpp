// A class virtual dispatched through a base whose slot a subclass hides.
// `new virtual` opens a fresh slot: an override below the hider overrides the
// hider's slot, so a base-typed call still runs the base slot's most derived
// body. A generic base's vtable is built per specialization, shared bodies
// included, so the chain is repeated over one.
//
// RunCrossLevelCollisions: an override passes a nearer method alike only once T
// closes for the base slot its definition repeats, and a listing level's own alike
// method leaves the interface method to the base body whose definition implements
// it.
//
// RunDefinitionMirrors: the mirrored plain-virtual chain binds an override to the
// root slot its definition repeats past a nearer alike method, and a constrained
// call on a struct binds an interface method, directly, through an interface
// argument spelled concretely or through a variant instantiation, and
// Object.Equals to the method whose definition fills the slot beside an overload
// alike only once T closes; the box runs that Equals override for a null argument.
using System;

namespace VirtualHiderSubset
{
    internal class Animal
    {
        public virtual string Speak() => "animal";
    }

    internal class Dog : Animal
    {
        public override string Speak() => "dog";
    }

    internal class Puppy : Dog
    {
        public new virtual string Speak() => "puppy";
    }

    internal sealed class LoudPuppy : Puppy
    {
        public override string Speak() => "loud-puppy";
    }

    internal class Cell<T>
    {
        public virtual string Name() => "cell:" + typeof(T).Name;
    }

    internal class HiddenCell<T> : Cell<T>
    {
        public new virtual string Name() => "hidden:" + typeof(T).Name;
    }

    internal sealed class LeafCell<T> : HiddenCell<T>
    {
        public override string Name() => "leaf:" + typeof(T).Name;
    }

    internal class HandlerBase
    {
        public virtual string Handle(object o) => "base";
    }

    internal class Handler<T> : HandlerBase
    {
        public virtual string Handle(T item) => "T";

        public override string Handle(object o) => "object";
    }

    internal class ForwardingHandler<T> : HandlerBase
    {
        public virtual string Handle(T item) => "forwarded T";

        public override string Handle(object o) => o is T t ? Handle(t) : base.Handle(o);
    }

    internal class Visitor<T>
    {
        public virtual string Visit(T item) => "visitor T";

        public virtual string Visit(object o) => "visitor object";
    }

    internal sealed class TypedVisitor<T> : Visitor<T>
    {
        public override string Visit(T item) => "typed T";
    }

    internal class ObjectFirstVisitor<T>
    {
        public virtual string Visit(object o) => "visitor object";

        public virtual string Visit(T item) => "visitor T";
    }

    internal sealed class TypedObjectFirstVisitor<T> : ObjectFirstVisitor<T>
    {
        public override string Visit(T item) => "typed T";
    }

    internal class Box<T>
    {
        public virtual string Get(T item) => "box";
    }

    internal class HiddenBox : Box<object>
    {
        public new virtual string Get(object item) => "hidden box";
    }

    internal sealed class LeafBox : HiddenBox
    {
        public override string Get(object item) => "leaf box";
    }

    internal interface IInspect<T>
    {
        string Inspect(T item);
    }

    internal class DualInspector<T> : IInspect<T>
    {
        public virtual string Inspect(object item) => "object";
        public virtual string Inspect(T item) => "T";
    }

    internal class InspectorBase<T>
    {
        public virtual string Inspect(object item) => "base object";
        public virtual string Inspect(T item) => "base T";
    }

    internal sealed class InheritingInspector<T> : InspectorBase<T>, IInspect<T>
    {
    }

    internal interface IDerivedInspect<T> : IInspect<T>
    {
    }

    internal class DerivedInspector<T> : IDerivedInspect<T>
    {
        public virtual string Inspect(object item) => "derived object";
        public virtual string Inspect(T item) => "derived T";
    }

    internal class LevelVisitorRoot<T>
    {
        public virtual string Visit(T item) => "root T";
    }

    internal class LevelVisitorMid<T> : LevelVisitorRoot<T>
    {
        public virtual string Visit(object item) => "mid object";
    }

    internal sealed class LevelVisitorLeaf<T> : LevelVisitorMid<T>
    {
        public override string Visit(T item) => "leaf T";
    }

    internal class LevelInspectorBase<T>
    {
        public virtual string Inspect(T item) => "base T";
    }

    internal class LevelInspector<T> : LevelInspectorBase<T>, IInspect<T>
    {
        public virtual string Inspect(object item) => "own object";
    }

    internal class MirrorVisitorRoot<T>
    {
        public virtual string Visit(object item) => "root object";
    }

    internal class MirrorVisitorMid<T> : MirrorVisitorRoot<T>
    {
        public virtual string Visit(T item) => "mid T";
    }

    internal sealed class MirrorVisitorLeaf<T> : MirrorVisitorMid<T>
    {
        public override string Visit(object item) => "leaf object";
    }

    internal interface IObjectInspect
    {
        string Inspect(object item);
    }

    // Each alike method is declared first, so a scan taking the first closed match
    // binds it.
    internal struct DualInspectStruct<T> : IInspect<T>, IObjectInspect
    {
        public string Inspect(object item) => "struct object";
        public string Inspect(T item) => "struct T";
    }

    internal interface IContraInspect<in T>
    {
        string Inspect(T item);
    }

    internal struct ContraInspectStruct<T> : IContraInspect<T>, IObjectInspect
    {
        public string Inspect(object item) => "contra object";
        public string Inspect(T item) => "contra T";
    }

    internal struct EqualsPair<T>
    {
        public bool Equals(T other) => false;
        public override bool Equals(object obj) => true;
        public override int GetHashCode() => 0;
    }

    internal static class Program
    {
        private static string AsAnimal(Animal animal) => animal.Speak();

        private static string AsDog(Dog dog) => dog.Speak();

        private static string AsPuppy(Puppy puppy) => puppy.Speak();

        private static string AsCell<T>(Cell<T> cell) => cell.Name();

        private static string AsHidden<T>(HiddenCell<T> cell) => cell.Name();

        internal static void Run()
        {
            var loud = new LoudPuppy();
            Console.WriteLine("virtual hider base=" + AsAnimal(loud));
            Console.WriteLine("virtual hider mid=" + AsDog(loud));
            Console.WriteLine("virtual hider hider=" + AsPuppy(loud));
            Console.WriteLine("virtual hider plain=" + AsAnimal(new Puppy()) + "/" + AsPuppy(new Puppy()));
            var number = new LeafCell<int>();
            Console.WriteLine("virtual hider value base=" + AsCell(number));
            Console.WriteLine("virtual hider value hider=" + AsHidden(number));
            var text = new LeafCell<string>();
            Console.WriteLine("virtual hider shared base=" + AsCell(text));
            Console.WriteLine("virtual hider shared hider=" + AsHidden(text));
        }

        private static string ViaHandler<T>(Handler<T> handler, T item) => handler.Handle(item);

        private static string ViaVisitor<T>(Visitor<T> visitor, T item) => visitor.Visit(item);

        private static string ViaObjectFirst<T>(ObjectFirstVisitor<T> visitor, T item) => visitor.Visit(item);

        // An implicit override binds an inherited slot matched on the generic
        // definitions. A substitution that gives the class's own new slot, or a
        // second inherited slot, the override's signature never captures it.
        internal static void RunSubstitutionCollisions()
        {
            var handler = new Handler<object>();
            Console.WriteLine("virtual collision base=" + ((HandlerBase)handler).Handle(1));
            Console.WriteLine("virtual collision own=" + ViaHandler<object>(handler, 1));
            var shared = new Handler<string>();
            Console.WriteLine("virtual collision shared=" + ((HandlerBase)shared).Handle(1) + "/" + ViaHandler(shared, "s"));
            Console.WriteLine("virtual collision forwarding=" + ((HandlerBase)new ForwardingHandler<object>()).Handle("x"));
            var visitor = new TypedVisitor<object>();
            Console.WriteLine("virtual collision inherited=" + ViaVisitor<object>(visitor, 1) + "/" + ((Visitor<object>)visitor).Visit((object)1));
            var objectFirst = new TypedObjectFirstVisitor<object>();
            Console.WriteLine("virtual collision inherited reversed=" + ViaObjectFirst<object>(objectFirst, 1)
                + "/" + ((ObjectFirstVisitor<object>)objectFirst).Visit((object)1));
            var leaf = new LeafBox();
            Console.WriteLine("virtual collision spec hider=" + ((Box<object>)leaf).Get(1) + "/" + ((HiddenBox)leaf).Get(1));
        }

        // An interface method binds the public virtual its definition names through
        // the interface list, beside a substitution-alike overload.
        internal static void RunInterfaceCollisions()
        {
            Console.WriteLine("virtual collision interface=" + ((IInspect<object>)new DualInspector<object>()).Inspect(null)
                + "/" + ((IInspect<object>)new InheritingInspector<object>()).Inspect(null)
                + "/" + ((IInspect<object>)new DerivedInspector<object>()).Inspect(null));
        }

        internal static void RunCrossLevelCollisions()
        {
            var leaf = new LevelVisitorLeaf<object>();
            Console.WriteLine("virtual cross level class=" + ((LevelVisitorRoot<object>)leaf).Visit(null)
                + "/" + ((LevelVisitorMid<object>)leaf).Visit((object)null));
            Console.WriteLine("virtual cross level interface=" + ((IInspect<object>)new LevelInspector<object>()).Inspect(null)
                + "/" + ((IInspect<string>)new LevelInspector<string>()).Inspect(null));
        }

        private static string ViaMirrorMid<T>(MirrorVisitorMid<T> visitor, T item) => visitor.Visit(item);

        private static string ViaInspect<T, X>(T target, X item) where T : IInspect<X> => target.Inspect(item);

        private static string ViaInspectOfObject<T>(T target) where T : IInspect<object> => target.Inspect(null);

        private static string ViaObjectInspect<T>(T target) where T : IObjectInspect => target.Inspect(null);

        private static string ViaContra<T>(T target, string item) where T : IContraInspect<string> => target.Inspect(item);

        private static bool SameAs<T>(T value, object other) => value.Equals(other);

        internal static void RunDefinitionMirrors()
        {
            var leaf = new MirrorVisitorLeaf<object>();
            Console.WriteLine("virtual cross level mirrored=" + ((MirrorVisitorRoot<object>)leaf).Visit(null)
                + "/" + ((MirrorVisitorMid<object>)leaf).Visit(null) + "/" + ViaMirrorMid<object>(leaf, null));
            Console.WriteLine("virtual constrained interface=" + ViaInspect<DualInspectStruct<object>, object>(default, null)
                + "/" + ViaInspectOfObject(new DualInspectStruct<object>())
                + "/" + ((IInspect<object>)new DualInspectStruct<object>()).Inspect(null)
                + "/" + ViaObjectInspect(new DualInspectStruct<object>()));
            Console.WriteLine("virtual constrained variant=" + ViaContra(new ContraInspectStruct<object>(), "s")
                + "/" + ((IContraInspect<string>)new ContraInspectStruct<object>()).Inspect("s"));
            Console.WriteLine("virtual constrained equals=" + SameAs(new EqualsPair<object>(), null)
                + "/" + ((object)new EqualsPair<object>()).Equals(null));
        }
    }
}
