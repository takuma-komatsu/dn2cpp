using System;

namespace GodotSample
{
    internal abstract class Animal
    {
        public abstract string Speak();
    }

    internal sealed class Dog : Animal
    {
        public override string Speak()
        {
            return "Woof!";
        }
    }

    internal sealed class Cat : Animal
    {
        public override string Speak()
        {
            return "Meow!";
        }
    }

    internal struct Vec2
    {
        public float X;
        public float Y;

        public Vec2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float LengthSquared()
        {
            return X * X + Y * Y;
        }
    }

    /// <summary>Public static methods here are exposed to GDScript as
    /// `GodotApi.MethodName(...)` by the dn2cpp GDExtension bridge.</summary>
    public static class GodotApi
    {
        public static float VecLengthSquared(float x, float y)
        {
            Vec2 v = new Vec2(x, y);
            return v.LengthSquared();
        }

        public static float VecLengthSquared2(Godot.Vector2 v)
        {
            return v.X * v.X + v.Y * v.Y;
        }

        // Matrix transform operators (hand-written shim partials). test.gd computes the same
        // product with a native Transform2D/Basis, so a wrong row/column convention fails
        // loudly. Components are returned individually — float return is the proven path.
        public static float Transform2DXformX(Godot.Vector2 col0, Godot.Vector2 col1, Godot.Vector2 origin, Godot.Vector2 v)
        {
            Godot.Transform2D t = new Godot.Transform2D(col0, col1, origin);
            return (t * v).X;
        }

        public static float Transform2DXformY(Godot.Vector2 col0, Godot.Vector2 col1, Godot.Vector2 origin, Godot.Vector2 v)
        {
            Godot.Transform2D t = new Godot.Transform2D(col0, col1, origin);
            return (t * v).Y;
        }

        // A fixed asymmetric Basis, mirrored verbatim in test.gd's native Basis; `v` is
        // passed in so the GDScript side controls the vector.
        private static Godot.Basis CrossCheckBasis() =>
            new Godot.Basis(new Godot.Vector3(1, 2, 3), new Godot.Vector3(4, 5, 6), new Godot.Vector3(7, 8, 10));

        public static float BasisXformX(float vx, float vy, float vz) =>
            (CrossCheckBasis() * new Godot.Vector3(vx, vy, vz)).X;

        public static float BasisXformY(float vx, float vy, float vz) =>
            (CrossCheckBasis() * new Godot.Vector3(vx, vy, vz)).Y;

        public static float BasisXformZ(float vx, float vy, float vz) =>
            (CrossCheckBasis() * new Godot.Vector3(vx, vy, vz)).Z;

        // Transform3D = the cross-check Basis plus a baked origin, mirrored in test.gd.
        private static Godot.Transform3D CrossCheckTransform3D() =>
            new Godot.Transform3D(CrossCheckBasis(), new Godot.Vector3(100, 200, 300));

        public static float Transform3DXformX(float vx, float vy, float vz) =>
            (CrossCheckTransform3D() * new Godot.Vector3(vx, vy, vz)).X;

        public static float Transform3DXformY(float vx, float vy, float vz) =>
            (CrossCheckTransform3D() * new Godot.Vector3(vx, vy, vz)).Y;

        public static float Transform3DXformZ(float vx, float vy, float vz) =>
            (CrossCheckTransform3D() * new Godot.Vector3(vx, vy, vz)).Z;

        // ---- Composition cross-checks ----
        // Compose two fixed matrices, apply to a probe vector, return the components.
        // GDScript probes with the basis vectors + origin so every element is recovered.
        private static Godot.Transform2D CrossCheckT2DB() =>
            new Godot.Transform2D(new Godot.Vector2(1, 2), new Godot.Vector2(3, 4), new Godot.Vector2(5, 6));
        private static Godot.Transform2D CrossCheckT2DA() =>
            new Godot.Transform2D(new Godot.Vector2(2, 0), new Godot.Vector2(0, 3), new Godot.Vector2(10, 20));

        public static float Transform2DMulXformX(float vx, float vy) =>
            ((CrossCheckT2DA() * CrossCheckT2DB()) * new Godot.Vector2(vx, vy)).X;

        public static float Transform2DMulXformY(float vx, float vy) =>
            ((CrossCheckT2DA() * CrossCheckT2DB()) * new Godot.Vector2(vx, vy)).Y;

        private static Godot.Basis CrossCheckBasisB() =>
            new Godot.Basis(new Godot.Vector3(2, 0, 1), new Godot.Vector3(1, 3, 0), new Godot.Vector3(0, 1, 2));

        public static float BasisMulXformX(float vx, float vy, float vz) =>
            ((CrossCheckBasis() * CrossCheckBasisB()) * new Godot.Vector3(vx, vy, vz)).X;

        public static float BasisMulXformY(float vx, float vy, float vz) =>
            ((CrossCheckBasis() * CrossCheckBasisB()) * new Godot.Vector3(vx, vy, vz)).Y;

        public static float BasisMulXformZ(float vx, float vy, float vz) =>
            ((CrossCheckBasis() * CrossCheckBasisB()) * new Godot.Vector3(vx, vy, vz)).Z;

        private static Godot.Transform3D CrossCheckTransform3DB() =>
            new Godot.Transform3D(CrossCheckBasisB(), new Godot.Vector3(1, 2, 3));

        public static float Transform3DMulXformX(float vx, float vy, float vz) =>
            ((CrossCheckTransform3D() * CrossCheckTransform3DB()) * new Godot.Vector3(vx, vy, vz)).X;

