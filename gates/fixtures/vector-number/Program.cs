using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

internal static class Program
{
    private static int Order, ThrowAt;
    private static void Check(bool ok) { if (!ok) throw new InvalidOperationException("vector number mismatch"); }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Effect(int step)
    {
        Order = Order * 10 + step;
        if (ThrowAt == step) throw new InvalidOperationException("number operand");
    }
    private static string Bits(float value) => BitConverter.SingleToInt32Bits(value).ToString("X8");
    private static string Bits(double value) => BitConverter.DoubleToInt64Bits(value).ToString("X16");
    // An unreachable body proves the Diet axis rewrites the application.
    private static int DietRewriteSentinel() => 548246;
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        uint[] fb = { 0x00000000U, 0x80000000U, 0x3F800000U, 0xBF800000U, 0x3F800001U, 0xBF800001U, 0x40000000U, 0xC0000000U, 0x00000001U, 0x80000001U, 0x007FFFFFU, 0x807FFFFFU, 0x00800000U, 0x80800000U, 0x7F7FFFFFU, 0xFF7FFFFFU, 0x7F800000U, 0xFF800000U, 0x7FC12345U, 0xFFC54321U, 0x7FC54321U, 0xFFC12345U, 0x7F812345U, 0xFF854321U };
        ulong[] db = { 0x0000000000000000UL, 0x8000000000000000UL, 0x3FF0000000000000UL, 0xBFF0000000000000UL, 0x3FF0000000000001UL, 0xBFF0000000000001UL, 0x4000000000000000UL, 0xC000000000000000UL, 0x0000000000000001UL, 0x8000000000000001UL, 0x000FFFFFFFFFFFFFUL, 0x800FFFFFFFFFFFFFUL, 0x0010000000000000UL, 0x8010000000000000UL, 0x7FEFFFFFFFFFFFFFUL, 0xFFEFFFFFFFFFFFFFUL, 0x7FF0000000000000UL, 0xFFF0000000000000UL, 0x7FF8123456789ABCUL, 0xFFF8543212345678UL, 0x7FF8543212345678UL, 0xFFF8123456789ABCUL, 0x7FF0123456789ABCUL, 0xFFF0543212345678UL };
        var fa = new float[fb.Length]; var da = new double[db.Length];
        for (int i = 0; i < fb.Length; i++) fa[i] = BitConverter.Int32BitsToSingle(unchecked((int)fb[i]));
        for (int i = 0; i < db.Length; i++) da[i] = BitConverter.Int64BitsToDouble(unchecked((long)db[i]));
        TestMaxNumber64f(fa);
        TestMaxNumber64d(da);
        TestMaxNumber128f(fa);
        TestMaxNumber128d(da);
        TestMaxNumber256f(fa);
        TestMaxNumber256d(da);
        TestMaxNumber512f(fa);
        TestMaxNumber512d(da);
        TestMaxNumbernf(fa);
        TestMaxNumbernd(da);
        TestMaxNumberc2(fa);
        TestMaxNumberc3(fa);
        TestMaxNumberc4(fa);
        TestMinNumber64f(fa);
        TestMinNumber64d(da);
        TestMinNumber128f(fa);
        TestMinNumber128d(da);
        TestMinNumber256f(fa);
        TestMinNumber256d(da);
        TestMinNumber512f(fa);
        TestMinNumber512d(da);
        TestMinNumbernf(fa);
        TestMinNumbernd(da);
        TestMinNumberc2(fa);
        TestMinNumberc3(fa);
        TestMinNumberc4(fa);
        Console.WriteLine("vector-number:all-routes-passed");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector64<float> Operand64f(int step, Vector64<float> value) { Effect(step); return value; }
    private static unsafe void TestMaxNumber64f(float[] values)
    {
        Func<Vector64<float>, Vector64<float>, Vector64<float>> callback = Vector64.MaxNumber<float>;
        delegate* managed<Vector64<float>, Vector64<float>, Vector64<float>> pointer = &Vector64.MaxNumber<float>;
        Console.WriteLine("width:MaxNumber64f:" + Vector64<float>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[Vector64<float>.Count]; var bb = new float[Vector64<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector64.Create(aa); var bv = Vector64.Create(bb);
            var actual = Vector64.MaxNumber<float>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MaxNumber64f:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector64<float> value;
                if (route == 0) value = Vector64.MaxNumber<float>(Operand64f(1, Vector64.Create(values[2])), Operand64f(2, Vector64.Create(values[6])));
                else if (route == 1) value = callback(Operand64f(1, Vector64.Create(values[2])), Operand64f(2, Vector64.Create(values[6])));
                else value = pointer(Operand64f(1, Vector64.Create(values[2])), Operand64f(2, Vector64.Create(values[6])));
                Check(value.GetElement(0) == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "number operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxNumber64f:direct-delegate-calli");
    }
    private static unsafe void TestMinNumber64f(float[] values)
    {
        Func<Vector64<float>, Vector64<float>, Vector64<float>> callback = Vector64.MinNumber<float>;
        delegate* managed<Vector64<float>, Vector64<float>, Vector64<float>> pointer = &Vector64.MinNumber<float>;
        Console.WriteLine("width:MinNumber64f:" + Vector64<float>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[Vector64<float>.Count]; var bb = new float[Vector64<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector64.Create(aa); var bv = Vector64.Create(bb);
            var actual = Vector64.MinNumber<float>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MinNumber64f:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector64<float> value;
                if (route == 0) value = Vector64.MinNumber<float>(Operand64f(1, Vector64.Create(values[2])), Operand64f(2, Vector64.Create(values[6])));
                else if (route == 1) value = callback(Operand64f(1, Vector64.Create(values[2])), Operand64f(2, Vector64.Create(values[6])));
                else value = pointer(Operand64f(1, Vector64.Create(values[2])), Operand64f(2, Vector64.Create(values[6])));
                Check(value.GetElement(0) == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "number operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinNumber64f:direct-delegate-calli");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector64<double> Operand64d(int step, Vector64<double> value) { Effect(step); return value; }
    private static unsafe void TestMaxNumber64d(double[] values)
    {
        Func<Vector64<double>, Vector64<double>, Vector64<double>> callback = Vector64.MaxNumber<double>;
        delegate* managed<Vector64<double>, Vector64<double>, Vector64<double>> pointer = &Vector64.MaxNumber<double>;
        Console.WriteLine("width:MaxNumber64d:" + Vector64<double>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new double[Vector64<double>.Count]; var bb = new double[Vector64<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector64.Create(aa); var bv = Vector64.Create(bb);
            var actual = Vector64.MaxNumber<double>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MaxNumber64d:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector64<double> value;
                if (route == 0) value = Vector64.MaxNumber<double>(Operand64d(1, Vector64.Create(values[2])), Operand64d(2, Vector64.Create(values[6])));
                else if (route == 1) value = callback(Operand64d(1, Vector64.Create(values[2])), Operand64d(2, Vector64.Create(values[6])));
                else value = pointer(Operand64d(1, Vector64.Create(values[2])), Operand64d(2, Vector64.Create(values[6])));
                Check(value.GetElement(0) == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "number operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxNumber64d:direct-delegate-calli");
    }
    private static unsafe void TestMinNumber64d(double[] values)
    {
        Func<Vector64<double>, Vector64<double>, Vector64<double>> callback = Vector64.MinNumber<double>;
        delegate* managed<Vector64<double>, Vector64<double>, Vector64<double>> pointer = &Vector64.MinNumber<double>;
        Console.WriteLine("width:MinNumber64d:" + Vector64<double>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new double[Vector64<double>.Count]; var bb = new double[Vector64<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector64.Create(aa); var bv = Vector64.Create(bb);
            var actual = Vector64.MinNumber<double>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MinNumber64d:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector64<double> value;
                if (route == 0) value = Vector64.MinNumber<double>(Operand64d(1, Vector64.Create(values[2])), Operand64d(2, Vector64.Create(values[6])));
                else if (route == 1) value = callback(Operand64d(1, Vector64.Create(values[2])), Operand64d(2, Vector64.Create(values[6])));
                else value = pointer(Operand64d(1, Vector64.Create(values[2])), Operand64d(2, Vector64.Create(values[6])));
                Check(value.GetElement(0) == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "number operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinNumber64d:direct-delegate-calli");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<float> Operand128f(int step, Vector128<float> value) { Effect(step); return value; }
    private static unsafe void TestMaxNumber128f(float[] values)
    {
        Func<Vector128<float>, Vector128<float>, Vector128<float>> callback = Vector128.MaxNumber<float>;
        delegate* managed<Vector128<float>, Vector128<float>, Vector128<float>> pointer = &Vector128.MaxNumber<float>;
        Console.WriteLine("width:MaxNumber128f:" + Vector128<float>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[Vector128<float>.Count]; var bb = new float[Vector128<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector128.Create(aa); var bv = Vector128.Create(bb);
            var actual = Vector128.MaxNumber<float>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MaxNumber128f:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector128<float> value;
                if (route == 0) value = Vector128.MaxNumber<float>(Operand128f(1, Vector128.Create(values[2])), Operand128f(2, Vector128.Create(values[6])));
                else if (route == 1) value = callback(Operand128f(1, Vector128.Create(values[2])), Operand128f(2, Vector128.Create(values[6])));
                else value = pointer(Operand128f(1, Vector128.Create(values[2])), Operand128f(2, Vector128.Create(values[6])));
                Check(value.GetElement(0) == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "number operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxNumber128f:direct-delegate-calli");
    }
    private static unsafe void TestMinNumber128f(float[] values)
    {
        Func<Vector128<float>, Vector128<float>, Vector128<float>> callback = Vector128.MinNumber<float>;
        delegate* managed<Vector128<float>, Vector128<float>, Vector128<float>> pointer = &Vector128.MinNumber<float>;
        Console.WriteLine("width:MinNumber128f:" + Vector128<float>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[Vector128<float>.Count]; var bb = new float[Vector128<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector128.Create(aa); var bv = Vector128.Create(bb);
            var actual = Vector128.MinNumber<float>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MinNumber128f:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector128<float> value;
                if (route == 0) value = Vector128.MinNumber<float>(Operand128f(1, Vector128.Create(values[2])), Operand128f(2, Vector128.Create(values[6])));
                else if (route == 1) value = callback(Operand128f(1, Vector128.Create(values[2])), Operand128f(2, Vector128.Create(values[6])));
                else value = pointer(Operand128f(1, Vector128.Create(values[2])), Operand128f(2, Vector128.Create(values[6])));
                Check(value.GetElement(0) == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "number operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinNumber128f:direct-delegate-calli");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<double> Operand128d(int step, Vector128<double> value) { Effect(step); return value; }
    private static unsafe void TestMaxNumber128d(double[] values)
    {
        Func<Vector128<double>, Vector128<double>, Vector128<double>> callback = Vector128.MaxNumber<double>;
        delegate* managed<Vector128<double>, Vector128<double>, Vector128<double>> pointer = &Vector128.MaxNumber<double>;
        Console.WriteLine("width:MaxNumber128d:" + Vector128<double>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new double[Vector128<double>.Count]; var bb = new double[Vector128<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector128.Create(aa); var bv = Vector128.Create(bb);
            var actual = Vector128.MaxNumber<double>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MaxNumber128d:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector128<double> value;
                if (route == 0) value = Vector128.MaxNumber<double>(Operand128d(1, Vector128.Create(values[2])), Operand128d(2, Vector128.Create(values[6])));
                else if (route == 1) value = callback(Operand128d(1, Vector128.Create(values[2])), Operand128d(2, Vector128.Create(values[6])));
                else value = pointer(Operand128d(1, Vector128.Create(values[2])), Operand128d(2, Vector128.Create(values[6])));
                Check(value.GetElement(0) == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "number operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxNumber128d:direct-delegate-calli");
    }
    private static unsafe void TestMinNumber128d(double[] values)
    {
        Func<Vector128<double>, Vector128<double>, Vector128<double>> callback = Vector128.MinNumber<double>;
        delegate* managed<Vector128<double>, Vector128<double>, Vector128<double>> pointer = &Vector128.MinNumber<double>;
        Console.WriteLine("width:MinNumber128d:" + Vector128<double>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new double[Vector128<double>.Count]; var bb = new double[Vector128<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector128.Create(aa); var bv = Vector128.Create(bb);
            var actual = Vector128.MinNumber<double>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MinNumber128d:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector128<double> value;
                if (route == 0) value = Vector128.MinNumber<double>(Operand128d(1, Vector128.Create(values[2])), Operand128d(2, Vector128.Create(values[6])));
                else if (route == 1) value = callback(Operand128d(1, Vector128.Create(values[2])), Operand128d(2, Vector128.Create(values[6])));
                else value = pointer(Operand128d(1, Vector128.Create(values[2])), Operand128d(2, Vector128.Create(values[6])));
                Check(value.GetElement(0) == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "number operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinNumber128d:direct-delegate-calli");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector256<float> Operand256f(int step, Vector256<float> value) { Effect(step); return value; }
    private static unsafe void TestMaxNumber256f(float[] values)
    {
        Func<Vector256<float>, Vector256<float>, Vector256<float>> callback = Vector256.MaxNumber<float>;
        delegate* managed<Vector256<float>, Vector256<float>, Vector256<float>> pointer = &Vector256.MaxNumber<float>;
        Console.WriteLine("width:MaxNumber256f:" + Vector256<float>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[Vector256<float>.Count]; var bb = new float[Vector256<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector256.Create(aa); var bv = Vector256.Create(bb);
            var actual = Vector256.MaxNumber<float>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MaxNumber256f:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector256<float> value;
                if (route == 0) value = Vector256.MaxNumber<float>(Operand256f(1, Vector256.Create(values[2])), Operand256f(2, Vector256.Create(values[6])));
                else if (route == 1) value = callback(Operand256f(1, Vector256.Create(values[2])), Operand256f(2, Vector256.Create(values[6])));
                else value = pointer(Operand256f(1, Vector256.Create(values[2])), Operand256f(2, Vector256.Create(values[6])));
                Check(value.GetElement(0) == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "number operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxNumber256f:direct-delegate-calli");
    }
    private static unsafe void TestMinNumber256f(float[] values)
    {
        Func<Vector256<float>, Vector256<float>, Vector256<float>> callback = Vector256.MinNumber<float>;
        delegate* managed<Vector256<float>, Vector256<float>, Vector256<float>> pointer = &Vector256.MinNumber<float>;
        Console.WriteLine("width:MinNumber256f:" + Vector256<float>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[Vector256<float>.Count]; var bb = new float[Vector256<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector256.Create(aa); var bv = Vector256.Create(bb);
            var actual = Vector256.MinNumber<float>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MinNumber256f:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector256<float> value;
                if (route == 0) value = Vector256.MinNumber<float>(Operand256f(1, Vector256.Create(values[2])), Operand256f(2, Vector256.Create(values[6])));
                else if (route == 1) value = callback(Operand256f(1, Vector256.Create(values[2])), Operand256f(2, Vector256.Create(values[6])));
                else value = pointer(Operand256f(1, Vector256.Create(values[2])), Operand256f(2, Vector256.Create(values[6])));
                Check(value.GetElement(0) == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "number operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinNumber256f:direct-delegate-calli");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector256<double> Operand256d(int step, Vector256<double> value) { Effect(step); return value; }
    private static unsafe void TestMaxNumber256d(double[] values)
    {
        Func<Vector256<double>, Vector256<double>, Vector256<double>> callback = Vector256.MaxNumber<double>;
        delegate* managed<Vector256<double>, Vector256<double>, Vector256<double>> pointer = &Vector256.MaxNumber<double>;
        Console.WriteLine("width:MaxNumber256d:" + Vector256<double>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new double[Vector256<double>.Count]; var bb = new double[Vector256<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector256.Create(aa); var bv = Vector256.Create(bb);
            var actual = Vector256.MaxNumber<double>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MaxNumber256d:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector256<double> value;
                if (route == 0) value = Vector256.MaxNumber<double>(Operand256d(1, Vector256.Create(values[2])), Operand256d(2, Vector256.Create(values[6])));
                else if (route == 1) value = callback(Operand256d(1, Vector256.Create(values[2])), Operand256d(2, Vector256.Create(values[6])));
                else value = pointer(Operand256d(1, Vector256.Create(values[2])), Operand256d(2, Vector256.Create(values[6])));
                Check(value.GetElement(0) == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "number operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxNumber256d:direct-delegate-calli");
    }
    private static unsafe void TestMinNumber256d(double[] values)
    {
        Func<Vector256<double>, Vector256<double>, Vector256<double>> callback = Vector256.MinNumber<double>;
        delegate* managed<Vector256<double>, Vector256<double>, Vector256<double>> pointer = &Vector256.MinNumber<double>;
        Console.WriteLine("width:MinNumber256d:" + Vector256<double>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new double[Vector256<double>.Count]; var bb = new double[Vector256<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector256.Create(aa); var bv = Vector256.Create(bb);
            var actual = Vector256.MinNumber<double>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MinNumber256d:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector256<double> value;
                if (route == 0) value = Vector256.MinNumber<double>(Operand256d(1, Vector256.Create(values[2])), Operand256d(2, Vector256.Create(values[6])));
                else if (route == 1) value = callback(Operand256d(1, Vector256.Create(values[2])), Operand256d(2, Vector256.Create(values[6])));
                else value = pointer(Operand256d(1, Vector256.Create(values[2])), Operand256d(2, Vector256.Create(values[6])));
                Check(value.GetElement(0) == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "number operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinNumber256d:direct-delegate-calli");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector512<float> Operand512f(int step, Vector512<float> value) { Effect(step); return value; }
    private static unsafe void TestMaxNumber512f(float[] values)
    {
        Func<Vector512<float>, Vector512<float>, Vector512<float>> callback = Vector512.MaxNumber<float>;
        delegate* managed<Vector512<float>, Vector512<float>, Vector512<float>> pointer = &Vector512.MaxNumber<float>;
        Console.WriteLine("width:MaxNumber512f:" + Vector512<float>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[Vector512<float>.Count]; var bb = new float[Vector512<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector512.Create(aa); var bv = Vector512.Create(bb);
            var actual = Vector512.MaxNumber<float>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MaxNumber512f:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector512<float> value;
                if (route == 0) value = Vector512.MaxNumber<float>(Operand512f(1, Vector512.Create(values[2])), Operand512f(2, Vector512.Create(values[6])));
                else if (route == 1) value = callback(Operand512f(1, Vector512.Create(values[2])), Operand512f(2, Vector512.Create(values[6])));
                else value = pointer(Operand512f(1, Vector512.Create(values[2])), Operand512f(2, Vector512.Create(values[6])));
                Check(value.GetElement(0) == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "number operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxNumber512f:direct-delegate-calli");
    }
    private static unsafe void TestMinNumber512f(float[] values)
    {
        Func<Vector512<float>, Vector512<float>, Vector512<float>> callback = Vector512.MinNumber<float>;
        delegate* managed<Vector512<float>, Vector512<float>, Vector512<float>> pointer = &Vector512.MinNumber<float>;
        Console.WriteLine("width:MinNumber512f:" + Vector512<float>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[Vector512<float>.Count]; var bb = new float[Vector512<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector512.Create(aa); var bv = Vector512.Create(bb);
            var actual = Vector512.MinNumber<float>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MinNumber512f:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector512<float> value;
                if (route == 0) value = Vector512.MinNumber<float>(Operand512f(1, Vector512.Create(values[2])), Operand512f(2, Vector512.Create(values[6])));
                else if (route == 1) value = callback(Operand512f(1, Vector512.Create(values[2])), Operand512f(2, Vector512.Create(values[6])));
                else value = pointer(Operand512f(1, Vector512.Create(values[2])), Operand512f(2, Vector512.Create(values[6])));
                Check(value.GetElement(0) == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "number operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinNumber512f:direct-delegate-calli");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector512<double> Operand512d(int step, Vector512<double> value) { Effect(step); return value; }
    private static unsafe void TestMaxNumber512d(double[] values)
    {
        Func<Vector512<double>, Vector512<double>, Vector512<double>> callback = Vector512.MaxNumber<double>;
        delegate* managed<Vector512<double>, Vector512<double>, Vector512<double>> pointer = &Vector512.MaxNumber<double>;
        Console.WriteLine("width:MaxNumber512d:" + Vector512<double>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new double[Vector512<double>.Count]; var bb = new double[Vector512<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector512.Create(aa); var bv = Vector512.Create(bb);
            var actual = Vector512.MaxNumber<double>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MaxNumber512d:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector512<double> value;
                if (route == 0) value = Vector512.MaxNumber<double>(Operand512d(1, Vector512.Create(values[2])), Operand512d(2, Vector512.Create(values[6])));
                else if (route == 1) value = callback(Operand512d(1, Vector512.Create(values[2])), Operand512d(2, Vector512.Create(values[6])));
                else value = pointer(Operand512d(1, Vector512.Create(values[2])), Operand512d(2, Vector512.Create(values[6])));
                Check(value.GetElement(0) == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "number operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxNumber512d:direct-delegate-calli");
    }
    private static unsafe void TestMinNumber512d(double[] values)
    {
        Func<Vector512<double>, Vector512<double>, Vector512<double>> callback = Vector512.MinNumber<double>;
        delegate* managed<Vector512<double>, Vector512<double>, Vector512<double>> pointer = &Vector512.MinNumber<double>;
        Console.WriteLine("width:MinNumber512d:" + Vector512<double>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new double[Vector512<double>.Count]; var bb = new double[Vector512<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = Vector512.Create(aa); var bv = Vector512.Create(bb);
            var actual = Vector512.MinNumber<double>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MinNumber512d:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual.GetElement(lane)) + ":" + Bits(delegated.GetElement(lane)) + ":" + Bits(indirect.GetElement(lane)) + ":" + Bits(av.GetElement(lane)) + ":" + Bits(bv.GetElement(lane)));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector512<double> value;
                if (route == 0) value = Vector512.MinNumber<double>(Operand512d(1, Vector512.Create(values[2])), Operand512d(2, Vector512.Create(values[6])));
                else if (route == 1) value = callback(Operand512d(1, Vector512.Create(values[2])), Operand512d(2, Vector512.Create(values[6])));
                else value = pointer(Operand512d(1, Vector512.Create(values[2])), Operand512d(2, Vector512.Create(values[6])));
                Check(value.GetElement(0) == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "number operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinNumber512d:direct-delegate-calli");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<float> Operandnf(int step, Vector<float> value) { Effect(step); return value; }
    private static unsafe void TestMaxNumbernf(float[] values)
    {
        Func<Vector<float>, Vector<float>, Vector<float>> callback = Vector.MaxNumber<float>;
        delegate* managed<Vector<float>, Vector<float>, Vector<float>> pointer = &Vector.MaxNumber<float>;
        Console.WriteLine("width:MaxNumbernf:" + Vector<float>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[Vector<float>.Count]; var bb = new float[Vector<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = new Vector<float>(aa); var bv = new Vector<float>(bb);
            var actual = Vector.MaxNumber<float>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MaxNumbernf:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual[lane]) + ":" + Bits(delegated[lane]) + ":" + Bits(indirect[lane]) + ":" + Bits(av[lane]) + ":" + Bits(bv[lane]));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector<float> value;
                if (route == 0) value = Vector.MaxNumber<float>(Operandnf(1, new Vector<float>(values[2])), Operandnf(2, new Vector<float>(values[6])));
                else if (route == 1) value = callback(Operandnf(1, new Vector<float>(values[2])), Operandnf(2, new Vector<float>(values[6])));
                else value = pointer(Operandnf(1, new Vector<float>(values[2])), Operandnf(2, new Vector<float>(values[6])));
                Check(value[0] == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "number operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxNumbernf:direct-delegate-calli");
    }
    private static unsafe void TestMinNumbernf(float[] values)
    {
        Func<Vector<float>, Vector<float>, Vector<float>> callback = Vector.MinNumber<float>;
        delegate* managed<Vector<float>, Vector<float>, Vector<float>> pointer = &Vector.MinNumber<float>;
        Console.WriteLine("width:MinNumbernf:" + Vector<float>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[Vector<float>.Count]; var bb = new float[Vector<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = new Vector<float>(aa); var bv = new Vector<float>(bb);
            var actual = Vector.MinNumber<float>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MinNumbernf:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual[lane]) + ":" + Bits(delegated[lane]) + ":" + Bits(indirect[lane]) + ":" + Bits(av[lane]) + ":" + Bits(bv[lane]));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector<float> value;
                if (route == 0) value = Vector.MinNumber<float>(Operandnf(1, new Vector<float>(values[2])), Operandnf(2, new Vector<float>(values[6])));
                else if (route == 1) value = callback(Operandnf(1, new Vector<float>(values[2])), Operandnf(2, new Vector<float>(values[6])));
                else value = pointer(Operandnf(1, new Vector<float>(values[2])), Operandnf(2, new Vector<float>(values[6])));
                Check(value[0] == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "number operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinNumbernf:direct-delegate-calli");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<double> Operandnd(int step, Vector<double> value) { Effect(step); return value; }
    private static unsafe void TestMaxNumbernd(double[] values)
    {
        Func<Vector<double>, Vector<double>, Vector<double>> callback = Vector.MaxNumber<double>;
        delegate* managed<Vector<double>, Vector<double>, Vector<double>> pointer = &Vector.MaxNumber<double>;
        Console.WriteLine("width:MaxNumbernd:" + Vector<double>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new double[Vector<double>.Count]; var bb = new double[Vector<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = new Vector<double>(aa); var bv = new Vector<double>(bb);
            var actual = Vector.MaxNumber<double>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MaxNumbernd:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual[lane]) + ":" + Bits(delegated[lane]) + ":" + Bits(indirect[lane]) + ":" + Bits(av[lane]) + ":" + Bits(bv[lane]));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector<double> value;
                if (route == 0) value = Vector.MaxNumber<double>(Operandnd(1, new Vector<double>(values[2])), Operandnd(2, new Vector<double>(values[6])));
                else if (route == 1) value = callback(Operandnd(1, new Vector<double>(values[2])), Operandnd(2, new Vector<double>(values[6])));
                else value = pointer(Operandnd(1, new Vector<double>(values[2])), Operandnd(2, new Vector<double>(values[6])));
                Check(value[0] == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "number operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxNumbernd:direct-delegate-calli");
    }
    private static unsafe void TestMinNumbernd(double[] values)
    {
        Func<Vector<double>, Vector<double>, Vector<double>> callback = Vector.MinNumber<double>;
        delegate* managed<Vector<double>, Vector<double>, Vector<double>> pointer = &Vector.MinNumber<double>;
        Console.WriteLine("width:MinNumbernd:" + Vector<double>.Count);
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new double[Vector<double>.Count]; var bb = new double[Vector<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = new Vector<double>(aa); var bv = new Vector<double>(bb);
            var actual = Vector.MinNumber<double>(av, bv);
            var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < aa.Length; lane++)
                Console.WriteLine("MinNumbernd:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual[lane]) + ":" + Bits(delegated[lane]) + ":" + Bits(indirect[lane]) + ":" + Bits(av[lane]) + ":" + Bits(bv[lane]));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector<double> value;
                if (route == 0) value = Vector.MinNumber<double>(Operandnd(1, new Vector<double>(values[2])), Operandnd(2, new Vector<double>(values[6])));
                else if (route == 1) value = callback(Operandnd(1, new Vector<double>(values[2])), Operandnd(2, new Vector<double>(values[6])));
                else value = pointer(Operandnd(1, new Vector<double>(values[2])), Operandnd(2, new Vector<double>(values[6])));
                Check(value[0] == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "number operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinNumbernd:direct-delegate-calli");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector2 Operandc2(int step, Vector2 value) { Effect(step); return value; }
    private static unsafe void TestMaxNumberc2(float[] values)
    {
        Func<Vector2, Vector2, Vector2> callback = Vector2.MaxNumber;
        delegate* managed<Vector2, Vector2, Vector2> pointer = &Vector2.MaxNumber;
        Console.WriteLine("width:MaxNumberc2:2");
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[2]; var bb = new float[2];
            for (int lane = 0; lane < 2; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = new Vector2(aa[0], aa[1]); var bv = new Vector2(bb[0], bb[1]);
            var actual = Vector2.MaxNumber(av, bv); var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < 2; lane++)
                Console.WriteLine("MaxNumberc2:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual[lane]) + ":" + Bits(delegated[lane]) + ":" + Bits(indirect[lane]) + ":" + Bits(av[lane]) + ":" + Bits(bv[lane]));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector2 value;
                if (route == 0) value = Vector2.MaxNumber(Operandc2(1, new Vector2(values[2])), Operandc2(2, new Vector2(values[6])));
                else if (route == 1) value = callback(Operandc2(1, new Vector2(values[2])), Operandc2(2, new Vector2(values[6])));
                else value = pointer(Operandc2(1, new Vector2(values[2])), Operandc2(2, new Vector2(values[6])));
                Check(value[0] == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "number operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxNumberc2:direct-delegate-calli");
    }
    private static unsafe void TestMinNumberc2(float[] values)
    {
        Func<Vector2, Vector2, Vector2> callback = Vector2.MinNumber;
        delegate* managed<Vector2, Vector2, Vector2> pointer = &Vector2.MinNumber;
        Console.WriteLine("width:MinNumberc2:2");
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[2]; var bb = new float[2];
            for (int lane = 0; lane < 2; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = new Vector2(aa[0], aa[1]); var bv = new Vector2(bb[0], bb[1]);
            var actual = Vector2.MinNumber(av, bv); var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < 2; lane++)
                Console.WriteLine("MinNumberc2:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual[lane]) + ":" + Bits(delegated[lane]) + ":" + Bits(indirect[lane]) + ":" + Bits(av[lane]) + ":" + Bits(bv[lane]));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector2 value;
                if (route == 0) value = Vector2.MinNumber(Operandc2(1, new Vector2(values[2])), Operandc2(2, new Vector2(values[6])));
                else if (route == 1) value = callback(Operandc2(1, new Vector2(values[2])), Operandc2(2, new Vector2(values[6])));
                else value = pointer(Operandc2(1, new Vector2(values[2])), Operandc2(2, new Vector2(values[6])));
                Check(value[0] == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "number operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinNumberc2:direct-delegate-calli");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector3 Operandc3(int step, Vector3 value) { Effect(step); return value; }
    private static unsafe void TestMaxNumberc3(float[] values)
    {
        Func<Vector3, Vector3, Vector3> callback = Vector3.MaxNumber;
        delegate* managed<Vector3, Vector3, Vector3> pointer = &Vector3.MaxNumber;
        Console.WriteLine("width:MaxNumberc3:3");
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[3]; var bb = new float[3];
            for (int lane = 0; lane < 3; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = new Vector3(aa[0], aa[1], aa[2]); var bv = new Vector3(bb[0], bb[1], bb[2]);
            var actual = Vector3.MaxNumber(av, bv); var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < 3; lane++)
                Console.WriteLine("MaxNumberc3:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual[lane]) + ":" + Bits(delegated[lane]) + ":" + Bits(indirect[lane]) + ":" + Bits(av[lane]) + ":" + Bits(bv[lane]));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector3 value;
                if (route == 0) value = Vector3.MaxNumber(Operandc3(1, new Vector3(values[2])), Operandc3(2, new Vector3(values[6])));
                else if (route == 1) value = callback(Operandc3(1, new Vector3(values[2])), Operandc3(2, new Vector3(values[6])));
                else value = pointer(Operandc3(1, new Vector3(values[2])), Operandc3(2, new Vector3(values[6])));
                Check(value[0] == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "number operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxNumberc3:direct-delegate-calli");
    }
    private static unsafe void TestMinNumberc3(float[] values)
    {
        Func<Vector3, Vector3, Vector3> callback = Vector3.MinNumber;
        delegate* managed<Vector3, Vector3, Vector3> pointer = &Vector3.MinNumber;
        Console.WriteLine("width:MinNumberc3:3");
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[3]; var bb = new float[3];
            for (int lane = 0; lane < 3; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = new Vector3(aa[0], aa[1], aa[2]); var bv = new Vector3(bb[0], bb[1], bb[2]);
            var actual = Vector3.MinNumber(av, bv); var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < 3; lane++)
                Console.WriteLine("MinNumberc3:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual[lane]) + ":" + Bits(delegated[lane]) + ":" + Bits(indirect[lane]) + ":" + Bits(av[lane]) + ":" + Bits(bv[lane]));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector3 value;
                if (route == 0) value = Vector3.MinNumber(Operandc3(1, new Vector3(values[2])), Operandc3(2, new Vector3(values[6])));
                else if (route == 1) value = callback(Operandc3(1, new Vector3(values[2])), Operandc3(2, new Vector3(values[6])));
                else value = pointer(Operandc3(1, new Vector3(values[2])), Operandc3(2, new Vector3(values[6])));
                Check(value[0] == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "number operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinNumberc3:direct-delegate-calli");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector4 Operandc4(int step, Vector4 value) { Effect(step); return value; }
    private static unsafe void TestMaxNumberc4(float[] values)
    {
        Func<Vector4, Vector4, Vector4> callback = Vector4.MaxNumber;
        delegate* managed<Vector4, Vector4, Vector4> pointer = &Vector4.MaxNumber;
        Console.WriteLine("width:MaxNumberc4:4");
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[4]; var bb = new float[4];
            for (int lane = 0; lane < 4; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = new Vector4(aa[0], aa[1], aa[2], aa[3]); var bv = new Vector4(bb[0], bb[1], bb[2], bb[3]);
            var actual = Vector4.MaxNumber(av, bv); var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < 4; lane++)
                Console.WriteLine("MaxNumberc4:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual[lane]) + ":" + Bits(delegated[lane]) + ":" + Bits(indirect[lane]) + ":" + Bits(av[lane]) + ":" + Bits(bv[lane]));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector4 value;
                if (route == 0) value = Vector4.MaxNumber(Operandc4(1, new Vector4(values[2])), Operandc4(2, new Vector4(values[6])));
                else if (route == 1) value = callback(Operandc4(1, new Vector4(values[2])), Operandc4(2, new Vector4(values[6])));
                else value = pointer(Operandc4(1, new Vector4(values[2])), Operandc4(2, new Vector4(values[6])));
                Check(value[0] == 2);
            }
            catch (InvalidOperationException error) when (error.Message == "number operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MaxNumberc4:direct-delegate-calli");
    }
    private static unsafe void TestMinNumberc4(float[] values)
    {
        Func<Vector4, Vector4, Vector4> callback = Vector4.MinNumber;
        delegate* managed<Vector4, Vector4, Vector4> pointer = &Vector4.MinNumber;
        Console.WriteLine("width:MinNumberc4:4");
        for (int test = 0; test < values.Length * values.Length; test++)
        {
            var aa = new float[4]; var bb = new float[4];
            for (int lane = 0; lane < 4; lane++)
            { int pair = (test + lane) % (values.Length * values.Length); aa[lane] = values[pair / values.Length]; bb[lane] = values[pair % values.Length]; }
            var av = new Vector4(aa[0], aa[1], aa[2], aa[3]); var bv = new Vector4(bb[0], bb[1], bb[2], bb[3]);
            var actual = Vector4.MinNumber(av, bv); var delegated = callback(av, bv); var indirect = pointer(av, bv);
            for (int lane = 0; lane < 4; lane++)
                Console.WriteLine("MinNumberc4:" + test + ":" + lane + ":" + Bits(aa[lane]) + ":" + Bits(bb[lane]) + ":" + Bits(actual[lane]) + ":" + Bits(delegated[lane]) + ":" + Bits(indirect[lane]) + ":" + Bits(av[lane]) + ":" + Bits(bv[lane]));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 2; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector4 value;
                if (route == 0) value = Vector4.MinNumber(Operandc4(1, new Vector4(values[2])), Operandc4(2, new Vector4(values[6])));
                else if (route == 1) value = callback(Operandc4(1, new Vector4(values[2])), Operandc4(2, new Vector4(values[6])));
                else value = pointer(Operandc4(1, new Vector4(values[2])), Operandc4(2, new Vector4(values[6])));
                Check(value[0] == 1);
            }
            catch (InvalidOperationException error) when (error.Message == "number operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : 12));
        }
        ThrowAt = 0; Console.WriteLine("order:MinNumberc4:direct-delegate-calli");
    }
}
