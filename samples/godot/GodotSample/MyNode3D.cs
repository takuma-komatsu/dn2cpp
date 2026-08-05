namespace GodotSample
{
    /// <summary>A 3D node exercising the math value types in engine calls: Vector3
    /// argument/return, engine-computed Transform3D and Basis returns (validating the
    /// column-major shim decode against real engine math rather than a set/get inverse
    /// pair), and a Transform3D argument.</summary>
    public class MyNode3D : Godot.Node3D
    {
        private int _ticks;
        private bool _checkedMetaObject;

        private static bool Approx(float a, float b)
        {
            float d = a - b;
            if (d < 0f)
            {
                d = -d;
            }
            return d < 0.0001f;
        }

        public override void _Ready()
        {
            // Vector3 argument + return round trip (exactly representable values).
            SetPosition(new Godot.Vector3(1.5f, 2.5f, 3.5f));
            Godot.Vector3 pos = GetPosition();
            bool posOk = pos.X == 1.5f && pos.Y == 2.5f && pos.Z == 3.5f;
            Godot.GD.Print(string.Concat("MyNode3D: Vector3 position round trip = ", posOk.ToString()));

            // Rotate PI/2 about Y and read the transform back: the engine builds the matrix,
            // so the row-major-wire -> column-major-shim transpose is checked against engine
            // math. A missing or doubled transpose reads the inverse rotation.
            SetRotation(new Godot.Vector3(0f, Godot.Mathf.Pi / 2f, 0f));
            Godot.Transform3D xform = GetTransform();
            bool xformOk = Approx(xform.Basis.X.X, 0f) && Approx(xform.Basis.X.Z, -1f)
                && Approx(xform.Basis.Y.Y, 1f)
                && Approx(xform.Basis.Z.X, 1f) && Approx(xform.Basis.Z.Z, 0f)
                && Approx(xform.Origin.X, 1.5f) && Approx(xform.Origin.Z, 3.5f);
            Godot.GD.Print(string.Concat("MyNode3D: Transform3D return decodes engine rotation = ", xformOk.ToString()));

            // The bare Basis return rides the same transpose path as a 9-float POD.
            Godot.Basis basis = GetBasis();
            bool basisOk = Approx(basis.X.Z, -1f) && Approx(basis.Z.X, 1f) && Approx(basis.Y.Y, 1f);
            Godot.GD.Print(string.Concat("MyNode3D: Basis return decodes engine rotation = ", basisOk.ToString()));

            // Transform3D argument: re-send the returned transform with a new origin and ask
            // for the euler angle. A wrong argument-direction transpose flips the sign.
            SetTransform(new Godot.Transform3D(xform.Basis, new Godot.Vector3(7f, 8f, 9f)));
            Godot.Vector3 rot = GetRotation();
            Godot.Vector3 moved = GetPosition();
            bool argOk = Approx(rot.X, 0f) && Approx(rot.Y, Godot.Mathf.Pi / 2f)
                && Approx(moved.X, 7f) && Approx(moved.Y, 8f) && Approx(moved.Z, 9f);
            Godot.GD.Print(string.Concat("MyNode3D: Transform3D argument round trip = ", argOk.ToString()));

            // A Variant carrying an engine-computed Transform3D must decode to the same matrix
            // the typed getter returns — already validated above against real engine rotation,
            // so a wrong Variant-side transpose cannot cancel out as an encode/decode echo would.
            Godot.Transform3D typed = GetTransform();
            Godot.Transform3D viaVariant = (Godot.Transform3D)Get("transform");
            bool vXformOk = Approx(viaVariant.Basis.X.X, typed.Basis.X.X)
                && Approx(viaVariant.Basis.X.Z, typed.Basis.X.Z)
                && Approx(viaVariant.Basis.Z.X, typed.Basis.Z.X)
                && Approx(viaVariant.Basis.Z.Z, typed.Basis.Z.Z)
                && Approx(viaVariant.Basis.X.Z, -1f)
                && Approx(viaVariant.Origin.X, 7f) && Approx(viaVariant.Origin.Z, 9f);
            Godot.GD.Print(string.Concat("MyNode3D: Variant Transform3D matches typed getter = ", vXformOk.ToString()));

            // The Variant argument direction: Set routes the boxed Transform3D through an engine
            // Variant into the typed property, and the engine's euler decode flips the sign if
            // the encode-side transpose is wrong.
            Set("transform", new Godot.Transform3D(typed.Basis, new Godot.Vector3(4f, 5f, 6f)));
            Godot.Vector3 vRot = GetRotation();
            Godot.Vector3 vPos = GetPosition();
            bool vArgOk = Approx(vRot.X, 0f) && Approx(vRot.Y, Godot.Mathf.Pi / 2f)
                && Approx(vPos.X, 4f) && Approx(vPos.Y, 5f) && Approx(vPos.Z, 6f);
            Godot.GD.Print(string.Concat("MyNode3D: Variant Transform3D argument decodes engine rotation = ", vArgOk.ToString()));

            // RefCounted hand-off through a Variant argument: the engine Variant built from the
            // Object payload takes its own reference, so the meta entry must keep the resource
            // alive after the only C# reference is dropped and collected. _Process reads it back
            // two frames later. It lives here rather than in MyNode because MyNode3D runs only
            // in the scene run.
            MyResource metaRes = new MyResource();
            SetMeta("v_obj", metaRes);
            metaRes = null!;
            System.GC.Collect();
        }

        public override void _Process(double delta)
        {
            _ticks = _ticks + 1;
            if (_ticks == 2 && !_checkedMetaObject)
            {
                // At least one per-frame finalizer drain has run since _Ready collected the shim,
                // so only the meta Variant's own reference keeps the resource alive. Call an
                // engine method through the wrapped handle to prove it is neither freed nor hung.
                _checkedMetaObject = true;
                Godot.Variant objBack = GetMeta("v_obj", default);
                Godot.Object? obj = (Godot.Object?)objBack;
                bool objOk = obj is not null && obj.GetClass() == "MyResource";
                Godot.GD.Print(string.Concat("MyNode3D: Variant RefCounted hand-off survives GC = ", objOk.ToString()));
                // Drop the engine's meta reference; the decode's lifetime guard releases the last
                // one once this Variant becomes unreachable, so it must not appear in the leak report.
                RemoveMeta("v_obj");
            }
        }
    }
}
