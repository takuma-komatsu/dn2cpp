#nullable enable
using System;
using System.Globalization;
using System.IO;
using System.Reflection;

// SUBJECT: MethodInfo.Invoke, PropertyInfo.GetValue/SetValue and a delegate
// CreateDelegate binds, entered through a virtual row, run the receiver's most
// derived body in that row's slot, as a callvirt would: overrides through base
// and middle rows, abstract rows, new-slot hiders, sealed overrides, setter-only
// overrides, generic bases over reference and value arguments, a
// MakeGenericType receiver, a boxed enum through a System.Enum row, compiled
// framework overrides of abstract rows, framework overrides only reflection
// reaches, which a string literal after typeof names, and interface rows whose
// declaration has a default body. An abstract row checks its receiver and
// arguments first, and a closed CreateDelegate binding reports the body it runs;
// a boxed value binds as a receiver or a first argument and runs on its box. An
// application interface's static, non-virtual and private members that nothing
// calls run as themselves. RunStripped (dn2cpp only) reaches bodies the image
// stripped through each trap shape.
namespace ReflectVirtualInvokeSubset;

class Base
{
    public virtual string Who() => "base";
}

class Mid : Base
{
    public override string Who() => "mid";
}

class Leaf : Mid
{
    public override string Who() => "leaf";
}

abstract class Shape
{
    public abstract string Kind();
    public abstract int Sides { get; }
    public abstract string Scale(int factor);
}

sealed class Square : Shape
{
    public override string Kind() => "square";
    public override int Sides => 4;
    public override string Scale(int factor) => "square*" + factor;
}

class Animal
{
    public virtual string Speak() => "animal";
}

class Dog : Animal
{
    public override string Speak() => "dog";
}

// A new slot: the base rows keep the base slot, whose most derived body for a
// LoudPuppy is Dog's; the hider's row runs the hider chain.
class Puppy : Dog
{
    public new virtual string Speak() => "puppy";
}

class LoudPuppy : Puppy
{
    public override string Speak() => "loud-puppy";
}

class Vehicle
{
    public virtual string Wheels() => "vehicle";
}

class Car : Vehicle
{
    public sealed override string Wheels() => "car";
}

class SportsCar : Car { }

class Setting
{
    public string Log = "";

    public virtual string Value
    {
        get => "value:" + Log;
        set => Log += "base=" + value + ";";
    }
}

// Overrides the setter alone; the getter stays the base body.
class TracedSetting : Setting
{
    public override string Value
    {
        set => Log += "traced=" + value + ";";
    }
}

class Box<T>
{
    public virtual string Describe(T value) => "box:" + value;
}

class NamedBox : Box<string>
{
    public override string Describe(string value) => "named:" + value;
}

class NumberBox : Box<int>
{
    public override string Describe(int value) => "number:" + value;
}

// A generic override reads its own type argument: a shared body over a
// reference argument, a specialized one over a value argument.
class Wrapper<T> : Box<T>
{
    public override string Describe(T value) => "wrapper<" + typeof(T).Name + ">:" + value;
}

abstract class Greeter
{
    public abstract string Greet();
    public virtual string Farewell() => "bye";
}

// Only typeof names this definition, so MakeGenericType mints the instance.
class MintedGreeter<T> : Greeter
{
    public override string Greet() => "hello:" + typeof(T).Name;
    public override string Farewell() => "bye:" + typeof(T).Name;
}

enum Tone { Low, High }

interface IGreeting
{
    string Hello() => "default-hello";
    string Tag();

    // Not virtual: a class method of the same shape never implements it.
    sealed string Shout() => Hello().ToUpperInvariant();
}

class DefaultGreeting : IGreeting
{
    public string Tag() => "default-tag";
}

class CustomGreeting : IGreeting
{
    public string Hello() => "custom-hello";
    public string Tag() => "custom-tag";
    public virtual string Shout() => "class-shout";
}

class DerivedGreeting : CustomGreeting { }

class ExplicitGreeting : IGreeting
{
    string IGreeting.Hello() => "explicit-hello";
    string IGreeting.Tag() => "explicit-tag";
}

struct StructGreeting : IGreeting
{
    public int Id;

    public string Hello() => "struct-hello:" + Id;
    public string Tag() => "struct-tag:" + Id;
}

interface IFancyGreeting : IGreeting
{
    string IGreeting.Hello() => "fancy-hello";
}

class FancyGreeting : IFancyGreeting
{
    public string Tag() => "fancy-tag";
}