        public static float Transform3DMulXformY(float vx, float vy, float vz) =>
            ((CrossCheckTransform3D() * CrossCheckTransform3DB()) * new Godot.Vector3(vx, vy, vz)).Y;

        public static float Transform3DMulXformZ(float vx, float vy, float vz) =>
            ((CrossCheckTransform3D() * CrossCheckTransform3DB()) * new Godot.Vector3(vx, vy, vz)).Z;

        // ---- Vector3 as a real export arg/return type ----
        // Vector3 is a supported arg AND return type, so these pass it natively instead of
        // baking the inputs and returning floats.
        public static Godot.Vector3 Vec3Cross(Godot.Vector3 a, Godot.Vector3 b) => a.Cross(b);

        public static Godot.Vector3 BasisXform3(Godot.Vector3 col0, Godot.Vector3 col1, Godot.Vector3 col2, Godot.Vector3 v) =>
            new Godot.Basis(col0, col1, col2) * v;

        // ---- Quaternion operators ----
        // Hamilton product, passed as 8 floats; test.gd asserts against native quaternions.
        public static float QuatMulX(float ax, float ay, float az, float aw, float bx, float by, float bz, float bw) =>
            (new Godot.Quaternion(ax, ay, az, aw) * new Godot.Quaternion(bx, by, bz, bw)).X;

        public static float QuatMulY(float ax, float ay, float az, float aw, float bx, float by, float bz, float bw) =>
            (new Godot.Quaternion(ax, ay, az, aw) * new Godot.Quaternion(bx, by, bz, bw)).Y;

        public static float QuatMulZ(float ax, float ay, float az, float aw, float bx, float by, float bz, float bw) =>
            (new Godot.Quaternion(ax, ay, az, aw) * new Godot.Quaternion(bx, by, bz, bw)).Z;

        public static float QuatMulW(float ax, float ay, float az, float aw, float bx, float by, float bz, float bw) =>
            (new Godot.Quaternion(ax, ay, az, aw) * new Godot.Quaternion(bx, by, bz, bw)).W;

        // Quaternion rotation builders. Components are returned individually — a Quaternion
        // is four floats, not an export type.
        public static float QuatAxisAngleX(Godot.Vector3 axis, float angle) => new Godot.Quaternion(axis, angle).X;
        public static float QuatAxisAngleY(Godot.Vector3 axis, float angle) => new Godot.Quaternion(axis, angle).Y;
        public static float QuatAxisAngleZ(Godot.Vector3 axis, float angle) => new Godot.Quaternion(axis, angle).Z;
        public static float QuatAxisAngleW(Godot.Vector3 axis, float angle) => new Godot.Quaternion(axis, angle).W;

        public static float QuatFromEulerX(Godot.Vector3 e) => Godot.Quaternion.FromEuler(e).X;
        public static float QuatFromEulerY(Godot.Vector3 e) => Godot.Quaternion.FromEuler(e).Y;
        public static float QuatFromEulerZ(Godot.Vector3 e) => Godot.Quaternion.FromEuler(e).Z;
        public static float QuatFromEulerW(Godot.Vector3 e) => Godot.Quaternion.FromEuler(e).W;

        // Slerp between two fixed unit quaternions, mirrored in test.gd; only the weight is
        // passed, since two quaternions plus a weight exceeds the 8-arg export limit.
        private static Godot.Quaternion SlerpA() => new Godot.Quaternion(0.1f, 0.2f, 0.3f, 0.9f).Normalized();
        private static Godot.Quaternion SlerpB() => new Godot.Quaternion(0.5f, -0.2f, 0.1f, 0.8f).Normalized();

        public static float QuatSlerpX(float w) => SlerpA().Slerp(SlerpB(), w).X;
        public static float QuatSlerpY(float w) => SlerpA().Slerp(SlerpB(), w).Y;
        public static float QuatSlerpZ(float w) => SlerpA().Slerp(SlerpB(), w).Z;
        public static float QuatSlerpW(float w) => SlerpA().Slerp(SlerpB(), w).W;

        // Rotate a vector by a (unit) quaternion (quaternion as 4 floats + vector as 3).
        public static float QuatXformX(float qx, float qy, float qz, float qw, float vx, float vy, float vz) =>
            (new Godot.Quaternion(qx, qy, qz, qw) * new Godot.Vector3(vx, vy, vz)).X;

        public static float QuatXformY(float qx, float qy, float qz, float qw, float vx, float vy, float vz) =>
            (new Godot.Quaternion(qx, qy, qz, qw) * new Godot.Vector3(vx, vy, vz)).Y;

        public static float QuatXformZ(float qx, float qy, float qz, float qw, float vx, float vy, float vz) =>
            (new Godot.Quaternion(qx, qy, qz, qw) * new Godot.Vector3(vx, vy, vz)).Z;

        // ---- Relational operators (lexicographic) ----
        // Godot's vector `< <= > >=` compare X, tie-breaking on Y then Z/W — NOT
        // component-wise. The 8-float form exercises them at the 8-arg limit; the
        // native-Vector4 variants are in the block below.
        public static bool Vec2Less(Godot.Vector2 a, Godot.Vector2 b) => a < b;
        public static bool Vec2LessEqual(Godot.Vector2 a, Godot.Vector2 b) => a <= b;
        public static bool Vec2Greater(Godot.Vector2 a, Godot.Vector2 b) => a > b;
        public static bool Vec2GreaterEqual(Godot.Vector2 a, Godot.Vector2 b) => a >= b;

