using Godot;
using System;

public partial class MyNode : Node2D
{
    [Signal]
    public delegate void PingedEventHandler();

    [Export]
    public int Health { get; set; } = 100;

    public override void _Ready()
    {
        GD.Print("Hello from standard Godot C#!");
        GD.Print("the answer is ", 42);
        GD.Print("pi ~ ", 3.14, " enabled=", true);

        // Engine interaction through the shim: a real Node2D property round-trip.
        Position = new Vector2(12.5f, 34.5f);
        Vector2 p = Position;
        GD.Print("position = (", p.X, ", ", p.Y, ")");

        // Generated Mathf utilities, lowered straight to C++ <cmath>.
        GD.Print("sqrt(2) = ", Mathf.Sqrt(2.0), ", pow(2,10) = ", Mathf.Pow(2.0, 10.0));

        // Vector2 arithmetic (pure C#, computed in the shim value type).
        Vector2 v = new Vector2(3, 4);
        Vector2 sum = v + Vector2.One;
        GD.Print("v.Length() = ", v.Length(), ", v+One = (", sum.X, ", ", sum.Y, ")");

        // Vector3 — a builtin value type generated from extension_api.json's builtin_classes.
        Vector3 a = new Vector3(1, 2, 3);
        Vector3 b = Vector3.One;
        Vector3 c = new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        GD.Print("Vector3 c = (", c.X, ", ", c.Y, ", ", c.Z, "), forward.Z = ", Vector3.Forward.Z);

        // More generated builtin value types: integer + 4-component vectors.
        Vector2i grid = new Vector2i(3, 4);
        Vector4 v4 = new Vector4(1.5f, 2.5f, 3.5f, 4.5f);
        Quaternion q = Quaternion.Identity;
        Quaternion qn = new Quaternion(0, 3, 0, 4).Normalized();
        Quaternion qi = Quaternion.Identity.Inverse();
        GD.Print("grid = (", grid.X, ", ", grid.Y, "), v4.W = ", v4.W, ", qn.W = ", qn.W, ", qi.W = ", qi.W);

        // Generated component-wise operators on the builtin value types.
        Vector3 d = new Vector3(2, 4, 6) * 0.5f + Vector3.One;
        Vector2i scaled = grid * 2 + new Vector2i(1, 1);
        GD.Print("d = (", d.X, ", ", d.Y, ", ", d.Z, "), scaled = (", scaled.X, ", ", scaled.Y, ")");

        // Generated component-wise equality operators.
        bool isOrigin = grid == Vector2i.Zero;
        bool dIsForward = d == new Vector3(2, 3, 4);
        GD.Print("grid==Zero? ", isOrigin, ", d==(2,3,4)? ", dIsForward);

        // Hand-written Vector3 math (pure C#, reusing the generated operators/ctors).
        Vector3 e = new Vector3(1, 2, 2);
        Vector3 cross = new Vector3(1, 0, 0).Cross(new Vector3(0, 1, 0));
        GD.Print("e.Length() = ", e.Length(), ", X cross Y = (", cross.X, ", ", cross.Y, ", ", cross.Z, ")");

        // Color: a generated value type (real r/g/b/a fields filtered from the derived
        // members); the 3-arg ctor is hand-written.
        Color tint = new Color(0.5f, 0.25f, 0.75f);
        Color half = tint * 0.5f;
        Color red = Colors.Red; // standard Godot exposes named colors via Colors
        Color inv = Colors.Red.Inverted();
        GD.Print("tint = (", tint.R, ", ", tint.G, ", ", tint.B, ", ", tint.A, "), Colors.Red.R = ", red.R, ", half.R = ", half.R, ", inv(red) = (", inv.R, ", ", inv.G, ", ", inv.B, ")");

        // Rect2: a generated value type with nested Vector2 fields (the derived `end` member
        // is filtered out via the real-member override).
        Rect2 rect = new Rect2(new Vector2(10, 20), new Vector2(30, 40));
        GD.Print("rect.Position = (", rect.Position.X, ", ", rect.Position.Y, "), rect.Size.X = ", rect.Size.X);

        // Matrix/geometry value types: nested value-type fields, name-independent positional
        // ctor matching.
        Transform2D xform = new Transform2D(new Vector2(1, 0), new Vector2(0, 1), new Vector2(7, 8));
        Plane plane = new Plane(new Vector3(0, 1, 0), 5);
        AABB box = new AABB(new Vector3(0, 0, 0), new Vector3(2, 3, 4));
        GD.Print("xform.Origin = (", xform.Origin.X, ", ", xform.Origin.Y, "), plane.D = ", plane.D, ", box.Size.Z = ", box.Size.Z);

        // Common pure-C# vector math (hand-written over the generated structs).
        Vector2 lerped = Vector2.Zero.Lerp(new Vector2(10, 20), 0.5f);
        Vector2 clamped = new Vector2(5, -5).Clamp(Vector2.Zero, Vector2.One);
        Vector2 rot = new Vector2(1, 0).Rotated((float)(System.Math.PI / 2));
        GD.Print("lerp = (", lerped.X, ", ", lerped.Y, "), clamp = (", clamped.X, ", ", clamped.Y, "), rot90.Y = ", rot.Y);

        // Matrix constants: flat-scalar dump values rebuilt into nested ctors.
        Transform2D id = Transform2D.Identity;
        Basis basisId = Basis.Identity;
        GD.Print("Transform2D.Identity.X = (", id.X.X, ", ", id.X.Y, "), Basis.Identity.Z.Z = ", basisId.Z.Z);

        // Mathf constants (hand-written; the dump only carries Mathf functions).
        GD.Print("Mathf.Pi = ", Mathf.Pi, ", Mathf.Tau = ", Mathf.Tau, ", cos(Pi) = ", Mathf.Cos(Mathf.Pi));

        // More pure-C# vector methods (real Godot API surface).
        Vector2 refl = new Vector2(1, -1).Reflect(new Vector2(0, 1));
        Vector2 dir = Vector2.Zero.DirectionTo(new Vector2(0, 5));
        Vector2 flo = new Vector2(1.7f, -1.2f).Floor();
        GD.Print("reflect = (", refl.X, ", ", refl.Y, "), dir = (", dir.X, ", ", dir.Y, "), floor = (", flo.X, ", ", flo.Y, ")");

        // Matrix transform operator (hand-written shim partial): scale by the basis columns
        // and translate by Origin. Cross-checked against native Godot in the godot sample.
        Transform2D tx = new Transform2D(new Vector2(2, 0), new Vector2(0, 3), new Vector2(10, 20));
        Vector2 txp = tx * new Vector2(5, 7);
        GD.Print("Transform2D * (5,7) = (", txp.X, ", ", txp.Y, ")");

        // Basis * Vector3: a 90-degree rotation about Z maps +X to +Y. The columns are the
        // transformed basis vectors.
        Basis rotZ = new Basis(new Vector3(0, 1, 0), new Vector3(-1, 0, 0), new Vector3(0, 0, 1));
        Vector3 rotated = rotZ * new Vector3(1, 0, 0);
        GD.Print("Basis(rotZ90) * (1,0,0) = (", rotated.X, ", ", rotated.Y, ", ", rotated.Z, ")");

        // Transform3D * Vector3: rotate by the basis, then translate by Origin.
        Transform3D xf3 = new Transform3D(rotZ, new Vector3(10, 20, 30));
        Vector3 xf3p = xf3 * new Vector3(1, 0, 0);
        GD.Print("Transform3D(rotZ90, o=(10,20,30)) * (1,0,0) = (", xf3p.X, ", ", xf3p.Y, ", ", xf3p.Z, ")");

        // Composition: (A * B) applied to a point equals A applied to (B applied to it).
        Transform2D scale2 = new Transform2D(new Vector2(2, 0), new Vector2(0, 2), Vector2.Zero);
        Transform2D move = new Transform2D(new Vector2(1, 0), new Vector2(0, 1), new Vector2(5, 5));
        Vector2 composed = (scale2 * move) * Vector2.Zero;
        GD.Print("(scale2 * move) * (0,0) = (", composed.X, ", ", composed.Y, ")");

        // Quaternion rotation: a 90-degree rotation about +Z maps +X to +Y, and the
        // Quaternion * Quaternion product is the Hamilton product.
        float sin45 = (float)System.Math.Sin(System.Math.PI / 4);
        float cos45 = (float)System.Math.Cos(System.Math.PI / 4);
        Quaternion rotZ90 = new Quaternion(0, 0, sin45, cos45);
        Vector3 qr = rotZ90 * new Vector3(1, 0, 0);
        Quaternion rotZ180 = rotZ90 * rotZ90;
        GD.Print("Quat(rotZ90) * (1,0,0) = (", qr.X, ", ", qr.Y, ", ", qr.Z, "), (rotZ90*rotZ90).Z = ", rotZ180.Z);

        // An axis-angle 90-degree rotation about +Z must match the literal rotZ90 above.
        Quaternion aa = new Quaternion(new Vector3(0, 0, 1), (float)(System.Math.PI / 2));
        Quaternion fe = Quaternion.FromEuler(new Vector3(0, 0, (float)(System.Math.PI / 2)));
        GD.Print("Quat(axis +Z, 90deg).Z = ", aa.Z, ", FromEuler(0,0,90deg).Z = ", fe.Z);

        // Slerp: halfway between identity and the 90-degree +Z rotation is 45 degrees.
        Quaternion mid = Quaternion.Identity.Slerp(aa, 0.5f);
        GD.Print("Slerp(Identity, rotZ90, 0.5).Z = ", mid.Z);

        // Vector2/3 Slerp, Snapped, Inverse print examples:
        Vector2 slerp2 = new Vector2(1f, 0f).Slerp(new Vector2(0f, 1f), 0.5f);
        Vector3 slerp3 = new Vector3(1f, 0f, 0f).Slerp(new Vector3(0f, 1f, 0f), 0.5f);
        Vector2 snapped2 = new Vector2(1.23f, 4.56f).Snapped(0.5f);
        Vector3 snapped3 = new Vector3(1.23f, 4.56f, 7.89f).Snapped(new Vector3(0.5f, 2f, 5f));
        Vector3 inverse3 = new Vector3(2.0f, -4.0f, 0.5f).Inverse();
        GD.Print("Vector2 Slerp = (", slerp2.X, ", ", slerp2.Y, "), Vector3 Slerp = (", slerp3.X, ", ", slerp3.Y, ", ", slerp3.Z, ")");
        GD.Print("Vector2 Snapped = (", snapped2.X, ", ", snapped2.Y, "), Vector3 Snapped = (", snapped3.X, ", ", snapped3.Y, ", ", snapped3.Z, "), Vector3 Inverse = (", inverse3.X, ", ", inverse3.Y, ", ", inverse3.Z, ")");

        // Fire a [Signal] declared on this standard-style node; a GDScript listener
        // connected before the node entered the tree receives it.
        EmitSignal("Pinged");
    }
}