// Declares members only reflection runs: a static method and property, and a
// non-virtual and a private instance member.
interface IToolbox
{
    static string Make() => "made";
    static string Label => "toolbox";
    sealed string Stamp() => "stamp:" + Id();
    private string Secret() => "secret:" + Id();
    string Id();
}

class Toolbox : IToolbox
{
    public string Id() => "box";
}

interface ICounter
{
    int Get();
    void Bump();
    string Label() => "counter-default:" + Get();
}

struct Tally : ICounter
{
    public int N;

    public int Get() => N;
    public void Bump() => N++;
}

struct NamedTally : ICounter
{
    public int N;

    public int Get() => N;
    public void Bump() => N += 10;
    public string Label() => "named:" + N;
}

static class Program
{
    private static void Try(string label, Func<object?> invoke)
    {
        string text;
        try
        {
            text = invoke()?.ToString() ?? "null";
        }
        catch (Exception ex)
        {
            text = ex.GetType().Name
                + (ex.InnerException is null ? "" : "/" + ex.InnerException.GetType().Name);
        }
        Console.WriteLine(label + ": " + text);
    }

    private static void Fault(string label, Action invoke)
    {
        try
        {
            invoke();
            Console.WriteLine($"{label}: no fault");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{label}: {ex.GetType().Name} 0x{ex.HResult:X8} {ex.Message}");
        }
    }

    private static string Describe(MethodInfo method) => method.DeclaringType!.Name + "." + method.Name;

    private static MethodInfo Method(Type type, string name) =>
        type.GetMethod(name, Type.EmptyTypes) ?? throw new MissingMethodException(type.Name, name);

    private static int Hundreds(ICounter counter) => counter.Get() * 100;

    private static string Ymd(object? value)
    {
        var date = (DateTime)value!;
        return date.Year + "-" + date.Month + "-" + date.Day;
    }

    private static MethodInfo Named(Type type, string name)
    {
        foreach (var method in type.GetMethods())
            if (method.Name == name)
                return method;
        throw new MissingMethodException(type.Name, name);
    }

    // dn2cpp only: GetMethods names no member, so these overrides are compiled for
    // no other reason and stay stripped. Each slot shape reports that as a
    // catchable NotSupportedException; .NET runs the bodies.
    internal static void RunStripped()
    {
        Type calendarType = typeof(Calendar);
        Type convertibleType = typeof(IConvertible);
        var calendar = new GregorianCalendar();
        var leap = new DateTime(2020, 2, 29);
        Fault("stripped struct-returning slot", () => Named(calendarType, "AddYears").Invoke(calendar, new object[] { leap, 1 }));
        Fault("stripped slot", () => Named(calendarType, "GetDayOfMonth").Invoke(calendar, new object[] { leap }));
        Fault("stripped interface struct-returning slot", () => Named(convertibleType, "ToDateTime")
            .Invoke(DBNull.Value, new object?[] { null }));
        Fault("stripped interface slot", () => Named(convertibleType, "ToInt32").Invoke(DBNull.Value, new object?[] { null }));
        var day = (Func<DateTime, int>)Delegate.CreateDelegate(typeof(Func<DateTime, int>), calendar,
            Named(calendarType, "GetDayOfMonth"));
        Fault("stripped slot, delegate", () => day(leap));
        Console.WriteLine("stripped end");
    }

