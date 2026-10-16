using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ActivationTargets;

namespace ILDietUnknownTargetOnly;

internal static class Program
{
    internal static bool initOnlyInitialized;

    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("unknown-target prefix");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_UNKNOWN_TARGET") == "1")
            return;
        Console.WriteLine("== reflection targets from parameters ==");
        try
        {
            Console.WriteLine("invoke=" + Describe(typeof(InvokedTarget)));
        }
        catch (NotSupportedException e)
        {
            Console.WriteLine("invoke refused: " + e.GetType().Name + ": " + e.Message);
        }
        try
        {
            Console.WriteLine("construct=" + Construct(typeof(ConstructedTarget)));
        }
        catch (NotSupportedException e)
        {
            Console.WriteLine("construct refused: " + e.GetType().Name + ": " + e.Message);
        }
        Console.WriteLine("reflection targets from parameters end");
        Console.WriteLine("== reflection targets from collections ==");
        try
        {
            Console.WriteLine("element=" + RunElements());
        }
        catch (NotSupportedException e)
        {
            Console.WriteLine("element refused: " + e.GetType().Name + ": " + e.Message);
        }
        try
        {
            Console.WriteLine("copy=" + RunCopy());
        }
        catch (NotSupportedException e)
        {
            Console.WriteLine("copy refused: " + e.GetType().Name + ": " + e.Message);
        }
        Console.WriteLine("reflection targets from collections end");
        Console.WriteLine("== construction of omitted and never-boxed types ==");
        try
        {
            Console.WriteLine("library class=" + CreateLibraryClass(typeof(LibraryClass)));
        }
        catch (NotSupportedException e)
        {
            Console.WriteLine("library class refused: " + e.GetType().Name + ": " + e.Message);
        }
        // Declares no parameterless constructor, which its omitted (int) row does not change.
        try
        {
            Console.WriteLine("arguments only=" + CreateLibraryClass(typeof(ArgumentsOnly)));
        }
        catch (MissingMethodException e)
        {
            Console.WriteLine("arguments only missing: " + e.GetType().Name);
        }
        try
        {
            Console.WriteLine("library class with count=" + CreateWithCount(typeof(LibraryClass)));
        }
        catch (NotSupportedException e)
        {
            Console.WriteLine("library class with count refused: " + e.GetType().Name + ": " + e.Message);
        }
        object boxedDefault = default(LibraryStruct);
        Console.WriteLine("boxed default=" + boxedDefault);
        try
        {
            Console.WriteLine("library struct=" + CreateLibraryStruct(typeof(LibraryStruct)));
        }
        catch (NotSupportedException e)
        {
            Console.WriteLine("library struct refused: " + e.GetType().Name + ": " + e.Message);
        }
        try
        {
            Console.WriteLine("unboxed struct=" + CreateDescribed(typeof(UnboxedTarget)));
        }
        catch (NotSupportedException e)
        {
            Console.WriteLine("unboxed struct refused: " + e.GetType().Name + ": " + e.Message);
        }
        Console.WriteLine("construction of omitted and never-boxed types end");
        Console.WriteLine("== reflection targets on runtime-made instantiations ==");
        try
        {
            Console.WriteLine("template invoke=" + DescribeMade(typeof(TemplateTarget<>).MakeGenericType(typeof(Guid))));
        }
        catch (NotSupportedException e)
        {
            Console.WriteLine("template invoke refused: " + e.GetType().Name + ": " + e.Message);
        }
        Console.WriteLine("reflection targets on runtime-made instantiations end");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_WRITTEN_TARGET") == "1")
            return;
        Console.WriteLine("== reflection targets written back or boxed from a type argument ==");
        try
        {
            Console.WriteLine("written back=" + RunWrittenBack());
        }
        catch (NotSupportedException e)
        {
            Console.WriteLine("written back refused: " + e.GetType().Name + ": " + e.Message);
        }
        try
        {
            Console.WriteLine("boxed derived=" + RunBoxed<BoxedBase>(new BoxedDerived()));
        }
        catch (NotSupportedException e)
        {
            Console.WriteLine("boxed derived refused: " + e.GetType().Name + ": " + e.Message);
        }
        Console.WriteLine("boxed value=" + RunBoxed(new BoxedValue()));
        Console.WriteLine("reflection targets written back or boxed from a type argument end");
        Console.WriteLine("== reflection targets through an array type or a runtime name ==");
        try
        {
            // MakeArrayType finds only an array type the image instantiates.
            ArrayElementTarget[] instantiated = new ArrayElementTarget[0];
            Console.WriteLine("array element=" + RunArrayElement(typeof(ArrayElementTarget)) + "/" + instantiated.Length);
        }
        catch (NotSupportedException e)
        {
            Console.WriteLine("array element refused: " + e.GetType().Name + ": " + e.Message);
        }
        try
        {
            Console.WriteLine("generated member=" + GeneratedMember("get_Value"));
        }
        catch (NotSupportedException e)
        {
            Console.WriteLine("generated member refused: " + e.GetType().Name + ": " + e.Message);
        }
        Console.WriteLine("reflection targets through an array type or a runtime name end");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_INITONLY_FIELD") == "1")
            return;
        Console.WriteLine("== a static readonly field set on a parameter type ==");
        try
        {
            Console.WriteLine("initonly field=" + SetInitOnly(typeof(InitOnlyTarget)));
        }
        catch (NotSupportedException e)
        {
            Console.WriteLine("initonly field refused: " + e.GetType().Name + ": " + e.Message);
        }
        catch (FieldAccessException e)
        {
            Console.WriteLine("initonly field rejected: " + e.GetType().Name + ": " + e.Message);
        }
        Console.WriteLine("initonly target initialized=" + initOnlyInitialized);
        Console.WriteLine("a static readonly field set on a parameter type end");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_DISPATCHED_TARGET") == "1")
            return;
        Console.WriteLine("== a virtual delegate's method, a name bound per instantiation and an uninitialized parameter type ==");
        try
        {
            Console.WriteLine("virtual command=" + VirtualCommand(new CommandDerived()));
        }
        catch (NotSupportedException e)
        {
            Console.WriteLine("virtual command refused: " + e.GetType().Name + ": " + e.Message);
        }
        try
        {
            Console.WriteLine("merged a=" + ((Func<string>)BindNamed<MergedA>("Run"))());
        }
        catch (NotSupportedException e)
        {
            Console.WriteLine("merged a refused: " + e.GetType().Name + ": " + e.Message);
        }
        try
        {
            Console.WriteLine("merged b=" + ((Func<string>)BindNamed<MergedB>("Run"))());
        }
        catch (NotSupportedException e)
        {
            Console.WriteLine("merged b refused: " + e.GetType().Name + ": " + e.Message);
        }
        try
        {
            Console.WriteLine("blank=" + Blank(typeof(BlankTarget)));
        }
        catch (NotSupportedException e)
        {
            Console.WriteLine("blank refused: " + e.GetType().Name + ": " + e.Message);
        }
        Console.WriteLine("a virtual delegate's method, a name bound per instantiation and an uninitialized parameter type end");
    }

    // Delegate.Method answers the override the delegate's target selects, whose attribute
    // the declared method does not carry.
    private static string VirtualCommand(CommandBase target)
    {
        Action handle = target.Handle;
        object[] attributes = handle.Method.GetCustomAttributes(typeof(CommandAttribute), false);
        return attributes.Length == 0 ? "none" : ((CommandAttribute)attributes[0]).Name;
    }

    // Each exact instantiation binds a name known only at run time on its own owner, so
    // the one site's fix preserves every owner.
    private static Delegate BindNamed<T>(string name) where T : struct =>
        Delegate.CreateDelegate(typeof(Func<string>), typeof(T), name);

    // The allocated type arrives as a parameter, and nothing else allocates it, so only
    // an image that allocates it dispatches the override.
    private static string Blank(Type type)
    {
        object blank = RuntimeHelpers.GetUninitializedObject(type);
        return blank.GetType().Name + "/" + blank;
    }

    // SetValue runs the owner's type initializer before it rejects the read-only
    // field, and nothing names the owner's initializer.
    private static string SetInitOnly(Type owner)
    {
        owner.GetField("Value", BindingFlags.Public | BindingFlags.Static)!.SetValue(null, 5);
        return "set";
    }

    // An array type formed from a parameter has an element type as unknown as the parameter.
    private static string RunArrayElement(Type element) =>
        (string)element.MakeArrayType().GetElementType()!.GetMethod("Run")!.Invoke(null, null)!;

    // The owner is known but compiler-generated, so the build-time fix and the refusal's
    // remedy both escape its name.
    private static string GeneratedMember(string name)
    {
        object generated = new { Value = 1 };
        return ((Func<int>)Delegate.CreateDelegate(typeof(Func<int>), generated, name))().ToString();
    }

    // The invoked method's out parameter writes the target type back into the argument array.
    private static string RunWrittenBack()
    {
        object?[] slots = { null };
        typeof(Program).GetMethod(nameof(PickWritten), BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, slots);
        return (string)((Type)slots[0]!).GetMethod("Run")!.Invoke(null, null)!;
    }

    private static void PickWritten(out Type target) => target = typeof(WrittenTarget);

    // Boxing a reference-type argument keeps the object, whose type may derive from the
    // argument; a value-type argument's box has exactly that type.
    private static string RunBoxed<T>(T value) =>
        (string)((object)value!).GetType().GetMethod("Run")!.Invoke(null, null)!;

    // Each construction site takes its type as a parameter, so the image keeps no
    // constructor body for it.
    private static string CreateLibraryClass(Type target) => Activator.CreateInstance(target)!.ToString()!;

    private static string CreateWithCount(Type target) => Activator.CreateInstance(target, 3)!.ToString()!;

    private static string CreateLibraryStruct(Type target) => Activator.CreateInstance(target)!.ToString()!;

    private static string CreateDescribed(Type target) => ((IDescribed)Activator.CreateInstance(target)!).Describe();

    // Each target reaches its reflecting method as a parameter, which a method-local
    // summary cannot resolve.
    private static string Describe(Type target) => (string)target.GetMethod("Describe")!.Invoke(null, null)!;

    private static string Construct(Type target) => Activator.CreateInstance(target)!.ToString()!;

    // The program names no instantiation of the definition, so its runtime-instantiation
    // template serves the type, and the clone's member arrives as a parameter.
    private static string DescribeMade(Type target) => (string)target.GetMethod("Describe")!.Invoke(null, null)!;

    // A summary models no collection object's elements.
    private static string RunElements()
    {
        var targets = new List<Type> { typeof(ElementTarget) };
        string result = "";
        foreach (Type target in targets)
            result += (string)target.GetMethod("Run")!.Invoke(null, null)!;
        return result;
    }

    // A copied list reaches a call that may add to it. Each element refuses on its own.
    private static string RunCopy()
    {
        Type[] originals = { typeof(CopiedTarget) };
        List<Type> targets = originals.ToList();
        targets.Add(typeof(AddedTarget));
        string result = "";
        foreach (Type target in targets)
        {
            try
            {
                result += (string)target.GetMethod("Run")!.Invoke(null, null)! + ";";
            }
            catch (NotSupportedException e)
            {
                Console.WriteLine("copy refused: " + e.GetType().Name + ": " + e.Message);
            }
        }
        return result;
    }
}

