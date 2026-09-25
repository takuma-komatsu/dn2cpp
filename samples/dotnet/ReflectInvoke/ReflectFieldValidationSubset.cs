#nullable enable
using System;
using System.Reflection;

// SUBJECT: FieldInfo.GetValue/SetValue check the receiver, then the value,
// before touching the field, with .NET's exceptions, HResults and messages. An
// instance field takes an instance of its declaring type (a derived instance, a
// boxed struct and a MakeGenericType instantiation's own instance pass) and a
// static field ignores its receiver. A value converts as a reflected method
// argument does, widening primitives and enums and taking a boxed U for
// Nullable<U>; null stores the field type's default.
namespace ReflectFieldValidationSubset;

enum Level { Low, Mid, High }

enum Tone { Soft, Loud }

enum Wide : long { Small, Big = 5 }

struct Spot
{
    public int X;
    public string? Tag;

    public override string ToString() => "(" + X + "," + (Tag ?? "null") + ")";
}

class Animal
{
    public override string ToString() => "animal";
}

class Dog : Animal
{
    public override string ToString() => "dog";
}

class Stranger
{
    public int Number = 99;
}

class Target
{
    public int Number = 1;
    public string? Text = "t";
    public Spot Where = new Spot { X = 2, Tag = "w" };
    public Level Grade = Level.Mid;
    public int? Maybe = 4;
    public long Big = 6;
    public double Ratio = 1.5;
    public Wide Span = Wide.Big;
    public Animal? Pet = new Animal();
    public object? Anything;

    public static int Shared = 3;
    public static string? SharedText = "s";
}

class DerivedTarget : Target { }

// No code names Unnamed, so its type-info is not emitted and the field's
// reflected type reads Object; only its setter knows the field is a value.
enum Unnamed { None, Some }

class Quiet
{
    public Unnamed Mode;
}

// Only typeof names this definition, so MakeGenericType mints each instantiation
// from one template, whose field rows serve every instantiation.
class Holder<T>
{
    public string? Label = "holder";
    public int Count;
}

static class Program
{
    private static void Show(string label, Func<object?> run)
    {
        string text;
        try
        {
            object? value = run();
            text = value is null ? "null" : value.GetType().Name + ":" + value;
        }
        catch (Exception ex)
        {
            text = $"{ex.GetType().Name} 0x{ex.HResult:X8} {ex.Message}";
        }
        Console.WriteLine($"{label}: {text}");
    }

    private static FieldInfo Field(Type type, string name) =>
        type.GetField(name) ?? throw new MissingFieldException(type.Name, name);

