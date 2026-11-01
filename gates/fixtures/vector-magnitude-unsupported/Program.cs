using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

internal enum GuardEnum : int { Zero, One }
internal struct GuardStruct { public int Value; public GuardStruct(int value) { Value = value; } }
internal struct GuardReferenceStruct { public string? Value; public GuardReferenceStruct(string? value) { Value = value; } }

internal static unsafe class Program
{
    private static string effects = "";

    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        if (args.Length == 1 && args[0] == "--capabilities")
        {
            Console.WriteLine("numerics-hardware=" + Vector.IsHardwareAccelerated);
            return;
        }
        Run<decimal>("decimal");
        Run<Int128>("int128");
        Run<UInt128>("uint128");
        Run<GuardEnum>("enum");
        Run<GuardStruct>("struct");
        Run<GuardReferenceStruct>("reference-struct");
        Console.WriteLine("vector magnitude unsupported public routes passed");
    }

    private static void Run<T>(string name) where T : struct
    {
        for (int op = 0; op < 4; op++)
            for (int route = 0; route < 3; route++)
                for (int fail = 0; fail < 3; fail++)
                    Record<T>(name, op, route, fail);
    }

    private static void Record<T>(string name, int op, int route, int fail) where T : struct
    {
        effects = "";
        try
        {
            Invoke<T>(op, route, fail);
            Console.WriteLine($"{name}|op={op}|route={route}|fail={fail}|returned|effects={effects}");
        }
        catch (Exception error)
        {
            Console.WriteLine($"{name}|op={op}|route={route}|fail={fail}|exception={error.GetType().FullName}|message={error.Message}|effects={effects}");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<T> Operand<T>(bool left, int fail) where T : struct
    {
        effects += left ? "L" : "R";
        if ((left && fail == 1) || (!left && fail == 2))
            throw new InvalidOperationException(left ? "left-producer" : "right-producer");
        return default;
    }

    private static void Invoke<T>(int op, int route, int fail) where T : struct
    {
        if (route == 0)
        {
            switch (op)
            {
                case 0: _ = Vector.MaxMagnitude(Operand<T>(true, fail), Operand<T>(false, fail)); break;
                case 1: _ = Vector.MaxMagnitudeNumber(Operand<T>(true, fail), Operand<T>(false, fail)); break;
                case 2: _ = Vector.MinMagnitude(Operand<T>(true, fail), Operand<T>(false, fail)); break;
                case 3: _ = Vector.MinMagnitudeNumber(Operand<T>(true, fail), Operand<T>(false, fail)); break;
            }
            return;
        }
        if (route == 1)
        {
            Func<Vector<T>, Vector<T>, Vector<T>> selector;
            switch (op)
            {
                case 0: selector = Vector.MaxMagnitude<T>; break;
                case 1: selector = Vector.MaxMagnitudeNumber<T>; break;
                case 2: selector = Vector.MinMagnitude<T>; break;
                default: selector = Vector.MinMagnitudeNumber<T>; break;
            }
            _ = selector(Operand<T>(true, fail), Operand<T>(false, fail));
            return;
        }
        delegate* managed<Vector<T>, Vector<T>, Vector<T>> pointer;
        switch (op)
        {
            case 0: pointer = &Vector.MaxMagnitude<T>; break;
            case 1: pointer = &Vector.MaxMagnitudeNumber<T>; break;
            case 2: pointer = &Vector.MinMagnitude<T>; break;
            default: pointer = &Vector.MinMagnitudeNumber<T>; break;
        }
        _ = pointer(Operand<T>(true, fail), Operand<T>(false, fail));
    }

    // The Diet gate proves this unreachable body is rewritten, not merely copied.
    private static int UnreachableDietSentinel() => 546;
}