[AttributeUsage(AttributeTargets.Method)]
internal sealed class CommandAttribute : Attribute
{
    public CommandAttribute(string name) => Name = name;

    public string Name { get; }
}

internal class CommandBase
{
    public virtual void Handle()
    {
    }
}

internal sealed class CommandDerived : CommandBase
{
    [Command("derived")]
    public override void Handle()
    {
    }
}

internal struct MergedA
{
    public static string Run() => "merged a";
}

internal struct MergedB
{
    public static string Run() => "merged b";
}

internal sealed class BlankTarget
{
    public override string ToString() => "blank";
}

internal static class ElementTarget
{
    public static string Run() => "element";
}

internal static class CopiedTarget
{
    public static string Run() => "copied";
}

internal static class AddedTarget
{
    public static string Run() => "added";
}

internal static class WrittenTarget
{
    public static string Run() => "written";
}

internal sealed class ArrayElementTarget
{
    public static string Run() => "array";
}

internal static class InitOnlyTarget
{
    public static readonly int Value = Initialize();

    // Not beforefieldinit. A retained initializer may run before .NET runs it, so it
    // only records that it ran.
    static InitOnlyTarget()
    {
    }

    private static int Initialize()
    {
        Program.initOnlyInitialized = true;
        return 7;
    }
}

internal class BoxedBase
{
}

internal sealed class BoxedDerived : BoxedBase
{
    public static string Run() => "derived";
}

internal struct BoxedValue
{
    public static string Run() => "value";
}

internal static class InvokedTarget
{
    public static string Describe() => "described";
}

internal class TemplateTarget<T>
{
    public static string Describe() => "template " + typeof(T).Name;
}

internal interface IDescribed
{
    string Describe();
}

// Nothing boxes it, so the image wires none of its interface slots.
internal struct UnboxedTarget : IDescribed
{
    private int _count;

    public string Describe() => "unboxed " + _count++;
}

internal sealed class ConstructedTarget
{
    private readonly string _label;

    public ConstructedTarget() => _label = "constructed";

    public override string ToString() => _label;
}
