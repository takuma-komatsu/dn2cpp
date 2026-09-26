#nullable enable
using System;
using System.Reflection;

// SUBJECT: FieldInfo.GetValue/SetValue check the receiver, then the value,
// before touching the field, with .NET's exceptions, HResults and messages. An
// instance field takes an instance of its declaring type (a derived instance, a
// boxed struct and a MakeGenericType instantiation's own instance pass) and a
// static field ignores its receiver. A value converts as a reflected method
// argument does, widening primitives and enums and taking a boxed U for
// Nullable<U>; null stores the field type's default. A constant answers from
// metadata whatever the receiver and refuses SetValue before any check; a static
// read-only field refuses SetValue once the value checks; a Nullable<T> field
// reads back as null or a boxed T. An enum that only a reflected member row or a
// closed generic argument names reports its own type, and SetValue on a boxed
// enum's value__ writes the box.
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

// Only a reflected field row names Unnamed.
enum Unnamed { None, Some }

class Quiet
{
    public Unnamed Mode;
    public Unnamed? MaybeMode;
    public UnnamedProp Prop { get; set; }
    public static int Take(UnnamedParam p) => (int)p;
    public static UnnamedRet Give() => UnnamedRet.R1;
}

// Only a reflected member row or a closed generic argument names each of these.
enum UnnamedConst { First, Second }

enum UnnamedParam { P0, P1 }

enum UnnamedProp { Q0, Q1 }

enum UnnamedRet { R0, R1 }

enum UnnamedArg { A0 }

class Marker<T> { }

// Only typeof names this definition, so MakeGenericType mints each instantiation
// from one template, whose field rows serve every instantiation.
class Holder<T>
{
    public string? Label = "holder";
    public int Count;
}

// A constant answers from metadata; only its encoded type decides the box, so
// C#'s nint constant, stored as an int, reads back as one.
class Constants
{
    public const int Answer = 42;
    public const string Word = "word";
    public const string? Nothing = null;
    public const object? NoObject = null;
    public const string EmptyWord = "";
    public const char Letter = 'Z';
    public const bool Flag = true;
    public const float Half = 0.5f;
    public const float FloatNaN = float.NaN;
    public const double NegativeZero = -0.0;
    public const double Infinite = double.PositiveInfinity;
    public const long Min = long.MinValue;
    public const ulong Max = ulong.MaxValue;
    public const byte Small = 200;
    public const sbyte Signed = -100;
    public const short Short = -3;
    public const ushort UShort = 65535;
    public const uint Large = 4000000000;
    public const Level Grade = Level.High;
    public const Wide Span = Wide.Big;
    public const UnnamedConst Fixed = UnnamedConst.Second;
    public const nint Native = 7;
    public const decimal Money = 1.25m;
    private const int Hidden = 5;
}

class ReadOnlyStatics
{
    public static readonly int Count = 3;
    public static readonly string? Name = "name";
    public static readonly Spot Place = new Spot { X = 1, Tag = "p" };
    public readonly int Own = 4;
}

class NoInitializer
{
    public static readonly int Value;
}

class Outer
{
    public class Nested
    {
        public static readonly int Value = 1;
    }
}

class Generic<T>
{
    public static readonly int Value = 1;
}

class Optional
{
    public int? Some = 4;
    public int? None;
    public Level? Grade = Level.High;
    public Spot? Place = new Spot { X = 9, Tag = "o" };
    public static double? Shared;
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

        foreach (string name in new[] { "Answer", "Word", "Nothing", "NoObject", "EmptyWord", "Letter", "Flag",
            "Half", "FloatNaN", "NegativeZero", "Infinite", "Min", "Max", "Small", "Signed", "Short", "UShort",
            "Large", "Grade", "Span", "Native", "Money" })
        {
            FieldInfo constant = Field(typeof(Constants), name);
            Show($"const {name} {constant.FieldType.Name} literal={constant.IsLiteral} initonly={constant.IsInitOnly}",
                () => constant.GetValue(null));
        }
        FieldInfo answer = Field(typeof(Constants), "Answer");
        Show("const bits", () =>
            BitConverter.SingleToInt32Bits((float)Field(typeof(Constants), "FloatNaN").GetValue(null)!).ToString("X8") + "/"
            + BitConverter.DoubleToInt64Bits((double)Field(typeof(Constants), "NegativeZero").GetValue(null)!).ToString("X16"));
        Show("private const", () =>
            typeof(Constants).GetField("Hidden", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null));
        Show("const, stranger receiver", () => answer.GetValue(stranger));
        Show("const set", () => { answer.SetValue(null, 43); return null; });
        Show("const set, stranger receiver", () => { answer.SetValue(stranger, 43); return null; });
        Show("const set, wrong value", () => { answer.SetValue(null, "x"); return null; });
        Show("const string set", () => { Field(typeof(Constants), "Word").SetValue(null, "other"); return null; });
        Show("enum member set", () => { Field(typeof(Level), "High").SetValue(null, Level.Low); return null; });
        Show("primitive const set", () => { Field(typeof(int), "MaxValue").SetValue(null, 1); return null; });
        Show("decimal const set", () => { Field(typeof(Constants), "Money").SetValue(null, 2m); return Constants.Money; });

