using System;
using System.Globalization;
using System.Numerics;
using System.Runtime.Intrinsics;

namespace VectorProbe;

// The portable-SIMD vector lowering over int / byte / float, diffed against real
// .NET. Only ops whose scalar emulation is bit-exact are printed:
// IsHardwareAccelerated is deliberately not observed (it folds to false here) and
// float Sum is avoided, being rounding-order dependent.
internal static class Program
{
    private static void Main()
    {
        // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        Vector128<int> a = Vector128.Create(1, 2, 3, 4);   // element list
        Vector128<int> b = Vector128.Create(10);           // broadcast
        PrintI32("a", a);
        PrintI32("b", b);
        PrintI32("a+b", a + b);
        PrintI32("b-a", b - a);
        PrintI32("a*b", a * b);
        PrintI32("a*3", a * 3);                            // vector * scalar
        Console.WriteLine("count=" + Vector128<int>.Count);
        Console.WriteLine("sum(a)=" + Vector128.Sum(a));
        Console.WriteLine("dot=" + Vector128.Dot(a, b));

        // Bitwise and shift.
        Vector128<int> x = Vector128.Create(0xF0, 0x0F, 0xFF, 0x00);
        Vector128<int> y = Vector128.Create(0x3C, 0x3C, 0x3C, 0x3C);
        PrintI32("x&y", x & y);
        PrintI32("x|y", x | y);
        PrintI32("x^y", x ^ y);
        PrintI32("x<<2", x << 2);
        PrintI32("~x", ~x);

        // Lane access.
        Vector128<int> w = Vector128.Create(5, 6, 7, 8);
        Console.WriteLine("w[2]=" + w.GetElement(2));
        PrintI32("w.With(1,99)", w.WithElement(1, 99));
        Console.WriteLine("toScalar=" + w.ToScalar());

        // Comparison masks and MSB extraction.
        Vector128<int> p = Vector128.Create(1, 5, 3, 9);
        Vector128<int> q = Vector128.Create(4, 4, 4, 4);
        Vector128<int> gt = Vector128.GreaterThan(p, q);
        Vector128<int> eq = Vector128.Equals(p, Vector128.Create(1, 4, 3, 4));
        Console.WriteLine("gt msb=" + gt.ExtractMostSignificantBits());
        Console.WriteLine("eq msb=" + eq.ExtractMostSignificantBits());
        Console.WriteLine("eqAll=" + (p == Vector128.Create(1, 5, 3, 9)));
        Console.WriteLine("neq=" + (p != q));
        PrintI32("cond", Vector128.ConditionalSelect(gt, p, q));

        // Sub-word lanes.
        Vector128<byte> bytes = Vector128.Create(
            (byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
        Console.WriteLine("byteSum=" + (int)Vector128.Sum(bytes));
        Console.WriteLine("byte[7]=" + (int)bytes.GetElement(7));

        // float -> int, over whole values so the conversion is exact.
        Vector128<float> fa = Vector128.Create(1.0f, 2.0f, 3.0f, 4.0f);
        Vector128<float> fb = Vector128.Create(2.0f, 2.0f, 2.0f, 2.0f);
        PrintI32("conv(fa*fb)", Vector128.ConvertToInt32(fa * fb));

        // A wider register width.
        Vector256<int> va = Vector256.Create(1, 2, 3, 4, 5, 6, 7, 8);
        Vector256<int> vb = Vector256.Create(2);
        Console.WriteLine("v256 count=" + Vector256<int>.Count);
        Console.WriteLine("v256 sum=" + Vector256.Sum(va + vb));
        Console.WriteLine("v256[5]=" + (va + vb).GetElement(5));

        // Saturating arithmetic: lanes clamp at the type bounds.
        Vector128<int> satA = Vector128.Create(int.MaxValue, int.MaxValue - 5, int.MinValue, 100);
        Vector128<int> satB = Vector128.Create(10, 10, -10, -50);
        PrintI32("addSat", Vector128.AddSaturate(satA, satB));          // [MAX, MAX, MIN, 50]
        PrintI32("subSat", Vector128.SubtractSaturate(
            Vector128.Create(int.MinValue, 0, int.MaxValue, 5),
            Vector128.Create(10, int.MinValue, -10, 3)));               // [MIN, MAX, MAX, 2]
        Vector128<byte> bsat = Vector128.AddSaturate(
            Vector128.Create((byte)250, 200, 0, 100, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12),
            Vector128.Create((byte)10, 100, 5, 50, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
        Console.WriteLine("bSat=[" + (int)bsat.GetElement(0) + "," + (int)bsat.GetElement(1)
            + "," + (int)bsat.GetElement(2) + "," + (int)bsat.GetElement(3) + "]");   // [255,255,5,150]

        PrintI32("min", Vector128.Min(p, q));
        PrintI32("max", Vector128.Max(p, q));
        PrintI32("minN", Vector128.MinNative(p, q));
        PrintI32("clampN", Vector128.ClampNative(p, Vector128.Create(2), Vector128.Create(6)));

        // Float rounding; the results are whole, so the int conversion stays exact.
        Vector128<float> rf = Vector128.Create(1.4f, -1.5f, 2.5f, -2.6f);
        PrintI32("floor", ToI(Vector128.Floor(rf)));     // [1,-2,2,-3]
        PrintI32("ceil", ToI(Vector128.Ceiling(rf)));    // [2,-1,3,-2]
        PrintI32("round", ToI(Vector128.Round(rf)));     // banker's: [1,-2,2,-3]
        PrintI32("trunc", ToI(Vector128.Truncate(rf)));  // toward zero: [1,-1,2,-2]

        // Float category predicates, printed as MSB masks.
        Vector128<float> spec = Vector128.Create(1.0f, float.NaN, float.PositiveInfinity, -2.0f);
        Console.WriteLine("isNaN=" + Vector128.IsNaN(spec).ExtractMostSignificantBits());
        Console.WriteLine("isFinite=" + Vector128.IsFinite(spec).ExtractMostSignificantBits());
        Console.WriteLine("isInf=" + Vector128.IsInfinity(spec).ExtractMostSignificantBits());
        // IsNegative reads the sign bit, and a NaN's is not stable across emitters,
        // so these lanes must stay sign-unambiguous.
        Vector128<float> negspec = Vector128.Create(1.0f, -3.0f, float.PositiveInfinity, -2.0f);
        Console.WriteLine("isNeg=" + Vector128.IsNegative(negspec).ExtractMostSignificantBits());
        Vector128<float> zspec = Vector128.Create(0.0f, -0.0f, 1.0f, 0.0f);
        Console.WriteLine("isZeroF=" + Vector128.IsZero(zspec).ExtractMostSignificantBits());

        Vector128<int> ipred = Vector128.Create(0, -3, 4, 7);
        Console.WriteLine("intIsZero=" + Vector128.IsZero(ipred).ExtractMostSignificantBits());
        Console.WriteLine("intIsNeg=" + Vector128.IsNegative(ipred).ExtractMostSignificantBits());
        Console.WriteLine("intIsEven=" + Vector128.IsEvenInteger(ipred).ExtractMostSignificantBits());
        Console.WriteLine("intIsOdd=" + Vector128.IsOddInteger(ipred).ExtractMostSignificantBits());

        // Element search and mask population.
        Vector128<int> sv = Vector128.Create(5, 7, 5, 9);
        Console.WriteLine("count5=" + Vector128.Count(sv, 5));
        Console.WriteLine("idx5=" + Vector128.IndexOf(sv, 5));
        Console.WriteLine("lidx5=" + Vector128.LastIndexOf(sv, 5));
        Console.WriteLine("idx8=" + Vector128.IndexOf(sv, 8));
        Console.WriteLine("all5=" + Vector128.All(sv, 5));
        Console.WriteLine("any9=" + Vector128.Any(sv, 9));
        Console.WriteLine("all7=" + Vector128.All(Vector128.Create(7, 7, 7, 7), 7));
        Vector128<int> abs1 = Vector128.Create(-1, 0, -1, 5);
        Console.WriteLine("cntABS=" + Vector128.CountWhereAllBitsSet(abs1));
        Console.WriteLine("idxABS=" + Vector128.IndexOfWhereAllBitsSet(abs1));
        Console.WriteLine("lidxABS=" + Vector128.LastIndexOfWhereAllBitsSet(abs1));
        Console.WriteLine("anyABS=" + Vector128.AnyWhereAllBitsSet(abs1));
        Console.WriteLine("allABS=" + Vector128.AllWhereAllBitsSet(Vector128<int>.AllBitsSet));

        // CopySign: IEEE for float, magnitude-of-a-with-sign-of-b for signed int.
        PrintI32("copysignF", ToI(Vector128.CopySign(
            Vector128.Create(3.0f, 4.0f, 5.0f, 6.0f), Vector128.Create(-1.0f, 1.0f, -2.0f, 2.0f))));
        PrintI32("copysignI", Vector128.CopySign(
            Vector128.Create(3, 4, 5, 6), Vector128.Create(-1, 1, -2, 2)));

        // Transcendentals, scaled and rounded to ints so the output is stable.
        Vector128<float> cosv = Vector128.Cos(Vector128.Create(0.0f, 0.5f, 1.0f, 2.0f));
        Console.WriteLine("cos100=[" + R100(cosv.GetElement(0)) + "," + R100(cosv.GetElement(1))
            + "," + R100(cosv.GetElement(2)) + "," + R100(cosv.GetElement(3)) + "]");
        Vector128<float> expv = Vector128.Exp(Vector128.Create(0.0f, 1.0f, 2.0f, -1.0f));
        Console.WriteLine("exp100=[" + R100(expv.GetElement(0)) + "," + R100(expv.GetElement(1))
            + "," + R100(expv.GetElement(2)) + "," + R100(expv.GetElement(3)) + "]");
        // Pythagorean triples keep Hypot exactly integral.
        PrintI32("hypot", ToI(Vector128.Hypot(
            Vector128.Create(3.0f, 5.0f, 8.0f, 0.0f), Vector128.Create(4.0f, 12.0f, 6.0f, 7.0f))));

        // All whole-valued, so the printed output stays bit-exact.
        Vector128<int> nn = Vector128.Create(3, -4, 0, -7);
        PrintI32("neg", -nn);
        PrintI32("absI", Vector128.Abs(nn));
        PrintI32("sqrtI", ToI(Vector128.Sqrt(Vector128.Create(4.0f, 9.0f, 16.0f, 25.0f))));
        PrintI32("fma", ToI(Vector128.FusedMultiplyAdd(
            Vector128.Create(2.0f, 3.0f, 4.0f, 5.0f), Vector128.Create(3.0f, 3.0f, 3.0f, 3.0f),
            Vector128.Create(1.0f, 1.0f, 1.0f, 1.0f))));                 // a*b+c = [7,10,13,16]
        PrintI32("andnot", Vector128.AndNot(                              // a & ~b
            Vector128.Create(0xFF, 0x0F, 0xF0, 0xFF), Vector128.Create(0x0F, 0x0F, 0x0F, 0x0F)));
        Vector128<int> sh = Vector128.Create(-16, 64, -128, 256);
        PrintI32("shrL", Vector128.ShiftRightLogical(sh, 2));             // unsigned-fill
        PrintI32("shrA", Vector128.ShiftRightArithmetic(sh, 2));          // sign-fill: [-4,16,-32,64]
        Vector128<int> cl = Vector128.Create(1, 5, 3, 9);
        Vector128<int> cr = Vector128.Create(4, 4, 4, 4);
        Console.WriteLine("lt msb=" + Vector128.LessThan(cl, cr).ExtractMostSignificantBits());
        Console.WriteLine("le msb=" + Vector128.LessThanOrEqual(cl, cr).ExtractMostSignificantBits());
        Console.WriteLine("ge msb=" + Vector128.GreaterThanOrEqual(cl, cr).ExtractMostSignificantBits());

        Console.WriteLine("gtAll=" + Vector128.GreaterThanAll(Vector128.Create(5, 6, 7, 8), cr));
        Console.WriteLine("gtAny=" + Vector128.GreaterThanAny(Vector128.Create(5, 1, 1, 1), cr));
        Console.WriteLine("ltAll=" + Vector128.LessThanAll(cl, Vector128.Create(10, 10, 10, 10)));
        Console.WriteLine("ltAny=" + Vector128.LessThanAny(Vector128.Create(9, 9, 2, 9), cr));

        // Whole-valued, so the float division stays exact.
        PrintI32("divF", ToI(Vector128.Create(12.0f, 20.0f, 30.0f, 56.0f)
            / Vector128.Create(3.0f, 4.0f, 5.0f, 8.0f)));                 // [4,5,6,7]
        PrintI32("divI", Vector128.Create(20, 21, 30, 41)
            / Vector128.Create(4, 7, 5, 8));                             // integer division: [5,3,6,5]

        // 16-bit lanes, which is the Highway SaturatedAdd/Sub path.
        PrintI16("addSat16", Vector128.AddSaturate(
            Vector128.Create((short)30000, -30000, 100, 200, 0, 1, 2, 3),
            Vector128.Create((short)5000, -5000, 50, 60, 0, 1, 2, 3)));   // [32767,-32768,150,260,0,2,4,6]
        PrintI16("subSat16", Vector128.SubtractSaturate(
            Vector128.Create((short)-30000, 30000, 100, 200, 0, 1, 2, 3),
            Vector128.Create((short)5000, -5000, 50, 60, 0, 1, 2, 3)));   // [-32768,32767,50,140,0,0,0,0]

        // Sign-unambiguous lanes only: -0.0f folds to +0.0f through some emitters, so
        // the sign-bit-sensitive IsPositive would diverge on it.
        Vector128<float> posspec = Vector128.Create(1.0f, -2.0f, float.PositiveInfinity, -3.0f);
        Console.WriteLine("isPos=" + Vector128.IsPositive(posspec).ExtractMostSignificantBits());
        Console.WriteLine("intIsPos=" + Vector128.IsPositive(Vector128.Create(0, -3, 4, 7)).ExtractMostSignificantBits());
        Vector128<float> infspec = Vector128.Create(float.PositiveInfinity, float.NegativeInfinity, 1.0f, float.PositiveInfinity);
        Console.WriteLine("isPosInf=" + Vector128.IsPositiveInfinity(infspec).ExtractMostSignificantBits());
        Console.WriteLine("isNegInf=" + Vector128.IsNegativeInfinity(infspec).ExtractMostSignificantBits());

        AsVectorReinterprets();
        AsVector234Conversions();
        BoxedVectorIdentity();
        QuaternionPlaneReinterprets();
        VectorToStrings();
    }

    private static void VectorToStrings()
    {
        Console.WriteLine("-- vector tostring --");
        Vector64<int> v64 = Vector64.Create(1, 2);
        Vector128<float> v128 = Vector128.Create(1.5f, 2.5f, 3.5f, 4.5f);
        Vector256<int> v256 = Vector256.Create(1, 2, 3, 4, 5, 6, 7, 8);
        Vector512<int> v512 = Vector512.Create(1, 2, 3, 4, 5, 6, 7, 8,
            9, 10, 11, 12, 13, 14, 15, 16);
        Console.WriteLine("v64=" + v64.ToString());
        Console.WriteLine("v128=" + v128.ToString());
        Console.WriteLine("v256=" + v256.ToString());
        Console.WriteLine("v512=" + v512.ToString());

        Vector<float> vn = new Vector<float>(1.5f);
        Console.WriteLine("vn=" + vn.ToString());
        Console.WriteLine("vn F2=" + vn.ToString("F2"));
        Console.WriteLine("vn de=" + vn.ToString("F2", new CultureInfo("de-DE")));

        object boxedFixed = v64;
        object boxedNumerics = vn;
        Console.WriteLine("boxed v64=" + boxedFixed.ToString());
        Console.WriteLine("boxed vn=" + boxedNumerics.ToString());
        Console.WriteLine("generic v64=" + GenericToString(v64));
        Console.WriteLine("generic vn=" + GenericToString(vn));
        Console.WriteLine($"interp v64={v64}");
        Console.WriteLine($"interp vn={vn:F2}");
    }

    private static string GenericToString<T>(T value) => value!.ToString()!;

    // Quaternion/Plane.Equals pivot through the internal AsVector128 reinterpret and
    // the lane-wise ops return through AsQuaternion/AsPlane — both directions of the
    // 16-byte System.Numerics struct <-> Vector128<float> reinterpret. These bodies
    // enter the emit set from the LOAD SET (a serializer's well-known formatter table
    // names the family), so they must compile even in a program that never uses them.
    private static void QuaternionPlaneReinterprets()
    {
        var q1 = new Quaternion(1f, 2f, 3f, 4f);
        var q2 = new Quaternion(1f, 2f, 3f, 4f);
        var q3 = new Quaternion(1f, 2f, 3f, 5f);
        Console.WriteLine("q== " + (q1 == q2) + " " + q1.Equals(q3) + " " + (q1 != q3));
        Console.WriteLine("qneg " + Quaternion.Negate(q1));
        Console.WriteLine("qconj " + Quaternion.Conjugate(q1));
        Console.WriteLine("qadd " + Quaternion.Add(q1, q3));
        var p1 = new Plane(2f, 4f, 8f, 16f);
        var p2 = new Plane(2f, 4f, 8f, 16f);
        var p3 = new Plane(2f, 4f, 8f, 17f);
        Console.WriteLine("p== " + (p1 == p2) + " " + p1.Equals(p3) + " " + (p1 != p3));
        Console.WriteLine("pdot " + Plane.Dot(p1, new Vector4(1f, 1f, 1f, 1f)));
    }

    // A boxed vector's type identity, which must be exact PER INSTANTIATION even
    // though the whole Vector64/128/256/512<T> and Vector<T> families collapse onto
    // one C++ struct per width. That holds because a vector is a headerless POD the
    // runtime never allocates: the only way one becomes an object is a generated
    // `box`, which stamps the per-instantiation ti_ the type token names. A
    // regression here is a COLLAPSE (one handle answering for two element types),
    // which is what the `is` cross-checks below are for.
    private static void BoxedVectorIdentity()
    {
        // This is the only place the intrinsic generics' NAME surface is diffed
        // against real .NET: an intrinsic type is outside the emit set, so its
        // type-info is the minimal row TypeMetadataEmitter writes, and the three
        // names below compose from the genericDef/genericArgs that row carries.
        object bi = Vector128.Create(1, 2, 3, 4);
        object bf = Vector128.Create(1.0f, 2.0f, 3.0f, 4.0f);
        Console.WriteLine("boxv128i is i=" + (bi is Vector128<int>) + " is f=" + (bi is Vector128<float>));
        Console.WriteLine("boxv128f is i=" + (bf is Vector128<int>) + " is f=" + (bf is Vector128<float>));
        Console.WriteLine("boxv128i eq=" + (bi.GetType() == typeof(Vector128<int>)));
        Console.WriteLine("boxv128f eq=" + (bf.GetType() == typeof(Vector128<float>)));
        Console.WriteLine("boxv128 distinct=" + (bi.GetType() != bf.GetType()));
        // A second width, so a per-width collapse shows up as well as a per-element one.
        object b64 = Vector64.Create(1, 2);
        Console.WriteLine("boxv64i is64=" + (b64 is Vector64<int>) + " is128=" + (b64 is Vector128<int>));
        // The tightest cell: Vector<T> shares the 128-bit struct with Vector128<T>, so
        // same width, same struct, different CLR type.
        object bn = new Vector<int>(7);
        Console.WriteLine("boxvnum isN=" + (bn is Vector<int>) + " is128=" + (bn is Vector128<int>));
        // The payload must survive too — an identity fix that moved it is worse.
        Console.WriteLine("unboxv128i=" + ((Vector128<int>)bi).GetElement(2));
        // GetGenericArguments reads the same two row members the names do, so it is
        // the cheapest way to catch a half-filled row.
        Type ti = bi.GetType();
        Console.WriteLine("v128i Name=" + ti.Name + " Namespace=" + ti.Namespace);
        Console.WriteLine("v128i ToString=" + ti.ToString());
        Console.WriteLine("v128i FullName=" + ti.FullName);
        Console.WriteLine("v128i args=" + ti.GetGenericArguments().Length
            + " [0]=" + ti.GetGenericArguments()[0].Name
            + " def=" + ti.GetGenericTypeDefinition().FullName
            + " defshared=" + (ti.GetGenericTypeDefinition() == bf.GetType().GetGenericTypeDefinition()));
        Console.WriteLine("v128f ToString=" + bf.GetType().ToString());
        Console.WriteLine("v64i ToString=" + b64.GetType().ToString());
        Console.WriteLine("vnum ToString=" + bn.GetType().ToString());
    }

    // Vector<T>'s AsVector<elem> reinterprets. Vector<T>'s width is
    // HARDWARE-DEPENDENT in real .NET while dn2cpp fixes it at 16 bytes, so this
    // section must never observe it: every source is a BROADCAST (lane 0 speaks for
    // the register at any width) and every assertion is a round-trip equality or a
    // low-lane read. Little-endian is assumed, as every supported target is.
    private static void AsVectorReinterprets()
    {
        // A reinterpret preserves BITS, not value: 0x01020304 read back as bytes is
        // 04,03,02,01, which a conversion would not produce.
        var i32 = new Vector<int>(0x01020304);
        var asB = Vector.AsVectorByte(i32);
        Console.WriteLine("asVectorByte lanes0..3=" + asB[0] + "," + asB[1] + "," + asB[2] + "," + asB[3]);
        var asSB = Vector.AsVectorSByte(i32);
        Console.WriteLine("asVectorSByte lane0=" + asSB[0]);
        var asU16 = Vector.AsVectorUInt16(i32);
        Console.WriteLine("asVectorUInt16 lanes0..1=" + asU16[0] + "," + asU16[1]);
        var asI16 = Vector.AsVectorInt16(i32);
        Console.WriteLine("asVectorInt16 lane0=" + asI16[0]);
        var asU32 = Vector.AsVectorUInt32(i32);
        Console.WriteLine("asVectorUInt32 lane0=" + asU32[0]);

        // Across the float/int boundary, both directions.
        var fbits = new Vector<int>(BitConverter.SingleToInt32Bits(1.5f));
        Console.WriteLine("asVectorSingle lane0=" + Vector.AsVectorSingle(fbits)[0]);
        Console.WriteLine("single->int32 bits=" + (Vector.AsVectorInt32(new Vector<float>(1.5f))[0]
            == BitConverter.SingleToInt32Bits(1.5f)));
        var dbits = new Vector<long>(BitConverter.DoubleToInt64Bits(2.25));
        Console.WriteLine("asVectorDouble lane0=" + Vector.AsVectorDouble(dbits)[0]);
        var i64 = new Vector<long>(0x0102030405060708L);
        Console.WriteLine("asVectorInt64 lane0=" + Vector.AsVectorInt64(i64)[0]);
        Console.WriteLine("asVectorUInt64 lane0=" + Vector.AsVectorUInt64(i64)[0]);

        // A reinterpret is its own inverse, so each must restore the exact vector at
        // any register width. For the pointer-width-dependent AsVectorNInt/NUInt the
        // round-trip is the only assertion available.
        Console.WriteLine("rt byte    =" + (Vector.AsVectorInt32(Vector.AsVectorByte(i32)) == i32));
        Console.WriteLine("rt sbyte   =" + (Vector.AsVectorInt32(Vector.AsVectorSByte(i32)) == i32));
        Console.WriteLine("rt int16   =" + (Vector.AsVectorInt32(Vector.AsVectorInt16(i32)) == i32));
        Console.WriteLine("rt uint16  =" + (Vector.AsVectorInt32(Vector.AsVectorUInt16(i32)) == i32));
        Console.WriteLine("rt int32   =" + (Vector.AsVectorInt32(Vector.AsVectorInt32(i32)) == i32));
        Console.WriteLine("rt uint32  =" + (Vector.AsVectorInt32(Vector.AsVectorUInt32(i32)) == i32));
        Console.WriteLine("rt int64   =" + (Vector.AsVectorInt32(Vector.AsVectorInt64(i32)) == i32));
        Console.WriteLine("rt uint64  =" + (Vector.AsVectorInt32(Vector.AsVectorUInt64(i32)) == i32));
        Console.WriteLine("rt nint    =" + (Vector.AsVectorInt32(Vector.AsVectorNInt(i32)) == i32));
        Console.WriteLine("rt nuint   =" + (Vector.AsVectorInt32(Vector.AsVectorNUInt(i32)) == i32));
        Console.WriteLine("rt single  =" + (Vector.AsVectorInt32(Vector.AsVectorSingle(i32)) == i32));
        Console.WriteLine("rt double  =" + (Vector.AsVectorInt32(Vector.AsVectorDouble(i32)) == i32));
    }

    // Vector2/3/4 <-> Vector128 conversions: pure byte reinterprets pivoting through
    // Vector128<float>. The SAFE forms zero-extend the unused lanes on both sides, so
    // those zeros are observed; the *Unsafe forms leave the padding UNDEFINED in real
    // .NET, so only the prefix a source actually filled may be printed.
    private static void AsVector234Conversions()
    {
        // Safe widen: the zeros are the assertion.
        Vector3 s23 = new Vector2(1f, 2f).AsVector3();
        Console.WriteLine("v2->v3   =" + s23.X + "," + s23.Y + "," + s23.Z);
        Vector4 s24 = new Vector2(1f, 2f).AsVector4();
        Console.WriteLine("v2->v4   =" + s24.X + "," + s24.Y + "," + s24.Z + "," + s24.W);
        Vector4 s34 = new Vector3(1f, 2f, 3f).AsVector4();
        Console.WriteLine("v3->v4   =" + s34.X + "," + s34.Y + "," + s34.Z + "," + s34.W);

        // Unsafe widen: prefix only.
        Vector3 u23 = new Vector2(5f, 6f).AsVector3Unsafe();
        Console.WriteLine("v2->v3!  =" + u23.X + "," + u23.Y);
        Vector4 u24 = new Vector2(5f, 6f).AsVector4Unsafe();
        Console.WriteLine("v2->v4!  =" + u24.X + "," + u24.Y);
        Vector4 u34 = new Vector3(5f, 6f, 7f).AsVector4Unsafe();
        Console.WriteLine("v3->v4!  =" + u34.X + "," + u34.Y + "," + u34.Z);

        // Narrow: exact prefix, no padding.
        Vector2 n42 = new Vector4(1f, 2f, 3f, 4f).AsVector2();
        Console.WriteLine("v4->v2   =" + n42.X + "," + n42.Y);
        Vector3 n43 = new Vector4(1f, 2f, 3f, 4f).AsVector3();
        Console.WriteLine("v4->v3   =" + n43.X + "," + n43.Y + "," + n43.Z);
        Vector2 n32 = new Vector3(1f, 2f, 3f).AsVector2();
        Console.WriteLine("v3->v2   =" + n32.X + "," + n32.Y);

        // The Vector128<float> pivot: safe, so all four lanes are defined.
        Console.WriteLine("v2->v128 =" + LanesF(new Vector2(8f, 9f).AsVector128()));
        Console.WriteLine("v3->v128 =" + LanesF(new Vector3(8f, 9f, 10f).AsVector128()));
        Console.WriteLine("v4->v128 =" + LanesF(new Vector4(1f, 2f, 3f, 4f).AsVector128()));

        // Back out of the pivot, reading the low prefix.
        Vector128<float> p = Vector128.Create(1f, 2f, 3f, 4f);
        Vector2 q2 = p.AsVector2();
        Console.WriteLine("v128->v2 =" + q2.X + "," + q2.Y);
        Vector3 q3 = p.AsVector3();
        Console.WriteLine("v128->v3 =" + q3.X + "," + q3.Y + "," + q3.Z);
        Vector4 q4 = p.AsVector4();
        Console.WriteLine("v128->v4 =" + q4.X + "," + q4.Y + "," + q4.Z + "," + q4.W);

        // A reinterpret is its own inverse on the defined lanes.
        Vector4 rt4 = new Vector4(11f, 22f, 33f, 44f).AsVector128().AsVector4();
        Console.WriteLine("rt v4v128=" + rt4.X + "," + rt4.Y + "," + rt4.Z + "," + rt4.W);
        Vector2 rt2 = new Vector2(7f, 8f).AsVector3().AsVector2();
        Console.WriteLine("rt v2v3  =" + rt2.X + "," + rt2.Y);
    }

    private static string LanesF(Vector128<float> v) =>
        v.GetElement(0) + "," + v.GetElement(1) + "," + v.GetElement(2) + "," + v.GetElement(3);

    private static void PrintI16(string label, Vector128<short> v)
    {
        string s = label + "=[";
        for (int i = 0; i < Vector128<short>.Count; i++)
            s += (i > 0 ? "," : "") + (int)v.GetElement(i);
        Console.WriteLine(s + "]");
    }

    // Exact only for whole-valued inputs, which every caller passes.
    private static Vector128<int> ToI(Vector128<float> v) => Vector128.ConvertToInt32(v);

    // Absorbs the sub-ULP gap between libm and the BCL's vectorized approximation,
    // and makes the printed value independent of float formatting.
    private static int R100(float v) => (int)Math.Round((double)v * 100.0);

    private static void PrintI32(string label, Vector128<int> v)
    {
        Console.WriteLine(label + "=[" + v.GetElement(0) + "," + v.GetElement(1)
            + "," + v.GetElement(2) + "," + v.GetElement(3) + "]");
    }
}