        public static bool Vec3Less(Godot.Vector3 a, Godot.Vector3 b) => a < b;
        public static bool Vec3LessEqual(Godot.Vector3 a, Godot.Vector3 b) => a <= b;
        public static bool Vec3Greater(Godot.Vector3 a, Godot.Vector3 b) => a > b;
        public static bool Vec3GreaterEqual(Godot.Vector3 a, Godot.Vector3 b) => a >= b;

        public static bool Vec4Less(float ax, float ay, float az, float aw, float bx, float by, float bz, float bw) =>
            new Godot.Vector4(ax, ay, az, aw) < new Godot.Vector4(bx, by, bz, bw);
        public static bool Vec4LessEqual(float ax, float ay, float az, float aw, float bx, float by, float bz, float bw) =>
            new Godot.Vector4(ax, ay, az, aw) <= new Godot.Vector4(bx, by, bz, bw);
        public static bool Vec4Greater(float ax, float ay, float az, float aw, float bx, float by, float bz, float bw) =>
            new Godot.Vector4(ax, ay, az, aw) > new Godot.Vector4(bx, by, bz, bw);
        public static bool Vec4GreaterEqual(float ax, float ay, float az, float aw, float bx, float by, float bz, float bw) =>
            new Godot.Vector4(ax, ay, az, aw) >= new Godot.Vector4(bx, by, bz, bw);

        // Integer-vector relational operators, same lexicographic semantics. The i-variants
        // are not supported export arg types, so the components are passed as ints.
        public static bool Vec2iLess(int ax, int ay, int bx, int by) =>
            new Godot.Vector2i(ax, ay) < new Godot.Vector2i(bx, by);
        public static bool Vec2iLessEqual(int ax, int ay, int bx, int by) =>
            new Godot.Vector2i(ax, ay) <= new Godot.Vector2i(bx, by);
        public static bool Vec2iGreater(int ax, int ay, int bx, int by) =>
            new Godot.Vector2i(ax, ay) > new Godot.Vector2i(bx, by);
        public static bool Vec2iGreaterEqual(int ax, int ay, int bx, int by) =>
            new Godot.Vector2i(ax, ay) >= new Godot.Vector2i(bx, by);

        public static bool Vec3iLess(int ax, int ay, int az, int bx, int by, int bz) =>
            new Godot.Vector3i(ax, ay, az) < new Godot.Vector3i(bx, by, bz);
        public static bool Vec3iLessEqual(int ax, int ay, int az, int bx, int by, int bz) =>
            new Godot.Vector3i(ax, ay, az) <= new Godot.Vector3i(bx, by, bz);
        public static bool Vec3iGreater(int ax, int ay, int az, int bx, int by, int bz) =>
            new Godot.Vector3i(ax, ay, az) > new Godot.Vector3i(bx, by, bz);
        public static bool Vec3iGreaterEqual(int ax, int ay, int az, int bx, int by, int bz) =>
            new Godot.Vector3i(ax, ay, az) >= new Godot.Vector3i(bx, by, bz);

        public static bool Vec4iLess(int ax, int ay, int az, int aw, int bx, int by, int bz, int bw) =>
            new Godot.Vector4i(ax, ay, az, aw) < new Godot.Vector4i(bx, by, bz, bw);
        public static bool Vec4iLessEqual(int ax, int ay, int az, int aw, int bx, int by, int bz, int bw) =>
            new Godot.Vector4i(ax, ay, az, aw) <= new Godot.Vector4i(bx, by, bz, bw);
        public static bool Vec4iGreater(int ax, int ay, int az, int aw, int bx, int by, int bz, int bw) =>
            new Godot.Vector4i(ax, ay, az, aw) > new Godot.Vector4i(bx, by, bz, bw);
        public static bool Vec4iGreaterEqual(int ax, int ay, int az, int aw, int bx, int by, int bz, int bw) =>
            new Godot.Vector4i(ax, ay, az, aw) >= new Godot.Vector4i(bx, by, bz, bw);

        // ---- Vector2/Vector3 Slerp, Snapped, Inverse cross-check helpers ----
        public static Godot.Vector2 Vec2Slerp(Godot.Vector2 from, Godot.Vector2 to, float weight) =>
            from.Slerp(to, weight);

        public static Godot.Vector3 Vec3Slerp(Godot.Vector3 from, Godot.Vector3 to, float weight) =>
            from.Slerp(to, weight);

        public static Godot.Vector2 Vec2Snapped(Godot.Vector2 v, Godot.Vector2 step) =>
            v.Snapped(step);

        public static Godot.Vector3 Vec3Snapped(Godot.Vector3 v, Godot.Vector3 step) =>
            v.Snapped(step);

        public static Godot.Vector2 Vec2SnappedVal(Godot.Vector2 v, float step) =>
            v.Snapped(step);

        public static Godot.Vector3 Vec3SnappedVal(Godot.Vector3 v, float step) =>
            v.Snapped(step);

        public static Godot.Vector2 Vec2Inverse(Godot.Vector2 v) =>
            v.Inverse();

        public static Godot.Vector3 Vec3Inverse(Godot.Vector3 v) =>
            v.Inverse();

        // ---- Vector4 / Color as real export arg+return types ----
        // Both are four contiguous floats, so they pass and return natively rather than
        // being decomposed into loose floats.
        public static Godot.Vector4 Vec4Add(Godot.Vector4 a, Godot.Vector4 b) => a + b;
        public static Godot.Vector4 Vec4Mul(Godot.Vector4 a, Godot.Vector4 b) => a * b;

        // Lexicographic relational operators over native Vector4 args.
        public static bool Vec4LessV(Godot.Vector4 a, Godot.Vector4 b) => a < b;
        public static bool Vec4GreaterEqualV(Godot.Vector4 a, Godot.Vector4 b) => a >= b;

