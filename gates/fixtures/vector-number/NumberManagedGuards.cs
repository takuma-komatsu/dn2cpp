using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

internal enum NumberGuardEnum : int { Zero, One }
internal struct NumberGuardStruct { public int Value; public NumberGuardStruct(int value) { Value = value; } }
internal struct NumberGuardReferenceStruct { public string? Value; public NumberGuardReferenceStruct(string? value) { Value = value; } }

internal static unsafe class NumberManagedGuards
{
    private static string effects = "";

    internal static void Capabilities()
    {
        Console.WriteLine("numerics-hardware=" + Vector.IsHardwareAccelerated);
        Console.WriteLine("vector64-hardware=" + Vector64.IsHardwareAccelerated);
        Console.WriteLine("vector128-hardware=" + Vector128.IsHardwareAccelerated);
        Console.WriteLine("vector256-hardware=" + Vector256.IsHardwareAccelerated);
        Console.WriteLine("vector512-hardware=" + Vector512.IsHardwareAccelerated);
    }

    internal static void Run()
    {
        Console.WriteLine("vector-number:managed-guards:start");
        Run<decimal>("decimal");
        Run<NumberGuardEnum>("enum");
        Run<NumberGuardStruct>("struct");
        Run<NumberGuardReferenceStruct>("reference-struct");
        Run<Int128>("int128");
        Run<UInt128>("uint128");
        Console.WriteLine("vector-number:managed-guards-passed");
    }