    internal static void Run()
    {
        Console.WriteLine("== virtual invoke ==");

        MethodInfo baseWho = Method(typeof(Base), "Who");
        MethodInfo midWho = Method(typeof(Mid), "Who");
        Try("base row, leaf", () => baseWho.Invoke(new Leaf(), null));
        Try("base row, mid", () => baseWho.Invoke(new Mid(), null));
        Try("base row, base", () => baseWho.Invoke(new Base(), null));
        Try("mid row, leaf", () => midWho.Invoke(new Leaf(), null));
        Try("base row declaring type", () => baseWho.DeclaringType!.Name);

        MethodInfo kind = Method(typeof(Shape), "Kind");
        PropertyInfo sides = typeof(Shape).GetProperty("Sides")!;
        MethodInfo scale = typeof(Shape).GetMethod("Scale")!;
        Try("abstract row", () => kind.Invoke(new Square(), null));
        Try("abstract property", () => sides.GetValue(new Square()));
        Try("abstract getter row", () => sides.GetGetMethod()!.Invoke(new Square(), null));
        Try("abstract row, argument", () => scale.Invoke(new Square(), new object[] { 3 }));
        Fault("abstract row, null receiver", () => kind.Invoke(null, null));
        Fault("abstract row, wrong receiver", () => kind.Invoke(new Leaf(), null));
        Fault("abstract row, extra argument", () => kind.Invoke(new Square(), new object[] { 1 }));
        Fault("abstract row, wrong argument", () => scale.Invoke(new Square(), new object[] { "3" }));
        Fault("abstract property, null receiver", () => sides.GetValue(null));

        Animal animal = new LoudPuppy();
        Puppy puppy = new LoudPuppy();
        Try("direct call, loud puppy", () => animal.Speak() + "/" + puppy.Speak());
        Try("animal row, loud puppy", () => Method(typeof(Animal), "Speak").Invoke(new LoudPuppy(), null));
        Try("dog row, loud puppy", () => Method(typeof(Dog), "Speak").Invoke(new LoudPuppy(), null));
        Try("puppy row, loud puppy", () => Method(typeof(Puppy), "Speak").Invoke(new LoudPuppy(), null));
        Try("puppy row, puppy", () => Method(typeof(Puppy), "Speak").Invoke(new Puppy(), null));

        Try("vehicle row, sports car", () => Method(typeof(Vehicle), "Wheels").Invoke(new SportsCar(), null));
        Try("car row, sports car", () => Method(typeof(Car), "Wheels").Invoke(new SportsCar(), null));

        var traced = new TracedSetting();
        PropertyInfo value = typeof(Setting).GetProperty("Value")!;
        value.SetValue(traced, "a");
        typeof(TracedSetting).GetProperty("Value")!.SetValue(traced, "b");
        Try("setter override", () => traced.Log);
        Try("inherited getter", () => value.GetValue(traced));

        MethodInfo describeText = typeof(Box<string>).GetMethod("Describe")!;
        MethodInfo describeNumber = typeof(Box<int>).GetMethod("Describe")!;
        Try("generic base, reference override", () => describeText.Invoke(new NamedBox(), new object[] { "a" }));
        Try("generic base, value override", () => describeNumber.Invoke(new NumberBox(), new object[] { 7 }));
        Try("generic base, shared override", () => describeText.Invoke(new Wrapper<string>(), new object[] { "b" }));
        Try("generic base, value generic override", () => describeNumber.Invoke(new Wrapper<int>(), new object[] { 8 }));
        Try("generic base, own body", () => describeText.Invoke(new Box<string>(), new object[] { "c" }));

        object minted = Activator.CreateInstance(typeof(MintedGreeter<>).MakeGenericType(typeof(long)))!;
        Try("minted receiver, abstract row", () => Method(typeof(Greeter), "Greet").Invoke(minted, null));
        Try("minted receiver, virtual row", () => Method(typeof(Greeter), "Farewell").Invoke(minted, null));

        // The direct call gives System.Enum's row a body; a boxed enum has no vtable.
        Enum high = Tone.High;
        Try("enum direct", () => high.CompareTo(Tone.Low));
        Try("Enum row, enum", () => typeof(Enum).GetMethod("CompareTo")!.Invoke(Tone.High, new object[] { Tone.Low }));

        // The direct calls compile the framework overrides the framework rows reach.
        var stream = new MemoryStream();
        stream.SetLength(1);
        Try("framework direct", () => stream.CanSeek + "/" + stream.Length);
        Try("framework abstract property", () => typeof(Stream).GetProperty("CanSeek")!.GetValue(stream));
        Try("framework abstract row", () => typeof(Stream).GetMethod("SetLength")!
            .Invoke(stream, new object[] { 3L }) ?? stream.Length);

        var toWho = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), new Leaf(), baseWho);
        Try("closed delegate, base row", () => toWho());
        Try("closed delegate method", () => Describe(toWho.Method));
        var toBase = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), new Base(), baseWho);
        Try("closed delegate, own body", () => toBase() + "/" + Describe(toBase.Method));
        var toKind = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), new Square(), kind);
        Try("closed delegate, abstract row", () => toKind() + "/" + Describe(toKind.Method));
        var openWho = (Func<Base, string>)Delegate.CreateDelegate(typeof(Func<Base, string>), baseWho);
        Try("open delegate, base row", () => openWho(new Leaf()) + "/" + openWho(new Base()) + "/" + Describe(openWho.Method));
        var openKind = (Func<Shape, string>)Delegate.CreateDelegate(typeof(Func<Shape, string>), kind);
        Try("open delegate, abstract row", () => openKind(new Square()) + "/" + Describe(openKind.Method));

        MethodInfo hello = Method(typeof(IGreeting), "Hello");
        MethodInfo tag = Method(typeof(IGreeting), "Tag");
        MethodInfo shout = Method(typeof(IGreeting), "Shout");
        Try("default row, inherited default", () => hello.Invoke(new DefaultGreeting(), null));
        Try("default row, class body", () => hello.Invoke(new CustomGreeting(), null));
        Try("default row, inherited class body", () => hello.Invoke(new DerivedGreeting(), null));
        Try("default row, explicit body", () => hello.Invoke(new ExplicitGreeting(), null));
        Try("default row, struct body", () => hello.Invoke(new StructGreeting { Id = 2 }, null));
        Try("default row, derived interface body", () => hello.Invoke(new FancyGreeting(), null));
        Try("abstract interface row, struct", () => tag.Invoke(new StructGreeting { Id = 3 }, null));
        // The direct call gives the non-virtual member a body; a class method of
        // the same shape must not answer for it.
        IGreeting plain = new DefaultGreeting();
        Try("sealed interface direct", () => plain.Shout());
        Try("sealed interface row, class", () => shout.Invoke(new CustomGreeting(), null));
        Try("sealed interface row, default", () => shout.Invoke(new DefaultGreeting(), null));
        var toHello = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), new CustomGreeting(), hello);
        Try("closed delegate, default row", () => toHello() + "/" + Describe(toHello.Method));
        var toDefault = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), new DefaultGreeting(), hello);
        Try("closed delegate, inherited default", () => toDefault() + "/" + Describe(toDefault.Method));
        var toTag = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), new ExplicitGreeting(), tag);
        Try("closed delegate, interface row", () => toTag() + "/" + Describe(toTag.Method));

        MethodInfo stamp = typeof(IToolbox).GetMethod("Stamp")!;
        MethodInfo make = typeof(IToolbox).GetMethod("Make")!;
        Try("interface static row", () => make.Invoke(null, null));
        Try("interface static row, ignored receiver", () => make.Invoke(new Toolbox(), null));
        Try("interface static property", () => typeof(IToolbox).GetProperty("Label")!.GetValue(null));
        Try("interface non-virtual row", () => stamp.Invoke(new Toolbox(), null));
        Try("interface private row", () => typeof(IToolbox).GetMethod("Secret", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(new Toolbox(), null));
        Try("interface static delegate", () =>
        {
            var toMake = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), make);
            return toMake() + "/" + Describe(toMake.Method);
        });
        Try("interface non-virtual delegate", () =>
        {
            var toStamp = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), new Toolbox(), stamp);
            return toStamp() + "/" + Describe(toStamp.Method);
        });
        Fault("interface non-virtual row, null receiver", () => stamp.Invoke(null, null));

        // typeof names each framework row below, so the receiver's override is
        // compiled although only reflection reaches it.
        var buffer = new MemoryStream();
        var buffered = new BufferedStream(buffer);
        buffered.WriteByte(1);
        Try("reflection-only framework override", () => typeof(Stream).GetMethod("Flush")!.Invoke(buffered, null)
            ?? "flushed:" + buffer.Length);
        buffered.WriteByte(2);
        Try("reflection-only framework override, delegate", () =>
        {
            var flushBuffered = (Action)Delegate.CreateDelegate(typeof(Action), buffered, typeof(Stream).GetMethod("Flush")!);
            flushBuffered();
            return buffer.Length + "/" + Describe(flushBuffered.Method);
        });
        PropertyInfo position = typeof(Stream).GetProperty("Position")!;
        Try("reflection-only framework getter", () => position.GetValue(buffered));
        Try("reflection-only framework setter", () =>
        {
            position.SetValue(buffered, 1L);
            return position.GetValue(buffered) + "/" + buffer.Position;
        });
        Try("reflection-only struct-returning override", () => Ymd(typeof(Calendar).GetMethod("AddMonths")!
            .Invoke(new GregorianCalendar(), new object[] { new DateTime(2020, 1, 31), 1 })));
        Try("reflection-only framework interface impl", () => typeof(ICloneable).GetMethod("Clone")!.Invoke(new Version(1, 2), null));

        // A boxed value binds an interface row or a reference base's row, and each
        // call runs the value's body on the box the delegate holds.
        object tally = new Tally { N = 5 };
        MethodInfo counterGet = typeof(ICounter).GetMethod("Get")!;
        MethodInfo counterBump = typeof(ICounter).GetMethod("Bump")!;
        MethodInfo counterLabel = typeof(ICounter).GetMethod("Label")!;
        Try("boxed struct delegate", () =>
        {
            var getTally = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), tally, counterGet);
            var bumpTally = (Action)Delegate.CreateDelegate(typeof(Action), tally, counterBump);
            bumpTally();
            bumpTally();
            return getTally() + "/" + ((Tally)tally).N + "/" + ReferenceEquals(bumpTally.Target, tally);
        });
        Try("boxed struct delegate method", () =>
            Describe(Delegate.CreateDelegate(typeof(Func<int>), tally, counterGet).Method) + "/"
            + Describe(Delegate.CreateDelegate(typeof(Action), tally, counterBump).Method));
        Try("boxed struct delegate equality", () => Delegate.CreateDelegate(typeof(Func<int>), tally, counterGet)
            .Equals(Delegate.CreateDelegate(typeof(Func<int>), tally, counterGet)));
        Try("boxed struct default body", () =>
        {
            var labelTally = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), tally, counterLabel);
            return labelTally() + "/" + Describe(labelTally.Method);
        });
        Try("boxed struct own body", () =>
        {
            var labelNamed = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), new NamedTally { N = 3 },
                counterLabel);
            return labelNamed() + "/" + Describe(labelNamed.Method);
        });
        Try("boxed struct generic CreateDelegate", () => counterGet.CreateDelegate<Func<int>>(tally)());
        Try("boxed struct first argument", () =>
        {
            var hundreds = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), tally,
                typeof(Program).GetMethod(nameof(Hundreds), BindingFlags.NonPublic | BindingFlags.Static)!);
            return hundreds() + "/" + ReferenceEquals(hundreds.Target, tally);
        });
        Try("boxed enum delegate", () =>
        {
            var compareTone = (Func<object, int>)Delegate.CreateDelegate(typeof(Func<object, int>), Tone.High,
                typeof(Enum).GetMethod("CompareTo")!);
            return compareTone(Tone.Low) + "/" + Describe(compareTone.Method) + "/" + compareTone.Target;
        });
        Try("boxed struct delegate, unrelated row", () =>
            Delegate.CreateDelegate(typeof(Func<string>), tally, kind, false) is null);

        // Bindings through a declaration and through the override it resolves to run
        // one body, so they are equal delegates.
        Try("boxed struct delegate equality across rows", () =>
        {
            var viaInterface = Delegate.CreateDelegate(typeof(Func<int>), tally, counterGet);
            var viaStruct = Delegate.CreateDelegate(typeof(Func<int>), tally, Method(typeof(Tally), "Get"));
            return viaInterface.Equals(viaStruct) + "/" + (viaInterface.GetHashCode() == viaStruct.GetHashCode());
        });
        var sharedSquare = new Square();
        Try("closed delegate equality across rows", () =>
        {
            var viaBase = Delegate.CreateDelegate(typeof(Func<string>), sharedSquare, kind);
            var viaOverride = Delegate.CreateDelegate(typeof(Func<string>), sharedSquare, Method(typeof(Square), "Kind"));
            return viaBase.Equals(viaOverride) + "/" + (viaBase.GetHashCode() == viaOverride.GetHashCode());
        });
        var sharedCustom = new CustomGreeting();
        Try("interface delegate equality across rows", () =>
        {
            var viaInterface = Delegate.CreateDelegate(typeof(Func<string>), sharedCustom, hello);
            var viaClass = Delegate.CreateDelegate(typeof(Func<string>), sharedCustom, Method(typeof(CustomGreeting), "Hello"));
            return viaInterface.Equals(viaClass) + "/" + (viaInterface.GetHashCode() == viaClass.GetHashCode());
        });
        Try("closed delegate equality, other body", () => Delegate.CreateDelegate(typeof(Func<string>), sharedCustom, hello)
            .Equals(Delegate.CreateDelegate(typeof(Func<string>), sharedCustom, tag)));

        Fault("boxed struct delegate, unrelated row, throwing", () => Delegate.CreateDelegate(typeof(Func<string>), tally, kind));
        Fault("closed delegate, unrelated receiver", () => Delegate.CreateDelegate(typeof(Func<string>), new Leaf(), kind));
        Fault("open delegate, unrelated receiver type", () => Delegate.CreateDelegate(typeof(Func<object, int>), counterGet));

        Console.WriteLine("virtual invoke end");
    }
}