        public static Godot.Color ColorInverted(Godot.Color c) => c.Inverted();
        public static Godot.Color ColorLerp(Godot.Color a, Godot.Color b, float weight) => a.Lerp(b, weight);
        public static Godot.Color ColorDarkened(Godot.Color c, float amount) => c.Darkened(amount);
        public static float ColorLuminance(Godot.Color c) => c.GetLuminance();

        // ---- Large value types as real export arg+return types ----
        // Transform2D/Basis/Transform3D/Projection marshal natively via the generic `big`
        // slot. Basis is stored column-major in the shim but row-major natively, so the
        // bridge transposes; the cross-checks are deliberately asymmetric (apply-to-vector
        // on the arg side, build-from-columns on the return side) so a flipped transpose
        // fails loudly instead of cancelling out.
        public static Godot.Transform2D T2DRoundtrip(Godot.Transform2D t) => t;
        public static Godot.Vector2 T2DApply(Godot.Transform2D t, Godot.Vector2 v) => t * v;

        public static Godot.Vector3 BasisApply(Godot.Basis b, Godot.Vector3 v) => b * v;
        public static Godot.Basis BasisFromCols(Godot.Vector3 x, Godot.Vector3 y, Godot.Vector3 z) =>
            new Godot.Basis(x, y, z);

        public static Godot.Vector3 T3DApply(Godot.Transform3D t, Godot.Vector3 v) => t * v;
        public static Godot.Transform3D T3DFromParts(Godot.Basis basis, Godot.Vector3 origin) =>
            new Godot.Transform3D(basis, origin);

        public static Godot.Projection ProjRoundtrip(Godot.Projection p) => p;
        public static Godot.Projection ProjFromCols(Godot.Vector4 x, Godot.Vector4 y, Godot.Vector4 z, Godot.Vector4 w) =>
            new Godot.Projection(x, y, z, w);

        // ---- Basis math methods ----
        // Hand-written over the column-stored Basis, cross-checked against native Godot.
        public static Godot.Basis BasisTransposed(Godot.Basis b) => b.Transposed();
        public static float BasisDeterminant(Godot.Basis b) => b.Determinant();
        public static Godot.Basis BasisInverse(Godot.Basis b) => b.Inverse();

        // ---- Builtin method engine bridge ----
        // Not hand-written: the generated shim signatures lower to the engine's own
        // implementation via variant_get_ptr_builtin_method, so results are bit-identical.
        public static Godot.Color ColorSrgbToLinear(Godot.Color c) => c.SrgbToLinear();
        public static Godot.Color ColorLinearToSrgb(Godot.Color c) => c.LinearToSrgb();
        public static Godot.Color ColorBlend(Godot.Color baseColor, Godot.Color over) => baseColor.Blend(over);

        // bool / float returns through the same bridge.
        public static bool ColorIsEqualApprox(Godot.Color a, Godot.Color b) => a.IsEqualApprox(b);
        public static float Vec2Aspect(Godot.Vector2 v) => v.Aspect();
        public static bool Vec2IsNormalized(Godot.Vector2 v) => v.IsNormalized();
        public static bool Vec3IsEqualApprox(Godot.Vector3 a, Godot.Vector3 b) => a.IsEqualApprox(b);

        // Vector4 method surface via the same bridge: value-type, float and bool returns
        // across 0/1/2-arg and float-arg shapes.
        public static Godot.Vector4 Vec4Abs(Godot.Vector4 v) => v.Abs();
        public static Godot.Vector4 Vec4Normalized(Godot.Vector4 v) => v.Normalized();
        public static Godot.Vector4 Vec4Inverse(Godot.Vector4 v) => v.Inverse();
        public static Godot.Vector4 Vec4Min(Godot.Vector4 a, Godot.Vector4 b) => a.Min(b);
        public static Godot.Vector4 Vec4Max(Godot.Vector4 a, Godot.Vector4 b) => a.Max(b);
        public static Godot.Vector4 Vec4DirectionTo(Godot.Vector4 a, Godot.Vector4 b) => a.DirectionTo(b);
        public static Godot.Vector4 Vec4Snappedf(Godot.Vector4 v, float step) => v.Snappedf(step);
        public static Godot.Vector4 Vec4Lerp(Godot.Vector4 a, Godot.Vector4 b, float w) => a.Lerp(b, w);
        public static Godot.Vector4 Vec4Clamp(Godot.Vector4 v, Godot.Vector4 lo, Godot.Vector4 hi) => v.Clamp(lo, hi);
        public static Godot.Vector4 Vec4Clampf(Godot.Vector4 v, float lo, float hi) => v.Clampf(lo, hi);
        public static float Vec4Length(Godot.Vector4 v) => v.Length();
        public static float Vec4Dot(Godot.Vector4 a, Godot.Vector4 b) => a.Dot(b);
        public static float Vec4DistanceTo(Godot.Vector4 a, Godot.Vector4 b) => a.DistanceTo(b);
        public static bool Vec4IsNormalized(Godot.Vector4 v) => v.IsNormalized();
        public static bool Vec4IsEqualApprox(Godot.Vector4 a, Godot.Vector4 b) => a.IsEqualApprox(b);
        public static bool Vec4IsFinite(Godot.Vector4 v) => v.IsFinite();

        // Quaternion builtin methods via the bridge. Quaternion is not an export type, so
        // quaternions are built from floats in C# and a quaternion result is read back per
        // component (`c` = 0/1/2/3 -> X/Y/Z/W).
        private static float QComp(Godot.Quaternion q, int c) => c == 0 ? q.X : c == 1 ? q.Y : c == 2 ? q.Z : q.W;

