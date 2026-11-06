using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

internal static class Program
{
    private static int Order, ThrowAt;
    private static void Check(bool ok) { if (!ok) throw new InvalidOperationException("vector magnitude mismatch"); }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Effect(int step)
    {
        Order = Order * 10 + step;
        if (ThrowAt == step) throw new InvalidOperationException("magnitude operand");
    }
    private static string Bits(float value) => BitConverter.SingleToInt32Bits(value).ToString("X8");
    private static string Bits(double value) => BitConverter.DoubleToInt64Bits(value).ToString("X16");
    // An unreachable body proves the Diet axis rewrites the application.
    private static int DietRewriteSentinel() => 545246;
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        uint[] fb = { 0x00000000U, 0x80000000U, 0x3F800000U, 0xBF800000U, 0x3F800001U, 0xBF800001U, 0x40000000U, 0xC0000000U, 0x00000001U, 0x80000001U, 0x007FFFFFU, 0x807FFFFFU, 0x00800000U, 0x80800000U, 0x7F7FFFFFU, 0xFF7FFFFFU, 0x7F800000U, 0xFF800000U, 0x7FC12345U, 0xFFC54321U, 0x7FC54321U, 0xFFC12345U, 0x7F812345U, 0xFF854321U };
        ulong[] db = { 0x0000000000000000UL, 0x8000000000000000UL, 0x3FF0000000000000UL, 0xBFF0000000000000UL, 0x3FF0000000000001UL, 0xBFF0000000000001UL, 0x4000000000000000UL, 0xC000000000000000UL, 0x0000000000000001UL, 0x8000000000000001UL, 0x000FFFFFFFFFFFFFUL, 0x800FFFFFFFFFFFFFUL, 0x0010000000000000UL, 0x8010000000000000UL, 0x7FEFFFFFFFFFFFFFUL, 0xFFEFFFFFFFFFFFFFUL, 0x7FF0000000000000UL, 0xFFF0000000000000UL, 0x7FF8123456789ABCUL, 0xFFF8543212345678UL, 0x7FF8543212345678UL, 0xFFF8123456789ABCUL, 0x7FF0123456789ABCUL, 0xFFF0543212345678UL };
        var fa = new float[fb.Length]; var da = new double[db.Length];
        for (int i = 0; i < fb.Length; i++) fa[i] = BitConverter.Int32BitsToSingle(unchecked((int)fb[i]));
        for (int i = 0; i < db.Length; i++) da[i] = BitConverter.Int64BitsToDouble(unchecked((long)db[i]));
        TestMaxMagnitude64f(fa);
        TestMaxMagnitude64d(da);
        TestMaxMagnitude128f(fa);
        TestMaxMagnitude128d(da);
        TestMaxMagnitude256f(fa);
        TestMaxMagnitude256d(da);
        TestMaxMagnitude512f(fa);
        TestMaxMagnitude512d(da);
        TestMaxMagnitudenf(fa);
        TestMaxMagnitudend(da);
        TestMaxMagnitudec2(fa);
        TestMaxMagnitudec3(fa);
        TestMaxMagnitudec4(fa);
        TestMinMagnitude64f(fa);
        TestMinMagnitude64d(da);
        TestMinMagnitude128f(fa);
        TestMinMagnitude128d(da);
        TestMinMagnitude256f(fa);
        TestMinMagnitude256d(da);
        TestMinMagnitude512f(fa);
        TestMinMagnitude512d(da);
        TestMinMagnitudenf(fa);
        TestMinMagnitudend(da);
        TestMinMagnitudec2(fa);
        TestMinMagnitudec3(fa);
        TestMinMagnitudec4(fa);
        TestMaxMagnitudeNumber64f(fa);
        TestMaxMagnitudeNumber64d(da);
        TestMaxMagnitudeNumber128f(fa);
        TestMaxMagnitudeNumber128d(da);
        TestMaxMagnitudeNumber256f(fa);
        TestMaxMagnitudeNumber256d(da);
        TestMaxMagnitudeNumber512f(fa);
        TestMaxMagnitudeNumber512d(da);
        TestMaxMagnitudeNumbernf(fa);
        TestMaxMagnitudeNumbernd(da);
        TestMaxMagnitudeNumberc2(fa);
        TestMaxMagnitudeNumberc3(fa);
        TestMaxMagnitudeNumberc4(fa);
        TestMinMagnitudeNumber64f(fa);
        TestMinMagnitudeNumber64d(da);
        TestMinMagnitudeNumber128f(fa);
        TestMinMagnitudeNumber128d(da);
        TestMinMagnitudeNumber256f(fa);
        TestMinMagnitudeNumber256d(da);
        TestMinMagnitudeNumber512f(fa);
        TestMinMagnitudeNumber512d(da);
        TestMinMagnitudeNumbernf(fa);
        TestMinMagnitudeNumbernd(da);
        TestMinMagnitudeNumberc2(fa);
        TestMinMagnitudeNumberc3(fa);
        TestMinMagnitudeNumberc4(fa);
        Console.WriteLine("vector-magnitude:all-routes-passed");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector64<float> Operand64f(int step, Vector64<float> value) { Effect(step); return value; }
    private static unsafe void TestMaxMagnitude64f(float[] values)
    {
        Func<Vector64<float>, Vector64<float>, Vector64<float>> callback = Vector64.MaxMagnitude<float>;
        delegate* managed<Vector64<float>, Vector64<float>, Vector64<float>> pointer = &Vector64.MaxMagnitude<float>;
        Console.WriteLine("width:MaxMagnitude64f:" + Vector64<float>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[Vector64<float>.Count]; var bb = new float[Vector64<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector64.Create(aa); var bv = Vector64.Create(bb);
            var actual = Vector64.MaxMagnitude<float>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MaxMagnitude64f:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector64<float> value;
                if (route == 0) value = Vector64.MaxMagnitude<float>(Operand64f(1, Vector64.Create(values[2])), Operand64f(2, Vector64.Create(values[6])));
                else if (route == 1) value = callback(Operand64f(1, Vector64.Create(values[2])), Operand64f(2, Vector64.Create(values[6])));
                else value = pointer(Operand64f(1, Vector64.Create(values[2])), Operand64f(2, Vector64.Create(values[6])));
                Check(value.GetElement(0) == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxMagnitude64f:direct-delegate-calli");
    }
    private static unsafe void TestMinMagnitude64f(float[] values)
    {
        Func<Vector64<float>, Vector64<float>, Vector64<float>> callback = Vector64.MinMagnitude<float>;
        delegate* managed<Vector64<float>, Vector64<float>, Vector64<float>> pointer = &Vector64.MinMagnitude<float>;
        Console.WriteLine("width:MinMagnitude64f:" + Vector64<float>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[Vector64<float>.Count]; var bb = new float[Vector64<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector64.Create(aa); var bv = Vector64.Create(bb);
            var actual = Vector64.MinMagnitude<float>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MinMagnitude64f:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector64<float> value;
                if (route == 0) value = Vector64.MinMagnitude<float>(Operand64f(1, Vector64.Create(values[2])), Operand64f(2, Vector64.Create(values[6])));
                else if (route == 1) value = callback(Operand64f(1, Vector64.Create(values[2])), Operand64f(2, Vector64.Create(values[6])));
                else value = pointer(Operand64f(1, Vector64.Create(values[2])), Operand64f(2, Vector64.Create(values[6])));
                Check(value.GetElement(0) == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinMagnitude64f:direct-delegate-calli");
    }
    private static unsafe void TestMaxMagnitudeNumber64f(float[] values)
    {
        Func<Vector64<float>, Vector64<float>, Vector64<float>> callback = Vector64.MaxMagnitudeNumber<float>;
        delegate* managed<Vector64<float>, Vector64<float>, Vector64<float>> pointer = &Vector64.MaxMagnitudeNumber<float>;
        Console.WriteLine("width:MaxMagnitudeNumber64f:" + Vector64<float>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[Vector64<float>.Count]; var bb = new float[Vector64<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector64.Create(aa); var bv = Vector64.Create(bb);
            var actual = Vector64.MaxMagnitudeNumber<float>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MaxMagnitudeNumber64f:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector64<float> value;
                if (route == 0) value = Vector64.MaxMagnitudeNumber<float>(Operand64f(1, Vector64.Create(values[2])), Operand64f(2, Vector64.Create(values[6])));
                else if (route == 1) value = callback(Operand64f(1, Vector64.Create(values[2])), Operand64f(2, Vector64.Create(values[6])));
                else value = pointer(Operand64f(1, Vector64.Create(values[2])), Operand64f(2, Vector64.Create(values[6])));
                Check(value.GetElement(0) == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxMagnitudeNumber64f:direct-delegate-calli");
    }
    private static unsafe void TestMinMagnitudeNumber64f(float[] values)
    {
        Func<Vector64<float>, Vector64<float>, Vector64<float>> callback = Vector64.MinMagnitudeNumber<float>;
        delegate* managed<Vector64<float>, Vector64<float>, Vector64<float>> pointer = &Vector64.MinMagnitudeNumber<float>;
        Console.WriteLine("width:MinMagnitudeNumber64f:" + Vector64<float>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[Vector64<float>.Count]; var bb = new float[Vector64<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector64.Create(aa); var bv = Vector64.Create(bb);
            var actual = Vector64.MinMagnitudeNumber<float>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MinMagnitudeNumber64f:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector64<float> value;
                if (route == 0) value = Vector64.MinMagnitudeNumber<float>(Operand64f(1, Vector64.Create(values[2])), Operand64f(2, Vector64.Create(values[6])));
                else if (route == 1) value = callback(Operand64f(1, Vector64.Create(values[2])), Operand64f(2, Vector64.Create(values[6])));
                else value = pointer(Operand64f(1, Vector64.Create(values[2])), Operand64f(2, Vector64.Create(values[6])));
                Check(value.GetElement(0) == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinMagnitudeNumber64f:direct-delegate-calli");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector64<double> Operand64d(int step, Vector64<double> value) { Effect(step); return value; }
    private static unsafe void TestMaxMagnitude64d(double[] values)
    {
        Func<Vector64<double>, Vector64<double>, Vector64<double>> callback = Vector64.MaxMagnitude<double>;
        delegate* managed<Vector64<double>, Vector64<double>, Vector64<double>> pointer = &Vector64.MaxMagnitude<double>;
        Console.WriteLine("width:MaxMagnitude64d:" + Vector64<double>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new double[Vector64<double>.Count]; var bb = new double[Vector64<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector64.Create(aa); var bv = Vector64.Create(bb);
            var actual = Vector64.MaxMagnitude<double>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MaxMagnitude64d:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector64<double> value;
                if (route == 0) value = Vector64.MaxMagnitude<double>(Operand64d(1, Vector64.Create(values[2])), Operand64d(2, Vector64.Create(values[6])));
                else if (route == 1) value = callback(Operand64d(1, Vector64.Create(values[2])), Operand64d(2, Vector64.Create(values[6])));
                else value = pointer(Operand64d(1, Vector64.Create(values[2])), Operand64d(2, Vector64.Create(values[6])));
                Check(value.GetElement(0) == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxMagnitude64d:direct-delegate-calli");
    }
    private static unsafe void TestMinMagnitude64d(double[] values)
    {
        Func<Vector64<double>, Vector64<double>, Vector64<double>> callback = Vector64.MinMagnitude<double>;
        delegate* managed<Vector64<double>, Vector64<double>, Vector64<double>> pointer = &Vector64.MinMagnitude<double>;
        Console.WriteLine("width:MinMagnitude64d:" + Vector64<double>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new double[Vector64<double>.Count]; var bb = new double[Vector64<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector64.Create(aa); var bv = Vector64.Create(bb);
            var actual = Vector64.MinMagnitude<double>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MinMagnitude64d:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector64<double> value;
                if (route == 0) value = Vector64.MinMagnitude<double>(Operand64d(1, Vector64.Create(values[2])), Operand64d(2, Vector64.Create(values[6])));
                else if (route == 1) value = callback(Operand64d(1, Vector64.Create(values[2])), Operand64d(2, Vector64.Create(values[6])));
                else value = pointer(Operand64d(1, Vector64.Create(values[2])), Operand64d(2, Vector64.Create(values[6])));
                Check(value.GetElement(0) == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinMagnitude64d:direct-delegate-calli");
    }
    private static unsafe void TestMaxMagnitudeNumber64d(double[] values)
    {
        Func<Vector64<double>, Vector64<double>, Vector64<double>> callback = Vector64.MaxMagnitudeNumber<double>;
        delegate* managed<Vector64<double>, Vector64<double>, Vector64<double>> pointer = &Vector64.MaxMagnitudeNumber<double>;
        Console.WriteLine("width:MaxMagnitudeNumber64d:" + Vector64<double>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new double[Vector64<double>.Count]; var bb = new double[Vector64<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector64.Create(aa); var bv = Vector64.Create(bb);
            var actual = Vector64.MaxMagnitudeNumber<double>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MaxMagnitudeNumber64d:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector64<double> value;
                if (route == 0) value = Vector64.MaxMagnitudeNumber<double>(Operand64d(1, Vector64.Create(values[2])), Operand64d(2, Vector64.Create(values[6])));
                else if (route == 1) value = callback(Operand64d(1, Vector64.Create(values[2])), Operand64d(2, Vector64.Create(values[6])));
                else value = pointer(Operand64d(1, Vector64.Create(values[2])), Operand64d(2, Vector64.Create(values[6])));
                Check(value.GetElement(0) == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxMagnitudeNumber64d:direct-delegate-calli");
    }
    private static unsafe void TestMinMagnitudeNumber64d(double[] values)
    {
        Func<Vector64<double>, Vector64<double>, Vector64<double>> callback = Vector64.MinMagnitudeNumber<double>;
        delegate* managed<Vector64<double>, Vector64<double>, Vector64<double>> pointer = &Vector64.MinMagnitudeNumber<double>;
        Console.WriteLine("width:MinMagnitudeNumber64d:" + Vector64<double>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new double[Vector64<double>.Count]; var bb = new double[Vector64<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector64.Create(aa); var bv = Vector64.Create(bb);
            var actual = Vector64.MinMagnitudeNumber<double>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MinMagnitudeNumber64d:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector64<double> value;
                if (route == 0) value = Vector64.MinMagnitudeNumber<double>(Operand64d(1, Vector64.Create(values[2])), Operand64d(2, Vector64.Create(values[6])));
                else if (route == 1) value = callback(Operand64d(1, Vector64.Create(values[2])), Operand64d(2, Vector64.Create(values[6])));
                else value = pointer(Operand64d(1, Vector64.Create(values[2])), Operand64d(2, Vector64.Create(values[6])));
                Check(value.GetElement(0) == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinMagnitudeNumber64d:direct-delegate-calli");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<float> Operand128f(int step, Vector128<float> value) { Effect(step); return value; }
    private static unsafe void TestMaxMagnitude128f(float[] values)
    {
        Func<Vector128<float>, Vector128<float>, Vector128<float>> callback = Vector128.MaxMagnitude<float>;
        delegate* managed<Vector128<float>, Vector128<float>, Vector128<float>> pointer = &Vector128.MaxMagnitude<float>;
        Console.WriteLine("width:MaxMagnitude128f:" + Vector128<float>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[Vector128<float>.Count]; var bb = new float[Vector128<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector128.Create(aa); var bv = Vector128.Create(bb);
            var actual = Vector128.MaxMagnitude<float>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MaxMagnitude128f:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector128<float> value;
                if (route == 0) value = Vector128.MaxMagnitude<float>(Operand128f(1, Vector128.Create(values[2])), Operand128f(2, Vector128.Create(values[6])));
                else if (route == 1) value = callback(Operand128f(1, Vector128.Create(values[2])), Operand128f(2, Vector128.Create(values[6])));
                else value = pointer(Operand128f(1, Vector128.Create(values[2])), Operand128f(2, Vector128.Create(values[6])));
                Check(value.GetElement(0) == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxMagnitude128f:direct-delegate-calli");
    }
    private static unsafe void TestMinMagnitude128f(float[] values)
    {
        Func<Vector128<float>, Vector128<float>, Vector128<float>> callback = Vector128.MinMagnitude<float>;
        delegate* managed<Vector128<float>, Vector128<float>, Vector128<float>> pointer = &Vector128.MinMagnitude<float>;
        Console.WriteLine("width:MinMagnitude128f:" + Vector128<float>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[Vector128<float>.Count]; var bb = new float[Vector128<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector128.Create(aa); var bv = Vector128.Create(bb);
            var actual = Vector128.MinMagnitude<float>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MinMagnitude128f:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector128<float> value;
                if (route == 0) value = Vector128.MinMagnitude<float>(Operand128f(1, Vector128.Create(values[2])), Operand128f(2, Vector128.Create(values[6])));
                else if (route == 1) value = callback(Operand128f(1, Vector128.Create(values[2])), Operand128f(2, Vector128.Create(values[6])));
                else value = pointer(Operand128f(1, Vector128.Create(values[2])), Operand128f(2, Vector128.Create(values[6])));
                Check(value.GetElement(0) == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinMagnitude128f:direct-delegate-calli");
    }
    private static unsafe void TestMaxMagnitudeNumber128f(float[] values)
    {
        Func<Vector128<float>, Vector128<float>, Vector128<float>> callback = Vector128.MaxMagnitudeNumber<float>;
        delegate* managed<Vector128<float>, Vector128<float>, Vector128<float>> pointer = &Vector128.MaxMagnitudeNumber<float>;
        Console.WriteLine("width:MaxMagnitudeNumber128f:" + Vector128<float>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[Vector128<float>.Count]; var bb = new float[Vector128<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector128.Create(aa); var bv = Vector128.Create(bb);
            var actual = Vector128.MaxMagnitudeNumber<float>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MaxMagnitudeNumber128f:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector128<float> value;
                if (route == 0) value = Vector128.MaxMagnitudeNumber<float>(Operand128f(1, Vector128.Create(values[2])), Operand128f(2, Vector128.Create(values[6])));
                else if (route == 1) value = callback(Operand128f(1, Vector128.Create(values[2])), Operand128f(2, Vector128.Create(values[6])));
                else value = pointer(Operand128f(1, Vector128.Create(values[2])), Operand128f(2, Vector128.Create(values[6])));
                Check(value.GetElement(0) == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxMagnitudeNumber128f:direct-delegate-calli");
    }
    private static unsafe void TestMinMagnitudeNumber128f(float[] values)
    {
        Func<Vector128<float>, Vector128<float>, Vector128<float>> callback = Vector128.MinMagnitudeNumber<float>;
        delegate* managed<Vector128<float>, Vector128<float>, Vector128<float>> pointer = &Vector128.MinMagnitudeNumber<float>;
        Console.WriteLine("width:MinMagnitudeNumber128f:" + Vector128<float>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[Vector128<float>.Count]; var bb = new float[Vector128<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector128.Create(aa); var bv = Vector128.Create(bb);
            var actual = Vector128.MinMagnitudeNumber<float>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MinMagnitudeNumber128f:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector128<float> value;
                if (route == 0) value = Vector128.MinMagnitudeNumber<float>(Operand128f(1, Vector128.Create(values[2])), Operand128f(2, Vector128.Create(values[6])));
                else if (route == 1) value = callback(Operand128f(1, Vector128.Create(values[2])), Operand128f(2, Vector128.Create(values[6])));
                else value = pointer(Operand128f(1, Vector128.Create(values[2])), Operand128f(2, Vector128.Create(values[6])));
                Check(value.GetElement(0) == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinMagnitudeNumber128f:direct-delegate-calli");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<double> Operand128d(int step, Vector128<double> value) { Effect(step); return value; }
    private static unsafe void TestMaxMagnitude128d(double[] values)
    {
        Func<Vector128<double>, Vector128<double>, Vector128<double>> callback = Vector128.MaxMagnitude<double>;
        delegate* managed<Vector128<double>, Vector128<double>, Vector128<double>> pointer = &Vector128.MaxMagnitude<double>;
        Console.WriteLine("width:MaxMagnitude128d:" + Vector128<double>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new double[Vector128<double>.Count]; var bb = new double[Vector128<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector128.Create(aa); var bv = Vector128.Create(bb);
            var actual = Vector128.MaxMagnitude<double>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MaxMagnitude128d:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector128<double> value;
                if (route == 0) value = Vector128.MaxMagnitude<double>(Operand128d(1, Vector128.Create(values[2])), Operand128d(2, Vector128.Create(values[6])));
                else if (route == 1) value = callback(Operand128d(1, Vector128.Create(values[2])), Operand128d(2, Vector128.Create(values[6])));
                else value = pointer(Operand128d(1, Vector128.Create(values[2])), Operand128d(2, Vector128.Create(values[6])));
                Check(value.GetElement(0) == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxMagnitude128d:direct-delegate-calli");
    }
    private static unsafe void TestMinMagnitude128d(double[] values)
    {
        Func<Vector128<double>, Vector128<double>, Vector128<double>> callback = Vector128.MinMagnitude<double>;
        delegate* managed<Vector128<double>, Vector128<double>, Vector128<double>> pointer = &Vector128.MinMagnitude<double>;
        Console.WriteLine("width:MinMagnitude128d:" + Vector128<double>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new double[Vector128<double>.Count]; var bb = new double[Vector128<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector128.Create(aa); var bv = Vector128.Create(bb);
            var actual = Vector128.MinMagnitude<double>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MinMagnitude128d:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector128<double> value;
                if (route == 0) value = Vector128.MinMagnitude<double>(Operand128d(1, Vector128.Create(values[2])), Operand128d(2, Vector128.Create(values[6])));
                else if (route == 1) value = callback(Operand128d(1, Vector128.Create(values[2])), Operand128d(2, Vector128.Create(values[6])));
                else value = pointer(Operand128d(1, Vector128.Create(values[2])), Operand128d(2, Vector128.Create(values[6])));
                Check(value.GetElement(0) == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinMagnitude128d:direct-delegate-calli");
    }
    private static unsafe void TestMaxMagnitudeNumber128d(double[] values)
    {
        Func<Vector128<double>, Vector128<double>, Vector128<double>> callback = Vector128.MaxMagnitudeNumber<double>;
        delegate* managed<Vector128<double>, Vector128<double>, Vector128<double>> pointer = &Vector128.MaxMagnitudeNumber<double>;
        Console.WriteLine("width:MaxMagnitudeNumber128d:" + Vector128<double>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new double[Vector128<double>.Count]; var bb = new double[Vector128<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector128.Create(aa); var bv = Vector128.Create(bb);
            var actual = Vector128.MaxMagnitudeNumber<double>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MaxMagnitudeNumber128d:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector128<double> value;
                if (route == 0) value = Vector128.MaxMagnitudeNumber<double>(Operand128d(1, Vector128.Create(values[2])), Operand128d(2, Vector128.Create(values[6])));
                else if (route == 1) value = callback(Operand128d(1, Vector128.Create(values[2])), Operand128d(2, Vector128.Create(values[6])));
                else value = pointer(Operand128d(1, Vector128.Create(values[2])), Operand128d(2, Vector128.Create(values[6])));
                Check(value.GetElement(0) == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxMagnitudeNumber128d:direct-delegate-calli");
    }
    private static unsafe void TestMinMagnitudeNumber128d(double[] values)
    {
        Func<Vector128<double>, Vector128<double>, Vector128<double>> callback = Vector128.MinMagnitudeNumber<double>;
        delegate* managed<Vector128<double>, Vector128<double>, Vector128<double>> pointer = &Vector128.MinMagnitudeNumber<double>;
        Console.WriteLine("width:MinMagnitudeNumber128d:" + Vector128<double>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new double[Vector128<double>.Count]; var bb = new double[Vector128<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector128.Create(aa); var bv = Vector128.Create(bb);
            var actual = Vector128.MinMagnitudeNumber<double>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MinMagnitudeNumber128d:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector128<double> value;
                if (route == 0) value = Vector128.MinMagnitudeNumber<double>(Operand128d(1, Vector128.Create(values[2])), Operand128d(2, Vector128.Create(values[6])));
                else if (route == 1) value = callback(Operand128d(1, Vector128.Create(values[2])), Operand128d(2, Vector128.Create(values[6])));
                else value = pointer(Operand128d(1, Vector128.Create(values[2])), Operand128d(2, Vector128.Create(values[6])));
                Check(value.GetElement(0) == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinMagnitudeNumber128d:direct-delegate-calli");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector256<float> Operand256f(int step, Vector256<float> value) { Effect(step); return value; }
    private static unsafe void TestMaxMagnitude256f(float[] values)
    {
        Func<Vector256<float>, Vector256<float>, Vector256<float>> callback = Vector256.MaxMagnitude<float>;
        delegate* managed<Vector256<float>, Vector256<float>, Vector256<float>> pointer = &Vector256.MaxMagnitude<float>;
        Console.WriteLine("width:MaxMagnitude256f:" + Vector256<float>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[Vector256<float>.Count]; var bb = new float[Vector256<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector256.Create(aa); var bv = Vector256.Create(bb);
            var actual = Vector256.MaxMagnitude<float>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MaxMagnitude256f:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector256<float> value;
                if (route == 0) value = Vector256.MaxMagnitude<float>(Operand256f(1, Vector256.Create(values[2])), Operand256f(2, Vector256.Create(values[6])));
                else if (route == 1) value = callback(Operand256f(1, Vector256.Create(values[2])), Operand256f(2, Vector256.Create(values[6])));
                else value = pointer(Operand256f(1, Vector256.Create(values[2])), Operand256f(2, Vector256.Create(values[6])));
                Check(value.GetElement(0) == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxMagnitude256f:direct-delegate-calli");
    }
    private static unsafe void TestMinMagnitude256f(float[] values)
    {
        Func<Vector256<float>, Vector256<float>, Vector256<float>> callback = Vector256.MinMagnitude<float>;
        delegate* managed<Vector256<float>, Vector256<float>, Vector256<float>> pointer = &Vector256.MinMagnitude<float>;
        Console.WriteLine("width:MinMagnitude256f:" + Vector256<float>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[Vector256<float>.Count]; var bb = new float[Vector256<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector256.Create(aa); var bv = Vector256.Create(bb);
            var actual = Vector256.MinMagnitude<float>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MinMagnitude256f:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector256<float> value;
                if (route == 0) value = Vector256.MinMagnitude<float>(Operand256f(1, Vector256.Create(values[2])), Operand256f(2, Vector256.Create(values[6])));
                else if (route == 1) value = callback(Operand256f(1, Vector256.Create(values[2])), Operand256f(2, Vector256.Create(values[6])));
                else value = pointer(Operand256f(1, Vector256.Create(values[2])), Operand256f(2, Vector256.Create(values[6])));
                Check(value.GetElement(0) == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinMagnitude256f:direct-delegate-calli");
    }
    private static unsafe void TestMaxMagnitudeNumber256f(float[] values)
    {
        Func<Vector256<float>, Vector256<float>, Vector256<float>> callback = Vector256.MaxMagnitudeNumber<float>;
        delegate* managed<Vector256<float>, Vector256<float>, Vector256<float>> pointer = &Vector256.MaxMagnitudeNumber<float>;
        Console.WriteLine("width:MaxMagnitudeNumber256f:" + Vector256<float>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[Vector256<float>.Count]; var bb = new float[Vector256<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector256.Create(aa); var bv = Vector256.Create(bb);
            var actual = Vector256.MaxMagnitudeNumber<float>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MaxMagnitudeNumber256f:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector256<float> value;
                if (route == 0) value = Vector256.MaxMagnitudeNumber<float>(Operand256f(1, Vector256.Create(values[2])), Operand256f(2, Vector256.Create(values[6])));
                else if (route == 1) value = callback(Operand256f(1, Vector256.Create(values[2])), Operand256f(2, Vector256.Create(values[6])));
                else value = pointer(Operand256f(1, Vector256.Create(values[2])), Operand256f(2, Vector256.Create(values[6])));
                Check(value.GetElement(0) == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxMagnitudeNumber256f:direct-delegate-calli");
    }
    private static unsafe void TestMinMagnitudeNumber256f(float[] values)
    {
        Func<Vector256<float>, Vector256<float>, Vector256<float>> callback = Vector256.MinMagnitudeNumber<float>;
        delegate* managed<Vector256<float>, Vector256<float>, Vector256<float>> pointer = &Vector256.MinMagnitudeNumber<float>;
        Console.WriteLine("width:MinMagnitudeNumber256f:" + Vector256<float>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[Vector256<float>.Count]; var bb = new float[Vector256<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector256.Create(aa); var bv = Vector256.Create(bb);
            var actual = Vector256.MinMagnitudeNumber<float>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MinMagnitudeNumber256f:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector256<float> value;
                if (route == 0) value = Vector256.MinMagnitudeNumber<float>(Operand256f(1, Vector256.Create(values[2])), Operand256f(2, Vector256.Create(values[6])));
                else if (route == 1) value = callback(Operand256f(1, Vector256.Create(values[2])), Operand256f(2, Vector256.Create(values[6])));
                else value = pointer(Operand256f(1, Vector256.Create(values[2])), Operand256f(2, Vector256.Create(values[6])));
                Check(value.GetElement(0) == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinMagnitudeNumber256f:direct-delegate-calli");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector256<double> Operand256d(int step, Vector256<double> value) { Effect(step); return value; }
    private static unsafe void TestMaxMagnitude256d(double[] values)
    {
        Func<Vector256<double>, Vector256<double>, Vector256<double>> callback = Vector256.MaxMagnitude<double>;
        delegate* managed<Vector256<double>, Vector256<double>, Vector256<double>> pointer = &Vector256.MaxMagnitude<double>;
        Console.WriteLine("width:MaxMagnitude256d:" + Vector256<double>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new double[Vector256<double>.Count]; var bb = new double[Vector256<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector256.Create(aa); var bv = Vector256.Create(bb);
            var actual = Vector256.MaxMagnitude<double>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MaxMagnitude256d:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector256<double> value;
                if (route == 0) value = Vector256.MaxMagnitude<double>(Operand256d(1, Vector256.Create(values[2])), Operand256d(2, Vector256.Create(values[6])));
                else if (route == 1) value = callback(Operand256d(1, Vector256.Create(values[2])), Operand256d(2, Vector256.Create(values[6])));
                else value = pointer(Operand256d(1, Vector256.Create(values[2])), Operand256d(2, Vector256.Create(values[6])));
                Check(value.GetElement(0) == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxMagnitude256d:direct-delegate-calli");
    }
    private static unsafe void TestMinMagnitude256d(double[] values)
    {
        Func<Vector256<double>, Vector256<double>, Vector256<double>> callback = Vector256.MinMagnitude<double>;
        delegate* managed<Vector256<double>, Vector256<double>, Vector256<double>> pointer = &Vector256.MinMagnitude<double>;
        Console.WriteLine("width:MinMagnitude256d:" + Vector256<double>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new double[Vector256<double>.Count]; var bb = new double[Vector256<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector256.Create(aa); var bv = Vector256.Create(bb);
            var actual = Vector256.MinMagnitude<double>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MinMagnitude256d:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector256<double> value;
                if (route == 0) value = Vector256.MinMagnitude<double>(Operand256d(1, Vector256.Create(values[2])), Operand256d(2, Vector256.Create(values[6])));
                else if (route == 1) value = callback(Operand256d(1, Vector256.Create(values[2])), Operand256d(2, Vector256.Create(values[6])));
                else value = pointer(Operand256d(1, Vector256.Create(values[2])), Operand256d(2, Vector256.Create(values[6])));
                Check(value.GetElement(0) == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinMagnitude256d:direct-delegate-calli");
    }
    private static unsafe void TestMaxMagnitudeNumber256d(double[] values)
    {
        Func<Vector256<double>, Vector256<double>, Vector256<double>> callback = Vector256.MaxMagnitudeNumber<double>;
        delegate* managed<Vector256<double>, Vector256<double>, Vector256<double>> pointer = &Vector256.MaxMagnitudeNumber<double>;
        Console.WriteLine("width:MaxMagnitudeNumber256d:" + Vector256<double>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new double[Vector256<double>.Count]; var bb = new double[Vector256<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector256.Create(aa); var bv = Vector256.Create(bb);
            var actual = Vector256.MaxMagnitudeNumber<double>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MaxMagnitudeNumber256d:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector256<double> value;
                if (route == 0) value = Vector256.MaxMagnitudeNumber<double>(Operand256d(1, Vector256.Create(values[2])), Operand256d(2, Vector256.Create(values[6])));
                else if (route == 1) value = callback(Operand256d(1, Vector256.Create(values[2])), Operand256d(2, Vector256.Create(values[6])));
                else value = pointer(Operand256d(1, Vector256.Create(values[2])), Operand256d(2, Vector256.Create(values[6])));
                Check(value.GetElement(0) == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxMagnitudeNumber256d:direct-delegate-calli");
    }
    private static unsafe void TestMinMagnitudeNumber256d(double[] values)
    {
        Func<Vector256<double>, Vector256<double>, Vector256<double>> callback = Vector256.MinMagnitudeNumber<double>;
        delegate* managed<Vector256<double>, Vector256<double>, Vector256<double>> pointer = &Vector256.MinMagnitudeNumber<double>;
        Console.WriteLine("width:MinMagnitudeNumber256d:" + Vector256<double>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new double[Vector256<double>.Count]; var bb = new double[Vector256<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector256.Create(aa); var bv = Vector256.Create(bb);
            var actual = Vector256.MinMagnitudeNumber<double>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MinMagnitudeNumber256d:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector256<double> value;
                if (route == 0) value = Vector256.MinMagnitudeNumber<double>(Operand256d(1, Vector256.Create(values[2])), Operand256d(2, Vector256.Create(values[6])));
                else if (route == 1) value = callback(Operand256d(1, Vector256.Create(values[2])), Operand256d(2, Vector256.Create(values[6])));
                else value = pointer(Operand256d(1, Vector256.Create(values[2])), Operand256d(2, Vector256.Create(values[6])));
                Check(value.GetElement(0) == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinMagnitudeNumber256d:direct-delegate-calli");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector512<float> Operand512f(int step, Vector512<float> value) { Effect(step); return value; }
    private static unsafe void TestMaxMagnitude512f(float[] values)
    {
        Func<Vector512<float>, Vector512<float>, Vector512<float>> callback = Vector512.MaxMagnitude<float>;
        delegate* managed<Vector512<float>, Vector512<float>, Vector512<float>> pointer = &Vector512.MaxMagnitude<float>;
        Console.WriteLine("width:MaxMagnitude512f:" + Vector512<float>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[Vector512<float>.Count]; var bb = new float[Vector512<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector512.Create(aa); var bv = Vector512.Create(bb);
            var actual = Vector512.MaxMagnitude<float>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MaxMagnitude512f:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector512<float> value;
                if (route == 0) value = Vector512.MaxMagnitude<float>(Operand512f(1, Vector512.Create(values[2])), Operand512f(2, Vector512.Create(values[6])));
                else if (route == 1) value = callback(Operand512f(1, Vector512.Create(values[2])), Operand512f(2, Vector512.Create(values[6])));
                else value = pointer(Operand512f(1, Vector512.Create(values[2])), Operand512f(2, Vector512.Create(values[6])));
                Check(value.GetElement(0) == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxMagnitude512f:direct-delegate-calli");
    }
    private static unsafe void TestMinMagnitude512f(float[] values)
    {
        Func<Vector512<float>, Vector512<float>, Vector512<float>> callback = Vector512.MinMagnitude<float>;
        delegate* managed<Vector512<float>, Vector512<float>, Vector512<float>> pointer = &Vector512.MinMagnitude<float>;
        Console.WriteLine("width:MinMagnitude512f:" + Vector512<float>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[Vector512<float>.Count]; var bb = new float[Vector512<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector512.Create(aa); var bv = Vector512.Create(bb);
            var actual = Vector512.MinMagnitude<float>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MinMagnitude512f:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector512<float> value;
                if (route == 0) value = Vector512.MinMagnitude<float>(Operand512f(1, Vector512.Create(values[2])), Operand512f(2, Vector512.Create(values[6])));
                else if (route == 1) value = callback(Operand512f(1, Vector512.Create(values[2])), Operand512f(2, Vector512.Create(values[6])));
                else value = pointer(Operand512f(1, Vector512.Create(values[2])), Operand512f(2, Vector512.Create(values[6])));
                Check(value.GetElement(0) == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinMagnitude512f:direct-delegate-calli");
    }
    private static unsafe void TestMaxMagnitudeNumber512f(float[] values)
    {
        Func<Vector512<float>, Vector512<float>, Vector512<float>> callback = Vector512.MaxMagnitudeNumber<float>;
        delegate* managed<Vector512<float>, Vector512<float>, Vector512<float>> pointer = &Vector512.MaxMagnitudeNumber<float>;
        Console.WriteLine("width:MaxMagnitudeNumber512f:" + Vector512<float>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[Vector512<float>.Count]; var bb = new float[Vector512<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector512.Create(aa); var bv = Vector512.Create(bb);
            var actual = Vector512.MaxMagnitudeNumber<float>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MaxMagnitudeNumber512f:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector512<float> value;
                if (route == 0) value = Vector512.MaxMagnitudeNumber<float>(Operand512f(1, Vector512.Create(values[2])), Operand512f(2, Vector512.Create(values[6])));
                else if (route == 1) value = callback(Operand512f(1, Vector512.Create(values[2])), Operand512f(2, Vector512.Create(values[6])));
                else value = pointer(Operand512f(1, Vector512.Create(values[2])), Operand512f(2, Vector512.Create(values[6])));
                Check(value.GetElement(0) == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxMagnitudeNumber512f:direct-delegate-calli");
    }
    private static unsafe void TestMinMagnitudeNumber512f(float[] values)
    {
        Func<Vector512<float>, Vector512<float>, Vector512<float>> callback = Vector512.MinMagnitudeNumber<float>;
        delegate* managed<Vector512<float>, Vector512<float>, Vector512<float>> pointer = &Vector512.MinMagnitudeNumber<float>;
        Console.WriteLine("width:MinMagnitudeNumber512f:" + Vector512<float>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[Vector512<float>.Count]; var bb = new float[Vector512<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector512.Create(aa); var bv = Vector512.Create(bb);
            var actual = Vector512.MinMagnitudeNumber<float>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MinMagnitudeNumber512f:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector512<float> value;
                if (route == 0) value = Vector512.MinMagnitudeNumber<float>(Operand512f(1, Vector512.Create(values[2])), Operand512f(2, Vector512.Create(values[6])));
                else if (route == 1) value = callback(Operand512f(1, Vector512.Create(values[2])), Operand512f(2, Vector512.Create(values[6])));
                else value = pointer(Operand512f(1, Vector512.Create(values[2])), Operand512f(2, Vector512.Create(values[6])));
                Check(value.GetElement(0) == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinMagnitudeNumber512f:direct-delegate-calli");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector512<double> Operand512d(int step, Vector512<double> value) { Effect(step); return value; }
    private static unsafe void TestMaxMagnitude512d(double[] values)
    {
        Func<Vector512<double>, Vector512<double>, Vector512<double>> callback = Vector512.MaxMagnitude<double>;
        delegate* managed<Vector512<double>, Vector512<double>, Vector512<double>> pointer = &Vector512.MaxMagnitude<double>;
        Console.WriteLine("width:MaxMagnitude512d:" + Vector512<double>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new double[Vector512<double>.Count]; var bb = new double[Vector512<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector512.Create(aa); var bv = Vector512.Create(bb);
            var actual = Vector512.MaxMagnitude<double>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MaxMagnitude512d:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector512<double> value;
                if (route == 0) value = Vector512.MaxMagnitude<double>(Operand512d(1, Vector512.Create(values[2])), Operand512d(2, Vector512.Create(values[6])));
                else if (route == 1) value = callback(Operand512d(1, Vector512.Create(values[2])), Operand512d(2, Vector512.Create(values[6])));
                else value = pointer(Operand512d(1, Vector512.Create(values[2])), Operand512d(2, Vector512.Create(values[6])));
                Check(value.GetElement(0) == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxMagnitude512d:direct-delegate-calli");
    }
    private static unsafe void TestMinMagnitude512d(double[] values)
    {
        Func<Vector512<double>, Vector512<double>, Vector512<double>> callback = Vector512.MinMagnitude<double>;
        delegate* managed<Vector512<double>, Vector512<double>, Vector512<double>> pointer = &Vector512.MinMagnitude<double>;
        Console.WriteLine("width:MinMagnitude512d:" + Vector512<double>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new double[Vector512<double>.Count]; var bb = new double[Vector512<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector512.Create(aa); var bv = Vector512.Create(bb);
            var actual = Vector512.MinMagnitude<double>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MinMagnitude512d:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector512<double> value;
                if (route == 0) value = Vector512.MinMagnitude<double>(Operand512d(1, Vector512.Create(values[2])), Operand512d(2, Vector512.Create(values[6])));
                else if (route == 1) value = callback(Operand512d(1, Vector512.Create(values[2])), Operand512d(2, Vector512.Create(values[6])));
                else value = pointer(Operand512d(1, Vector512.Create(values[2])), Operand512d(2, Vector512.Create(values[6])));
                Check(value.GetElement(0) == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinMagnitude512d:direct-delegate-calli");
    }
    private static unsafe void TestMaxMagnitudeNumber512d(double[] values)
    {
        Func<Vector512<double>, Vector512<double>, Vector512<double>> callback = Vector512.MaxMagnitudeNumber<double>;
        delegate* managed<Vector512<double>, Vector512<double>, Vector512<double>> pointer = &Vector512.MaxMagnitudeNumber<double>;
        Console.WriteLine("width:MaxMagnitudeNumber512d:" + Vector512<double>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new double[Vector512<double>.Count]; var bb = new double[Vector512<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector512.Create(aa); var bv = Vector512.Create(bb);
            var actual = Vector512.MaxMagnitudeNumber<double>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MaxMagnitudeNumber512d:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector512<double> value;
                if (route == 0) value = Vector512.MaxMagnitudeNumber<double>(Operand512d(1, Vector512.Create(values[2])), Operand512d(2, Vector512.Create(values[6])));
                else if (route == 1) value = callback(Operand512d(1, Vector512.Create(values[2])), Operand512d(2, Vector512.Create(values[6])));
                else value = pointer(Operand512d(1, Vector512.Create(values[2])), Operand512d(2, Vector512.Create(values[6])));
                Check(value.GetElement(0) == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxMagnitudeNumber512d:direct-delegate-calli");
    }
    private static unsafe void TestMinMagnitudeNumber512d(double[] values)
    {
        Func<Vector512<double>, Vector512<double>, Vector512<double>> callback = Vector512.MinMagnitudeNumber<double>;
        delegate* managed<Vector512<double>, Vector512<double>, Vector512<double>> pointer = &Vector512.MinMagnitudeNumber<double>;
        Console.WriteLine("width:MinMagnitudeNumber512d:" + Vector512<double>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new double[Vector512<double>.Count]; var bb = new double[Vector512<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector512.Create(aa); var bv = Vector512.Create(bb);
            var actual = Vector512.MinMagnitudeNumber<double>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MinMagnitudeNumber512d:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector512<double> value;
                if (route == 0) value = Vector512.MinMagnitudeNumber<double>(Operand512d(1, Vector512.Create(values[2])), Operand512d(2, Vector512.Create(values[6])));
                else if (route == 1) value = callback(Operand512d(1, Vector512.Create(values[2])), Operand512d(2, Vector512.Create(values[6])));
                else value = pointer(Operand512d(1, Vector512.Create(values[2])), Operand512d(2, Vector512.Create(values[6])));
                Check(value.GetElement(0) == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinMagnitudeNumber512d:direct-delegate-calli");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<float> Operandnf(int step, Vector<float> value) { Effect(step); return value; }
    private static unsafe void TestMaxMagnitudenf(float[] values)
    {
        Func<Vector<float>, Vector<float>, Vector<float>> callback = Vector.MaxMagnitude<float>;
        delegate* managed<Vector<float>, Vector<float>, Vector<float>> pointer = &Vector.MaxMagnitude<float>;
        Console.WriteLine("width:MaxMagnitudenf:" + Vector<float>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[Vector<float>.Count]; var bb = new float[Vector<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = new Vector<float>(aa); var bv = new Vector<float>(bb);
            var actual = Vector.MaxMagnitude<float>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MaxMagnitudenf:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual[lane]) + ":" + Bits(delegated[lane]) + ":" + Bits(indirect[lane]) + ":" + Bits(av[lane]) + ":" + Bits(bv[lane]));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector<float> value;
                if (route == 0) value = Vector.MaxMagnitude<float>(Operandnf(1, new Vector<float>(values[2])), Operandnf(2, new Vector<float>(values[6])));
                else if (route == 1) value = callback(Operandnf(1, new Vector<float>(values[2])), Operandnf(2, new Vector<float>(values[6])));
                else value = pointer(Operandnf(1, new Vector<float>(values[2])), Operandnf(2, new Vector<float>(values[6])));
                Check(value[0] == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxMagnitudenf:direct-delegate-calli");
    }
    private static unsafe void TestMinMagnitudenf(float[] values)
    {
        Func<Vector<float>, Vector<float>, Vector<float>> callback = Vector.MinMagnitude<float>;
        delegate* managed<Vector<float>, Vector<float>, Vector<float>> pointer = &Vector.MinMagnitude<float>;
        Console.WriteLine("width:MinMagnitudenf:" + Vector<float>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[Vector<float>.Count]; var bb = new float[Vector<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = new Vector<float>(aa); var bv = new Vector<float>(bb);
            var actual = Vector.MinMagnitude<float>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MinMagnitudenf:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual[lane]) + ":" + Bits(delegated[lane]) + ":" + Bits(indirect[lane]) + ":" + Bits(av[lane]) + ":" + Bits(bv[lane]));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector<float> value;
                if (route == 0) value = Vector.MinMagnitude<float>(Operandnf(1, new Vector<float>(values[2])), Operandnf(2, new Vector<float>(values[6])));
                else if (route == 1) value = callback(Operandnf(1, new Vector<float>(values[2])), Operandnf(2, new Vector<float>(values[6])));
                else value = pointer(Operandnf(1, new Vector<float>(values[2])), Operandnf(2, new Vector<float>(values[6])));
                Check(value[0] == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinMagnitudenf:direct-delegate-calli");
    }
    private static unsafe void TestMaxMagnitudeNumbernf(float[] values)
    {
        Func<Vector<float>, Vector<float>, Vector<float>> callback = Vector.MaxMagnitudeNumber<float>;
        delegate* managed<Vector<float>, Vector<float>, Vector<float>> pointer = &Vector.MaxMagnitudeNumber<float>;
        Console.WriteLine("width:MaxMagnitudeNumbernf:" + Vector<float>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[Vector<float>.Count]; var bb = new float[Vector<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = new Vector<float>(aa); var bv = new Vector<float>(bb);
            var actual = Vector.MaxMagnitudeNumber<float>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MaxMagnitudeNumbernf:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual[lane]) + ":" + Bits(delegated[lane]) + ":" + Bits(indirect[lane]) + ":" + Bits(av[lane]) + ":" + Bits(bv[lane]));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector<float> value;
                if (route == 0) value = Vector.MaxMagnitudeNumber<float>(Operandnf(1, new Vector<float>(values[2])), Operandnf(2, new Vector<float>(values[6])));
                else if (route == 1) value = callback(Operandnf(1, new Vector<float>(values[2])), Operandnf(2, new Vector<float>(values[6])));
                else value = pointer(Operandnf(1, new Vector<float>(values[2])), Operandnf(2, new Vector<float>(values[6])));
                Check(value[0] == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxMagnitudeNumbernf:direct-delegate-calli");
    }
    private static unsafe void TestMinMagnitudeNumbernf(float[] values)
    {
        Func<Vector<float>, Vector<float>, Vector<float>> callback = Vector.MinMagnitudeNumber<float>;
        delegate* managed<Vector<float>, Vector<float>, Vector<float>> pointer = &Vector.MinMagnitudeNumber<float>;
        Console.WriteLine("width:MinMagnitudeNumbernf:" + Vector<float>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[Vector<float>.Count]; var bb = new float[Vector<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = new Vector<float>(aa); var bv = new Vector<float>(bb);
            var actual = Vector.MinMagnitudeNumber<float>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MinMagnitudeNumbernf:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual[lane]) + ":" + Bits(delegated[lane]) + ":" + Bits(indirect[lane]) + ":" + Bits(av[lane]) + ":" + Bits(bv[lane]));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector<float> value;
                if (route == 0) value = Vector.MinMagnitudeNumber<float>(Operandnf(1, new Vector<float>(values[2])), Operandnf(2, new Vector<float>(values[6])));
                else if (route == 1) value = callback(Operandnf(1, new Vector<float>(values[2])), Operandnf(2, new Vector<float>(values[6])));
                else value = pointer(Operandnf(1, new Vector<float>(values[2])), Operandnf(2, new Vector<float>(values[6])));
                Check(value[0] == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinMagnitudeNumbernf:direct-delegate-calli");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<double> Operandnd(int step, Vector<double> value) { Effect(step); return value; }
    private static unsafe void TestMaxMagnitudend(double[] values)
    {
        Func<Vector<double>, Vector<double>, Vector<double>> callback = Vector.MaxMagnitude<double>;
        delegate* managed<Vector<double>, Vector<double>, Vector<double>> pointer = &Vector.MaxMagnitude<double>;
        Console.WriteLine("width:MaxMagnitudend:" + Vector<double>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new double[Vector<double>.Count]; var bb = new double[Vector<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = new Vector<double>(aa); var bv = new Vector<double>(bb);
            var actual = Vector.MaxMagnitude<double>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MaxMagnitudend:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual[lane]) + ":" + Bits(delegated[lane]) + ":" + Bits(indirect[lane]) + ":" + Bits(av[lane]) + ":" + Bits(bv[lane]));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector<double> value;
                if (route == 0) value = Vector.MaxMagnitude<double>(Operandnd(1, new Vector<double>(values[2])), Operandnd(2, new Vector<double>(values[6])));
                else if (route == 1) value = callback(Operandnd(1, new Vector<double>(values[2])), Operandnd(2, new Vector<double>(values[6])));
                else value = pointer(Operandnd(1, new Vector<double>(values[2])), Operandnd(2, new Vector<double>(values[6])));
                Check(value[0] == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxMagnitudend:direct-delegate-calli");
    }
    private static unsafe void TestMinMagnitudend(double[] values)
    {
        Func<Vector<double>, Vector<double>, Vector<double>> callback = Vector.MinMagnitude<double>;
        delegate* managed<Vector<double>, Vector<double>, Vector<double>> pointer = &Vector.MinMagnitude<double>;
        Console.WriteLine("width:MinMagnitudend:" + Vector<double>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new double[Vector<double>.Count]; var bb = new double[Vector<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = new Vector<double>(aa); var bv = new Vector<double>(bb);
            var actual = Vector.MinMagnitude<double>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MinMagnitudend:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual[lane]) + ":" + Bits(delegated[lane]) + ":" + Bits(indirect[lane]) + ":" + Bits(av[lane]) + ":" + Bits(bv[lane]));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector<double> value;
                if (route == 0) value = Vector.MinMagnitude<double>(Operandnd(1, new Vector<double>(values[2])), Operandnd(2, new Vector<double>(values[6])));
                else if (route == 1) value = callback(Operandnd(1, new Vector<double>(values[2])), Operandnd(2, new Vector<double>(values[6])));
                else value = pointer(Operandnd(1, new Vector<double>(values[2])), Operandnd(2, new Vector<double>(values[6])));
                Check(value[0] == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinMagnitudend:direct-delegate-calli");
    }
    private static unsafe void TestMaxMagnitudeNumbernd(double[] values)
    {
        Func<Vector<double>, Vector<double>, Vector<double>> callback = Vector.MaxMagnitudeNumber<double>;
        delegate* managed<Vector<double>, Vector<double>, Vector<double>> pointer = &Vector.MaxMagnitudeNumber<double>;
        Console.WriteLine("width:MaxMagnitudeNumbernd:" + Vector<double>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new double[Vector<double>.Count]; var bb = new double[Vector<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = new Vector<double>(aa); var bv = new Vector<double>(bb);
            var actual = Vector.MaxMagnitudeNumber<double>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MaxMagnitudeNumbernd:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual[lane]) + ":" + Bits(delegated[lane]) + ":" + Bits(indirect[lane]) + ":" + Bits(av[lane]) + ":" + Bits(bv[lane]));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector<double> value;
                if (route == 0) value = Vector.MaxMagnitudeNumber<double>(Operandnd(1, new Vector<double>(values[2])), Operandnd(2, new Vector<double>(values[6])));
                else if (route == 1) value = callback(Operandnd(1, new Vector<double>(values[2])), Operandnd(2, new Vector<double>(values[6])));
                else value = pointer(Operandnd(1, new Vector<double>(values[2])), Operandnd(2, new Vector<double>(values[6])));
                Check(value[0] == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxMagnitudeNumbernd:direct-delegate-calli");
    }
    private static unsafe void TestMinMagnitudeNumbernd(double[] values)
    {
        Func<Vector<double>, Vector<double>, Vector<double>> callback = Vector.MinMagnitudeNumber<double>;
        delegate* managed<Vector<double>, Vector<double>, Vector<double>> pointer = &Vector.MinMagnitudeNumber<double>;
        Console.WriteLine("width:MinMagnitudeNumbernd:" + Vector<double>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new double[Vector<double>.Count]; var bb = new double[Vector<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = new Vector<double>(aa); var bv = new Vector<double>(bb);
            var actual = Vector.MinMagnitudeNumber<double>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MinMagnitudeNumbernd:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual[lane]) + ":" + Bits(delegated[lane]) + ":" + Bits(indirect[lane]) + ":" + Bits(av[lane]) + ":" + Bits(bv[lane]));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector<double> value;
                if (route == 0) value = Vector.MinMagnitudeNumber<double>(Operandnd(1, new Vector<double>(values[2])), Operandnd(2, new Vector<double>(values[6])));
                else if (route == 1) value = callback(Operandnd(1, new Vector<double>(values[2])), Operandnd(2, new Vector<double>(values[6])));
                else value = pointer(Operandnd(1, new Vector<double>(values[2])), Operandnd(2, new Vector<double>(values[6])));
                Check(value[0] == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinMagnitudeNumbernd:direct-delegate-calli");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector2 Operandc2(int step, Vector2 value) { Effect(step); return value; }
    private static unsafe void TestMaxMagnitudec2(float[] values)
    {
        Func<Vector2, Vector2, Vector2> callback = Vector2.MaxMagnitude;
        delegate* managed<Vector2, Vector2, Vector2> pointer = &Vector2.MaxMagnitude;
        Console.WriteLine("width:MaxMagnitudec2:2");
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[2]; var bb = new float[2];
            for (int lane = 0; lane < 2; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = new Vector2(aa[0], aa[1]); var bv = new Vector2(bb[0], bb[1]);
            var actual = Vector2.MaxMagnitude(av, bv); var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < 2; lane++)
                Console.WriteLine("MaxMagnitudec2:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual[lane]) + ":" + Bits(delegated[lane]) + ":" + Bits(indirect[lane]) + ":" + Bits(av[lane]) + ":" + Bits(bv[lane]));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector2 value;
                if (route == 0) value = Vector2.MaxMagnitude(Operandc2(1, new Vector2(values[2])), Operandc2(2, new Vector2(values[6])));
                else if (route == 1) value = callback(Operandc2(1, new Vector2(values[2])), Operandc2(2, new Vector2(values[6])));
                else value = pointer(Operandc2(1, new Vector2(values[2])), Operandc2(2, new Vector2(values[6])));
                Check(value[0] == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxMagnitudec2:direct-delegate-calli");
    }
    private static unsafe void TestMinMagnitudec2(float[] values)
    {
        Func<Vector2, Vector2, Vector2> callback = Vector2.MinMagnitude;
        delegate* managed<Vector2, Vector2, Vector2> pointer = &Vector2.MinMagnitude;
        Console.WriteLine("width:MinMagnitudec2:2");
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[2]; var bb = new float[2];
            for (int lane = 0; lane < 2; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = new Vector2(aa[0], aa[1]); var bv = new Vector2(bb[0], bb[1]);
            var actual = Vector2.MinMagnitude(av, bv); var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < 2; lane++)
                Console.WriteLine("MinMagnitudec2:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual[lane]) + ":" + Bits(delegated[lane]) + ":" + Bits(indirect[lane]) + ":" + Bits(av[lane]) + ":" + Bits(bv[lane]));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector2 value;
                if (route == 0) value = Vector2.MinMagnitude(Operandc2(1, new Vector2(values[2])), Operandc2(2, new Vector2(values[6])));
                else if (route == 1) value = callback(Operandc2(1, new Vector2(values[2])), Operandc2(2, new Vector2(values[6])));
                else value = pointer(Operandc2(1, new Vector2(values[2])), Operandc2(2, new Vector2(values[6])));
                Check(value[0] == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinMagnitudec2:direct-delegate-calli");
    }
    private static unsafe void TestMaxMagnitudeNumberc2(float[] values)
    {
        Func<Vector2, Vector2, Vector2> callback = Vector2.MaxMagnitudeNumber;
        delegate* managed<Vector2, Vector2, Vector2> pointer = &Vector2.MaxMagnitudeNumber;
        Console.WriteLine("width:MaxMagnitudeNumberc2:2");
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[2]; var bb = new float[2];
            for (int lane = 0; lane < 2; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = new Vector2(aa[0], aa[1]); var bv = new Vector2(bb[0], bb[1]);
            var actual = Vector2.MaxMagnitudeNumber(av, bv); var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < 2; lane++)
                Console.WriteLine("MaxMagnitudeNumberc2:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual[lane]) + ":" + Bits(delegated[lane]) + ":" + Bits(indirect[lane]) + ":" + Bits(av[lane]) + ":" + Bits(bv[lane]));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector2 value;
                if (route == 0) value = Vector2.MaxMagnitudeNumber(Operandc2(1, new Vector2(values[2])), Operandc2(2, new Vector2(values[6])));
                else if (route == 1) value = callback(Operandc2(1, new Vector2(values[2])), Operandc2(2, new Vector2(values[6])));
                else value = pointer(Operandc2(1, new Vector2(values[2])), Operandc2(2, new Vector2(values[6])));
                Check(value[0] == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxMagnitudeNumberc2:direct-delegate-calli");
    }
    private static unsafe void TestMinMagnitudeNumberc2(float[] values)
    {
        Func<Vector2, Vector2, Vector2> callback = Vector2.MinMagnitudeNumber;
        delegate* managed<Vector2, Vector2, Vector2> pointer = &Vector2.MinMagnitudeNumber;
        Console.WriteLine("width:MinMagnitudeNumberc2:2");
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[2]; var bb = new float[2];
            for (int lane = 0; lane < 2; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = new Vector2(aa[0], aa[1]); var bv = new Vector2(bb[0], bb[1]);
            var actual = Vector2.MinMagnitudeNumber(av, bv); var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < 2; lane++)
                Console.WriteLine("MinMagnitudeNumberc2:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual[lane]) + ":" + Bits(delegated[lane]) + ":" + Bits(indirect[lane]) + ":" + Bits(av[lane]) + ":" + Bits(bv[lane]));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector2 value;
                if (route == 0) value = Vector2.MinMagnitudeNumber(Operandc2(1, new Vector2(values[2])), Operandc2(2, new Vector2(values[6])));
                else if (route == 1) value = callback(Operandc2(1, new Vector2(values[2])), Operandc2(2, new Vector2(values[6])));
                else value = pointer(Operandc2(1, new Vector2(values[2])), Operandc2(2, new Vector2(values[6])));
                Check(value[0] == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinMagnitudeNumberc2:direct-delegate-calli");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector3 Operandc3(int step, Vector3 value) { Effect(step); return value; }
    private static unsafe void TestMaxMagnitudec3(float[] values)
    {
        Func<Vector3, Vector3, Vector3> callback = Vector3.MaxMagnitude;
        delegate* managed<Vector3, Vector3, Vector3> pointer = &Vector3.MaxMagnitude;
        Console.WriteLine("width:MaxMagnitudec3:3");
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[3]; var bb = new float[3];
            for (int lane = 0; lane < 3; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = new Vector3(aa[0], aa[1], aa[2]); var bv = new Vector3(bb[0], bb[1], bb[2]);
            var actual = Vector3.MaxMagnitude(av, bv); var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < 3; lane++)
                Console.WriteLine("MaxMagnitudec3:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual[lane]) + ":" + Bits(delegated[lane]) + ":" + Bits(indirect[lane]) + ":" + Bits(av[lane]) + ":" + Bits(bv[lane]));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector3 value;
                if (route == 0) value = Vector3.MaxMagnitude(Operandc3(1, new Vector3(values[2])), Operandc3(2, new Vector3(values[6])));
                else if (route == 1) value = callback(Operandc3(1, new Vector3(values[2])), Operandc3(2, new Vector3(values[6])));
                else value = pointer(Operandc3(1, new Vector3(values[2])), Operandc3(2, new Vector3(values[6])));
                Check(value[0] == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxMagnitudec3:direct-delegate-calli");
    }
    private static unsafe void TestMinMagnitudec3(float[] values)
    {
        Func<Vector3, Vector3, Vector3> callback = Vector3.MinMagnitude;
        delegate* managed<Vector3, Vector3, Vector3> pointer = &Vector3.MinMagnitude;
        Console.WriteLine("width:MinMagnitudec3:3");
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[3]; var bb = new float[3];
            for (int lane = 0; lane < 3; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = new Vector3(aa[0], aa[1], aa[2]); var bv = new Vector3(bb[0], bb[1], bb[2]);
            var actual = Vector3.MinMagnitude(av, bv); var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < 3; lane++)
                Console.WriteLine("MinMagnitudec3:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual[lane]) + ":" + Bits(delegated[lane]) + ":" + Bits(indirect[lane]) + ":" + Bits(av[lane]) + ":" + Bits(bv[lane]));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector3 value;
                if (route == 0) value = Vector3.MinMagnitude(Operandc3(1, new Vector3(values[2])), Operandc3(2, new Vector3(values[6])));
                else if (route == 1) value = callback(Operandc3(1, new Vector3(values[2])), Operandc3(2, new Vector3(values[6])));
                else value = pointer(Operandc3(1, new Vector3(values[2])), Operandc3(2, new Vector3(values[6])));
                Check(value[0] == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinMagnitudec3:direct-delegate-calli");
    }
    private static unsafe void TestMaxMagnitudeNumberc3(float[] values)
    {
        Func<Vector3, Vector3, Vector3> callback = Vector3.MaxMagnitudeNumber;
        delegate* managed<Vector3, Vector3, Vector3> pointer = &Vector3.MaxMagnitudeNumber;
        Console.WriteLine("width:MaxMagnitudeNumberc3:3");
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[3]; var bb = new float[3];
            for (int lane = 0; lane < 3; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = new Vector3(aa[0], aa[1], aa[2]); var bv = new Vector3(bb[0], bb[1], bb[2]);
            var actual = Vector3.MaxMagnitudeNumber(av, bv); var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < 3; lane++)
                Console.WriteLine("MaxMagnitudeNumberc3:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual[lane]) + ":" + Bits(delegated[lane]) + ":" + Bits(indirect[lane]) + ":" + Bits(av[lane]) + ":" + Bits(bv[lane]));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector3 value;
                if (route == 0) value = Vector3.MaxMagnitudeNumber(Operandc3(1, new Vector3(values[2])), Operandc3(2, new Vector3(values[6])));
                else if (route == 1) value = callback(Operandc3(1, new Vector3(values[2])), Operandc3(2, new Vector3(values[6])));
                else value = pointer(Operandc3(1, new Vector3(values[2])), Operandc3(2, new Vector3(values[6])));
                Check(value[0] == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxMagnitudeNumberc3:direct-delegate-calli");
    }
    private static unsafe void TestMinMagnitudeNumberc3(float[] values)
    {
        Func<Vector3, Vector3, Vector3> callback = Vector3.MinMagnitudeNumber;
        delegate* managed<Vector3, Vector3, Vector3> pointer = &Vector3.MinMagnitudeNumber;
        Console.WriteLine("width:MinMagnitudeNumberc3:3");
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[3]; var bb = new float[3];
            for (int lane = 0; lane < 3; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = new Vector3(aa[0], aa[1], aa[2]); var bv = new Vector3(bb[0], bb[1], bb[2]);
            var actual = Vector3.MinMagnitudeNumber(av, bv); var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < 3; lane++)
                Console.WriteLine("MinMagnitudeNumberc3:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual[lane]) + ":" + Bits(delegated[lane]) + ":" + Bits(indirect[lane]) + ":" + Bits(av[lane]) + ":" + Bits(bv[lane]));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector3 value;
                if (route == 0) value = Vector3.MinMagnitudeNumber(Operandc3(1, new Vector3(values[2])), Operandc3(2, new Vector3(values[6])));
                else if (route == 1) value = callback(Operandc3(1, new Vector3(values[2])), Operandc3(2, new Vector3(values[6])));
                else value = pointer(Operandc3(1, new Vector3(values[2])), Operandc3(2, new Vector3(values[6])));
                Check(value[0] == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinMagnitudeNumberc3:direct-delegate-calli");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector4 Operandc4(int step, Vector4 value) { Effect(step); return value; }
    private static unsafe void TestMaxMagnitudec4(float[] values)
    {
        Func<Vector4, Vector4, Vector4> callback = Vector4.MaxMagnitude;
        delegate* managed<Vector4, Vector4, Vector4> pointer = &Vector4.MaxMagnitude;
        Console.WriteLine("width:MaxMagnitudec4:4");
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[4]; var bb = new float[4];
            for (int lane = 0; lane < 4; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = new Vector4(aa[0], aa[1], aa[2], aa[3]); var bv = new Vector4(bb[0], bb[1], bb[2], bb[3]);
            var actual = Vector4.MaxMagnitude(av, bv); var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < 4; lane++)
                Console.WriteLine("MaxMagnitudec4:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual[lane]) + ":" + Bits(delegated[lane]) + ":" + Bits(indirect[lane]) + ":" + Bits(av[lane]) + ":" + Bits(bv[lane]));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector4 value;
                if (route == 0) value = Vector4.MaxMagnitude(Operandc4(1, new Vector4(values[2])), Operandc4(2, new Vector4(values[6])));
                else if (route == 1) value = callback(Operandc4(1, new Vector4(values[2])), Operandc4(2, new Vector4(values[6])));
                else value = pointer(Operandc4(1, new Vector4(values[2])), Operandc4(2, new Vector4(values[6])));
                Check(value[0] == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxMagnitudec4:direct-delegate-calli");
    }
    private static unsafe void TestMinMagnitudec4(float[] values)
    {
        Func<Vector4, Vector4, Vector4> callback = Vector4.MinMagnitude;
        delegate* managed<Vector4, Vector4, Vector4> pointer = &Vector4.MinMagnitude;
        Console.WriteLine("width:MinMagnitudec4:4");
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[4]; var bb = new float[4];
            for (int lane = 0; lane < 4; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = new Vector4(aa[0], aa[1], aa[2], aa[3]); var bv = new Vector4(bb[0], bb[1], bb[2], bb[3]);
            var actual = Vector4.MinMagnitude(av, bv); var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < 4; lane++)
                Console.WriteLine("MinMagnitudec4:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual[lane]) + ":" + Bits(delegated[lane]) + ":" + Bits(indirect[lane]) + ":" + Bits(av[lane]) + ":" + Bits(bv[lane]));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector4 value;
                if (route == 0) value = Vector4.MinMagnitude(Operandc4(1, new Vector4(values[2])), Operandc4(2, new Vector4(values[6])));
                else if (route == 1) value = callback(Operandc4(1, new Vector4(values[2])), Operandc4(2, new Vector4(values[6])));
                else value = pointer(Operandc4(1, new Vector4(values[2])), Operandc4(2, new Vector4(values[6])));
                Check(value[0] == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinMagnitudec4:direct-delegate-calli");
    }
    private static unsafe void TestMaxMagnitudeNumberc4(float[] values)
    {
        Func<Vector4, Vector4, Vector4> callback = Vector4.MaxMagnitudeNumber;
        delegate* managed<Vector4, Vector4, Vector4> pointer = &Vector4.MaxMagnitudeNumber;
        Console.WriteLine("width:MaxMagnitudeNumberc4:4");
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[4]; var bb = new float[4];
            for (int lane = 0; lane < 4; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = new Vector4(aa[0], aa[1], aa[2], aa[3]); var bv = new Vector4(bb[0], bb[1], bb[2], bb[3]);
            var actual = Vector4.MaxMagnitudeNumber(av, bv); var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < 4; lane++)
                Console.WriteLine("MaxMagnitudeNumberc4:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual[lane]) + ":" + Bits(delegated[lane]) + ":" + Bits(indirect[lane]) + ":" + Bits(av[lane]) + ":" + Bits(bv[lane]));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector4 value;
                if (route == 0) value = Vector4.MaxMagnitudeNumber(Operandc4(1, new Vector4(values[2])), Operandc4(2, new Vector4(values[6])));
                else if (route == 1) value = callback(Operandc4(1, new Vector4(values[2])), Operandc4(2, new Vector4(values[6])));
                else value = pointer(Operandc4(1, new Vector4(values[2])), Operandc4(2, new Vector4(values[6])));
                Check(value[0] == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxMagnitudeNumberc4:direct-delegate-calli");
    }
    private static unsafe void TestMinMagnitudeNumberc4(float[] values)
    {
        Func<Vector4, Vector4, Vector4> callback = Vector4.MinMagnitudeNumber;
        delegate* managed<Vector4, Vector4, Vector4> pointer = &Vector4.MinMagnitudeNumber;
        Console.WriteLine("width:MinMagnitudeNumberc4:4");
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[4]; var bb = new float[4];
            for (int lane = 0; lane < 4; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = new Vector4(aa[0], aa[1], aa[2], aa[3]); var bv = new Vector4(bb[0], bb[1], bb[2], bb[3]);
            var actual = Vector4.MinMagnitudeNumber(av, bv); var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < 4; lane++)
                Console.WriteLine("MinMagnitudeNumberc4:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual[lane]) + ":" + Bits(delegated[lane]) + ":" + Bits(indirect[lane]) + ":" + Bits(av[lane]) + ":" + Bits(bv[lane]));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector4 value;
                if (route == 0) value = Vector4.MinMagnitudeNumber(Operandc4(1, new Vector4(values[2])), Operandc4(2, new Vector4(values[6])));
                else if (route == 1) value = callback(Operandc4(1, new Vector4(values[2])), Operandc4(2, new Vector4(values[6])));
                else value = pointer(Operandc4(1, new Vector4(values[2])), Operandc4(2, new Vector4(values[6])));
                Check(value[0] == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "magnitude operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinMagnitudeNumberc4:direct-delegate-calli");
    }
}
