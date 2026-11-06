using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

internal static class Program
{
    private static int Calls;
    private static bool ThrowOperand;
    private static void Check(bool ok) { if (!ok) throw new InvalidOperationException("vector log mismatch"); }
    private static void Effect()
    {
        Calls++;
        if (ThrowOperand) throw new InvalidOperationException("log operand");
    }
    private static bool Same(float a, float b) => BitConverter.SingleToInt32Bits(a) == BitConverter.SingleToInt32Bits(b) || float.IsNaN(a) && float.IsNaN(b);
    private static bool Same(double a, double b) => BitConverter.DoubleToInt64Bits(a) == BitConverter.DoubleToInt64Bits(b) || double.IsNaN(a) && double.IsNaN(b);
    private static string Bits(float value) => BitConverter.SingleToInt32Bits(value).ToString("X8");
    private static string Bits(double value) => BitConverter.DoubleToInt64Bits(value).ToString("X16");
    private static int DietRewriteSentinel() => 544246;
    private static ulong[] CreateBits(int precision, int exponentBits, int bias)
    {
        ulong limit = 1UL << precision;
        ulong infinity = ((1UL << exponentBits) - 1) << precision;
        ulong sign = 1UL << (precision + exponentBits);
        ulong one = (ulong)bias << precision;
        ulong[] special = { 0, sign, infinity, sign | infinity, infinity | (limit >> 1) | 123,
            sign | infinity | (limit >> 1) | 321, infinity | 123, sign | 1, sign | one,
            1, limit - 1, limit, infinity - 1, one, one - 1, one + 1 };
        var bits = new List<ulong>();
        for (int rotation = 0; rotation < 16; rotation++)
        for (int lane = 0; lane < 16; lane++) bits.Add(special[(rotation + lane) % 16]);
        for (int exponent = 1; exponent < (1 << exponentBits) - 1; exponent++)
        {
            ulong value = (ulong)exponent << precision;
            bits.Add(value - 1); bits.Add(value); bits.Add(value + 1);
        }
        for (int bit = 0; bit < precision; bit++)
        {
            ulong value = 1UL << bit;
            bits.Add(value - 1); bits.Add(value); bits.Add(value + 1);
        }
        for (int delta = -64; delta <= 64; delta++) bits.Add((ulong)((long)one + delta));
        int[] exponents = { 1, bias - 1, bias, bias + 1, (1 << exponentBits) - 2 };
        ulong[] mantissas = { 1, limit / 3, limit / 2, 2 * limit / 3, limit - 2 };
        foreach (int exponent in exponents)
        foreach (ulong mantissa in mantissas)
        {
            ulong value = ((ulong)exponent << precision) + mantissa;
            bits.Add(value - 1); bits.Add(value); bits.Add(value + 1);
        }
        ulong state = 0x5A17;
        for (int sample = 0; sample < 256; sample++)
        {
            state = unchecked(state * 6364136223846793005UL + 1442695040888963407UL);
            ulong exponent = 1 + state % ((1UL << exponentBits) - 2);
            bits.Add((exponent << precision) | ((state >> 11) & (limit - 1)));
        }
        return bits.ToArray();
    }
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        var fbits = CreateBits(23, 8, 127);
        var f = new float[fbits.Length];
        for (int i = 0; i < f.Length; i++) f[i] = BitConverter.Int32BitsToSingle(unchecked((int)fbits[i]));
        var dbits = CreateBits(52, 11, 1023);
        var d = new double[dbits.Length];
        for (int i = 0; i < d.Length; i++) d[i] = BitConverter.Int64BitsToDouble(unchecked((long)dbits[i]));
        TestLog64f(f);
        TestLog64d(d);
        TestLog128f(f);
        TestLog128d(d);
        TestLog256f(f);
        TestLog256d(d);
        TestLog512f(f);
        TestLog512d(d);
        TestLognf(f);
        TestLognd(d);
        TestLogv2(f);
        TestLogv3(f);
        TestLogv4(f);
        TestLog264f(f);
        TestLog264d(d);
        TestLog2128f(f);
        TestLog2128d(d);
        TestLog2256f(f);
        TestLog2256d(d);
        TestLog2512f(f);
        TestLog2512d(d);
        TestLog2nf(f);
        TestLog2nd(d);
        TestLog2v2(f);
        TestLog2v3(f);
        TestLog2v4(f);
        Console.WriteLine("vector-log:all-twenty-routes-and-concrete-passed");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector64<float> Operand64f(Vector64<float> value) { Effect(); return value; }
    private static unsafe void TestLog64f(float[] input)
    {
        Func<Vector64<float>, Vector64<float>> callback = Vector64.Log;
        delegate* managed<Vector64<float>, Vector64<float>> pointer = &Vector64.Log;
        for (int batch = 0; batch < input.Length; batch += Vector64<float>.Count)
        {
            var values = new float[Vector64<float>.Count];
            for (int lane = 0; lane < values.Length; lane++) values[lane] = input[(batch + lane) % input.Length];
            var value = Vector64.Create(values);
            var original = value;
            var actual = Vector64.Log(value);
            var delegated = callback(value); var indirect = pointer(value);
            for (int lane = 0; lane < values.Length; lane++)
            {
                Check(Same(actual.GetElement(lane), delegated.GetElement(lane)) && Same(actual.GetElement(lane), indirect.GetElement(lane)));
                Check(Bits(value.GetElement(lane)) == Bits(original.GetElement(lane)) && Bits(value.GetElement(lane)) == Bits(values[lane]));
                Console.WriteLine("Log:64f:" + batch + ":" + lane + ":" + Bits(values[lane]) + ":" + Bits(actual.GetElement(lane)));
            }
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail < 2; fail++)
        {
            Calls = 0; ThrowOperand = fail != 0; bool caught = false;
            try
            {
                Vector64<float> result;
                if (route == 0) result = Vector64.Log(Operand64f(Vector64.Create((float)1)));
                else if (route == 1) result = callback(Operand64f(Vector64.Create((float)1)));
                else result = pointer(Operand64f(Vector64.Create((float)1)));
                Check(Bits(result.GetElement(0)) == "00000000");
            }
            catch (InvalidOperationException error) when (error.Message == "log operand") { caught = true; }
            Check(Calls == 1 && caught == (fail != 0));
        }
        ThrowOperand = false;
        Console.WriteLine("order:Log:64f:direct-delegate-calli");
    }
    private static unsafe void TestLog264f(float[] input)
    {
        Func<Vector64<float>, Vector64<float>> callback = Vector64.Log2;
        delegate* managed<Vector64<float>, Vector64<float>> pointer = &Vector64.Log2;
        for (int batch = 0; batch < input.Length; batch += Vector64<float>.Count)
        {
            var values = new float[Vector64<float>.Count];
            for (int lane = 0; lane < values.Length; lane++) values[lane] = input[(batch + lane) % input.Length];
            var value = Vector64.Create(values);
            var original = value;
            var actual = Vector64.Log2(value);
            var delegated = callback(value); var indirect = pointer(value);
            for (int lane = 0; lane < values.Length; lane++)
            {
                Check(Same(actual.GetElement(lane), delegated.GetElement(lane)) && Same(actual.GetElement(lane), indirect.GetElement(lane)));
                Check(Bits(value.GetElement(lane)) == Bits(original.GetElement(lane)) && Bits(value.GetElement(lane)) == Bits(values[lane]));
                Console.WriteLine("Log2:64f:" + batch + ":" + lane + ":" + Bits(values[lane]) + ":" + Bits(actual.GetElement(lane)));
            }
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail < 2; fail++)
        {
            Calls = 0; ThrowOperand = fail != 0; bool caught = false;
            try
            {
                Vector64<float> result;
                if (route == 0) result = Vector64.Log2(Operand64f(Vector64.Create((float)1)));
                else if (route == 1) result = callback(Operand64f(Vector64.Create((float)1)));
                else result = pointer(Operand64f(Vector64.Create((float)1)));
                Check(Bits(result.GetElement(0)) == "00000000");
            }
            catch (InvalidOperationException error) when (error.Message == "log operand") { caught = true; }
            Check(Calls == 1 && caught == (fail != 0));
        }
        ThrowOperand = false;
        Console.WriteLine("order:Log2:64f:direct-delegate-calli");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector64<double> Operand64d(Vector64<double> value) { Effect(); return value; }
    private static unsafe void TestLog64d(double[] input)
    {
        Func<Vector64<double>, Vector64<double>> callback = Vector64.Log;
        delegate* managed<Vector64<double>, Vector64<double>> pointer = &Vector64.Log;
        for (int batch = 0; batch < input.Length; batch += Vector64<double>.Count)
        {
            var values = new double[Vector64<double>.Count];
            for (int lane = 0; lane < values.Length; lane++) values[lane] = input[(batch + lane) % input.Length];
            var value = Vector64.Create(values);
            var original = value;
            var actual = Vector64.Log(value);
            var delegated = callback(value); var indirect = pointer(value);
            for (int lane = 0; lane < values.Length; lane++)
            {
                Check(Same(actual.GetElement(lane), delegated.GetElement(lane)) && Same(actual.GetElement(lane), indirect.GetElement(lane)));
                Check(Bits(value.GetElement(lane)) == Bits(original.GetElement(lane)) && Bits(value.GetElement(lane)) == Bits(values[lane]));
                Console.WriteLine("Log:64d:" + batch + ":" + lane + ":" + Bits(values[lane]) + ":" + Bits(actual.GetElement(lane)));
            }
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail < 2; fail++)
        {
            Calls = 0; ThrowOperand = fail != 0; bool caught = false;
            try
            {
                Vector64<double> result;
                if (route == 0) result = Vector64.Log(Operand64d(Vector64.Create((double)1)));
                else if (route == 1) result = callback(Operand64d(Vector64.Create((double)1)));
                else result = pointer(Operand64d(Vector64.Create((double)1)));
                Check(Bits(result.GetElement(0)) == "0000000000000000");
            }
            catch (InvalidOperationException error) when (error.Message == "log operand") { caught = true; }
            Check(Calls == 1 && caught == (fail != 0));
        }
        ThrowOperand = false;
        Console.WriteLine("order:Log:64d:direct-delegate-calli");
    }
    private static unsafe void TestLog264d(double[] input)
    {
        Func<Vector64<double>, Vector64<double>> callback = Vector64.Log2;
        delegate* managed<Vector64<double>, Vector64<double>> pointer = &Vector64.Log2;
        for (int batch = 0; batch < input.Length; batch += Vector64<double>.Count)
        {
            var values = new double[Vector64<double>.Count];
            for (int lane = 0; lane < values.Length; lane++) values[lane] = input[(batch + lane) % input.Length];
            var value = Vector64.Create(values);
            var original = value;
            var actual = Vector64.Log2(value);
            var delegated = callback(value); var indirect = pointer(value);
            for (int lane = 0; lane < values.Length; lane++)
            {
                Check(Same(actual.GetElement(lane), delegated.GetElement(lane)) && Same(actual.GetElement(lane), indirect.GetElement(lane)));
                Check(Bits(value.GetElement(lane)) == Bits(original.GetElement(lane)) && Bits(value.GetElement(lane)) == Bits(values[lane]));
                Console.WriteLine("Log2:64d:" + batch + ":" + lane + ":" + Bits(values[lane]) + ":" + Bits(actual.GetElement(lane)));
            }
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail < 2; fail++)
        {
            Calls = 0; ThrowOperand = fail != 0; bool caught = false;
            try
            {
                Vector64<double> result;
                if (route == 0) result = Vector64.Log2(Operand64d(Vector64.Create((double)1)));
                else if (route == 1) result = callback(Operand64d(Vector64.Create((double)1)));
                else result = pointer(Operand64d(Vector64.Create((double)1)));
                Check(Bits(result.GetElement(0)) == "0000000000000000");
            }
            catch (InvalidOperationException error) when (error.Message == "log operand") { caught = true; }
            Check(Calls == 1 && caught == (fail != 0));
        }
        ThrowOperand = false;
        Console.WriteLine("order:Log2:64d:direct-delegate-calli");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<float> Operand128f(Vector128<float> value) { Effect(); return value; }
    private static unsafe void TestLog128f(float[] input)
    {
        Func<Vector128<float>, Vector128<float>> callback = Vector128.Log;
        delegate* managed<Vector128<float>, Vector128<float>> pointer = &Vector128.Log;
        for (int batch = 0; batch < input.Length; batch += Vector128<float>.Count)
        {
            var values = new float[Vector128<float>.Count];
            for (int lane = 0; lane < values.Length; lane++) values[lane] = input[(batch + lane) % input.Length];
            var value = Vector128.Create(values);
            var original = value;
            var actual = Vector128.Log(value);
            var delegated = callback(value); var indirect = pointer(value);
            for (int lane = 0; lane < values.Length; lane++)
            {
                Check(Same(actual.GetElement(lane), delegated.GetElement(lane)) && Same(actual.GetElement(lane), indirect.GetElement(lane)));
                Check(Bits(value.GetElement(lane)) == Bits(original.GetElement(lane)) && Bits(value.GetElement(lane)) == Bits(values[lane]));
                Console.WriteLine("Log:128f:" + batch + ":" + lane + ":" + Bits(values[lane]) + ":" + Bits(actual.GetElement(lane)));
            }
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail < 2; fail++)
        {
            Calls = 0; ThrowOperand = fail != 0; bool caught = false;
            try
            {
                Vector128<float> result;
                if (route == 0) result = Vector128.Log(Operand128f(Vector128.Create((float)1)));
                else if (route == 1) result = callback(Operand128f(Vector128.Create((float)1)));
                else result = pointer(Operand128f(Vector128.Create((float)1)));
                Check(Bits(result.GetElement(0)) == "00000000");
            }
            catch (InvalidOperationException error) when (error.Message == "log operand") { caught = true; }
            Check(Calls == 1 && caught == (fail != 0));
        }
        ThrowOperand = false;
        Console.WriteLine("order:Log:128f:direct-delegate-calli");
    }
    private static unsafe void TestLog2128f(float[] input)
    {
        Func<Vector128<float>, Vector128<float>> callback = Vector128.Log2;
        delegate* managed<Vector128<float>, Vector128<float>> pointer = &Vector128.Log2;
        for (int batch = 0; batch < input.Length; batch += Vector128<float>.Count)
        {
            var values = new float[Vector128<float>.Count];
            for (int lane = 0; lane < values.Length; lane++) values[lane] = input[(batch + lane) % input.Length];
            var value = Vector128.Create(values);
            var original = value;
            var actual = Vector128.Log2(value);
            var delegated = callback(value); var indirect = pointer(value);
            for (int lane = 0; lane < values.Length; lane++)
            {
                Check(Same(actual.GetElement(lane), delegated.GetElement(lane)) && Same(actual.GetElement(lane), indirect.GetElement(lane)));
                Check(Bits(value.GetElement(lane)) == Bits(original.GetElement(lane)) && Bits(value.GetElement(lane)) == Bits(values[lane]));
                Console.WriteLine("Log2:128f:" + batch + ":" + lane + ":" + Bits(values[lane]) + ":" + Bits(actual.GetElement(lane)));
            }
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail < 2; fail++)
        {
            Calls = 0; ThrowOperand = fail != 0; bool caught = false;
            try
            {
                Vector128<float> result;
                if (route == 0) result = Vector128.Log2(Operand128f(Vector128.Create((float)1)));
                else if (route == 1) result = callback(Operand128f(Vector128.Create((float)1)));
                else result = pointer(Operand128f(Vector128.Create((float)1)));
                Check(Bits(result.GetElement(0)) == "00000000");
            }
            catch (InvalidOperationException error) when (error.Message == "log operand") { caught = true; }
            Check(Calls == 1 && caught == (fail != 0));
        }
        ThrowOperand = false;
        Console.WriteLine("order:Log2:128f:direct-delegate-calli");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<double> Operand128d(Vector128<double> value) { Effect(); return value; }
    private static unsafe void TestLog128d(double[] input)
    {
        Func<Vector128<double>, Vector128<double>> callback = Vector128.Log;
        delegate* managed<Vector128<double>, Vector128<double>> pointer = &Vector128.Log;
        for (int batch = 0; batch < input.Length; batch += Vector128<double>.Count)
        {
            var values = new double[Vector128<double>.Count];
            for (int lane = 0; lane < values.Length; lane++) values[lane] = input[(batch + lane) % input.Length];
            var value = Vector128.Create(values);
            var original = value;
            var actual = Vector128.Log(value);
            var delegated = callback(value); var indirect = pointer(value);
            for (int lane = 0; lane < values.Length; lane++)
            {
                Check(Same(actual.GetElement(lane), delegated.GetElement(lane)) && Same(actual.GetElement(lane), indirect.GetElement(lane)));
                Check(Bits(value.GetElement(lane)) == Bits(original.GetElement(lane)) && Bits(value.GetElement(lane)) == Bits(values[lane]));
                Console.WriteLine("Log:128d:" + batch + ":" + lane + ":" + Bits(values[lane]) + ":" + Bits(actual.GetElement(lane)));
            }
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail < 2; fail++)
        {
            Calls = 0; ThrowOperand = fail != 0; bool caught = false;
            try
            {
                Vector128<double> result;
                if (route == 0) result = Vector128.Log(Operand128d(Vector128.Create((double)1)));
                else if (route == 1) result = callback(Operand128d(Vector128.Create((double)1)));
                else result = pointer(Operand128d(Vector128.Create((double)1)));
                Check(Bits(result.GetElement(0)) == "0000000000000000");
            }
            catch (InvalidOperationException error) when (error.Message == "log operand") { caught = true; }
            Check(Calls == 1 && caught == (fail != 0));
        }
        ThrowOperand = false;
        Console.WriteLine("order:Log:128d:direct-delegate-calli");
    }
    private static unsafe void TestLog2128d(double[] input)
    {
        Func<Vector128<double>, Vector128<double>> callback = Vector128.Log2;
        delegate* managed<Vector128<double>, Vector128<double>> pointer = &Vector128.Log2;
        for (int batch = 0; batch < input.Length; batch += Vector128<double>.Count)
        {
            var values = new double[Vector128<double>.Count];
            for (int lane = 0; lane < values.Length; lane++) values[lane] = input[(batch + lane) % input.Length];
            var value = Vector128.Create(values);
            var original = value;
            var actual = Vector128.Log2(value);
            var delegated = callback(value); var indirect = pointer(value);
            for (int lane = 0; lane < values.Length; lane++)
            {
                Check(Same(actual.GetElement(lane), delegated.GetElement(lane)) && Same(actual.GetElement(lane), indirect.GetElement(lane)));
                Check(Bits(value.GetElement(lane)) == Bits(original.GetElement(lane)) && Bits(value.GetElement(lane)) == Bits(values[lane]));
                Console.WriteLine("Log2:128d:" + batch + ":" + lane + ":" + Bits(values[lane]) + ":" + Bits(actual.GetElement(lane)));
            }
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail < 2; fail++)
        {
            Calls = 0; ThrowOperand = fail != 0; bool caught = false;
            try
            {
                Vector128<double> result;
                if (route == 0) result = Vector128.Log2(Operand128d(Vector128.Create((double)1)));
                else if (route == 1) result = callback(Operand128d(Vector128.Create((double)1)));
                else result = pointer(Operand128d(Vector128.Create((double)1)));
                Check(Bits(result.GetElement(0)) == "0000000000000000");
            }
            catch (InvalidOperationException error) when (error.Message == "log operand") { caught = true; }
            Check(Calls == 1 && caught == (fail != 0));
        }
        ThrowOperand = false;
        Console.WriteLine("order:Log2:128d:direct-delegate-calli");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector256<float> Operand256f(Vector256<float> value) { Effect(); return value; }
    private static unsafe void TestLog256f(float[] input)
    {
        Func<Vector256<float>, Vector256<float>> callback = Vector256.Log;
        delegate* managed<Vector256<float>, Vector256<float>> pointer = &Vector256.Log;
        for (int batch = 0; batch < input.Length; batch += Vector256<float>.Count)
        {
            var values = new float[Vector256<float>.Count];
            for (int lane = 0; lane < values.Length; lane++) values[lane] = input[(batch + lane) % input.Length];
            var value = Vector256.Create(values);
            var original = value;
            var actual = Vector256.Log(value);
            var delegated = callback(value); var indirect = pointer(value);
            for (int lane = 0; lane < values.Length; lane++)
            {
                Check(Same(actual.GetElement(lane), delegated.GetElement(lane)) && Same(actual.GetElement(lane), indirect.GetElement(lane)));
                Check(Bits(value.GetElement(lane)) == Bits(original.GetElement(lane)) && Bits(value.GetElement(lane)) == Bits(values[lane]));
                Console.WriteLine("Log:256f:" + batch + ":" + lane + ":" + Bits(values[lane]) + ":" + Bits(actual.GetElement(lane)));
            }
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail < 2; fail++)
        {
            Calls = 0; ThrowOperand = fail != 0; bool caught = false;
            try
            {
                Vector256<float> result;
                if (route == 0) result = Vector256.Log(Operand256f(Vector256.Create((float)1)));
                else if (route == 1) result = callback(Operand256f(Vector256.Create((float)1)));
                else result = pointer(Operand256f(Vector256.Create((float)1)));
                Check(Bits(result.GetElement(0)) == "00000000");
            }
            catch (InvalidOperationException error) when (error.Message == "log operand") { caught = true; }
            Check(Calls == 1 && caught == (fail != 0));
        }
        ThrowOperand = false;
        Console.WriteLine("order:Log:256f:direct-delegate-calli");
    }
    private static unsafe void TestLog2256f(float[] input)
    {
        Func<Vector256<float>, Vector256<float>> callback = Vector256.Log2;
        delegate* managed<Vector256<float>, Vector256<float>> pointer = &Vector256.Log2;
        for (int batch = 0; batch < input.Length; batch += Vector256<float>.Count)
        {
            var values = new float[Vector256<float>.Count];
            for (int lane = 0; lane < values.Length; lane++) values[lane] = input[(batch + lane) % input.Length];
            var value = Vector256.Create(values);
            var original = value;
            var actual = Vector256.Log2(value);
            var delegated = callback(value); var indirect = pointer(value);
            for (int lane = 0; lane < values.Length; lane++)
            {
                Check(Same(actual.GetElement(lane), delegated.GetElement(lane)) && Same(actual.GetElement(lane), indirect.GetElement(lane)));
                Check(Bits(value.GetElement(lane)) == Bits(original.GetElement(lane)) && Bits(value.GetElement(lane)) == Bits(values[lane]));
                Console.WriteLine("Log2:256f:" + batch + ":" + lane + ":" + Bits(values[lane]) + ":" + Bits(actual.GetElement(lane)));
            }
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail < 2; fail++)
        {
            Calls = 0; ThrowOperand = fail != 0; bool caught = false;
            try
            {
                Vector256<float> result;
                if (route == 0) result = Vector256.Log2(Operand256f(Vector256.Create((float)1)));
                else if (route == 1) result = callback(Operand256f(Vector256.Create((float)1)));
                else result = pointer(Operand256f(Vector256.Create((float)1)));
                Check(Bits(result.GetElement(0)) == "00000000");
            }
            catch (InvalidOperationException error) when (error.Message == "log operand") { caught = true; }
            Check(Calls == 1 && caught == (fail != 0));
        }
        ThrowOperand = false;
        Console.WriteLine("order:Log2:256f:direct-delegate-calli");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector256<double> Operand256d(Vector256<double> value) { Effect(); return value; }
    private static unsafe void TestLog256d(double[] input)
    {
        Func<Vector256<double>, Vector256<double>> callback = Vector256.Log;
        delegate* managed<Vector256<double>, Vector256<double>> pointer = &Vector256.Log;
        for (int batch = 0; batch < input.Length; batch += Vector256<double>.Count)
        {
            var values = new double[Vector256<double>.Count];
            for (int lane = 0; lane < values.Length; lane++) values[lane] = input[(batch + lane) % input.Length];
            var value = Vector256.Create(values);
            var original = value;
            var actual = Vector256.Log(value);
            var delegated = callback(value); var indirect = pointer(value);
            for (int lane = 0; lane < values.Length; lane++)
            {
                Check(Same(actual.GetElement(lane), delegated.GetElement(lane)) && Same(actual.GetElement(lane), indirect.GetElement(lane)));
                Check(Bits(value.GetElement(lane)) == Bits(original.GetElement(lane)) && Bits(value.GetElement(lane)) == Bits(values[lane]));
                Console.WriteLine("Log:256d:" + batch + ":" + lane + ":" + Bits(values[lane]) + ":" + Bits(actual.GetElement(lane)));
            }
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail < 2; fail++)
        {
            Calls = 0; ThrowOperand = fail != 0; bool caught = false;
            try
            {
                Vector256<double> result;
                if (route == 0) result = Vector256.Log(Operand256d(Vector256.Create((double)1)));
                else if (route == 1) result = callback(Operand256d(Vector256.Create((double)1)));
                else result = pointer(Operand256d(Vector256.Create((double)1)));
                Check(Bits(result.GetElement(0)) == "0000000000000000");
            }
            catch (InvalidOperationException error) when (error.Message == "log operand") { caught = true; }
            Check(Calls == 1 && caught == (fail != 0));
        }
        ThrowOperand = false;
        Console.WriteLine("order:Log:256d:direct-delegate-calli");
    }
    private static unsafe void TestLog2256d(double[] input)
    {
        Func<Vector256<double>, Vector256<double>> callback = Vector256.Log2;
        delegate* managed<Vector256<double>, Vector256<double>> pointer = &Vector256.Log2;
        for (int batch = 0; batch < input.Length; batch += Vector256<double>.Count)
        {
            var values = new double[Vector256<double>.Count];
            for (int lane = 0; lane < values.Length; lane++) values[lane] = input[(batch + lane) % input.Length];
            var value = Vector256.Create(values);
            var original = value;
            var actual = Vector256.Log2(value);
            var delegated = callback(value); var indirect = pointer(value);
            for (int lane = 0; lane < values.Length; lane++)
            {
                Check(Same(actual.GetElement(lane), delegated.GetElement(lane)) && Same(actual.GetElement(lane), indirect.GetElement(lane)));
                Check(Bits(value.GetElement(lane)) == Bits(original.GetElement(lane)) && Bits(value.GetElement(lane)) == Bits(values[lane]));
                Console.WriteLine("Log2:256d:" + batch + ":" + lane + ":" + Bits(values[lane]) + ":" + Bits(actual.GetElement(lane)));
            }
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail < 2; fail++)
        {
            Calls = 0; ThrowOperand = fail != 0; bool caught = false;
            try
            {
                Vector256<double> result;
                if (route == 0) result = Vector256.Log2(Operand256d(Vector256.Create((double)1)));
                else if (route == 1) result = callback(Operand256d(Vector256.Create((double)1)));
                else result = pointer(Operand256d(Vector256.Create((double)1)));
                Check(Bits(result.GetElement(0)) == "0000000000000000");
            }
            catch (InvalidOperationException error) when (error.Message == "log operand") { caught = true; }
            Check(Calls == 1 && caught == (fail != 0));
        }
        ThrowOperand = false;
        Console.WriteLine("order:Log2:256d:direct-delegate-calli");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector512<float> Operand512f(Vector512<float> value) { Effect(); return value; }
    private static unsafe void TestLog512f(float[] input)
    {
        Func<Vector512<float>, Vector512<float>> callback = Vector512.Log;
        delegate* managed<Vector512<float>, Vector512<float>> pointer = &Vector512.Log;
        for (int batch = 0; batch < input.Length; batch += Vector512<float>.Count)
        {
            var values = new float[Vector512<float>.Count];
            for (int lane = 0; lane < values.Length; lane++) values[lane] = input[(batch + lane) % input.Length];
            var value = Vector512.Create(values);
            var original = value;
            var actual = Vector512.Log(value);
            var delegated = callback(value); var indirect = pointer(value);
            for (int lane = 0; lane < values.Length; lane++)
            {
                Check(Same(actual.GetElement(lane), delegated.GetElement(lane)) && Same(actual.GetElement(lane), indirect.GetElement(lane)));
                Check(Bits(value.GetElement(lane)) == Bits(original.GetElement(lane)) && Bits(value.GetElement(lane)) == Bits(values[lane]));
                Console.WriteLine("Log:512f:" + batch + ":" + lane + ":" + Bits(values[lane]) + ":" + Bits(actual.GetElement(lane)));
            }
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail < 2; fail++)
        {
            Calls = 0; ThrowOperand = fail != 0; bool caught = false;
            try
            {
                Vector512<float> result;
                if (route == 0) result = Vector512.Log(Operand512f(Vector512.Create((float)1)));
                else if (route == 1) result = callback(Operand512f(Vector512.Create((float)1)));
                else result = pointer(Operand512f(Vector512.Create((float)1)));
                Check(Bits(result.GetElement(0)) == "00000000");
            }
            catch (InvalidOperationException error) when (error.Message == "log operand") { caught = true; }
            Check(Calls == 1 && caught == (fail != 0));
        }
        ThrowOperand = false;
        Console.WriteLine("order:Log:512f:direct-delegate-calli");
    }
    private static unsafe void TestLog2512f(float[] input)
    {
        Func<Vector512<float>, Vector512<float>> callback = Vector512.Log2;
        delegate* managed<Vector512<float>, Vector512<float>> pointer = &Vector512.Log2;
        for (int batch = 0; batch < input.Length; batch += Vector512<float>.Count)
        {
            var values = new float[Vector512<float>.Count];
            for (int lane = 0; lane < values.Length; lane++) values[lane] = input[(batch + lane) % input.Length];
            var value = Vector512.Create(values);
            var original = value;
            var actual = Vector512.Log2(value);
            var delegated = callback(value); var indirect = pointer(value);
            for (int lane = 0; lane < values.Length; lane++)
            {
                Check(Same(actual.GetElement(lane), delegated.GetElement(lane)) && Same(actual.GetElement(lane), indirect.GetElement(lane)));
                Check(Bits(value.GetElement(lane)) == Bits(original.GetElement(lane)) && Bits(value.GetElement(lane)) == Bits(values[lane]));
                Console.WriteLine("Log2:512f:" + batch + ":" + lane + ":" + Bits(values[lane]) + ":" + Bits(actual.GetElement(lane)));
            }
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail < 2; fail++)
        {
            Calls = 0; ThrowOperand = fail != 0; bool caught = false;
            try
            {
                Vector512<float> result;
                if (route == 0) result = Vector512.Log2(Operand512f(Vector512.Create((float)1)));
                else if (route == 1) result = callback(Operand512f(Vector512.Create((float)1)));
                else result = pointer(Operand512f(Vector512.Create((float)1)));
                Check(Bits(result.GetElement(0)) == "00000000");
            }
            catch (InvalidOperationException error) when (error.Message == "log operand") { caught = true; }
            Check(Calls == 1 && caught == (fail != 0));
        }
        ThrowOperand = false;
        Console.WriteLine("order:Log2:512f:direct-delegate-calli");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector512<double> Operand512d(Vector512<double> value) { Effect(); return value; }
    private static unsafe void TestLog512d(double[] input)
    {
        Func<Vector512<double>, Vector512<double>> callback = Vector512.Log;
        delegate* managed<Vector512<double>, Vector512<double>> pointer = &Vector512.Log;
        for (int batch = 0; batch < input.Length; batch += Vector512<double>.Count)
        {
            var values = new double[Vector512<double>.Count];
            for (int lane = 0; lane < values.Length; lane++) values[lane] = input[(batch + lane) % input.Length];
            var value = Vector512.Create(values);
            var original = value;
            var actual = Vector512.Log(value);
            var delegated = callback(value); var indirect = pointer(value);
            for (int lane = 0; lane < values.Length; lane++)
            {
                Check(Same(actual.GetElement(lane), delegated.GetElement(lane)) && Same(actual.GetElement(lane), indirect.GetElement(lane)));
                Check(Bits(value.GetElement(lane)) == Bits(original.GetElement(lane)) && Bits(value.GetElement(lane)) == Bits(values[lane]));
                Console.WriteLine("Log:512d:" + batch + ":" + lane + ":" + Bits(values[lane]) + ":" + Bits(actual.GetElement(lane)));
            }
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail < 2; fail++)
        {
            Calls = 0; ThrowOperand = fail != 0; bool caught = false;
            try
            {
                Vector512<double> result;
                if (route == 0) result = Vector512.Log(Operand512d(Vector512.Create((double)1)));
                else if (route == 1) result = callback(Operand512d(Vector512.Create((double)1)));
                else result = pointer(Operand512d(Vector512.Create((double)1)));
                Check(Bits(result.GetElement(0)) == "0000000000000000");
            }
            catch (InvalidOperationException error) when (error.Message == "log operand") { caught = true; }
            Check(Calls == 1 && caught == (fail != 0));
        }
        ThrowOperand = false;
        Console.WriteLine("order:Log:512d:direct-delegate-calli");
    }
    private static unsafe void TestLog2512d(double[] input)
    {
        Func<Vector512<double>, Vector512<double>> callback = Vector512.Log2;
        delegate* managed<Vector512<double>, Vector512<double>> pointer = &Vector512.Log2;
        for (int batch = 0; batch < input.Length; batch += Vector512<double>.Count)
        {
            var values = new double[Vector512<double>.Count];
            for (int lane = 0; lane < values.Length; lane++) values[lane] = input[(batch + lane) % input.Length];
            var value = Vector512.Create(values);
            var original = value;
            var actual = Vector512.Log2(value);
            var delegated = callback(value); var indirect = pointer(value);
            for (int lane = 0; lane < values.Length; lane++)
            {
                Check(Same(actual.GetElement(lane), delegated.GetElement(lane)) && Same(actual.GetElement(lane), indirect.GetElement(lane)));
                Check(Bits(value.GetElement(lane)) == Bits(original.GetElement(lane)) && Bits(value.GetElement(lane)) == Bits(values[lane]));
                Console.WriteLine("Log2:512d:" + batch + ":" + lane + ":" + Bits(values[lane]) + ":" + Bits(actual.GetElement(lane)));
            }
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail < 2; fail++)
        {
            Calls = 0; ThrowOperand = fail != 0; bool caught = false;
            try
            {
                Vector512<double> result;
                if (route == 0) result = Vector512.Log2(Operand512d(Vector512.Create((double)1)));
                else if (route == 1) result = callback(Operand512d(Vector512.Create((double)1)));
                else result = pointer(Operand512d(Vector512.Create((double)1)));
                Check(Bits(result.GetElement(0)) == "0000000000000000");
            }
            catch (InvalidOperationException error) when (error.Message == "log operand") { caught = true; }
            Check(Calls == 1 && caught == (fail != 0));
        }
        ThrowOperand = false;
        Console.WriteLine("order:Log2:512d:direct-delegate-calli");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<float> Operandnf(Vector<float> value) { Effect(); return value; }
    private static unsafe void TestLognf(float[] input)
    {
        Func<Vector<float>, Vector<float>> callback = Vector.Log;
        delegate* managed<Vector<float>, Vector<float>> pointer = &Vector.Log;
        for (int batch = 0; batch < input.Length; batch += Vector<float>.Count)
        {
            var values = new float[Vector<float>.Count];
            for (int lane = 0; lane < values.Length; lane++) values[lane] = input[(batch + lane) % input.Length];
            var value = new Vector<float>(values);
            var original = value;
            var actual = Vector.Log(value);
            var delegated = callback(value); var indirect = pointer(value);
            for (int lane = 0; lane < values.Length; lane++)
            {
                Check(Same(actual[lane], delegated[lane]) && Same(actual[lane], indirect[lane]));
                Check(Bits(value[lane]) == Bits(original[lane]) && Bits(value[lane]) == Bits(values[lane]));
                Console.WriteLine("Log:nf:" + batch + ":" + lane + ":" + Bits(values[lane]) + ":" + Bits(actual[lane]));
            }
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail < 2; fail++)
        {
            Calls = 0; ThrowOperand = fail != 0; bool caught = false;
            try
            {
                Vector<float> result;
                if (route == 0) result = Vector.Log(Operandnf(new Vector<float>((float)1)));
                else if (route == 1) result = callback(Operandnf(new Vector<float>((float)1)));
                else result = pointer(Operandnf(new Vector<float>((float)1)));
                Check(Bits(result[0]) == "00000000");
            }
            catch (InvalidOperationException error) when (error.Message == "log operand") { caught = true; }
            Check(Calls == 1 && caught == (fail != 0));
        }
        ThrowOperand = false;
        Console.WriteLine("order:Log:nf:direct-delegate-calli");
    }
    private static unsafe void TestLog2nf(float[] input)
    {
        Func<Vector<float>, Vector<float>> callback = Vector.Log2;
        delegate* managed<Vector<float>, Vector<float>> pointer = &Vector.Log2;
        for (int batch = 0; batch < input.Length; batch += Vector<float>.Count)
        {
            var values = new float[Vector<float>.Count];
            for (int lane = 0; lane < values.Length; lane++) values[lane] = input[(batch + lane) % input.Length];
            var value = new Vector<float>(values);
            var original = value;
            var actual = Vector.Log2(value);
            var delegated = callback(value); var indirect = pointer(value);
            for (int lane = 0; lane < values.Length; lane++)
            {
                Check(Same(actual[lane], delegated[lane]) && Same(actual[lane], indirect[lane]));
                Check(Bits(value[lane]) == Bits(original[lane]) && Bits(value[lane]) == Bits(values[lane]));
                Console.WriteLine("Log2:nf:" + batch + ":" + lane + ":" + Bits(values[lane]) + ":" + Bits(actual[lane]));
            }
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail < 2; fail++)
        {
            Calls = 0; ThrowOperand = fail != 0; bool caught = false;
            try
            {
                Vector<float> result;
                if (route == 0) result = Vector.Log2(Operandnf(new Vector<float>((float)1)));
                else if (route == 1) result = callback(Operandnf(new Vector<float>((float)1)));
                else result = pointer(Operandnf(new Vector<float>((float)1)));
                Check(Bits(result[0]) == "00000000");
            }
            catch (InvalidOperationException error) when (error.Message == "log operand") { caught = true; }
            Check(Calls == 1 && caught == (fail != 0));
        }
        ThrowOperand = false;
        Console.WriteLine("order:Log2:nf:direct-delegate-calli");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<double> Operandnd(Vector<double> value) { Effect(); return value; }
    private static unsafe void TestLognd(double[] input)
    {
        Func<Vector<double>, Vector<double>> callback = Vector.Log;
        delegate* managed<Vector<double>, Vector<double>> pointer = &Vector.Log;
        for (int batch = 0; batch < input.Length; batch += Vector<double>.Count)
        {
            var values = new double[Vector<double>.Count];
            for (int lane = 0; lane < values.Length; lane++) values[lane] = input[(batch + lane) % input.Length];
            var value = new Vector<double>(values);
            var original = value;
            var actual = Vector.Log(value);
            var delegated = callback(value); var indirect = pointer(value);
            for (int lane = 0; lane < values.Length; lane++)
            {
                Check(Same(actual[lane], delegated[lane]) && Same(actual[lane], indirect[lane]));
                Check(Bits(value[lane]) == Bits(original[lane]) && Bits(value[lane]) == Bits(values[lane]));
                Console.WriteLine("Log:nd:" + batch + ":" + lane + ":" + Bits(values[lane]) + ":" + Bits(actual[lane]));
            }
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail < 2; fail++)
        {
            Calls = 0; ThrowOperand = fail != 0; bool caught = false;
            try
            {
                Vector<double> result;
                if (route == 0) result = Vector.Log(Operandnd(new Vector<double>((double)1)));
                else if (route == 1) result = callback(Operandnd(new Vector<double>((double)1)));
                else result = pointer(Operandnd(new Vector<double>((double)1)));
                Check(Bits(result[0]) == "0000000000000000");
            }
            catch (InvalidOperationException error) when (error.Message == "log operand") { caught = true; }
            Check(Calls == 1 && caught == (fail != 0));
        }
        ThrowOperand = false;
        Console.WriteLine("order:Log:nd:direct-delegate-calli");
    }
    private static unsafe void TestLog2nd(double[] input)
    {
        Func<Vector<double>, Vector<double>> callback = Vector.Log2;
        delegate* managed<Vector<double>, Vector<double>> pointer = &Vector.Log2;
        for (int batch = 0; batch < input.Length; batch += Vector<double>.Count)
        {
            var values = new double[Vector<double>.Count];
            for (int lane = 0; lane < values.Length; lane++) values[lane] = input[(batch + lane) % input.Length];
            var value = new Vector<double>(values);
            var original = value;
            var actual = Vector.Log2(value);
            var delegated = callback(value); var indirect = pointer(value);
            for (int lane = 0; lane < values.Length; lane++)
            {
                Check(Same(actual[lane], delegated[lane]) && Same(actual[lane], indirect[lane]));
                Check(Bits(value[lane]) == Bits(original[lane]) && Bits(value[lane]) == Bits(values[lane]));
                Console.WriteLine("Log2:nd:" + batch + ":" + lane + ":" + Bits(values[lane]) + ":" + Bits(actual[lane]));
            }
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail < 2; fail++)
        {
            Calls = 0; ThrowOperand = fail != 0; bool caught = false;
            try
            {
                Vector<double> result;
                if (route == 0) result = Vector.Log2(Operandnd(new Vector<double>((double)1)));
                else if (route == 1) result = callback(Operandnd(new Vector<double>((double)1)));
                else result = pointer(Operandnd(new Vector<double>((double)1)));
                Check(Bits(result[0]) == "0000000000000000");
            }
            catch (InvalidOperationException error) when (error.Message == "log operand") { caught = true; }
            Check(Calls == 1 && caught == (fail != 0));
        }
        ThrowOperand = false;
        Console.WriteLine("order:Log2:nd:direct-delegate-calli");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector2 Operandv2(Vector2 value) { Effect(); return value; }
    private static unsafe void TestLogv2(float[] input)
    {
        Func<Vector2, Vector2> callback = Vector2.Log;
        delegate* managed<Vector2, Vector2> pointer = &Vector2.Log;
        for (int batch = 0; batch < input.Length; batch += 2)
        {
            var value = new Vector2(input[(batch + 0) % input.Length], input[(batch + 1) % input.Length]);
            var original = value;
            var actual = Vector2.Log(value);
            var delegated = callback(value); var indirect = pointer(value);
            Check(Same(actual.X, delegated.X) && Same(actual.X, indirect.X));
            Check(Bits(value.X) == Bits(original.X) && Bits(value.X) == Bits(input[(batch + 0) % input.Length]));
            Console.WriteLine("Log:v2:" + batch + ":0:" + Bits(value.X) + ":" + Bits(actual.X));
            Check(Same(actual.Y, delegated.Y) && Same(actual.Y, indirect.Y));
            Check(Bits(value.Y) == Bits(original.Y) && Bits(value.Y) == Bits(input[(batch + 1) % input.Length]));
            Console.WriteLine("Log:v2:" + batch + ":1:" + Bits(value.Y) + ":" + Bits(actual.Y));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail < 2; fail++)
        {
            Calls = 0; ThrowOperand = fail != 0; bool caught = false;
            try
            {
                Vector2 result;
                if (route == 0) result = Vector2.Log(Operandv2(new Vector2(1)));
                else if (route == 1) result = callback(Operandv2(new Vector2(1)));
                else result = pointer(Operandv2(new Vector2(1)));
                Check(Bits(result.X) == "00000000");
            }
            catch (InvalidOperationException error) when (error.Message == "log operand") { caught = true; }
            Check(Calls == 1 && caught == (fail != 0));
        }
        ThrowOperand = false;
        Console.WriteLine("order:Log:v2:direct-delegate-calli");
    }
    private static unsafe void TestLog2v2(float[] input)
    {
        Func<Vector2, Vector2> callback = Vector2.Log2;
        delegate* managed<Vector2, Vector2> pointer = &Vector2.Log2;
        for (int batch = 0; batch < input.Length; batch += 2)
        {
            var value = new Vector2(input[(batch + 0) % input.Length], input[(batch + 1) % input.Length]);
            var original = value;
            var actual = Vector2.Log2(value);
            var delegated = callback(value); var indirect = pointer(value);
            Check(Same(actual.X, delegated.X) && Same(actual.X, indirect.X));
            Check(Bits(value.X) == Bits(original.X) && Bits(value.X) == Bits(input[(batch + 0) % input.Length]));
            Console.WriteLine("Log2:v2:" + batch + ":0:" + Bits(value.X) + ":" + Bits(actual.X));
            Check(Same(actual.Y, delegated.Y) && Same(actual.Y, indirect.Y));
            Check(Bits(value.Y) == Bits(original.Y) && Bits(value.Y) == Bits(input[(batch + 1) % input.Length]));
            Console.WriteLine("Log2:v2:" + batch + ":1:" + Bits(value.Y) + ":" + Bits(actual.Y));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail < 2; fail++)
        {
            Calls = 0; ThrowOperand = fail != 0; bool caught = false;
            try
            {
                Vector2 result;
                if (route == 0) result = Vector2.Log2(Operandv2(new Vector2(1)));
                else if (route == 1) result = callback(Operandv2(new Vector2(1)));
                else result = pointer(Operandv2(new Vector2(1)));
                Check(Bits(result.X) == "00000000");
            }
            catch (InvalidOperationException error) when (error.Message == "log operand") { caught = true; }
            Check(Calls == 1 && caught == (fail != 0));
        }
        ThrowOperand = false;
        Console.WriteLine("order:Log2:v2:direct-delegate-calli");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector3 Operandv3(Vector3 value) { Effect(); return value; }
    private static unsafe void TestLogv3(float[] input)
    {
        Func<Vector3, Vector3> callback = Vector3.Log;
        delegate* managed<Vector3, Vector3> pointer = &Vector3.Log;
        for (int batch = 0; batch < input.Length; batch += 3)
        {
            var value = new Vector3(input[(batch + 0) % input.Length], input[(batch + 1) % input.Length], input[(batch + 2) % input.Length]);
            var original = value;
            var actual = Vector3.Log(value);
            var delegated = callback(value); var indirect = pointer(value);
            Check(Same(actual.X, delegated.X) && Same(actual.X, indirect.X));
            Check(Bits(value.X) == Bits(original.X) && Bits(value.X) == Bits(input[(batch + 0) % input.Length]));
            Console.WriteLine("Log:v3:" + batch + ":0:" + Bits(value.X) + ":" + Bits(actual.X));
            Check(Same(actual.Y, delegated.Y) && Same(actual.Y, indirect.Y));
            Check(Bits(value.Y) == Bits(original.Y) && Bits(value.Y) == Bits(input[(batch + 1) % input.Length]));
            Console.WriteLine("Log:v3:" + batch + ":1:" + Bits(value.Y) + ":" + Bits(actual.Y));
            Check(Same(actual.Z, delegated.Z) && Same(actual.Z, indirect.Z));
            Check(Bits(value.Z) == Bits(original.Z) && Bits(value.Z) == Bits(input[(batch + 2) % input.Length]));
            Console.WriteLine("Log:v3:" + batch + ":2:" + Bits(value.Z) + ":" + Bits(actual.Z));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail < 2; fail++)
        {
            Calls = 0; ThrowOperand = fail != 0; bool caught = false;
            try
            {
                Vector3 result;
                if (route == 0) result = Vector3.Log(Operandv3(new Vector3(1)));
                else if (route == 1) result = callback(Operandv3(new Vector3(1)));
                else result = pointer(Operandv3(new Vector3(1)));
                Check(Bits(result.X) == "00000000");
            }
            catch (InvalidOperationException error) when (error.Message == "log operand") { caught = true; }
            Check(Calls == 1 && caught == (fail != 0));
        }
        ThrowOperand = false;
        Console.WriteLine("order:Log:v3:direct-delegate-calli");
    }
    private static unsafe void TestLog2v3(float[] input)
    {
        Func<Vector3, Vector3> callback = Vector3.Log2;
        delegate* managed<Vector3, Vector3> pointer = &Vector3.Log2;
        for (int batch = 0; batch < input.Length; batch += 3)
        {
            var value = new Vector3(input[(batch + 0) % input.Length], input[(batch + 1) % input.Length], input[(batch + 2) % input.Length]);
            var original = value;
            var actual = Vector3.Log2(value);
            var delegated = callback(value); var indirect = pointer(value);
            Check(Same(actual.X, delegated.X) && Same(actual.X, indirect.X));
            Check(Bits(value.X) == Bits(original.X) && Bits(value.X) == Bits(input[(batch + 0) % input.Length]));
            Console.WriteLine("Log2:v3:" + batch + ":0:" + Bits(value.X) + ":" + Bits(actual.X));
            Check(Same(actual.Y, delegated.Y) && Same(actual.Y, indirect.Y));
            Check(Bits(value.Y) == Bits(original.Y) && Bits(value.Y) == Bits(input[(batch + 1) % input.Length]));
            Console.WriteLine("Log2:v3:" + batch + ":1:" + Bits(value.Y) + ":" + Bits(actual.Y));
            Check(Same(actual.Z, delegated.Z) && Same(actual.Z, indirect.Z));
            Check(Bits(value.Z) == Bits(original.Z) && Bits(value.Z) == Bits(input[(batch + 2) % input.Length]));
            Console.WriteLine("Log2:v3:" + batch + ":2:" + Bits(value.Z) + ":" + Bits(actual.Z));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail < 2; fail++)
        {
            Calls = 0; ThrowOperand = fail != 0; bool caught = false;
            try
            {
                Vector3 result;
                if (route == 0) result = Vector3.Log2(Operandv3(new Vector3(1)));
                else if (route == 1) result = callback(Operandv3(new Vector3(1)));
                else result = pointer(Operandv3(new Vector3(1)));
                Check(Bits(result.X) == "00000000");
            }
            catch (InvalidOperationException error) when (error.Message == "log operand") { caught = true; }
            Check(Calls == 1 && caught == (fail != 0));
        }
        ThrowOperand = false;
        Console.WriteLine("order:Log2:v3:direct-delegate-calli");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector4 Operandv4(Vector4 value) { Effect(); return value; }
    private static unsafe void TestLogv4(float[] input)
    {
        Func<Vector4, Vector4> callback = Vector4.Log;
        delegate* managed<Vector4, Vector4> pointer = &Vector4.Log;
        for (int batch = 0; batch < input.Length; batch += 4)
        {
            var value = new Vector4(input[(batch + 0) % input.Length], input[(batch + 1) % input.Length], input[(batch + 2) % input.Length], input[(batch + 3) % input.Length]);
            var original = value;
            var actual = Vector4.Log(value);
            var delegated = callback(value); var indirect = pointer(value);
            Check(Same(actual.X, delegated.X) && Same(actual.X, indirect.X));
            Check(Bits(value.X) == Bits(original.X) && Bits(value.X) == Bits(input[(batch + 0) % input.Length]));
            Console.WriteLine("Log:v4:" + batch + ":0:" + Bits(value.X) + ":" + Bits(actual.X));
            Check(Same(actual.Y, delegated.Y) && Same(actual.Y, indirect.Y));
            Check(Bits(value.Y) == Bits(original.Y) && Bits(value.Y) == Bits(input[(batch + 1) % input.Length]));
            Console.WriteLine("Log:v4:" + batch + ":1:" + Bits(value.Y) + ":" + Bits(actual.Y));
            Check(Same(actual.Z, delegated.Z) && Same(actual.Z, indirect.Z));
            Check(Bits(value.Z) == Bits(original.Z) && Bits(value.Z) == Bits(input[(batch + 2) % input.Length]));
            Console.WriteLine("Log:v4:" + batch + ":2:" + Bits(value.Z) + ":" + Bits(actual.Z));
            Check(Same(actual.W, delegated.W) && Same(actual.W, indirect.W));
            Check(Bits(value.W) == Bits(original.W) && Bits(value.W) == Bits(input[(batch + 3) % input.Length]));
            Console.WriteLine("Log:v4:" + batch + ":3:" + Bits(value.W) + ":" + Bits(actual.W));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail < 2; fail++)
        {
            Calls = 0; ThrowOperand = fail != 0; bool caught = false;
            try
            {
                Vector4 result;
                if (route == 0) result = Vector4.Log(Operandv4(new Vector4(1)));
                else if (route == 1) result = callback(Operandv4(new Vector4(1)));
                else result = pointer(Operandv4(new Vector4(1)));
                Check(Bits(result.X) == "00000000");
            }
            catch (InvalidOperationException error) when (error.Message == "log operand") { caught = true; }
            Check(Calls == 1 && caught == (fail != 0));
        }
        ThrowOperand = false;
        Console.WriteLine("order:Log:v4:direct-delegate-calli");
    }
    private static unsafe void TestLog2v4(float[] input)
    {
        Func<Vector4, Vector4> callback = Vector4.Log2;
        delegate* managed<Vector4, Vector4> pointer = &Vector4.Log2;
        for (int batch = 0; batch < input.Length; batch += 4)
        {
            var value = new Vector4(input[(batch + 0) % input.Length], input[(batch + 1) % input.Length], input[(batch + 2) % input.Length], input[(batch + 3) % input.Length]);
            var original = value;
            var actual = Vector4.Log2(value);
            var delegated = callback(value); var indirect = pointer(value);
            Check(Same(actual.X, delegated.X) && Same(actual.X, indirect.X));
            Check(Bits(value.X) == Bits(original.X) && Bits(value.X) == Bits(input[(batch + 0) % input.Length]));
            Console.WriteLine("Log2:v4:" + batch + ":0:" + Bits(value.X) + ":" + Bits(actual.X));
            Check(Same(actual.Y, delegated.Y) && Same(actual.Y, indirect.Y));
            Check(Bits(value.Y) == Bits(original.Y) && Bits(value.Y) == Bits(input[(batch + 1) % input.Length]));
            Console.WriteLine("Log2:v4:" + batch + ":1:" + Bits(value.Y) + ":" + Bits(actual.Y));
            Check(Same(actual.Z, delegated.Z) && Same(actual.Z, indirect.Z));
            Check(Bits(value.Z) == Bits(original.Z) && Bits(value.Z) == Bits(input[(batch + 2) % input.Length]));
            Console.WriteLine("Log2:v4:" + batch + ":2:" + Bits(value.Z) + ":" + Bits(actual.Z));
            Check(Same(actual.W, delegated.W) && Same(actual.W, indirect.W));
            Check(Bits(value.W) == Bits(original.W) && Bits(value.W) == Bits(input[(batch + 3) % input.Length]));
            Console.WriteLine("Log2:v4:" + batch + ":3:" + Bits(value.W) + ":" + Bits(actual.W));
        }
        for (int route = 0; route < 3; route++)
        for (int fail = 0; fail < 2; fail++)
        {
            Calls = 0; ThrowOperand = fail != 0; bool caught = false;
            try
            {
                Vector4 result;
                if (route == 0) result = Vector4.Log2(Operandv4(new Vector4(1)));
                else if (route == 1) result = callback(Operandv4(new Vector4(1)));
                else result = pointer(Operandv4(new Vector4(1)));
                Check(Bits(result.X) == "00000000");
            }
            catch (InvalidOperationException error) when (error.Message == "log operand") { caught = true; }
            Check(Calls == 1 && caught == (fail != 0));
        }
        ThrowOperand = false;
        Console.WriteLine("order:Log2:v4:direct-delegate-calli");
    }
}