        // bool / float / Vector3 returns (quaternion components fit the 8-arg limit).
        public static bool QuatIsNormalized(float x, float y, float z, float w) => new Godot.Quaternion(x, y, z, w).IsNormalized();
        public static bool QuatIsFinite(float x, float y, float z, float w) => new Godot.Quaternion(x, y, z, w).IsFinite();
        public static bool QuatIsEqualApprox(float ax, float ay, float az, float aw, float bx, float by, float bz, float bw) =>
            new Godot.Quaternion(ax, ay, az, aw).IsEqualApprox(new Godot.Quaternion(bx, by, bz, bw));
        public static float QuatAngleTo(float ax, float ay, float az, float aw, float bx, float by, float bz, float bw) =>
            new Godot.Quaternion(ax, ay, az, aw).AngleTo(new Godot.Quaternion(bx, by, bz, bw));
        public static float QuatGetAngle(float x, float y, float z, float w) => new Godot.Quaternion(x, y, z, w).GetAngle();
        public static Godot.Vector3 QuatGetAxis(float x, float y, float z, float w) => new Godot.Quaternion(x, y, z, w).GetAxis();
        public static Godot.Vector3 QuatGetEuler(float x, float y, float z, float w, int order) => new Godot.Quaternion(x, y, z, w).GetEuler(order);

        // Quaternion returns, read back per component. slerpni/spherical_cubic use fixed
        // operands (mirrored in test.gd) since four quaternions plus a weight would blow
        // the 8-arg export limit.
        public static float QuatLogComp(float x, float y, float z, float w, int c) => QComp(new Godot.Quaternion(x, y, z, w).Log(), c);
        public static float QuatExpComp(float x, float y, float z, float w, int c) => QComp(new Godot.Quaternion(x, y, z, w).Exp(), c);
        public static float QuatSlerpniComp(float w, int c) => QComp(SlerpA().Slerpni(SlerpB(), w), c);
        private static Godot.Quaternion CubicPreA() => new Godot.Quaternion(-0.2f, 0.1f, 0.4f, 0.85f).Normalized();
        private static Godot.Quaternion CubicPostB() => new Godot.Quaternion(0.3f, 0.3f, -0.1f, 0.9f).Normalized();
        public static float QuatSphCubicComp(float w, int c) =>
            QComp(SlerpA().SphericalCubicInterpolate(SlerpB(), CubicPreA(), CubicPostB(), w), c);

        // Transform2D + Projection builtin methods via the bridge: large (>16-byte) value
        // types with identical shim and native layout, passed and returned natively —
        // including as an argument.
        public static Godot.Transform2D T2DAffineInverse(Godot.Transform2D t) => t.AffineInverse();
        public static Godot.Transform2D T2DOrthonormalized(Godot.Transform2D t) => t.Orthonormalized();
        public static Godot.Transform2D T2DRotated(Godot.Transform2D t, float angle) => t.Rotated(angle);
        public static Godot.Transform2D T2DScaled(Godot.Transform2D t, Godot.Vector2 s) => t.Scaled(s);
        public static Godot.Transform2D T2DInterpolateWith(Godot.Transform2D a, Godot.Transform2D b, float w) => a.InterpolateWith(b, w);
        public static float T2DDeterminant(Godot.Transform2D t) => t.Determinant();
        public static float T2DGetRotation(Godot.Transform2D t) => t.GetRotation();
        public static Godot.Vector2 T2DGetOrigin(Godot.Transform2D t) => t.GetOrigin();
        public static Godot.Vector2 T2DBasisXform(Godot.Transform2D t, Godot.Vector2 v) => t.BasisXform(v);
        public static bool T2DIsConformal(Godot.Transform2D t) => t.IsConformal();
        public static bool T2DIsEqualApprox(Godot.Transform2D a, Godot.Transform2D b) => a.IsEqualApprox(b);
        public static bool T2DIsFinite(Godot.Transform2D t) => t.IsFinite();
        public static Godot.Projection ProjInverse(Godot.Projection p) => p.Inverse();
        public static Godot.Projection ProjPerspectiveZnearAdjusted(Godot.Projection p, float znear) => p.PerspectiveZnearAdjusted(znear);
        public static Godot.Projection ProjFlippedY(Godot.Projection p) => p.FlippedY();
        public static float ProjDeterminant(Godot.Projection p) => p.Determinant();
        public static float ProjGetAspect(Godot.Projection p) => p.GetAspect();
        public static float ProjGetZFar(Godot.Projection p) => p.GetZFar();
        public static bool ProjIsOrthogonal(Godot.Projection p) => p.IsOrthogonal();
        public static Godot.Vector2 ProjGetViewportHalfExtents(Godot.Projection p) => p.GetViewportHalfExtents();

