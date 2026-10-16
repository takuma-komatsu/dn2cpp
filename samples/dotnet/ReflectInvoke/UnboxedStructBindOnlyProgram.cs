using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

// SUBJECT: a CreateDelegate binding of a method that returns, or writes back
// through a by-ref, a struct this image never boxes. The bound delegate's call
// passes the struct through without a user-visible box, so it runs as .NET's
// does. Nothing here boxes Made or Kept, allocates a delegate type naming one,
// or binds or invokes at a site the summary resolves. Run with "invoke", the
// program also invokes Kept's methods: MethodInfo.Invoke would hand its caller
// a box of Kept, so this image refuses it before the target runs.
namespace UnboxedStructBind;

internal struct Made
{
    public int X;
}

internal struct Kept
{
    public int X;
}

internal delegate void FillMade(ref Made made);

internal static class Targets
{
    internal static int keptCalls;

    public static Made Make() => new Made { X = 42 };

    public static void Fill(ref Made made) => made.X += 7;

    public static Kept Keep()
    {
        keptCalls++;
        return new Kept { X = 5 };
    }

    public static void Refill(ref Kept kept)
    {
        keptCalls++;
        kept.X += 3;
    }
}

internal static class Program
{
    private static int Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Made direct = Targets.Make();
        Targets.Fill(ref direct);
        Console.WriteLine("direct: " + direct.X.ToString(CultureInfo.InvariantCulture));
        Console.WriteLine("== bound calls over a never-boxed struct ==");
        var make = (Func<Made>)Bind(typeof(Func<Made>), Find(typeof(Targets), "Make"));
        Console.WriteLine("bound make: " + make().X.ToString(CultureInfo.InvariantCulture));
        var fill = (FillMade)Bind(typeof(FillMade), Find(typeof(Targets), "Fill"));
        var filled = new Made { X = 1 };
        fill(ref filled);
        Console.WriteLine("bound fill: " + filled.X.ToString(CultureInfo.InvariantCulture));
        Console.WriteLine("bound calls over a never-boxed struct end");
        if (args.Length == 0 || args[0] != "invoke")
            return 0;
        Kept kept = Targets.Keep();
        Targets.Refill(ref kept);
        Console.WriteLine("direct kept: " + kept.X.ToString(CultureInfo.InvariantCulture));
        Console.WriteLine("== invoke over a never-boxed struct ==");
        Targets.keptCalls = 0;
        Invoke(Find(typeof(Targets), "Keep"), null, "invoke keep");
        Invoke(Find(typeof(Targets), "Refill"), new object?[] { null }, "invoke refill");
        Console.WriteLine("invoked targets ran: " + Targets.keptCalls.ToString(CultureInfo.InvariantCulture));
        Console.WriteLine("invoke over a never-boxed struct end");
        return 0;
    }

    private static void Invoke(MethodInfo method, object?[]? arguments, string label)
    {
        try
        {
            method.Invoke(null, arguments);
            Console.WriteLine(label + ": returned");
        }
        catch (PlatformNotSupportedException e)
        {
            Console.WriteLine(label + " refused: " + e.Message);
        }
    }

    // The delegate type, owner and name arrive as parameters, which a method-local
    // summary cannot resolve: the binding selects no target and allocates no
    // delegate type.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Delegate Bind(Type delegateType, MethodInfo method) => Delegate.CreateDelegate(delegateType, method);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static MethodInfo Find(Type owner, string name)
    {
        foreach (MethodInfo method in owner.GetMethods(BindingFlags.Public | BindingFlags.Static))
            if (method.Name == name)
                return method;
        throw new MissingMethodException(owner.FullName, name);
    }
}