    internal static void Run()
    {
        Console.WriteLine("== field validation ==");
        FieldInfo number = Field(typeof(Target), "Number");
        FieldInfo text = Field(typeof(Target), "Text");
        FieldInfo where = Field(typeof(Target), "Where");
        FieldInfo grade = Field(typeof(Target), "Grade");
        FieldInfo maybe = Field(typeof(Target), "Maybe");
        FieldInfo big = Field(typeof(Target), "Big");
        FieldInfo ratio = Field(typeof(Target), "Ratio");
        FieldInfo span = Field(typeof(Target), "Span");
        FieldInfo pet = Field(typeof(Target), "Pet");
        FieldInfo anything = Field(typeof(Target), "Anything");
        FieldInfo shared = Field(typeof(Target), "Shared");
        FieldInfo sharedText = Field(typeof(Target), "SharedText");
        FieldInfo spotX = Field(typeof(Spot), "X");
        FieldInfo spotTag = Field(typeof(Spot), "Tag");

        Show("null receiver, get int", () => number.GetValue(null));
        Show("null receiver, get string", () => text.GetValue(null));
        Show("null receiver, get struct", () => where.GetValue(null));
        Show("null receiver, set int", () => { number.SetValue(null, 5); return null; });
        Show("null receiver, set string", () => { text.SetValue(null, "x"); return null; });
        Show("null receiver, set struct", () => { where.SetValue(null, new Spot()); return null; });
        Show("null receiver, struct field", () => spotX.GetValue(null));

        var stranger = new Stranger();
        Show("stranger receiver, get", () => number.GetValue(stranger));
        Show("stranger receiver, set", () => { number.SetValue(stranger, 5); return null; });
        Show("stranger after set", () => stranger.Number);
        Show("string receiver, get", () => text.GetValue("receiver"));
        Show("boxed int receiver, struct field", () => spotX.GetValue(5));
        Show("class receiver, struct field", () => spotX.GetValue(new Target()));

        var derived = new DerivedTarget();
        Show("derived receiver, get", () => number.GetValue(derived));
        Show("derived receiver, set", () => { number.SetValue(derived, 11); return derived.Number; });
        Show("derived query, base receiver", () => Field(typeof(DerivedTarget), "Number").GetValue(new Target()));

        object spot = new Spot { X = 7, Tag = "a" };
        Show("boxed struct, get", () => spotX.GetValue(spot));
        Show("boxed struct, set", () => { spotX.SetValue(spot, 8); return spotX.GetValue(spot); });
        Show("boxed struct, set reference", () => { spotTag.SetValue(spot, "b"); return spot; });

        Show("static, null receiver", () => shared.GetValue(null));
        Show("static, stranger receiver", () => shared.GetValue(stranger));
        Show("static set, string receiver", () => { shared.SetValue("receiver", 9); return Target.Shared; });
        Show("static set, null value", () => { sharedText.SetValue(stranger, null); return Target.SharedText; });
        Show("static set, null into int", () => { shared.SetValue(null, null); return Target.Shared; });
        Show("static set, wrong value", () => { shared.SetValue(null, "x"); return Target.Shared; });

        var target = new Target();
        Show("null into int", () => { number.SetValue(target, null); return target.Number; });
        Show("null into enum", () => { grade.SetValue(target, null); return target.Grade; });
        Show("null into struct", () => { where.SetValue(target, null); return target.Where; });
        Show("null into nullable", () => { maybe.SetValue(target, null); return target.Maybe.HasValue; });
        Show("null into string", () => { text.SetValue(target, null); return target.Text; });
        Show("null into long enum", () => { span.SetValue(target, null); return target.Span; });
        Show("null into double", () => { ratio.SetValue(target, null); return target.Ratio; });
        Show("null into class", () => { pet.SetValue(target, null); return target.Pet; });

        Show("string into int", () => { number.SetValue(target, "12"); return target.Number; });
        Show("long into int", () => { number.SetValue(target, 12L); return target.Number; });
        Show("struct into int", () => { number.SetValue(target, new Spot()); return target.Number; });
        Show("int into struct", () => { where.SetValue(target, 12); return target.Where; });
        Show("long into nullable", () => { maybe.SetValue(target, 12L); return target.Maybe; });
        Show("long enum into enum", () => { grade.SetValue(target, Wide.Big); return target.Grade; });
        Show("stranger into class", () => { pet.SetValue(target, stranger); return target.Pet; });
        Show("int into string", () => { text.SetValue(target, 12); return target.Text; });

        Show("short into int", () => { number.SetValue(target, (short)12); return target.Number; });
        Show("byte into int", () => { number.SetValue(target, (byte)13); return target.Number; });
        Show("char into int", () => { number.SetValue(target, 'A'); return target.Number; });
        Show("int into long", () => { big.SetValue(target, 7); return target.Big; });
        Show("int into double", () => { ratio.SetValue(target, 2); return target.Ratio; });
        Show("int into enum", () => { grade.SetValue(target, 2); return target.Grade; });
        Show("enum into enum", () => { grade.SetValue(target, Level.Mid); return target.Grade; });
        Show("other enum into enum", () => { grade.SetValue(target, Tone.Loud); return target.Grade; });
        Show("enum into int", () => { number.SetValue(target, Level.High); return target.Number; });
        Show("int into long enum", () => { span.SetValue(target, 3); return target.Span; });
        Show("int into nullable", () => { maybe.SetValue(target, 5); return target.Maybe; });
        Show("struct into struct", () => { where.SetValue(target, new Spot { X = 3, Tag = "c" }); return target.Where; });
        Show("derived into class", () => { pet.SetValue(target, new Dog()); return target.Pet; });
        Show("int into object", () => { anything.SetValue(target, 5); return target.Anything; });

        Show("null receiver, wrong value", () => { number.SetValue(null, "x"); return null; });
        Show("stranger receiver, wrong value", () => { number.SetValue(stranger, "x"); return null; });
        Show("value after faults", () => target.Number + "/" + stranger.Number);

        Type minted = typeof(Holder<>).MakeGenericType(typeof(string));
        object holder = Activator.CreateInstance(minted)!;
        object other = Activator.CreateInstance(typeof(Holder<>).MakeGenericType(typeof(int)))!;
        FieldInfo label = Field(minted, "Label");
        FieldInfo count = Field(minted, "Count");
        Show("minted get", () => label.GetValue(holder));
        Show("minted set", () => { label.SetValue(holder, "minted"); return label.GetValue(holder); });
        Show("minted widening", () => { count.SetValue(holder, (short)4); return count.GetValue(holder); });
        Show("minted null into int", () => { count.SetValue(holder, null); return count.GetValue(holder); });
        Show("minted wrong value", () => { count.SetValue(holder, "4"); return null; });
        Show("minted null receiver", () => label.GetValue(null));
        Show("minted stranger receiver", () => label.GetValue(stranger));
        Show("minted other instantiation", () => label.GetValue(other));

        FieldInfo mode = Field(typeof(Quiet), "Mode");
        Show("null into unnamed enum", () => { mode.SetValue(new Quiet(), null); return "stored"; });

        Console.WriteLine("field validation end");
    }
}
