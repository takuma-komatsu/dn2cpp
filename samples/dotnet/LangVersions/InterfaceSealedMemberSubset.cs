using System;

// A sealed or private interface member is not virtual: a call through the
// interface runs the interface's own body, whatever a class or struct declares
// with the same signature. A constrained call on a struct boxes it for such a
// body and for a default it does not override.
namespace InterfaceSealedMemberSubset
{
    internal interface IShout
    {
        string Hello() => "default-hello";
        sealed string Shout() => "shout:" + Hello() + ":" + Helper();
        private string Helper() => "helper";
        string Echo() => Shout() + "|" + Helper();
        sealed string Twice<T>(T value) => "twice:" + typeof(T).Name + ":" + value;
    }

    internal class Loud : IShout
    {
        public string Hello() => "loud-hello";
        public virtual string Shout() => "class-shout";
        public virtual string Helper() => "class-helper";
        public virtual string Twice<T>(T value) => "class-twice";
    }

    internal sealed class Louder : Loud
    {
        public override string Shout() => "louder-shout";
    }

    internal struct Quiet : IShout
    {
        public int Id;
        public string Hello() => "quiet-hello:" + Id;
        public string Shout() => "struct-shout";
    }

    internal struct Mute : IShout
    {
        public int Id;
    }

    internal interface IValue<T>
    {
        T Value { get; }
        sealed string Show() => "show:" + Value + ":" + typeof(T).Name;
    }

    internal sealed class IntValue : IValue<int>
    {
        public int Value => 42;
        public string Show() => "class-show";
    }

    internal sealed class TextValue : IValue<string>
    {
        public string Value => "text";
        public string Show() => "class-show";
    }

    internal interface IPick
    {
        string Pick<T>(T value) => "base:" + value;
    }

    internal interface IPickLeft : IPick
    {
        string IPick.Pick<T>(T value) => "left:" + typeof(T).Name + ":" + value;
    }

    internal struct Picker : IPickLeft
    {
    }

    internal struct OwnPicker : IPick
    {
        public string Pick<T>(T value) => "own:" + value;
    }

    internal struct ExplicitPicker : IPick
    {
        string IPick.Pick<T>(T value) => "explicit:" + value;
    }

    internal struct PlainPicker : IPick
    {
    }

    internal static class Program
    {
        private static string Try(Func<string> call)
        {
            try
            {
                return call();
            }
            catch (Exception ex)
            {
                return ex.GetType().Name;
            }
        }

        private static string Shout<T>(T value) where T : IShout => value.Shout();
        private static string Hello<T>(T value) where T : IShout => value.Hello();
        private static string Echo<T>(T value) where T : IShout => value.Echo();
        private static string Twice<T>(T value) where T : IShout => value.Twice(7);
        private static string Show<T, U>(T value) where T : IValue<U> => value.Show();
        private static string PickOf<T>(T value) where T : IPick => value.Pick(8);

        internal static void __GateEntry()
        {
            Console.WriteLine("== sealed interface members ==");
            IShout loud = new Loud();
            IShout louder = new Louder();
            IShout quiet = new Quiet { Id = 1 };
            Console.WriteLine("class: " + loud.Shout() + " / " + louder.Shout() + " / " + loud.Echo());
            Console.WriteLine("generic: " + loud.Twice(5) + " / " + loud.Twice("x"));
            Console.WriteLine("boxed struct: " + quiet.Shout());
            IShout nothing = null;
            Console.WriteLine("null: " + Try(() => nothing.Shout()) + " / " + Try(() => nothing.Twice(1)));
            Func<string> bound = loud.Shout;
            Console.WriteLine("delegate: " + bound() + " / " + bound.Method.DeclaringType.Name);
            Console.WriteLine("constrained class: " + Shout(new Loud()) + " / " + Try(() => Shout<Loud>(null)));
            Console.WriteLine("constrained struct: " + Shout(new Quiet { Id = 2 }) + " / " + Twice(new Quiet { Id = 3 }));
            Console.WriteLine("constrained default: " + Shout(new Mute { Id = 4 }) + " / " + Hello(new Mute())
                + " / " + Echo(new Mute()));
            IValue<int> number = new IntValue();
            IValue<string> text = new TextValue();
            Console.WriteLine("generic interface: " + number.Show() + " / " + text.Show()
                + " / " + Show<IntValue, int>(new IntValue()) + " / " + Show<TextValue, string>(new TextValue()));
            Console.WriteLine("generic virtual: " + PickOf(new Picker()) + " / " + ((IPick)new Picker()).Pick("s")
                + " / " + PickOf(new OwnPicker()) + " / " + PickOf(new ExplicitPicker()) + " / " + PickOf(new PlainPicker()));
        }
    }
}