    private static void Run<T>(string name) where T : struct
    {
        Run64<T>("vector64:" + name);
        Run128<T>("vector128:" + name);
        Run256<T>("vector256:" + name);
        Run512<T>("vector512:" + name);
        RunNumerics<T>("numerics:" + name);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector64<T> Operand64<T>(bool left, int fail) where T : struct
    {
        effects += left ? "L" : "R";
        if ((left && fail == 1) || (!left && fail == 2))
            throw new InvalidOperationException(left ? "left-producer" : "right-producer");
        return default;
    }

    private static void Run64<T>(string name) where T : struct
    {
        for (int op = 0; op < 2; op++)
        {
            string operation = op == 0 ? "MaxNumber" : "MinNumber";
            effects = "";
            Func<Vector64<T>, Vector64<T>, Vector64<T>> selector = op == 0 ? Vector64.MaxNumber<T> : Vector64.MinNumber<T>;
            if (selector is null || effects != "") throw new InvalidOperationException("Number delegate acquisition");
            Console.WriteLine($"guard-acquired:{name}|op={operation}|route=delegate|effects={effects}");
            delegate* managed<Vector64<T>, Vector64<T>, Vector64<T>> pointer = op == 0 ? &Vector64.MaxNumber<T> : &Vector64.MinNumber<T>;
            if (pointer == null || effects != "") throw new InvalidOperationException("Number function address acquisition");
            Console.WriteLine($"guard-acquired:{name}|op={operation}|route=calli|effects={effects}");
            // Literal names retain only the exact private forwarding bodies.
            var method = op == 0
                ? typeof(Vector64<T>).GetMethod("System.Runtime.Intrinsics.ISimdVector<System.Runtime.Intrinsics.Vector64<T>,T>.MaxNumber", BindingFlags.Static | BindingFlags.NonPublic)
                : typeof(Vector64<T>).GetMethod("System.Runtime.Intrinsics.ISimdVector<System.Runtime.Intrinsics.Vector64<T>,T>.MinNumber", BindingFlags.Static | BindingFlags.NonPublic);
            if (method is null) throw new InvalidOperationException("missing private Number bridge");
            var retained = method.CreateDelegate<Func<Vector64<T>, Vector64<T>, Vector64<T>>>();
            if (retained is null || effects != "") throw new InvalidOperationException("Number retained delegate acquisition");
            Console.WriteLine($"guard-acquired:{name}|op={operation}|route=retained|effects={effects}");
            for (int route = 0; route < 4; route++)
            for (int fail = 0; fail < 3; fail++)
            {
                effects = "";
                try
                {
                    if (route == 0)
                    {
                        if (op == 0) _ = Vector64.MaxNumber(Operand64<T>(true, fail), Operand64<T>(false, fail));
                        else _ = Vector64.MinNumber(Operand64<T>(true, fail), Operand64<T>(false, fail));
                    }
                    else if (route == 1) _ = selector(Operand64<T>(true, fail), Operand64<T>(false, fail));
                    else if (route == 2) _ = pointer(Operand64<T>(true, fail), Operand64<T>(false, fail));
                    else _ = retained(Operand64<T>(true, fail), Operand64<T>(false, fail));
                    Console.WriteLine($"guard:{name}|op={operation}|route={route}|fail={fail}|returned|effects={effects}");
                }
                catch (Exception error)
                {
                    Console.WriteLine($"guard:{name}|op={operation}|route={route}|fail={fail}|exception={error.GetType().FullName}|message={error.Message}|effects={effects}");
                }
            }
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

    private static void Run128<T>(string name) where T : struct
    {
        for (int op = 0; op < 2; op++)
        {
            string operation = op == 0 ? "MaxNumber" : "MinNumber";
            effects = "";
            Func<Vector128<T>, Vector128<T>, Vector128<T>> selector = op == 0 ? Vector128.MaxNumber<T> : Vector128.MinNumber<T>;
            if (selector is null || effects != "") throw new InvalidOperationException("Number delegate acquisition");
            Console.WriteLine($"guard-acquired:{name}|op={operation}|route=delegate|effects={effects}");
            delegate* managed<Vector128<T>, Vector128<T>, Vector128<T>> pointer = op == 0 ? &Vector128.MaxNumber<T> : &Vector128.MinNumber<T>;
            if (pointer == null || effects != "") throw new InvalidOperationException("Number function address acquisition");
            Console.WriteLine($"guard-acquired:{name}|op={operation}|route=calli|effects={effects}");
            // Literal names retain only the exact private forwarding bodies.
            var method = op == 0
                ? typeof(Vector128<T>).GetMethod("System.Runtime.Intrinsics.ISimdVector<System.Runtime.Intrinsics.Vector128<T>,T>.MaxNumber", BindingFlags.Static | BindingFlags.NonPublic)
                : typeof(Vector128<T>).GetMethod("System.Runtime.Intrinsics.ISimdVector<System.Runtime.Intrinsics.Vector128<T>,T>.MinNumber", BindingFlags.Static | BindingFlags.NonPublic);
            if (method is null) throw new InvalidOperationException("missing private Number bridge");
            var retained = method.CreateDelegate<Func<Vector128<T>, Vector128<T>, Vector128<T>>>();
            if (retained is null || effects != "") throw new InvalidOperationException("Number retained delegate acquisition");
            Console.WriteLine($"guard-acquired:{name}|op={operation}|route=retained|effects={effects}");
            for (int route = 0; route < 4; route++)
            for (int fail = 0; fail < 3; fail++)
            {
                effects = "";
                try
                {
                    if (route == 0)
                    {
                        if (op == 0) _ = Vector128.MaxNumber(Operand128<T>(true, fail), Operand128<T>(false, fail));
                        else _ = Vector128.MinNumber(Operand128<T>(true, fail), Operand128<T>(false, fail));
                    }
                    else if (route == 1) _ = selector(Operand128<T>(true, fail), Operand128<T>(false, fail));
                    else if (route == 2) _ = pointer(Operand128<T>(true, fail), Operand128<T>(false, fail));
                    else _ = retained(Operand128<T>(true, fail), Operand128<T>(false, fail));
                    Console.WriteLine($"guard:{name}|op={operation}|route={route}|fail={fail}|returned|effects={effects}");
                }
                catch (Exception error)
                {
                    Console.WriteLine($"guard:{name}|op={operation}|route={route}|fail={fail}|exception={error.GetType().FullName}|message={error.Message}|effects={effects}");
                }
            }
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

    private static void Run256<T>(string name) where T : struct
    {
        for (int op = 0; op < 2; op++)
        {
            string operation = op == 0 ? "MaxNumber" : "MinNumber";
            effects = "";
            Func<Vector256<T>, Vector256<T>, Vector256<T>> selector = op == 0 ? Vector256.MaxNumber<T> : Vector256.MinNumber<T>;
            if (selector is null || effects != "") throw new InvalidOperationException("Number delegate acquisition");
            Console.WriteLine($"guard-acquired:{name}|op={operation}|route=delegate|effects={effects}");
            delegate* managed<Vector256<T>, Vector256<T>, Vector256<T>> pointer = op == 0 ? &Vector256.MaxNumber<T> : &Vector256.MinNumber<T>;
            if (pointer == null || effects != "") throw new InvalidOperationException("Number function address acquisition");
            Console.WriteLine($"guard-acquired:{name}|op={operation}|route=calli|effects={effects}");
            // Literal names retain only the exact private forwarding bodies.
            var method = op == 0
                ? typeof(Vector256<T>).GetMethod("System.Runtime.Intrinsics.ISimdVector<System.Runtime.Intrinsics.Vector256<T>,T>.MaxNumber", BindingFlags.Static | BindingFlags.NonPublic)
                : typeof(Vector256<T>).GetMethod("System.Runtime.Intrinsics.ISimdVector<System.Runtime.Intrinsics.Vector256<T>,T>.MinNumber", BindingFlags.Static | BindingFlags.NonPublic);
            if (method is null) throw new InvalidOperationException("missing private Number bridge");
            var retained = method.CreateDelegate<Func<Vector256<T>, Vector256<T>, Vector256<T>>>();
            if (retained is null || effects != "") throw new InvalidOperationException("Number retained delegate acquisition");
            Console.WriteLine($"guard-acquired:{name}|op={operation}|route=retained|effects={effects}");
            for (int route = 0; route < 4; route++)
            for (int fail = 0; fail < 3; fail++)
            {
                effects = "";
                try
                {
                    if (route == 0)
                    {
                        if (op == 0) _ = Vector256.MaxNumber(Operand256<T>(true, fail), Operand256<T>(false, fail));
                        else _ = Vector256.MinNumber(Operand256<T>(true, fail), Operand256<T>(false, fail));
                    }
                    else if (route == 1) _ = selector(Operand256<T>(true, fail), Operand256<T>(false, fail));
                    else if (route == 2) _ = pointer(Operand256<T>(true, fail), Operand256<T>(false, fail));
                    else _ = retained(Operand256<T>(true, fail), Operand256<T>(false, fail));
                    Console.WriteLine($"guard:{name}|op={operation}|route={route}|fail={fail}|returned|effects={effects}");
                }
                catch (Exception error)
                {
                    Console.WriteLine($"guard:{name}|op={operation}|route={route}|fail={fail}|exception={error.GetType().FullName}|message={error.Message}|effects={effects}");
                }
            }
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

    private static void Run512<T>(string name) where T : struct
    {
        for (int op = 0; op < 2; op++)
        {
            string operation = op == 0 ? "MaxNumber" : "MinNumber";
            effects = "";
            Func<Vector512<T>, Vector512<T>, Vector512<T>> selector = op == 0 ? Vector512.MaxNumber<T> : Vector512.MinNumber<T>;
            if (selector is null || effects != "") throw new InvalidOperationException("Number delegate acquisition");
            Console.WriteLine($"guard-acquired:{name}|op={operation}|route=delegate|effects={effects}");
            delegate* managed<Vector512<T>, Vector512<T>, Vector512<T>> pointer = op == 0 ? &Vector512.MaxNumber<T> : &Vector512.MinNumber<T>;
            if (pointer == null || effects != "") throw new InvalidOperationException("Number function address acquisition");
            Console.WriteLine($"guard-acquired:{name}|op={operation}|route=calli|effects={effects}");
            // Literal names retain only the exact private forwarding bodies.
            var method = op == 0
                ? typeof(Vector512<T>).GetMethod("System.Runtime.Intrinsics.ISimdVector<System.Runtime.Intrinsics.Vector512<T>,T>.MaxNumber", BindingFlags.Static | BindingFlags.NonPublic)
                : typeof(Vector512<T>).GetMethod("System.Runtime.Intrinsics.ISimdVector<System.Runtime.Intrinsics.Vector512<T>,T>.MinNumber", BindingFlags.Static | BindingFlags.NonPublic);
            if (method is null) throw new InvalidOperationException("missing private Number bridge");
            var retained = method.CreateDelegate<Func<Vector512<T>, Vector512<T>, Vector512<T>>>();
            if (retained is null || effects != "") throw new InvalidOperationException("Number retained delegate acquisition");
            Console.WriteLine($"guard-acquired:{name}|op={operation}|route=retained|effects={effects}");
            for (int route = 0; route < 4; route++)
            for (int fail = 0; fail < 3; fail++)
            {
                effects = "";
                try
                {
                    if (route == 0)
                    {
                        if (op == 0) _ = Vector512.MaxNumber(Operand512<T>(true, fail), Operand512<T>(false, fail));
                        else _ = Vector512.MinNumber(Operand512<T>(true, fail), Operand512<T>(false, fail));
                    }
                    else if (route == 1) _ = selector(Operand512<T>(true, fail), Operand512<T>(false, fail));
                    else if (route == 2) _ = pointer(Operand512<T>(true, fail), Operand512<T>(false, fail));
                    else _ = retained(Operand512<T>(true, fail), Operand512<T>(false, fail));
                    Console.WriteLine($"guard:{name}|op={operation}|route={route}|fail={fail}|returned|effects={effects}");
                }
                catch (Exception error)
                {
                    Console.WriteLine($"guard:{name}|op={operation}|route={route}|fail={fail}|exception={error.GetType().FullName}|message={error.Message}|effects={effects}");
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<T> OperandNumerics<T>(bool left, int fail) where T : struct
    {
        effects += left ? "L" : "R";
        if ((left && fail == 1) || (!left && fail == 2))
            throw new InvalidOperationException(left ? "left-producer" : "right-producer");
        return default;
    }

    private static void RunNumerics<T>(string name) where T : struct
    {
        for (int op = 0; op < 2; op++)
        {
            string operation = op == 0 ? "MaxNumber" : "MinNumber";
            effects = "";
            Func<Vector<T>, Vector<T>, Vector<T>> selector = op == 0 ? Vector.MaxNumber<T> : Vector.MinNumber<T>;
            if (selector is null || effects != "") throw new InvalidOperationException("Number delegate acquisition");
            Console.WriteLine($"guard-acquired:{name}|op={operation}|route=delegate|effects={effects}");
            delegate* managed<Vector<T>, Vector<T>, Vector<T>> pointer = op == 0 ? &Vector.MaxNumber<T> : &Vector.MinNumber<T>;
            if (pointer == null || effects != "") throw new InvalidOperationException("Number function address acquisition");
            Console.WriteLine($"guard-acquired:{name}|op={operation}|route=calli|effects={effects}");
            // Literal names retain only the exact private forwarding bodies.
            var method = op == 0
                ? typeof(Vector<T>).GetMethod("System.Runtime.Intrinsics.ISimdVector<System.Numerics.Vector<T>,T>.MaxNumber", BindingFlags.Static | BindingFlags.NonPublic)
                : typeof(Vector<T>).GetMethod("System.Runtime.Intrinsics.ISimdVector<System.Numerics.Vector<T>,T>.MinNumber", BindingFlags.Static | BindingFlags.NonPublic);
            if (method is null) throw new InvalidOperationException("missing private Number bridge");
            var retained = method.CreateDelegate<Func<Vector<T>, Vector<T>, Vector<T>>>();
            if (retained is null || effects != "") throw new InvalidOperationException("Number retained delegate acquisition");
            Console.WriteLine($"guard-acquired:{name}|op={operation}|route=retained|effects={effects}");
            for (int route = 0; route < 4; route++)
            for (int fail = 0; fail < 3; fail++)
            {
                effects = "";
                try
                {
                    if (route == 0)
                    {
                        if (op == 0) _ = Vector.MaxNumber(OperandNumerics<T>(true, fail), OperandNumerics<T>(false, fail));
                        else _ = Vector.MinNumber(OperandNumerics<T>(true, fail), OperandNumerics<T>(false, fail));
                    }
                    else if (route == 1) _ = selector(OperandNumerics<T>(true, fail), OperandNumerics<T>(false, fail));
                    else if (route == 2) _ = pointer(OperandNumerics<T>(true, fail), OperandNumerics<T>(false, fail));
                    else _ = retained(OperandNumerics<T>(true, fail), OperandNumerics<T>(false, fail));
                    Console.WriteLine($"guard:{name}|op={operation}|route={route}|fail={fail}|returned|effects={effects}");
                }
                catch (Exception error)
                {
                    Console.WriteLine($"guard:{name}|op={operation}|route={route}|fail={fail}|exception={error.GetType().FullName}|message={error.Message}|effects={effects}");
                }
            }
        }
    }
}