        // The first SetValue on a field checks the value before refusing it.
        FieldInfo readOnlyCount = Field(typeof(ReadOnlyStatics), "Count");
        Show("readonly static set, wrong value", () => { readOnlyCount.SetValue(null, "x"); return null; });
        Show("readonly static set", () => { readOnlyCount.SetValue(null, 4); return null; });
        Show("readonly static set, stranger receiver", () => { readOnlyCount.SetValue(stranger, 4); return null; });
        Show("readonly static set, null value", () => { readOnlyCount.SetValue(null, null); return null; });
        Show("readonly static get", () => readOnlyCount.GetValue(null));
        Show("readonly static string set", () => { Field(typeof(ReadOnlyStatics), "Name").SetValue(null, "other"); return null; });
        Show("readonly static struct set", () => { Field(typeof(ReadOnlyStatics), "Place").SetValue(null, new Spot()); return null; });
        Show("readonly statics after refusals", () =>
            ReadOnlyStatics.Count + "/" + ReadOnlyStatics.Name + "/" + ReadOnlyStatics.Place);
        var readOnly = new ReadOnlyStatics();
        Show("readonly instance set", () => { Field(typeof(ReadOnlyStatics), "Own").SetValue(readOnly, 9); return readOnly.Own; });
        Show("uninitialized readonly set", () => { Field(typeof(NoInitializer), "Value").SetValue(null, 1); return null; });
        Show("nested readonly set", () => { Field(typeof(Outer.Nested), "Value").SetValue(null, 2); return null; });
        Show("generic readonly set", () => { Field(typeof(Generic<string>), "Value").SetValue(null, 2); return null; });
        Show("String.Empty set", () => { Field(typeof(string), "Empty").SetValue(null, "x"); return null; });
        Show("Boolean.TrueString set", () => { Field(typeof(bool), "TrueString").SetValue(null, "x"); return null; });

        var optional = new Optional();
        FieldInfo some = Field(typeof(Optional), "Some");
        FieldInfo optionalShared = Field(typeof(Optional), "Shared");
        Show("nullable get", () => some.GetValue(optional));
        Show("nullable get, no value", () => Field(typeof(Optional), "None").GetValue(optional));
        Show("nullable enum get", () => Field(typeof(Optional), "Grade").GetValue(optional));
        Show("nullable struct get", () => Field(typeof(Optional), "Place").GetValue(optional));
        Show("static nullable get", () => optionalShared.GetValue(null));
        Show("nullable set, get", () => { some.SetValue(optional, 8); return some.GetValue(optional); });
        Show("nullable set null, get", () => { some.SetValue(optional, null); return some.GetValue(optional); });
        Show("static nullable set, get", () => { optionalShared.SetValue(null, 2.5); return optionalShared.GetValue(null); });
        Show("nullable set short", () => { some.SetValue(optional, (short)3); return null; });

        var quiet = new Quiet();
        FieldInfo maybeMode = Field(typeof(Quiet), "MaybeMode");
        Show("unnamed field type", () => mode.FieldType.FullName);
        Show("unnamed get", () => mode.GetValue(quiet));
        Show("unnamed set int", () => { mode.SetValue(quiet, 1); return mode.GetValue(quiet); });
        Show("unnamed set other enum", () => { mode.SetValue(quiet, Level.High); return mode.GetValue(quiet); });
        Show("unnamed set string", () => { mode.SetValue(quiet, "x"); return null; });
        Show("unnamed names", () => string.Join(",", Enum.GetNames(mode.FieldType)));
        Show("unnamed nullable type", () => maybeMode.FieldType.ToString());
        Show("unnamed nullable get", () => maybeMode.GetValue(quiet));
        Show("unnamed nullable set", () => { maybeMode.SetValue(quiet, mode.GetValue(quiet)); return maybeMode.GetValue(quiet); });
        FieldInfo fixedField = Field(typeof(Constants), "Fixed");
        Show($"unnamed constant {fixedField.FieldType.Name}", () => fixedField.GetValue(null));
        MethodInfo take = typeof(Quiet).GetMethod("Take")!;
        Show("unnamed parameter type", () => take.GetParameters()[0].ParameterType.FullName);
        Show("unnamed parameter invoke", () => take.Invoke(null, new object[] { 1 }));
        Show("unnamed parameter wrong argument", () => take.Invoke(null, new object[] { "x" }));
        PropertyInfo prop = typeof(Quiet).GetProperty("Prop")!;
        Show("unnamed property type", () => prop.PropertyType.FullName);
        Show("unnamed property get", () => prop.GetValue(quiet));
        Show("unnamed property wrong value", () => { prop.SetValue(quiet, "x"); return null; });
        MethodInfo give = typeof(Quiet).GetMethod("Give")!;
        Show("unnamed return type", () => give.ReturnType.FullName);
        Show("unnamed return invoke", () => give.Invoke(null, null));
        Show("unnamed generic argument", () => new Marker<UnnamedArg>().GetType().ToString());

        object boxedLevel = Level.Low;
        FieldInfo levelValue = Field(typeof(Level), "value__");
        Show("enum value__ set", () => { levelValue.SetValue(boxedLevel, 2); return boxedLevel; });
        Show("enum value__ set, wrong value", () => { levelValue.SetValue(boxedLevel, 2L); return null; });

        Console.WriteLine("field validation end");
    }
}
