using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

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
            Console.WriteLine("vector64-hardware=" + Vector64.IsHardwareAccelerated);
            Console.WriteLine("vector128-hardware=" + Vector128.IsHardwareAccelerated);
            Console.WriteLine("vector256-hardware=" + Vector256.IsHardwareAccelerated);
            Console.WriteLine("vector512-hardware=" + Vector512.IsHardwareAccelerated);
            return;
        }
        Run<decimal>("decimal");
        Run<Int128>("int128");
        Run<UInt128>("uint128");
        Run<GuardEnum>("enum");
        Run<GuardStruct>("struct");
        Run<GuardReferenceStruct>("reference-struct");
        Console.WriteLine("vector magnitude unsupported public routes passed");
        RunFixed<decimal>("decimal");
        RunFixed<Int128>("int128");
        RunFixed<UInt128>("uint128");
        RunFixed<GuardEnum>("enum");
        RunFixed<GuardStruct>("struct");
        RunFixed<GuardReferenceStruct>("reference-struct");
        Console.WriteLine("fixed vector magnitude unsupported public routes passed");
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

    private static void RunFixed<T>(string name) where T : struct
    {
        for (int op = 0; op < 4; op++)
            for (int route = 0; route < 3; route++)
                for (int fail = 0; fail < 3; fail++)
                {
                    Record64<T>("vector64:" + name, op, route, fail);
                    Record128<T>("vector128:" + name, op, route, fail);
                    Record256<T>("vector256:" + name, op, route, fail);
                    Record512<T>("vector512:" + name, op, route, fail);
                }
    }

    private static void Record64<T>(string name, int op, int route, int fail) where T : struct
    {
        effects = "";
        try
        {
            Invoke64<T>(op, route, fail);
            Console.WriteLine($"{name}|op={op}|route={route}|fail={fail}|returned|effects={effects}");
        }
        catch (Exception error)
        {
            Console.WriteLine($"{name}|op={op}|route={route}|fail={fail}|exception={error.GetType().FullName}|message={error.Message}|effects={effects}");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector64<T> Operand64<T>(bool left, int fail) where T : struct
    {
        effects += left ? "L" : "R";
        if ((left && fail == 1) || (!left && fail == 2))
            throw new InvalidOperationException(left ? "left-producer" : "right-producer");
        return default;
    }

    private static void Invoke64<T>(int op, int route, int fail) where T : struct
    {
        if (route == 0)
        {
            switch (op)
            {
                case 0: _ = Vector64.MaxMagnitude(Operand64<T>(true, fail), Operand64<T>(false, fail)); break;
                case 1: _ = Vector64.MaxMagnitudeNumber(Operand64<T>(true, fail), Operand64<T>(false, fail)); break;
                case 2: _ = Vector64.MinMagnitude(Operand64<T>(true, fail), Operand64<T>(false, fail)); break;
                case 3: _ = Vector64.MinMagnitudeNumber(Operand64<T>(true, fail), Operand64<T>(false, fail)); break;
            }
            return;
        }
        if (route == 1)
        {
            Func<Vector64<T>, Vector64<T>, Vector64<T>> selector;
            switch (op)
            {
                case 0: selector = Vector64.MaxMagnitude<T>; break;
                case 1: selector = Vector64.MaxMagnitudeNumber<T>; break;
                case 2: selector = Vector64.MinMagnitude<T>; break;
                default: selector = Vector64.MinMagnitudeNumber<T>; break;
            }
            _ = selector(Operand64<T>(true, fail), Operand64<T>(false, fail));
            return;
        }
        delegate* managed<Vector64<T>, Vector64<T>, Vector64<T>> pointer;
        switch (op)
        {
            case 0: pointer = &Vector64.MaxMagnitude<T>; break;
            case 1: pointer = &Vector64.MaxMagnitudeNumber<T>; break;
            case 2: pointer = &Vector64.MinMagnitude<T>; break;
            default: pointer = &Vector64.MinMagnitudeNumber<T>; break;
        }
        _ = pointer(Operand64<T>(true, fail), Operand64<T>(false, fail));
    }

    private static void Record128<T>(string name, int op, int route, int fail) where T : struct
    {
        effects = "";
        try
        {
            Invoke128<T>(op, route, fail);
            Console.WriteLine($"{name}|op={op}|route={route}|fail={fail}|returned|effects={effects}");
        }
        catch (Exception error)
        {
            Console.WriteLine($"{name}|op={op}|route={route}|fail={fail}|exception={error.GetType().FullName}|message={error.Message}|effects={effects}");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<T> Operand128<T>(bool left, int fail) where T : struct
    {
        effects += left ? "L" : "R";
        if ((left && fail == 1) || (!left && fail == 2))
            throw new InvalidOperationException(left ? "left-producer" : "right-producer");
        return default;
    }

    private static void Invoke128<T>(int op, int route, int fail) where T : struct
    {
        if (route == 0)
        {
            switch (op)
            {
                case 0: _ = Vector128.MaxMagnitude(Operand128<T>(true, fail), Operand128<T>(false, fail)); break;
                case 1: _ = Vector128.MaxMagnitudeNumber(Operand128<T>(true, fail), Operand128<T>(false, fail)); break;
                case 2: _ = Vector128.MinMagnitude(Operand128<T>(true, fail), Operand128<T>(false, fail)); break;
                case 3: _ = Vector128.MinMagnitudeNumber(Operand128<T>(true, fail), Operand128<T>(false, fail)); break;
            }
            return;
        }
        if (route == 1)
        {
            Func<Vector128<T>, Vector128<T>, Vector128<T>> selector;
            switch (op)
            {
                case 0: selector = Vector128.MaxMagnitude<T>; break;
                case 1: selector = Vector128.MaxMagnitudeNumber<T>; break;
                case 2: selector = Vector128.MinMagnitude<T>; break;
                default: selector = Vector128.MinMagnitudeNumber<T>; break;
            }
            _ = selector(Operand128<T>(true, fail), Operand128<T>(false, fail));
            return;
        }
        delegate* managed<Vector128<T>, Vector128<T>, Vector128<T>> pointer;
        switch (op)
        {
            case 0: pointer = &Vector128.MaxMagnitude<T>; break;
            case 1: pointer = &Vector128.MaxMagnitudeNumber<T>; break;
            case 2: pointer = &Vector128.MinMagnitude<T>; break;
            default: pointer = &Vector128.MinMagnitudeNumber<T>; break;
        }
        _ = pointer(Operand128<T>(true, fail), Operand128<T>(false, fail));
    }

    private static void Record256<T>(string name, int op, int route, int fail) where T : struct
    {
        effects = "";
        try
        {
            Invoke256<T>(op, route, fail);
            Console.WriteLine($"{name}|op={op}|route={route}|fail={fail}|returned|effects={effects}");
        }
        catch (Exception error)
        {
            Console.WriteLine($"{name}|op={op}|route={route}|fail={fail}|exception={error.GetType().FullName}|message={error.Message}|effects={effects}");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector256<T> Operand256<T>(bool left, int fail) where T : struct
    {
        effects += left ? "L" : "R";
        if ((left && fail == 1) || (!left && fail == 2))
            throw new InvalidOperationException(left ? "left-producer" : "right-producer");
        return default;
    }

    private static void Invoke256<T>(int op, int route, int fail) where T : struct
    {
        if (route == 0)
        {
            switch (op)
            {
                case 0: _ = Vector256.MaxMagnitude(Operand256<T>(true, fail), Operand256<T>(false, fail)); break;
                case 1: _ = Vector256.MaxMagnitudeNumber(Operand256<T>(true, fail), Operand256<T>(false, fail)); break;
                case 2: _ = Vector256.MinMagnitude(Operand256<T>(true, fail), Operand256<T>(false, fail)); break;
                case 3: _ = Vector256.MinMagnitudeNumber(Operand256<T>(true, fail), Operand256<T>(false, fail)); break;
            }
            return;
        }
        if (route == 1)
        {
            Func<Vector256<T>, Vector256<T>, Vector256<T>> selector;
            switch (op)
            {
                case 0: selector = Vector256.MaxMagnitude<T>; break;
                case 1: selector = Vector256.MaxMagnitudeNumber<T>; break;
                case 2: selector = Vector256.MinMagnitude<T>; break;
                default: selector = Vector256.MinMagnitudeNumber<T>; break;
            }
            _ = selector(Operand256<T>(true, fail), Operand256<T>(false, fail));
            return;
        }
        delegate* managed<Vector256<T>, Vector256<T>, Vector256<T>> pointer;
        switch (op)
        {
            case 0: pointer = &Vector256.MaxMagnitude<T>; break;
            case 1: pointer = &Vector256.MaxMagnitudeNumber<T>; break;
            case 2: pointer = &Vector256.MinMagnitude<T>; break;
            default: pointer = &Vector256.MinMagnitudeNumber<T>; break;
        }
        _ = pointer(Operand256<T>(true, fail), Operand256<T>(false, fail));
    }

    private static void Record512<T>(string name, int op, int route, int fail) where T : struct
    {
        effects = "";
        try
        {
            Invoke512<T>(op, route, fail);
            Console.WriteLine($"{name}|op={op}|route={route}|fail={fail}|returned|effects={effects}");
        }
        catch (Exception error)
        {
            Console.WriteLine($"{name}|op={op}|route={route}|fail={fail}|exception={error.GetType().FullName}|message={error.Message}|effects={effects}");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector512<T> Operand512<T>(bool left, int fail) where T : struct
    {
        effects += left ? "L" : "R";
        if ((left && fail == 1) || (!left && fail == 2))
            throw new InvalidOperationException(left ? "left-producer" : "right-producer");
        return default;
    }

    private static void Invoke512<T>(int op, int route, int fail) where T : struct
    {
        if (route == 0)
        {
            switch (op)
            {
                case 0: _ = Vector512.MaxMagnitude(Operand512<T>(true, fail), Operand512<T>(false, fail)); break;
                case 1: _ = Vector512.MaxMagnitudeNumber(Operand512<T>(true, fail), Operand512<T>(false, fail)); break;
                case 2: _ = Vector512.MinMagnitude(Operand512<T>(true, fail), Operand512<T>(false, fail)); break;
                case 3: _ = Vector512.MinMagnitudeNumber(Operand512<T>(true, fail), Operand512<T>(false, fail)); break;
            }
            return;
        }
        if (route == 1)
        {
            Func<Vector512<T>, Vector512<T>, Vector512<T>> selector;
            switch (op)
            {
                case 0: selector = Vector512.MaxMagnitude<T>; break;
                case 1: selector = Vector512.MaxMagnitudeNumber<T>; break;
                case 2: selector = Vector512.MinMagnitude<T>; break;
                default: selector = Vector512.MinMagnitudeNumber<T>; break;
            }
            _ = selector(Operand512<T>(true, fail), Operand512<T>(false, fail));
            return;
        }
        delegate* managed<Vector512<T>, Vector512<T>, Vector512<T>> pointer;
        switch (op)
        {
            case 0: pointer = &Vector512.MaxMagnitude<T>; break;
            case 1: pointer = &Vector512.MaxMagnitudeNumber<T>; break;
            case 2: pointer = &Vector512.MinMagnitude<T>; break;
            default: pointer = &Vector512.MinMagnitudeNumber<T>; break;
        }
        _ = pointer(Operand512<T>(true, fail), Operand512<T>(false, fail));
    }

    // The Diet gate proves this unreachable body is rewritten, not merely copied.
    private static int UnreachableDietSentinel() => 546;
}
