using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

internal static class Program
{
    private static int Order, ThrowAt;
    private static void Check(bool ok) { if (!ok) throw new InvalidOperationException("vector lerp mismatch"); }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Effect(int step)
    {
        Order = Order * 10 + step;
        if (ThrowAt == step) throw new InvalidOperationException("lerp operand");
    }
    // CLR reference intermediates stay separate. Native optimizers may remove these
    // identity calls; native oracle columns are diagnostic, not expected results.
    [MethodImpl(MethodImplOptions.NoInlining)] private static float Round(float value) => value;
    [MethodImpl(MethodImplOptions.NoInlining)] private static double Round(double value) => value;
    private static float Separate(float a, float b, float t) => Round(Round(a * Round(1.0f - t)) + Round(b * t));
    private static double Separate(double a, double b, double t) => Round(Round(a * Round(1.0 - t)) + Round(b * t));
    private static float Fused(float a, float b, float t) => MathF.FusedMultiplyAdd(a, Round(1.0f - t), Round(b * t));
    private static double Fused(double a, double b, double t) => Math.FusedMultiplyAdd(a, Round(1.0 - t), Round(b * t));
    private static bool Same(float a, float b) => BitConverter.SingleToInt32Bits(a) == BitConverter.SingleToInt32Bits(b) || float.IsNaN(a) && float.IsNaN(b);
    private static bool Same(double a, double b) => BitConverter.DoubleToInt64Bits(a) == BitConverter.DoubleToInt64Bits(b) || double.IsNaN(a) && double.IsNaN(b);
    private static string Bits(float value) => BitConverter.SingleToInt32Bits(value).ToString("X8");
    private static string Bits(double value) => BitConverter.DoubleToInt64Bits(value).ToString("X16");
    private static float[] Singles(uint[] bits)
    {
        var values = new float[bits.Length];
        for (int i = 0; i < bits.Length; i++) values[i] = BitConverter.Int32BitsToSingle(unchecked((int)bits[i]));
        return values;
    }
    private static double[] Doubles(ulong[] bits)
    {
        var values = new double[bits.Length];
        for (int i = 0; i < bits.Length; i++) values[i] = BitConverter.Int64BitsToDouble(unchecked((long)bits[i]));
        return values;
    }
    // Unreached application IL must disappear from an actual Diet rewrite.
    private static int DietRewriteSentinel() => 543246;
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        var fa = Singles(new uint[] { 0x40000000U, 0x3F800001U, 0x7F7FFFFFU, 0x00000001U, 0x00800000U, 0x80000000U, 0x00000000U, 0x7F800000U, 0xFF800000U, 0x7FC12345U, 0x7F812345U, 0x3F800000U, 0xBF800000U, 0x3F800001U, 0x7F7FFFFFU, 0xFF7FFFFFU, 0x3F000000U, 0x3F800000U, 0x80000001U, 0x3F7FFFFFU, 0x00800000U, 0x80800000U, 0x40000000U, 0x40000000U });
        var fb = Singles(new uint[] { 0x40800000U, 0xBF800000U, 0xFF7FFFFFU, 0x80000001U, 0x00000000U, 0x80000000U, 0x80000000U, 0x3F800000U, 0x7F800000U, 0x3F800000U, 0xFFC12345U, 0x7FC54321U, 0x7F812345U, 0xBF800001U, 0x7F7FFFFFU, 0xFF7FFFFFU, 0x3F800000U, 0x00000000U, 0x00000001U, 0x3F800001U, 0x80000001U, 0x00000001U, 0x40800000U, 0x40800000U });
        var ft = Singles(new uint[] { 0x3F000000U, 0x3F000001U, 0x3F000000U, 0x3F000000U, 0x3F7FFFFFU, 0x80000000U, 0x00000000U, 0x3F800000U, 0x00000000U, 0x3F000000U, 0x3F800000U, 0x00000000U, 0x3F000000U, 0x3F7FFFFFU, 0xBF800000U, 0x40000000U, 0x3EAAAAABU, 0x7F800000U, 0x3F000000U, 0x33800000U, 0x3F000000U, 0x3F000000U, 0xBF800000U, 0x40000000U });
        var da = Doubles(new ulong[] { 0x4000000000000000UL, 0x3FF0000000000001UL, 0x7FEFFFFFFFFFFFFFUL, 0x0000000000000001UL, 0x0010000000000000UL, 0x8000000000000000UL, 0x0000000000000000UL, 0x7FF0000000000000UL, 0xFFF0000000000000UL, 0x7FF8123456789ABCUL, 0x7FF0123456789ABCUL, 0x3FF0000000000000UL, 0xBFF0000000000000UL, 0x3FF0000000000001UL, 0x7FEFFFFFFFFFFFFFUL, 0xFFEFFFFFFFFFFFFFUL, 0x3FE0000000000000UL, 0x3FF0000000000000UL, 0x8000000000000001UL, 0x3FEFFFFFFFFFFFFFUL, 0x0010000000000000UL, 0x8010000000000000UL, 0x4000000000000000UL, 0x4000000000000000UL });
        var db = Doubles(new ulong[] { 0x4010000000000000UL, 0xBFF0000000000000UL, 0xFFEFFFFFFFFFFFFFUL, 0x8000000000000001UL, 0x0000000000000000UL, 0x8000000000000000UL, 0x8000000000000000UL, 0x3FF0000000000000UL, 0x7FF0000000000000UL, 0x3FF0000000000000UL, 0xFFF8543212345678UL, 0x7FF8543212345678UL, 0x7FF0123456789ABCUL, 0xBFF0000000000001UL, 0x7FEFFFFFFFFFFFFFUL, 0xFFEFFFFFFFFFFFFFUL, 0x3FF0000000000000UL, 0x0000000000000000UL, 0x0000000000000001UL, 0x3FF0000000000001UL, 0x8000000000000001UL, 0x0000000000000001UL, 0x4010000000000000UL, 0x4010000000000000UL });
        var dt = Doubles(new ulong[] { 0x3FE0000000000000UL, 0x3FE0000000000001UL, 0x3FE0000000000000UL, 0x3FE0000000000000UL, 0x3FEFFFFFFFFFFFFFUL, 0x8000000000000000UL, 0x0000000000000000UL, 0x3FF0000000000000UL, 0x0000000000000000UL, 0x3FE0000000000000UL, 0x3FF0000000000000UL, 0x0000000000000000UL, 0x3FE0000000000000UL, 0x3FEFFFFFFFFFFFFFUL, 0xBFF0000000000000UL, 0x4000000000000000UL, 0x3FD5555555555555UL, 0x7FF0000000000000UL, 0x3FE0000000000000UL, 0x3CA0000000000000UL, 0x3FE0000000000000UL, 0x3FE0000000000000UL, 0xBFF0000000000000UL, 0x4000000000000000UL });
        Test64f(fa, fb, ft);
        Test64d(da, db, dt);
        Test128f(fa, fb, ft);
        Test128d(da, db, dt);
        Test256f(fa, fb, ft);
        Test256d(da, db, dt);
        Test512f(fa, fb, ft);
        Test512d(da, db, dt);
        Testnf(fa, fb, ft);
        Testnd(da, db, dt);
        Concrete(fa, fb, ft);
        Console.WriteLine("vector-lerp:all-ten-routes-and-concrete-passed");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector64<float> Operand64f(int step, Vector64<float> value) { Effect(step); return value; }
    private static unsafe void Test64f(float[] a, float[] b, float[] t)
    {
        Func<Vector64<float>, Vector64<float>, Vector64<float>, Vector64<float>> callback = Vector64.Lerp;
        delegate* managed<Vector64<float>, Vector64<float>, Vector64<float>, Vector64<float>> pointer = &Vector64.Lerp;
        for (int test = 0; test < a.Length; test++)
        {
            var aa = new float[Vector64<float>.Count]; var bb = new float[Vector64<float>.Count]; var tt = new float[Vector64<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int k = (test + lane) % a.Length; aa[lane] = a[k]; bb[lane] = b[k]; tt[lane] = t[k]; }
            var av = Vector64.Create(aa); var bv = Vector64.Create(bb); var tv = Vector64.Create(tt);
            var actual = Vector64.Lerp(av, bv, tv);
            var delegated = callback(av, bv, tv); var indirect = pointer(av, bv, tv);
            for (int lane = 0; lane < aa.Length; lane++)
            {
                var value = actual.GetElement(lane);
                Check(Same(value, delegated.GetElement(lane)) && Same(value, indirect.GetElement(lane)));
                var separate = Separate(aa[lane], bb[lane], tt[lane]); var fused = Fused(aa[lane], bb[lane], tt[lane]);
                Console.WriteLine("64f:" + test + ":" + lane + ":" + Bits(value) + ":" + Bits(separate) + ":" + Bits(fused));
            }
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 3; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector64<float> value;
                if (route == 0) value = Vector64.Lerp(Operand64f(1, Vector64.Create(a[0])), Operand64f(2, Vector64.Create(b[0])), Operand64f(3, Vector64.Create(t[0])));
                else if (route == 1) value = callback(Operand64f(1, Vector64.Create(a[0])), Operand64f(2, Vector64.Create(b[0])), Operand64f(3, Vector64.Create(t[0])));
                else value = pointer(Operand64f(1, Vector64.Create(a[0])), Operand64f(2, Vector64.Create(b[0])), Operand64f(3, Vector64.Create(t[0])));
                Check(value.GetElement(0) == 3);
            }
            catch (InvalidOperationException error) when (error.Message == "lerp operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : fail == 2 ? 12 : 123));
        }
        ThrowAt = 0; Console.WriteLine("order:64f:direct-delegate-calli");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector64<double> Operand64d(int step, Vector64<double> value) { Effect(step); return value; }
    private static unsafe void Test64d(double[] a, double[] b, double[] t)
    {
        Func<Vector64<double>, Vector64<double>, Vector64<double>, Vector64<double>> callback = Vector64.Lerp;
        delegate* managed<Vector64<double>, Vector64<double>, Vector64<double>, Vector64<double>> pointer = &Vector64.Lerp;
        for (int test = 0; test < a.Length; test++)
        {
            var aa = new double[Vector64<double>.Count]; var bb = new double[Vector64<double>.Count]; var tt = new double[Vector64<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int k = (test + lane) % a.Length; aa[lane] = a[k]; bb[lane] = b[k]; tt[lane] = t[k]; }
            var av = Vector64.Create(aa); var bv = Vector64.Create(bb); var tv = Vector64.Create(tt);
            var actual = Vector64.Lerp(av, bv, tv);
            var delegated = callback(av, bv, tv); var indirect = pointer(av, bv, tv);
            for (int lane = 0; lane < aa.Length; lane++)
            {
                var value = actual.GetElement(lane);
                Check(Same(value, delegated.GetElement(lane)) && Same(value, indirect.GetElement(lane)));
                var separate = Separate(aa[lane], bb[lane], tt[lane]); var fused = Fused(aa[lane], bb[lane], tt[lane]);
                Console.WriteLine("64d:" + test + ":" + lane + ":" + Bits(value) + ":" + Bits(separate) + ":" + Bits(fused));
            }
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 3; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector64<double> value;
                if (route == 0) value = Vector64.Lerp(Operand64d(1, Vector64.Create(a[0])), Operand64d(2, Vector64.Create(b[0])), Operand64d(3, Vector64.Create(t[0])));
                else if (route == 1) value = callback(Operand64d(1, Vector64.Create(a[0])), Operand64d(2, Vector64.Create(b[0])), Operand64d(3, Vector64.Create(t[0])));
                else value = pointer(Operand64d(1, Vector64.Create(a[0])), Operand64d(2, Vector64.Create(b[0])), Operand64d(3, Vector64.Create(t[0])));
                Check(value.GetElement(0) == 3);
            }
            catch (InvalidOperationException error) when (error.Message == "lerp operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : fail == 2 ? 12 : 123));
        }
        ThrowAt = 0; Console.WriteLine("order:64d:direct-delegate-calli");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<float> Operand128f(int step, Vector128<float> value) { Effect(step); return value; }
    private static unsafe void Test128f(float[] a, float[] b, float[] t)
    {
        Func<Vector128<float>, Vector128<float>, Vector128<float>, Vector128<float>> callback = Vector128.Lerp;
        delegate* managed<Vector128<float>, Vector128<float>, Vector128<float>, Vector128<float>> pointer = &Vector128.Lerp;
        for (int test = 0; test < a.Length; test++)
        {
            var aa = new float[Vector128<float>.Count]; var bb = new float[Vector128<float>.Count]; var tt = new float[Vector128<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int k = (test + lane) % a.Length; aa[lane] = a[k]; bb[lane] = b[k]; tt[lane] = t[k]; }
            var av = Vector128.Create(aa); var bv = Vector128.Create(bb); var tv = Vector128.Create(tt);
            var actual = Vector128.Lerp(av, bv, tv);
            var delegated = callback(av, bv, tv); var indirect = pointer(av, bv, tv);
            for (int lane = 0; lane < aa.Length; lane++)
            {
                var value = actual.GetElement(lane);
                Check(Same(value, delegated.GetElement(lane)) && Same(value, indirect.GetElement(lane)));
                var separate = Separate(aa[lane], bb[lane], tt[lane]); var fused = Fused(aa[lane], bb[lane], tt[lane]);
                Console.WriteLine("128f:" + test + ":" + lane + ":" + Bits(value) + ":" + Bits(separate) + ":" + Bits(fused));
            }
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 3; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector128<float> value;
                if (route == 0) value = Vector128.Lerp(Operand128f(1, Vector128.Create(a[0])), Operand128f(2, Vector128.Create(b[0])), Operand128f(3, Vector128.Create(t[0])));
                else if (route == 1) value = callback(Operand128f(1, Vector128.Create(a[0])), Operand128f(2, Vector128.Create(b[0])), Operand128f(3, Vector128.Create(t[0])));
                else value = pointer(Operand128f(1, Vector128.Create(a[0])), Operand128f(2, Vector128.Create(b[0])), Operand128f(3, Vector128.Create(t[0])));
                Check(value.GetElement(0) == 3);
            }
            catch (InvalidOperationException error) when (error.Message == "lerp operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : fail == 2 ? 12 : 123));
        }
        ThrowAt = 0; Console.WriteLine("order:128f:direct-delegate-calli");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<double> Operand128d(int step, Vector128<double> value) { Effect(step); return value; }
    private static unsafe void Test128d(double[] a, double[] b, double[] t)
    {
        Func<Vector128<double>, Vector128<double>, Vector128<double>, Vector128<double>> callback = Vector128.Lerp;
        delegate* managed<Vector128<double>, Vector128<double>, Vector128<double>, Vector128<double>> pointer = &Vector128.Lerp;
        for (int test = 0; test < a.Length; test++)
        {
            var aa = new double[Vector128<double>.Count]; var bb = new double[Vector128<double>.Count]; var tt = new double[Vector128<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int k = (test + lane) % a.Length; aa[lane] = a[k]; bb[lane] = b[k]; tt[lane] = t[k]; }
            var av = Vector128.Create(aa); var bv = Vector128.Create(bb); var tv = Vector128.Create(tt);
            var actual = Vector128.Lerp(av, bv, tv);
            var delegated = callback(av, bv, tv); var indirect = pointer(av, bv, tv);
            for (int lane = 0; lane < aa.Length; lane++)
            {
                var value = actual.GetElement(lane);
                Check(Same(value, delegated.GetElement(lane)) && Same(value, indirect.GetElement(lane)));
                var separate = Separate(aa[lane], bb[lane], tt[lane]); var fused = Fused(aa[lane], bb[lane], tt[lane]);
                Console.WriteLine("128d:" + test + ":" + lane + ":" + Bits(value) + ":" + Bits(separate) + ":" + Bits(fused));
            }
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 3; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector128<double> value;
                if (route == 0) value = Vector128.Lerp(Operand128d(1, Vector128.Create(a[0])), Operand128d(2, Vector128.Create(b[0])), Operand128d(3, Vector128.Create(t[0])));
                else if (route == 1) value = callback(Operand128d(1, Vector128.Create(a[0])), Operand128d(2, Vector128.Create(b[0])), Operand128d(3, Vector128.Create(t[0])));
                else value = pointer(Operand128d(1, Vector128.Create(a[0])), Operand128d(2, Vector128.Create(b[0])), Operand128d(3, Vector128.Create(t[0])));
                Check(value.GetElement(0) == 3);
            }
            catch (InvalidOperationException error) when (error.Message == "lerp operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : fail == 2 ? 12 : 123));
        }
        ThrowAt = 0; Console.WriteLine("order:128d:direct-delegate-calli");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector256<float> Operand256f(int step, Vector256<float> value) { Effect(step); return value; }
    private static unsafe void Test256f(float[] a, float[] b, float[] t)
    {
        Func<Vector256<float>, Vector256<float>, Vector256<float>, Vector256<float>> callback = Vector256.Lerp;
        delegate* managed<Vector256<float>, Vector256<float>, Vector256<float>, Vector256<float>> pointer = &Vector256.Lerp;
        for (int test = 0; test < a.Length; test++)
        {
            var aa = new float[Vector256<float>.Count]; var bb = new float[Vector256<float>.Count]; var tt = new float[Vector256<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int k = (test + lane) % a.Length; aa[lane] = a[k]; bb[lane] = b[k]; tt[lane] = t[k]; }
            var av = Vector256.Create(aa); var bv = Vector256.Create(bb); var tv = Vector256.Create(tt);
            var actual = Vector256.Lerp(av, bv, tv);
            var delegated = callback(av, bv, tv); var indirect = pointer(av, bv, tv);
            for (int lane = 0; lane < aa.Length; lane++)
            {
                var value = actual.GetElement(lane);
                Check(Same(value, delegated.GetElement(lane)) && Same(value, indirect.GetElement(lane)));
                var separate = Separate(aa[lane], bb[lane], tt[lane]); var fused = Fused(aa[lane], bb[lane], tt[lane]);
                Console.WriteLine("256f:" + test + ":" + lane + ":" + Bits(value) + ":" + Bits(separate) + ":" + Bits(fused));
            }
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 3; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector256<float> value;
                if (route == 0) value = Vector256.Lerp(Operand256f(1, Vector256.Create(a[0])), Operand256f(2, Vector256.Create(b[0])), Operand256f(3, Vector256.Create(t[0])));
                else if (route == 1) value = callback(Operand256f(1, Vector256.Create(a[0])), Operand256f(2, Vector256.Create(b[0])), Operand256f(3, Vector256.Create(t[0])));
                else value = pointer(Operand256f(1, Vector256.Create(a[0])), Operand256f(2, Vector256.Create(b[0])), Operand256f(3, Vector256.Create(t[0])));
                Check(value.GetElement(0) == 3);
            }
            catch (InvalidOperationException error) when (error.Message == "lerp operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : fail == 2 ? 12 : 123));
        }
        ThrowAt = 0; Console.WriteLine("order:256f:direct-delegate-calli");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector256<double> Operand256d(int step, Vector256<double> value) { Effect(step); return value; }
    private static unsafe void Test256d(double[] a, double[] b, double[] t)
    {
        Func<Vector256<double>, Vector256<double>, Vector256<double>, Vector256<double>> callback = Vector256.Lerp;
        delegate* managed<Vector256<double>, Vector256<double>, Vector256<double>, Vector256<double>> pointer = &Vector256.Lerp;
        for (int test = 0; test < a.Length; test++)
        {
            var aa = new double[Vector256<double>.Count]; var bb = new double[Vector256<double>.Count]; var tt = new double[Vector256<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int k = (test + lane) % a.Length; aa[lane] = a[k]; bb[lane] = b[k]; tt[lane] = t[k]; }
            var av = Vector256.Create(aa); var bv = Vector256.Create(bb); var tv = Vector256.Create(tt);
            var actual = Vector256.Lerp(av, bv, tv);
            var delegated = callback(av, bv, tv); var indirect = pointer(av, bv, tv);
            for (int lane = 0; lane < aa.Length; lane++)
            {
                var value = actual.GetElement(lane);
                Check(Same(value, delegated.GetElement(lane)) && Same(value, indirect.GetElement(lane)));
                var separate = Separate(aa[lane], bb[lane], tt[lane]); var fused = Fused(aa[lane], bb[lane], tt[lane]);
                Console.WriteLine("256d:" + test + ":" + lane + ":" + Bits(value) + ":" + Bits(separate) + ":" + Bits(fused));
            }
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 3; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector256<double> value;
                if (route == 0) value = Vector256.Lerp(Operand256d(1, Vector256.Create(a[0])), Operand256d(2, Vector256.Create(b[0])), Operand256d(3, Vector256.Create(t[0])));
                else if (route == 1) value = callback(Operand256d(1, Vector256.Create(a[0])), Operand256d(2, Vector256.Create(b[0])), Operand256d(3, Vector256.Create(t[0])));
                else value = pointer(Operand256d(1, Vector256.Create(a[0])), Operand256d(2, Vector256.Create(b[0])), Operand256d(3, Vector256.Create(t[0])));
                Check(value.GetElement(0) == 3);
            }
            catch (InvalidOperationException error) when (error.Message == "lerp operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : fail == 2 ? 12 : 123));
        }
        ThrowAt = 0; Console.WriteLine("order:256d:direct-delegate-calli");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector512<float> Operand512f(int step, Vector512<float> value) { Effect(step); return value; }
    private static unsafe void Test512f(float[] a, float[] b, float[] t)
    {
        Func<Vector512<float>, Vector512<float>, Vector512<float>, Vector512<float>> callback = Vector512.Lerp;
        delegate* managed<Vector512<float>, Vector512<float>, Vector512<float>, Vector512<float>> pointer = &Vector512.Lerp;
        for (int test = 0; test < a.Length; test++)
        {
            var aa = new float[Vector512<float>.Count]; var bb = new float[Vector512<float>.Count]; var tt = new float[Vector512<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int k = (test + lane) % a.Length; aa[lane] = a[k]; bb[lane] = b[k]; tt[lane] = t[k]; }
            var av = Vector512.Create(aa); var bv = Vector512.Create(bb); var tv = Vector512.Create(tt);
            var actual = Vector512.Lerp(av, bv, tv);
            var delegated = callback(av, bv, tv); var indirect = pointer(av, bv, tv);
            for (int lane = 0; lane < aa.Length; lane++)
            {
                var value = actual.GetElement(lane);
                Check(Same(value, delegated.GetElement(lane)) && Same(value, indirect.GetElement(lane)));
                var separate = Separate(aa[lane], bb[lane], tt[lane]); var fused = Fused(aa[lane], bb[lane], tt[lane]);
                Console.WriteLine("512f:" + test + ":" + lane + ":" + Bits(value) + ":" + Bits(separate) + ":" + Bits(fused));
            }
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 3; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector512<float> value;
                if (route == 0) value = Vector512.Lerp(Operand512f(1, Vector512.Create(a[0])), Operand512f(2, Vector512.Create(b[0])), Operand512f(3, Vector512.Create(t[0])));
                else if (route == 1) value = callback(Operand512f(1, Vector512.Create(a[0])), Operand512f(2, Vector512.Create(b[0])), Operand512f(3, Vector512.Create(t[0])));
                else value = pointer(Operand512f(1, Vector512.Create(a[0])), Operand512f(2, Vector512.Create(b[0])), Operand512f(3, Vector512.Create(t[0])));
                Check(value.GetElement(0) == 3);
            }
            catch (InvalidOperationException error) when (error.Message == "lerp operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : fail == 2 ? 12 : 123));
        }
        ThrowAt = 0; Console.WriteLine("order:512f:direct-delegate-calli");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector512<double> Operand512d(int step, Vector512<double> value) { Effect(step); return value; }
    private static unsafe void Test512d(double[] a, double[] b, double[] t)
    {
        Func<Vector512<double>, Vector512<double>, Vector512<double>, Vector512<double>> callback = Vector512.Lerp;
        delegate* managed<Vector512<double>, Vector512<double>, Vector512<double>, Vector512<double>> pointer = &Vector512.Lerp;
        for (int test = 0; test < a.Length; test++)
        {
            var aa = new double[Vector512<double>.Count]; var bb = new double[Vector512<double>.Count]; var tt = new double[Vector512<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int k = (test + lane) % a.Length; aa[lane] = a[k]; bb[lane] = b[k]; tt[lane] = t[k]; }
            var av = Vector512.Create(aa); var bv = Vector512.Create(bb); var tv = Vector512.Create(tt);
            var actual = Vector512.Lerp(av, bv, tv);
            var delegated = callback(av, bv, tv); var indirect = pointer(av, bv, tv);
            for (int lane = 0; lane < aa.Length; lane++)
            {
                var value = actual.GetElement(lane);
                Check(Same(value, delegated.GetElement(lane)) && Same(value, indirect.GetElement(lane)));
                var separate = Separate(aa[lane], bb[lane], tt[lane]); var fused = Fused(aa[lane], bb[lane], tt[lane]);
                Console.WriteLine("512d:" + test + ":" + lane + ":" + Bits(value) + ":" + Bits(separate) + ":" + Bits(fused));
            }
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 3; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector512<double> value;
                if (route == 0) value = Vector512.Lerp(Operand512d(1, Vector512.Create(a[0])), Operand512d(2, Vector512.Create(b[0])), Operand512d(3, Vector512.Create(t[0])));
                else if (route == 1) value = callback(Operand512d(1, Vector512.Create(a[0])), Operand512d(2, Vector512.Create(b[0])), Operand512d(3, Vector512.Create(t[0])));
                else value = pointer(Operand512d(1, Vector512.Create(a[0])), Operand512d(2, Vector512.Create(b[0])), Operand512d(3, Vector512.Create(t[0])));
                Check(value.GetElement(0) == 3);
            }
            catch (InvalidOperationException error) when (error.Message == "lerp operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : fail == 2 ? 12 : 123));
        }
        ThrowAt = 0; Console.WriteLine("order:512d:direct-delegate-calli");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<float> Operandnf(int step, Vector<float> value) { Effect(step); return value; }
    private static unsafe void Testnf(float[] a, float[] b, float[] t)
    {
        Func<Vector<float>, Vector<float>, Vector<float>, Vector<float>> callback = Vector.Lerp;
        delegate* managed<Vector<float>, Vector<float>, Vector<float>, Vector<float>> pointer = &Vector.Lerp;
        for (int test = 0; test < a.Length; test++)
        {
            var aa = new float[Vector<float>.Count]; var bb = new float[Vector<float>.Count]; var tt = new float[Vector<float>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int k = (test + lane) % a.Length; aa[lane] = a[k]; bb[lane] = b[k]; tt[lane] = t[k]; }
            var av = new Vector<float>(aa); var bv = new Vector<float>(bb); var tv = new Vector<float>(tt);
            var actual = Vector.Lerp(av, bv, tv);
            var delegated = callback(av, bv, tv); var indirect = pointer(av, bv, tv);
            for (int lane = 0; lane < aa.Length; lane++)
            {
                var value = actual[lane];
                Check(Same(value, delegated[lane]) && Same(value, indirect[lane]));
                var separate = Separate(aa[lane], bb[lane], tt[lane]); var fused = Fused(aa[lane], bb[lane], tt[lane]);
                Console.WriteLine("nf:" + test + ":" + lane + ":" + Bits(value) + ":" + Bits(separate) + ":" + Bits(fused));
            }
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 3; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector<float> value;
                if (route == 0) value = Vector.Lerp(Operandnf(1, new Vector<float>(a[0])), Operandnf(2, new Vector<float>(b[0])), Operandnf(3, new Vector<float>(t[0])));
                else if (route == 1) value = callback(Operandnf(1, new Vector<float>(a[0])), Operandnf(2, new Vector<float>(b[0])), Operandnf(3, new Vector<float>(t[0])));
                else value = pointer(Operandnf(1, new Vector<float>(a[0])), Operandnf(2, new Vector<float>(b[0])), Operandnf(3, new Vector<float>(t[0])));
                Check(value[0] == 3);
            }
            catch (InvalidOperationException error) when (error.Message == "lerp operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : fail == 2 ? 12 : 123));
        }
        ThrowAt = 0; Console.WriteLine("order:nf:direct-delegate-calli");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<double> Operandnd(int step, Vector<double> value) { Effect(step); return value; }
    private static unsafe void Testnd(double[] a, double[] b, double[] t)
    {
        Func<Vector<double>, Vector<double>, Vector<double>, Vector<double>> callback = Vector.Lerp;
        delegate* managed<Vector<double>, Vector<double>, Vector<double>, Vector<double>> pointer = &Vector.Lerp;
        for (int test = 0; test < a.Length; test++)
        {
            var aa = new double[Vector<double>.Count]; var bb = new double[Vector<double>.Count]; var tt = new double[Vector<double>.Count];
            for (int lane = 0; lane < aa.Length; lane++)
            { int k = (test + lane) % a.Length; aa[lane] = a[k]; bb[lane] = b[k]; tt[lane] = t[k]; }
            var av = new Vector<double>(aa); var bv = new Vector<double>(bb); var tv = new Vector<double>(tt);
            var actual = Vector.Lerp(av, bv, tv);
            var delegated = callback(av, bv, tv); var indirect = pointer(av, bv, tv);
            for (int lane = 0; lane < aa.Length; lane++)
            {
                var value = actual[lane];
                Check(Same(value, delegated[lane]) && Same(value, indirect[lane]));
                var separate = Separate(aa[lane], bb[lane], tt[lane]); var fused = Fused(aa[lane], bb[lane], tt[lane]);
                Console.WriteLine("nd:" + test + ":" + lane + ":" + Bits(value) + ":" + Bits(separate) + ":" + Bits(fused));
            }
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail <= 3; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector<double> value;
                if (route == 0) value = Vector.Lerp(Operandnd(1, new Vector<double>(a[0])), Operandnd(2, new Vector<double>(b[0])), Operandnd(3, new Vector<double>(t[0])));
                else if (route == 1) value = callback(Operandnd(1, new Vector<double>(a[0])), Operandnd(2, new Vector<double>(b[0])), Operandnd(3, new Vector<double>(t[0])));
                else value = pointer(Operandnd(1, new Vector<double>(a[0])), Operandnd(2, new Vector<double>(b[0])), Operandnd(3, new Vector<double>(t[0])));
                Check(value[0] == 3);
            }
            catch (InvalidOperationException error) when (error.Message == "lerp operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : fail == 2 ? 12 : 123));
        }
        ThrowAt = 0; Console.WriteLine("order:nd:direct-delegate-calli");
    }

    [MethodImpl(MethodImplOptions.NoInlining)] private static float Amount(int step, float value) { Effect(step); return value; }
    [MethodImpl(MethodImplOptions.NoInlining)] private static Vector2 ConcreteOperand2(int step, Vector2 value) { Effect(step); return value; }
    [MethodImpl(MethodImplOptions.NoInlining)] private static Vector3 ConcreteOperand3(int step, Vector3 value) { Effect(step); return value; }
    [MethodImpl(MethodImplOptions.NoInlining)] private static Vector4 ConcreteOperand4(int step, Vector4 value) { Effect(step); return value; }
    private static void Concrete(float[] a, float[] b, float[] t)
    {
        for (int test = 0; test < a.Length; test++)
        {
            var av = new Vector2(a[(test + 0) % a.Length], a[(test + 1) % a.Length]); var bv = new Vector2(b[(test + 0) % a.Length], b[(test + 1) % a.Length]); var tv = new Vector2(t[(test + 0) % a.Length], t[(test + 1) % a.Length]);
            var variable = Vector2.Lerp(av, bv, tv); var scalar = Vector2.Lerp(av, bv, t[test]);
            Console.WriteLine("v2-vector:" + test + ":0:" + Bits(variable.X) + ":" + Bits(Separate(av.X, bv.X, tv.X)) + ":" + Bits(Fused(av.X, bv.X, tv.X)));
            Console.WriteLine("v2-scalar:" + test + ":0:" + Bits(scalar.X) + ":" + Bits(Separate(av.X, bv.X, t[test])) + ":" + Bits(Fused(av.X, bv.X, t[test])));
            Console.WriteLine("v2-vector:" + test + ":1:" + Bits(variable.Y) + ":" + Bits(Separate(av.Y, bv.Y, tv.Y)) + ":" + Bits(Fused(av.Y, bv.Y, tv.Y)));
            Console.WriteLine("v2-scalar:" + test + ":1:" + Bits(scalar.Y) + ":" + Bits(Separate(av.Y, bv.Y, t[test])) + ":" + Bits(Fused(av.Y, bv.Y, t[test])));
        }
        for (int mode = 0; mode < 2; mode++)
        for (int fail = 0; fail <= 3; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector2 result;
                if (mode == 0) result = Vector2.Lerp(ConcreteOperand2(1, new Vector2(2)), ConcreteOperand2(2, new Vector2(4)), ConcreteOperand2(3, new Vector2(0.5f)));
                else result = Vector2.Lerp(ConcreteOperand2(1, new Vector2(2)), ConcreteOperand2(2, new Vector2(4)), Amount(3, 0.5f));
                Check(result.X == 3);
            }
            catch (InvalidOperationException error) when (error.Message == "lerp operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : fail == 2 ? 12 : 123));
        }
        ThrowAt = 0; Console.WriteLine("order:v2:scalar-vector");
        for (int test = 0; test < a.Length; test++)
        {
            var av = new Vector3(a[(test + 0) % a.Length], a[(test + 1) % a.Length], a[(test + 2) % a.Length]); var bv = new Vector3(b[(test + 0) % a.Length], b[(test + 1) % a.Length], b[(test + 2) % a.Length]); var tv = new Vector3(t[(test + 0) % a.Length], t[(test + 1) % a.Length], t[(test + 2) % a.Length]);
            var variable = Vector3.Lerp(av, bv, tv); var scalar = Vector3.Lerp(av, bv, t[test]);
            Console.WriteLine("v3-vector:" + test + ":0:" + Bits(variable.X) + ":" + Bits(Separate(av.X, bv.X, tv.X)) + ":" + Bits(Fused(av.X, bv.X, tv.X)));
            Console.WriteLine("v3-scalar:" + test + ":0:" + Bits(scalar.X) + ":" + Bits(Separate(av.X, bv.X, t[test])) + ":" + Bits(Fused(av.X, bv.X, t[test])));
            Console.WriteLine("v3-vector:" + test + ":1:" + Bits(variable.Y) + ":" + Bits(Separate(av.Y, bv.Y, tv.Y)) + ":" + Bits(Fused(av.Y, bv.Y, tv.Y)));
            Console.WriteLine("v3-scalar:" + test + ":1:" + Bits(scalar.Y) + ":" + Bits(Separate(av.Y, bv.Y, t[test])) + ":" + Bits(Fused(av.Y, bv.Y, t[test])));
            Console.WriteLine("v3-vector:" + test + ":2:" + Bits(variable.Z) + ":" + Bits(Separate(av.Z, bv.Z, tv.Z)) + ":" + Bits(Fused(av.Z, bv.Z, tv.Z)));
            Console.WriteLine("v3-scalar:" + test + ":2:" + Bits(scalar.Z) + ":" + Bits(Separate(av.Z, bv.Z, t[test])) + ":" + Bits(Fused(av.Z, bv.Z, t[test])));
        }
        for (int mode = 0; mode < 2; mode++)
        for (int fail = 0; fail <= 3; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector3 result;
                if (mode == 0) result = Vector3.Lerp(ConcreteOperand3(1, new Vector3(2)), ConcreteOperand3(2, new Vector3(4)), ConcreteOperand3(3, new Vector3(0.5f)));
                else result = Vector3.Lerp(ConcreteOperand3(1, new Vector3(2)), ConcreteOperand3(2, new Vector3(4)), Amount(3, 0.5f));
                Check(result.X == 3);
            }
            catch (InvalidOperationException error) when (error.Message == "lerp operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : fail == 2 ? 12 : 123));
        }
        ThrowAt = 0; Console.WriteLine("order:v3:scalar-vector");
        for (int test = 0; test < a.Length; test++)
        {
            var av = new Vector4(a[(test + 0) % a.Length], a[(test + 1) % a.Length], a[(test + 2) % a.Length], a[(test + 3) % a.Length]); var bv = new Vector4(b[(test + 0) % a.Length], b[(test + 1) % a.Length], b[(test + 2) % a.Length], b[(test + 3) % a.Length]); var tv = new Vector4(t[(test + 0) % a.Length], t[(test + 1) % a.Length], t[(test + 2) % a.Length], t[(test + 3) % a.Length]);
            var variable = Vector4.Lerp(av, bv, tv); var scalar = Vector4.Lerp(av, bv, t[test]);
            Console.WriteLine("v4-vector:" + test + ":0:" + Bits(variable.X) + ":" + Bits(Separate(av.X, bv.X, tv.X)) + ":" + Bits(Fused(av.X, bv.X, tv.X)));
            Console.WriteLine("v4-scalar:" + test + ":0:" + Bits(scalar.X) + ":" + Bits(Separate(av.X, bv.X, t[test])) + ":" + Bits(Fused(av.X, bv.X, t[test])));
            Console.WriteLine("v4-vector:" + test + ":1:" + Bits(variable.Y) + ":" + Bits(Separate(av.Y, bv.Y, tv.Y)) + ":" + Bits(Fused(av.Y, bv.Y, tv.Y)));
            Console.WriteLine("v4-scalar:" + test + ":1:" + Bits(scalar.Y) + ":" + Bits(Separate(av.Y, bv.Y, t[test])) + ":" + Bits(Fused(av.Y, bv.Y, t[test])));
            Console.WriteLine("v4-vector:" + test + ":2:" + Bits(variable.Z) + ":" + Bits(Separate(av.Z, bv.Z, tv.Z)) + ":" + Bits(Fused(av.Z, bv.Z, tv.Z)));
            Console.WriteLine("v4-scalar:" + test + ":2:" + Bits(scalar.Z) + ":" + Bits(Separate(av.Z, bv.Z, t[test])) + ":" + Bits(Fused(av.Z, bv.Z, t[test])));
            Console.WriteLine("v4-vector:" + test + ":3:" + Bits(variable.W) + ":" + Bits(Separate(av.W, bv.W, tv.W)) + ":" + Bits(Fused(av.W, bv.W, tv.W)));
            Console.WriteLine("v4-scalar:" + test + ":3:" + Bits(scalar.W) + ":" + Bits(Separate(av.W, bv.W, t[test])) + ":" + Bits(Fused(av.W, bv.W, t[test])));
        }
        for (int mode = 0; mode < 2; mode++)
        for (int fail = 0; fail <= 3; fail++)
        {
            Order = 0; ThrowAt = fail; bool caught = false;
            try
            {
                Vector4 result;
                if (mode == 0) result = Vector4.Lerp(ConcreteOperand4(1, new Vector4(2)), ConcreteOperand4(2, new Vector4(4)), ConcreteOperand4(3, new Vector4(0.5f)));
                else result = Vector4.Lerp(ConcreteOperand4(1, new Vector4(2)), ConcreteOperand4(2, new Vector4(4)), Amount(3, 0.5f));
                Check(result.X == 3);
            }
            catch (InvalidOperationException error) when (error.Message == "lerp operand") { caught = true; }
            Check(caught == (fail != 0)); Check(Order == (fail == 1 ? 1 : fail == 2 ? 12 : 123));
        }
        ThrowAt = 0; Console.WriteLine("order:v4:scalar-vector");
    }
}