        // Basis + Transform3D builtin methods via the bridge — the TRANSPOSE matrix types
        // (shim columns vs Godot rows). The bridge transposes receiver, value-type args
        // and value-type return. GetRotationQuaternion is not an export type, so its
        // result is read back per component.
        public static Godot.Basis BasisOrthonormalized(Godot.Basis b) => b.Orthonormalized();
        public static Godot.Basis BasisRotated(Godot.Basis b, Godot.Vector3 axis, float angle) => b.Rotated(axis, angle);
        public static Godot.Basis BasisScaled(Godot.Basis b, Godot.Vector3 s) => b.Scaled(s);
        public static Godot.Vector3 BasisGetScale(Godot.Basis b) => b.GetScale();
        public static Godot.Vector3 BasisGetEuler(Godot.Basis b, int order) => b.GetEuler(order);
        public static float BasisGetRotQuatComp(Godot.Basis b, int c) => QComp(b.GetRotationQuaternion(), c);
        public static Godot.Basis BasisSlerp(Godot.Basis a, Godot.Basis b, float w) => a.Slerp(b, w);
        public static float BasisTdotx(Godot.Basis b, Godot.Vector3 v) => b.Tdotx(v);
        public static bool BasisIsConformal(Godot.Basis b) => b.IsConformal();
        public static bool BasisIsEqualApprox(Godot.Basis a, Godot.Basis b) => a.IsEqualApprox(b);
        public static bool BasisIsFinite(Godot.Basis b) => b.IsFinite();
        public static Godot.Transform3D T3DInverse(Godot.Transform3D t) => t.Inverse();
        public static Godot.Transform3D T3DAffineInverse(Godot.Transform3D t) => t.AffineInverse();
        public static Godot.Transform3D T3DOrthonormalized(Godot.Transform3D t) => t.Orthonormalized();
        public static Godot.Transform3D T3DRotated(Godot.Transform3D t, Godot.Vector3 axis, float angle) => t.Rotated(axis, angle);
        public static Godot.Transform3D T3DScaled(Godot.Transform3D t, Godot.Vector3 s) => t.Scaled(s);
        public static Godot.Transform3D T3DTranslated(Godot.Transform3D t, Godot.Vector3 ofs) => t.Translated(ofs);
        public static Godot.Transform3D T3DInterpolateWith(Godot.Transform3D a, Godot.Transform3D b, float w) => a.InterpolateWith(b, w);
        public static bool T3DIsEqualApprox(Godot.Transform3D a, Godot.Transform3D b) => a.IsEqualApprox(b);
        public static bool T3DIsFinite(Godot.Transform3D t) => t.IsFinite();

        // string return (Color.to_html): the engine writes a heap Godot String into the
        // ptrcall return, and the bridge converts it to a managed string and frees it.
        public static string ColorToHtml(Godot.Color c, bool withAlpha) => c.ToHtml(withAlpha);

        // Variant return (Plane/AABB intersection): NIL for a miss, Vector3 for the hit
        // point. Plane/AABB are not export types, so the geometry is fixed C#-side and
        // GDScript drives the endpoints to reach both branches.
        private static Godot.Plane SlicePlane() => new Godot.Plane(new Godot.Vector3(0f, 0f, 1f), 0f);
        public static bool PlaneRayHit(Godot.Vector3 from, Godot.Vector3 dir) => !SlicePlane().IntersectsRay(from, dir).IsNil;
        public static float PlaneRayX(Godot.Vector3 from, Godot.Vector3 dir) { Godot.Vector3 v = SlicePlane().IntersectsRay(from, dir); return v.X; }
        public static float PlaneRayY(Godot.Vector3 from, Godot.Vector3 dir) { Godot.Vector3 v = SlicePlane().IntersectsRay(from, dir); return v.Y; }
        public static float PlaneRayZ(Godot.Vector3 from, Godot.Vector3 dir) { Godot.Vector3 v = SlicePlane().IntersectsRay(from, dir); return v.Z; }

        private static Godot.AABB SliceAabb() => new Godot.AABB(new Godot.Vector3(0f, 0f, 0f), new Godot.Vector3(2f, 2f, 2f));
        public static bool AabbSegHit(Godot.Vector3 from, Godot.Vector3 to) => !SliceAabb().IntersectsSegment(from, to).IsNil;
        public static float AabbSegX(Godot.Vector3 from, Godot.Vector3 to) { Godot.Vector3 v = SliceAabb().IntersectsSegment(from, to); return v.X; }
        public static float AabbSegY(Godot.Vector3 from, Godot.Vector3 to) { Godot.Vector3 v = SliceAabb().IntersectsSegment(from, to); return v.Y; }
        public static float AabbSegZ(Godot.Vector3 from, Godot.Vector3 to) { Godot.Vector3 v = SliceAabb().IntersectsSegment(from, to); return v.Z; }

        // ---- Packed scalar arrays <-> C# arrays ----
        // A C# scalar array marshals to and from the matching Godot Packed* type by
        // element-wise copy; the assertions live in test.gd.
        public static int SumInts(int[] xs) { int s = 0; foreach (int x in xs) s += x; return s; }
        public static int[] MakeSquares(int n) { int[] a = new int[n]; for (int i = 0; i < n; i++) a[i] = i * i; return a; }
        public static long SumLongs(long[] xs) { long s = 0; foreach (long x in xs) s += x; return s; }
        public static double SumDoubles(double[] xs) { double s = 0; foreach (double x in xs) s += x; return s; }
        public static float SumFloats(float[] xs) { float s = 0; foreach (float x in xs) s += x; return s; }
        public static int SumBytes(byte[] xs) { int s = 0; foreach (byte x in xs) s += x; return s; }
        public static byte[] MakeBytes(int n) { byte[] a = new byte[n]; for (int i = 0; i < n; i++) a[i] = (byte)(i * 2); return a; }
        public static string JoinStrings(string[] xs) { return string.Join("-", xs); }
        public static string[] MakeWords(int n) { string[] a = new string[n]; for (int i = 0; i < n; i++) a[i] = string.Concat("w", i.ToString()); return a; }

        // Struct-element packed arrays: contiguous shim structs riding the same element-wise
        // copy. Components are summed by hand, with no operator dependency.
        public static Godot.Vector2 SumVec2(Godot.Vector2[] xs) { float x = 0, y = 0; foreach (var v in xs) { x += v.X; y += v.Y; } return new Godot.Vector2(x, y); }
        public static Godot.Vector2[] MakeVec2Ramp(int n) { var a = new Godot.Vector2[n]; for (int i = 0; i < n; i++) a[i] = new Godot.Vector2(i, i * 2); return a; }
        public static Godot.Vector3 SumVec3(Godot.Vector3[] xs) { float x = 0, y = 0, z = 0; foreach (var v in xs) { x += v.X; y += v.Y; z += v.Z; } return new Godot.Vector3(x, y, z); }
        public static Godot.Color BlendColors(Godot.Color[] xs) { float r = 0, g = 0, b = 0, a2 = 0; foreach (var c in xs) { r += c.R; g += c.G; b += c.B; a2 += c.A; } return new Godot.Color(r, g, b, a2); }
        public static Godot.Vector4[] MakeVec4Ramp(int n) { var a = new Godot.Vector4[n]; for (int i = 0; i < n; i++) a[i] = new Godot.Vector4(i, i, i, i); return a; }

        // Heterogeneous Array <-> Godot.Variant[], built via the shim Variant's implicit
        // conversions.
        public static int CountVariants(Godot.Variant[] xs) => xs.Length;
        public static int SumVariantInts(Godot.Variant[] xs) { int s = 0; foreach (var v in xs) s += (int)v; return s; }
        public static Godot.Variant[] MakeMixedVariants() => new Godot.Variant[] { 42, 3.5, "hello", true };
        public static Godot.Variant[] EchoVariants(Godot.Variant[] xs) => xs;

        // Richer Variant payloads: the small (<=4-float) builtin value types round-trip
        // through a Variant, read via the shim's reverse conversion.
        public static float SumVariantVectorComponents(Godot.Variant[] xs)
        {
            float s = 0;
            foreach (var v in xs)
            {
                Godot.Vector4 c = v; // Variant -> Vector4 (kind 7)
                s += c.X + c.Y + c.Z + c.W;
            }
            return s;
        }
        public static Godot.Variant[] MakeVectorVariants() => new Godot.Variant[]
        {
            new Godot.Vector2(1f, 2f),
            new Godot.Vector3(3f, 4f, 5f),
            new Godot.Vector4(6f, 7f, 8f, 9f),
            new Godot.Color(0.1f, 0.2f, 0.3f, 0.4f),
        };
        // Round-trip a Variant[] of mixed vector types: decode then re-encode each element.
        public static Godot.Variant[] EchoVectorVariants(Godot.Variant[] xs)
        {
            var r = new Godot.Variant[xs.Length];
            for (int i = 0; i < xs.Length; i++) r[i] = xs[i];
            return r;
        }

        // A bare Godot.Variant as an export arg / return: the engine Variant is decoded into
        // the shim Variant at the boundary, and a returned shim Variant is re-encoded.
        public static Godot.Variant EchoVariant(Godot.Variant v) => v;
        public static int VariantAsInt(Godot.Variant v) => (int)v;
        public static Godot.Variant MakeIntVariant(int n) => n;
        public static Godot.Variant MakeStringVariant(string s) => s;
        public static Godot.Variant NegateVariantVector(Godot.Variant v)
        {
            Godot.Vector3 p = v; // Variant -> Vector3
            return new Godot.Vector3(-p.X, -p.Y, -p.Z);
        }
        public static bool VariantIsNil(Godot.Variant v) => v.IsNil;

        // Object payload in a Variant: a Variant carries an engine Object as its borrowed
        // handle, and the echo helpers above round-trip it unchanged.

        // Godot.Collections.Dictionary <-> engine Dictionary. The shim is an engine-backed
        // wrapper: the boundary shares the reference-counted container with no element
        // copy, and entry access rides dictionary_operator_index plus the Variant codec.
        public static int DictCount(Godot.Collections.Dictionary d) => d.Count;
        public static Godot.Variant DictGet(Godot.Collections.Dictionary d, Godot.Variant key) => d[key];
        public static bool DictContains(Godot.Collections.Dictionary d, Godot.Variant key) => d.ContainsKey(key);
        public static Godot.Collections.Dictionary EchoDict(Godot.Collections.Dictionary d) => d;
        public static Godot.Collections.Dictionary MakeDict()
        {
            var d = new Godot.Collections.Dictionary();
            d["a"] = 1;
            d["b"] = 2;
            d["pi"] = 3.5;
            return d;
        }

        // Wrap an engine Object carried in a Variant back into a typed shim: the cast plants
        // the carried handle in a fresh Node shim, so a further engine call goes through it.
        public static string VariantNodeName(Godot.Variant v)
        {
            Godot.Node n = (Godot.Node)v;
            return n.GetName();
        }

        // The reverse: cast the carried Object back to a Variant, round-tripping the same
        // engine object out.
        public static Godot.Variant VariantNodeRoundTrip(Godot.Variant v)
        {
            Godot.Node n = (Godot.Node)v;
            return n;
        }

        public static int Add(int a, int b)
        {
            return a + b;
        }

        public static int Fib(int n)
        {
            if (n < 2)
            {
                return n;
            }
            return Fib(n - 1) + Fib(n - 2);
        }

        public static bool IsEven(int n)
        {
            return n % 2 == 0;
        }

        public static string Describe(string name)
        {
            return string.Concat("Hello, ", name, " — from C# transpiled to C++!");
        }

        public static void HelloFromCSharp()
        {
            Godot.GD.Print("GD.Print: this line went C# -> IL -> C++ -> Godot print()!");
        }

        public static string AnimalChorus()
        {
            Animal dog = new Dog();
            Animal cat = new Cat();
            return string.Concat(dog.Speak(), " ", cat.Speak());
        }

        // ---- Godot.RefCounted lifecycle ----
        // A bare engine RefCounted constructed from C#: the newobj intercept constructs the
        // engine object and calls init_ref(), so a fresh instance reports a count of 1.
        public static int NewBareResourceRefCount()
        {
            var r = new Godot.Resource();
            return (int)r.GetReferenceCount();
        }

        // A registered user RefCounted subclass constructed via C# `new` rather than
        // GDScript .new(): the always-pinned branch of the newobj intercept.
        public static string NewMyResourceClassName()
        {
            var r = new MyResource();
            return r.GetClass();
        }

        public static int NewMyResourceDoubledTag()
        {
            var r = new MyResource();
            return r.DoubledTag();
        }

        // Engine-method RefCounted return: the ptrcall return already carries one transferred
        // reference, so the shim only registers a finalizer to unreference it later.
        public static int DuplicatedResourceRefCount()
        {
            var r = new Godot.Resource();
            var dup = r.Duplicate(false);
            return (int)dup.GetReferenceCount();
        }

        // A registered RefCounted subclass built by C# `new` and dropped must be reclaimed,
        // not leaked. Constructing in a helper keeps the wrappers off the caller's frame so
        // the GC can collect them.
        public static void MakeAndDropRegisteredResources(int n)
        {
            for (int i = 0; i < n; i++)
            {
                var r = new MyResource();
                if (r.DoubledTag() != 14)
                    Godot.GD.Print("MakeAndDropRegisteredResources: unexpected DoubledTag");
            }
        }

        // A registered RefCounted handed back as a Variant return. The local dies the moment
        // this returns, so the object is reachable only through the caller's Variant — the
        // strong/weak toggle must keep the shim's state and overrides alive as long as that
        // holder exists, instead of degrading to the engine's override-less default.
        public static Godot.Variant MakeHandoffResource()
        {
            var r = new MyHandoffResource();
            return r;
        }

        // GC.Collect() only queues finalizers in the manual-drain mode Godot integration runs
        // under; WaitForPendingFinalizers drains them synchronously on the calling thread.
        public static void ForceGcAndDrain()
        {
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
        }

        // A registered RefCounted whose constructor throws: the engine object is constructed
        // and the shim bound before the ctor body runs, so the finalizer must be registered
        // first. Library-deinit reclamation must then release every constructed object.
        public static void MakeAndDropThrowingCtor(int n)
        {
            int caught = 0;
            for (int i = 0; i < n; i++)
            {
                try
                {
                    var r = new MyThrowingResource();
                    Godot.GD.Print("MakeAndDropThrowingCtor: ctor unexpectedly did not throw");
                    System.GC.KeepAlive(r);
                }
                catch (System.InvalidOperationException)
                {
                    caught++;
                }
            }
            if (caught != n)
                Godot.GD.Print("MakeAndDropThrowingCtor: unexpected caught count");
        }

        // Finalization-starvation probe: build and drop registered RefCounted objects,
        // collect, and hand the instance ids back. The collect only QUEUES the reclamation
        // finalizers, so GDScript can watch the ids flip from valid to freed and see
        // whether drains actually run.
        public static long[] MakeDroppedResourceIds(int n)
        {
            var ids = new long[n];
            for (int i = 0; i < n; i++)
            {
                var r = new MyResource();
                ids[i] = r.GetInstanceId();
            }
            System.GC.Collect();
            return ids;
        }

        // A managed class with a real C# destructor: the compiler wraps the body in
        // try/finally { base.Finalize(); }, so own-code destructors must compile and run
        // even with no BCL assembly loaded. The finalizer bumps a counter the gate reads.
        private sealed class DestructorProbe
        {
            ~DestructorProbe()
            {
                s_destructorRuns = s_destructorRuns + 1;
            }
        }

        private static int s_destructorRuns;

        // Drops finalizable probes and collects: the counter only moves once a main-thread
        // drain runs the queued Finalize() bodies.
        public static void DropDestructorProbes(int n)
        {
            for (int i = 0; i < n; i++)
            {
                var p = new DestructorProbe();
                if (p is null)
                    Godot.GD.Print("DropDestructorProbes: unreachable");
            }
            System.GC.Collect();
        }

        public static int DestructorRunCount()
        {
            return s_destructorRuns;
        }

        // A RefCounted decoded from a Variant container must outlive the container: the
        // element comes from a temporary GDScript Array destroyed right after this returns,
        // and GDScript then drops its own reference. The decode-time reference guard is the
        // only thing keeping the engine object alive until TakeHeldVariant hands it back.
        private static Godot.Variant s_heldVariant;

        public static void HoldFirstVariant(Godot.Variant[] xs)
        {
            s_heldVariant = xs[0];
        }

        public static Godot.Variant TakeHeldVariant()
        {
            var v = s_heldVariant;
            // Clear the root so the guard becomes collectible: the library-deinit collect+drain
            // must be able to return the reference, or the ObjectDB leak check trips.
            s_heldVariant = default;
            return v;
        }

        // Called from an engine-spawned worker thread. The result string is rooted only by
        // the worker's stack, so a bridge that fails to GC-register the thread is red twice:
        // the collect aborts on an unknown thread, and an unscanned stack would let the
        // allocation storm reuse the string's block.
        public static string DescribeFromEngineThread(string name)
        {
            string s = string.Concat("Hello, ", name, " — from C# transpiled to C++!");
            System.GC.Collect();
            for (int i = 0; i < 20000; i++)
            {
                string filler = string.Concat("XXXXX, ", name, " -- from C# transpiled to C++!");
                if (filler.Length == 0)
                    Godot.GD.Print("unreachable");
            }
            return s;
        }

    }
}
