namespace GodotSample
{
    /// <summary>A user-defined enum crossing the export boundary: an [Export] property,
    /// a signal argument and an exported method signature all carry it as its numeric
    /// value, since Variant has no enum kind.</summary>
    public enum GateMode
    {
        Off = 0,
        Slow = 3,
        Fast = 7,
    }

    /// <summary>A scene-tree node in C#, placed in main.tscn; Godot calls _ready/_process
    /// through the dn2cpp virtual bridge.</summary>
    public class MyNode : Godot.Node2D
    {
        [Godot.Signal]
        public delegate void MyCustomSignalEventHandler(bool enable);

        // Enum signal argument: registration must map the enum to an INT argument.
        [Godot.Signal]
        public delegate void EnumModeChangedEventHandler(GateMode mode);

        // Signal carrying an engine object (a registered RefCounted, not a Node): the emit
        // path must convert the shim to an Object Variant rather than falling through to NIL.
        [Godot.Signal]
        public delegate void ResourcePayloadEventHandler(Godot.Variant payload);

        // Target of the delegate-backed Callable test.
        [Godot.Signal]
        public delegate void ScoreChangedEventHandler(int score);

        private int _ticks;
        private int _physicsTicks;
        private bool _gotInput;
        private bool _gotUnhandledInput;

        [Godot.Export]
        public int MyExportedProperty { get; set; } = 100;

        // Enum / Quaternion / Plane / AABB [Export]: GdTypeOf must produce a tag for each —
        // enum as INT, the builtins as their own Variant types — not drop the registration.
        [Godot.Export]
        public GateMode Mode { get; set; } = GateMode.Slow;

        [Godot.Export]
        public Godot.Quaternion Spin { get; set; } = new Godot.Quaternion(0f, 0f, 0f, 1f);

        [Godot.Export]
        public Godot.Plane Clip { get; set; } = new Godot.Plane(new Godot.Vector3(0f, 1f, 0f), 2f);

        [Godot.Export]
        public Godot.AABB Bounds { get; set; } = new Godot.AABB(new Godot.Vector3(0f, 0f, 0f), new Godot.Vector3(1f, 1f, 1f));

        // Exported methods over the same kinds. Pure C# field math, so a wrong decode or
        // encode shows up in the returned components.
        public GateMode NextMode(GateMode m)
        {
            if (m == GateMode.Off) return GateMode.Slow;
            if (m == GateMode.Slow) return GateMode.Fast;
            return GateMode.Off;
        }

        public Godot.Quaternion ConjugateOf(Godot.Quaternion q)
        {
            return new Godot.Quaternion(-q.X, -q.Y, -q.Z, q.W);
        }

        public Godot.AABB GrowBounds(Godot.AABB box)
        {
            return new Godot.AABB(box.Position, new Godot.Vector3(box.Size.X * 2f, box.Size.Y * 2f, box.Size.Z * 2f));
        }

        // Emits the enum-argument signal from C#; the payload rides the int Variant path.
        public void RaiseEnumMode(int mode)
        {
            EmitSignal("EnumModeChanged", mode);
        }

        public override void _EnterTree()
        {
            Godot.GD.Print("MyNode._EnterTree: entering the scene tree");
        }

        public override void _Ready()
        {
            Godot.GD.Print("MyNode._Ready: C# node is alive in the scene tree!");

            // stdout marker for the mobile E2E gates: GD.Print goes to platform loggers that
            // never reach captured stdout, but the runtime's Console path is a plain write.
            System.Console.WriteLine(string.Concat("MyNode._Ready stdout marker: GetClass() = ", GetClass()));

            // Exercise BCL intrinsics (Math + ToString) from inside a Godot node.
            int hypot = (int)System.Math.Sqrt(3.0 * 3.0 + 4.0 * 4.0);
            Godot.GD.Print(string.Concat("MyNode: Math.Sqrt(3^2+4^2) = ", hypot.ToString()));

            // Real engine methods on this node (the reverse direction).
            bool inTree = IsInsideTree();
            Godot.GD.Print(string.Concat("MyNode: engine IsInsideTree() = ", inTree.ToString()));

            SetProcess(true);
            bool processing = IsProcessing();
            Godot.GD.Print(string.Concat("MyNode: after SetProcess(true), IsProcessing() = ", processing.ToString()));

            // Enable input so the engine delivers _Input / _UnhandledInput (a GDScript injector
            // pushes a key event).
            SetProcessInput(true);
            SetProcessUnhandledInput(true);

            // Connect to an engine method. The flags argument is omitted: the generated shim
            // carries the dump's default_value.
            Connect("MyCustomSignal", new Godot.Callable(this, "set_process"));
            EmitSignal("MyCustomSignal", false);
            bool processingAfterSignal = IsProcessing();
            Godot.GD.Print(string.Concat("MyNode: after EmitSignal(MyCustomSignal, false), IsProcessing() = ", processingAfterSignal.ToString()));

            // Signal argument that is a registered RefCounted instance, not a Node: the GDScript
            // side asserts the payload arrives live rather than NIL.
            EmitSignal("ResourcePayload", new MyResource());

            int children = (int)GetChildCount(false);
            Godot.GD.Print(string.Concat("MyNode: engine GetChildCount() = ", children.ToString()));

            long id = GetInstanceId();
            Godot.GD.Print(string.Concat("MyNode: engine GetInstanceId() != 0 = ", (id != 0).ToString()));

            // Generic ptrcall: object argument + bool return. A node is never its own ancestor.
            bool selfAncestor = IsAncestorOf(this);
            Godot.GD.Print(string.Concat("MyNode: IsAncestorOf(self) = ", selfAncestor.ToString()));

            // Test Vector2
            Godot.Vector2 pos = GetPosition();
            Godot.GD.Print(string.Concat("MyNode: GetPosition() = (", string.Concat(pos.X.ToString(), ", ", pos.Y.ToString()), ")"));

            SetPosition(new Godot.Vector2(30.5f, 40.5f));
            Godot.Vector2 newPos = GetPosition();
            Godot.GD.Print(string.Concat("MyNode: after SetPosition(30.5, 40.5), GetPosition() = (", string.Concat(newPos.X.ToString(), ", ", newPos.Y.ToString()), ")"));

            // Test Property Position
            Godot.Vector2 posProp = Position;
            Godot.GD.Print(string.Concat("MyNode: Property Position = (", string.Concat(posProp.X.ToString(), ", ", posProp.Y.ToString()), ")"));

            Position = new Godot.Vector2(50.5f, 60.5f);
            Godot.Vector2 newPosProp = Position;
            Godot.GD.Print(string.Concat("MyNode: after Position = (50.5, 60.5), Property Position = (", string.Concat(newPosProp.X.ToString(), ", ", newPosProp.Y.ToString()), ")"));

            // Engine method-bind calls marshalling a Godot String (a heap value type).
            string klass = GetClass();
            Godot.GD.Print(string.Concat("MyNode: GetClass() = ", klass));

            // String round trip: a String-argument setter then a String-returning getter.
            SetEditorDescription("hello from C#");
            string desc = GetEditorDescription();
            Godot.GD.Print(string.Concat("MyNode: GetEditorDescription() = ", desc));

            // StringName round trip: SetName then GetName, both marshalled via a Godot String.
            SetName("RenamedNode");
            string nm = GetName();
            Godot.GD.Print(string.Concat("MyNode: GetName() after SetName = ", nm));

            // NodePath argument: HasNode(NodePath); "." is the node's own path -> true.
            bool hasSelf = HasNode(".");
            Godot.GD.Print(string.Concat("MyNode: HasNode(\".\") = ", hasSelf.ToString()));

            // NodePath return: the absolute scene path, ending in the just-renamed node name.
            string path = GetPath();
            Godot.GD.Print(string.Concat("MyNode: GetPath() = ", path));

            // Engine method returning an Object: wrapped as a managed Godot.Node whose handle
            // drives a further engine call.
            Godot.Node parent = GetParent();
            string parentName = parent.GetName();
            Godot.GD.Print(string.Concat("MyNode: GetParent().GetName() = ", parentName));

            // Enum marshalling through the generic ptrcall: 64-bit on the wire, int32-backed in C#.
            SetProcessMode(Godot.Node.ProcessModeEnum.Always);
            Godot.Node.ProcessModeEnum pm = GetProcessMode();
            Godot.GD.Print(string.Concat("MyNode: GetProcessMode() after SetProcessMode(Always) = ",
                (pm == Godot.Node.ProcessModeEnum.Always).ToString()));

            // ...then the enum-typed property. Restore the Inherit default afterwards.
            ProcessMode = Godot.Node.ProcessModeEnum.Pausable;
            bool pmProp = ProcessMode == Godot.Node.ProcessModeEnum.Pausable;
            Godot.GD.Print(string.Concat("MyNode: ProcessMode property round trip (Pausable) = ", pmProp.ToString()));
            ProcessMode = Godot.Node.ProcessModeEnum.Inherit;

            // Bitfield: [Flags] values compose with | and the mask round-trips through the engine.
            ProcessThreadMessages = Godot.Node.ProcessThreadMessagesEnum.FlagProcessThreadMessages
                | Godot.Node.ProcessThreadMessagesEnum.FlagProcessThreadMessagesPhysics;
            bool bits = ProcessThreadMessages == Godot.Node.ProcessThreadMessagesEnum.FlagProcessThreadMessagesAll;
            Godot.GD.Print(string.Concat("MyNode: ProcessThreadMessages bitfield round trip (All) = ", bits.ToString()));
            ProcessThreadMessages = (Godot.Node.ProcessThreadMessagesEnum)0;

            // Global enums generate as top-level Godot types; check a few engine constant values.
            bool globalEnums = (int)Godot.Error.Ok == 0
                && (int)Godot.Side.Bottom == 3
                && (int)Godot.KeyModifierMask.KeyMaskShift == (1 << 25);
            Godot.GD.Print(string.Concat("MyNode: global enum constants match engine values = ", globalEnums.ToString()));

            // Builtin POD value types beyond Vector2 ride the generic ptrcall by pointer to their
            // native-layout shim struct. The Color components are exactly representable.
            Modulate = new Godot.Color(0.25f, 0.5f, 0.75f, 1f);
            Godot.Color mod = Modulate;
            bool colorOk = mod.R == 0.25f && mod.G == 0.5f && mod.B == 0.75f && mod.A == 1f;
            Godot.GD.Print(string.Concat("MyNode: Modulate Color round trip = ", colorOk.ToString()));
            Modulate = new Godot.Color(1f, 1f, 1f, 1f);

            // Rect2 return (a nested-Vector2 POD): the viewport rect is at the origin, size positive.
            Godot.Rect2 rect = GetViewportRect();
            bool rectOk = rect.Position.X == 0f && rect.Position.Y == 0f
                && rect.Size.X > 0f && rect.Size.Y > 0f;
            Godot.GD.Print(string.Concat("MyNode: GetViewportRect() Rect2 return = ", rectOk.ToString()));

            // RID return: an opaque 8-byte id passed by value; the root viewport's is non-zero.
            Godot.Viewport viewport = GetViewport();
            Godot.Rid rid = viewport.GetViewportRid();
            Godot.GD.Print(string.Concat("MyNode: GetViewportRid() RID is valid = ", rid.IsValid.ToString()));

            // Packed arrays marshal to and from plain managed arrays at the engine-call boundary
            // (GodotSharp-compatible: no wrapper type, no persistent engine value).
            Godot.StreamPeerBuffer spb = new Godot.StreamPeerBuffer();
            spb.DataArray = new byte[] { 1, 2, 250, 255 };
            byte[] bytes = spb.DataArray;
            bool bytesOk = bytes.Length == 4 && bytes[0] == 1 && bytes[1] == 2
                && bytes[2] == 250 && bytes[3] == 255;
            Godot.GD.Print(string.Concat("MyNode: byte[] PackedByteArray round trip = ", bytesOk.ToString()));

            // int[] round trip (PackedInt32Array) through a plain data property.
            Godot.GLTFNode gltfNode = new Godot.GLTFNode();
            gltfNode.Children = new int[] { 3, -1, 2000000000 };
            int[] childIds = gltfNode.Children;
            bool intsOk = childIds.Length == 3 && childIds[0] == 3 && childIds[1] == -1
                && childIds[2] == 2000000000;
            Godot.GD.Print(string.Concat("MyNode: int[] PackedInt32Array round trip = ", intsOk.ToString()));

            // long[] return: a value above int32 range survives the 8-byte element copy. The id
            // order is not contractual, so assert via the sum.
            Godot.AStar2D astar = new Godot.AStar2D();
            astar.AddPoint(1, new Godot.Vector2(0f, 0f), 1.0);
            astar.AddPoint(5000000000L, new Godot.Vector2(1f, 0f), 1.0);
            long[] pointIds = astar.GetPointIds();
            bool longsOk = pointIds.Length == 2 && pointIds[0] + pointIds[1] == 5000000001L;
            Godot.GD.Print(string.Concat("MyNode: long[] PackedInt64Array return = ", longsOk.ToString()));

            // float[] + Color[] round trips through Gradient's parallel point arrays (16-byte
            // struct elements for the colors).
            Godot.Gradient gradient = new Godot.Gradient();
            gradient.Offsets = new float[] { 0f, 0.25f, 1f };
            gradient.Colors = new Godot.Color[]
            {
                new Godot.Color(1f, 0f, 0f, 1f),
                new Godot.Color(0f, 1f, 0f, 0.5f),
                new Godot.Color(0f, 0f, 1f, 1f),
            };
            float[] offsets = gradient.Offsets;
            Godot.Color[] colors = gradient.Colors;
            bool gradientOk = offsets.Length == 3 && offsets[1] == 0.25f
                && colors.Length == 3 && colors[0].R == 1f && colors[1].G == 1f
                && colors[1].A == 0.5f && colors[2].B == 1f;
            Godot.GD.Print(string.Concat("MyNode: float[]/Color[] packed round trips = ", gradientOk.ToString()));

            // double[] round trip (PackedFloat64Array, 8-byte scalar elements).
            Godot.GLTFAccessor accessor = new Godot.GLTFAccessor();
            accessor.Min = new double[] { -1.5, 0.0, 2.25 };
            double[] accessorMin = accessor.Min;
            bool doublesOk = accessorMin.Length == 3 && accessorMin[0] == -1.5 && accessorMin[2] == 2.25;
            Godot.GD.Print(string.Concat("MyNode: double[] PackedFloat64Array round trip = ", doublesOk.ToString()));

            // Vector2[] round trip (PackedVector2Array, 8-byte struct elements).
            Godot.ConvexPolygonShape2D shape = new Godot.ConvexPolygonShape2D();
            shape.Points = new Godot.Vector2[]
            {
                new Godot.Vector2(0f, 0f),
                new Godot.Vector2(4f, 0f),
                new Godot.Vector2(0f, 3f),
            };
            Godot.Vector2[] points = shape.Points;
            bool v2Ok = points.Length == 3 && points[1].X == 4f && points[2].Y == 3f;
            Godot.GD.Print(string.Concat("MyNode: Vector2[] PackedVector2Array round trip = ", v2Ok.ToString()));

            // Vector3[] return (12-byte struct elements): engine-computed baked curve points,
            // not a set/get echo, with both endpoints exact.
            Godot.Curve3D curve = new Godot.Curve3D();
            Godot.Vector3 zero3 = new Godot.Vector3(0f, 0f, 0f);
            curve.AddPoint(zero3, zero3, zero3, -1);
            curve.AddPoint(new Godot.Vector3(10f, 0f, 0f), zero3, zero3, -1);
            Godot.Vector3[] baked = curve.GetBakedPoints();
            bool v3Ok = baked.Length >= 2 && baked[0].X == 0f
                && baked[baked.Length - 1].X == 10f;
            Godot.GD.Print(string.Concat("MyNode: Vector3[] PackedVector3Array return = ", v3Ok.ToString()));

            // string[] return: engine-computed regex capture groups, each marshalled as a String.
            Godot.RegEx regex = new Godot.RegEx();
            regex.Compile("(\\w+)=(\\d+)", true);
            Godot.RegExMatch match = regex.Search("count=42", 0, -1);
            string[] groups = match.GetStrings();
            bool stringsOk = groups.Length == 3 && groups[0] == "count=42"
                && groups[1] == "count" && groups[2] == "42";
            Godot.GD.Print(string.Concat("MyNode: string[] PackedStringArray return = ", stringsOk.ToString()));

            // Bare Variant arguments and returns. Object's meta storage keeps a real engine Variant,
            // so each SetMeta/GetMeta pair is a full encode -> engine -> decode round trip.
            SetMeta("v_int", 42);
            long viBack = (long)GetMeta("v_int", default);
            SetMeta("v_float", 2.5);
            double vfBack = (double)GetMeta("v_float", default);
            SetMeta("v_str", "variant hello");
            // The default argument is omitted: the shim defaults the Variant parameter.
            string vsBack = (string)GetMeta("v_str");
            bool vScalarsOk = viBack == 42 && vfBack == 2.5 && vsBack == "variant hello";
            Godot.GD.Print(string.Concat("MyNode: Variant int/float/string meta round trips = ", vScalarsOk.ToString()));

            // A small vector payload (carried inline in the Variant struct).
            SetMeta("v_vec3", new Godot.Vector3(1f, 2f, 3f));
            Godot.Vector3 v3Back = (Godot.Vector3)GetMeta("v_vec3", default);
            bool vVec3Ok = v3Back.X == 1f && v3Back.Y == 2f && v3Back.Z == 3f;
            Godot.GD.Print(string.Concat("MyNode: Variant Vector3 meta round trip = ", vVec3Ok.ToString()));

            // A big POD payload (carried as a boxed shim struct).
            SetMeta("v_rect", new Godot.Rect2(new Godot.Vector2(1f, 2f), new Godot.Vector2(3f, 4f)));
            Godot.Rect2 rectBack = (Godot.Rect2)GetMeta("v_rect", default);
            bool vRectOk = rectBack.Position.X == 1f && rectBack.Position.Y == 2f
                && rectBack.Size.X == 3f && rectBack.Size.Y == 4f;
            Godot.GD.Print(string.Concat("MyNode: Variant Rect2 meta round trip = ", vRectOk.ToString()));

            // A NIL Variant converts to a big-POD default instead of throwing, as GodotSharp does.
            Godot.Variant nilVariant = default;
            Godot.Rect2 nilRect = nilVariant.AsRect2();
            Godot.Transform3D nilXform = nilVariant.AsTransform3D();
            bool nilPodOk = nilRect.Position.X == 0f && nilRect.Size.Y == 0f
                && nilXform.Basis.X.X == 0f && nilXform.Origin.Z == 0f;
            Godot.GD.Print(string.Concat("MyNode: NIL Variant to big POD defaults = ", nilPodOk.ToString()));

            // An RID payload (the live viewport id from the POD section above).
            SetMeta("v_rid", rid);
            Godot.Rid ridBack = (Godot.Rid)GetMeta("v_rid", default);
            bool vRidOk = ridBack == rid && ridBack.IsValid;
            Godot.GD.Print(string.Concat("MyNode: Variant Rid meta round trip = ", vRidOk.ToString()));

            // A packed-array payload: converted to and from an engine PackedByteArray inside the
            // Variant at the boundary.
            SetMeta("v_bytes", new byte[] { 7, 8, 250 });
            byte[]? vBytes = (byte[]?)GetMeta("v_bytes", default);
            bool vBytesOk = vBytes is not null && vBytes.Length == 3 && vBytes[0] == 7
                && vBytes[1] == 8 && vBytes[2] == 250;
            Godot.GD.Print(string.Concat("MyNode: Variant byte[] meta round trip = ", vBytesOk.ToString()));

            // The engine must UNDERSTAND the payload, not merely echo it: Object.Set routes a
            // Variant into the typed property and Object.Get hands it back as a Variant.
            Set("position", new Godot.Vector2(11.5f, 12.5f));
            Godot.Vector2 setPos = GetPosition();
            Godot.Vector2 posBack = (Godot.Vector2)Get("position");
            bool vSetGetOk = setPos.X == 11.5f && setPos.Y == 12.5f
                && posBack.X == 11.5f && posBack.Y == 12.5f;
            Godot.GD.Print(string.Concat("MyNode: Variant Set/Get position round trip = ", vSetGetOk.ToString()));

            // A StringName-carrying Variant (Get("name")) decodes as its text.
            string nameViaVariant = (string)Get("name");
            bool vNameOk = nameViaVariant == "RenamedNode";
            Godot.GD.Print(string.Concat("MyNode: Variant StringName payload reads back = ", vNameOk.ToString()));

            // GD.Print stringifies any Variant payload through the engine.
            Godot.GD.Print("MyNode: variant print Vector3 = ", new Godot.Vector3(1.5f, 2.5f, 3.5f));

            // Engine containers (Array / Dictionary / typed arrays) as engine-backed wrappers with
            // GodotSharp identity semantics. Element access rewraps the engine object handle into
            // a typed shim whose engine calls work.
            Godot.Collections.Array<Godot.Node> siblings = GetParent().GetChildren(false);
            bool foundSelf = false;
            for (int si = 0; si < siblings.Count; si++)
            {
                Godot.Node sib = siblings[si];
                if (sib.GetName() == "RenamedNode")
                {
                    foundSelf = true;
                }
            }
            bool typedArrOk = siblings.Count >= 1 && foundSelf;
            Godot.GD.Print(string.Concat("MyNode: typed Array<Node> return + element access = ", typedArrOk.ToString()));

            // Typed array with string elements: the group added below must read back by name.
            AddToGroup("dn2cpp_group", false);
            Godot.Collections.Array<string> nodeGroups = GetGroups();
            bool foundGroup = false;
            for (int gi = 0; gi < nodeGroups.Count; gi++)
            {
                if (nodeGroups[gi] == "dn2cpp_group")
                {
                    foundGroup = true;
                }
            }
            Godot.GD.Print(string.Concat("MyNode: typed Array<string> groups = ", foundGroup.ToString()));

            // Typed Array<float>: the write side widens into the Variant's double payload, the
            // read side narrows it back.
            Godot.Collections.Array<float> floats = new Godot.Collections.Array<float>();
            floats.Add(1.5f);
            floats.Add(-0.25f);
            float f0 = floats[0];
            float f1 = floats[1];
            bool floatArrOk = floats.Count == 2 && f0 == 1.5f && f1 == -0.25f;
            Godot.GD.Print(string.Concat("MyNode: typed Array<float> add + element read = ", floatArrOk.ToString()));

            // Untyped Array ARGUMENT the engine reads: the inputs Array is borrowed for the call —
            // the wrapper still owns its container afterwards.
            Godot.Expression expr = new Godot.Expression();
            expr.Parse("a + b", new string[] { "a", "b" });
            Godot.Collections.Array inputs = new Godot.Collections.Array();
            inputs.Add(3);
            inputs.Add(4);
            Godot.Variant exprResult = expr.Execute(inputs, null, true, false);
            bool exprOk = (long)exprResult == 7 && inputs.Count == 2;
            Godot.GD.Print(string.Concat("MyNode: Array argument via Expression.Execute = ", exprOk.ToString()));

            // Array as a Variant payload: the engine's meta Variant holds its own reference to the
            // same shared container, so an element added AFTER SetMeta is visible on read-back.
            Godot.Collections.Array metaArr = new Godot.Collections.Array();
            metaArr.Add("x");
            SetMeta("v_arr", metaArr);
            metaArr.Add(42);
            Godot.Collections.Array metaBack = GetMeta("v_arr", default).AsGodotArray();
            bool arrShared = metaBack.Count == 2
                && (string)metaBack[0] == "x" && (long)metaBack[1] == 42;
            metaBack[1] = 43;
            bool arrSetOk = (long)metaArr[1] == 43;
            Godot.GD.Print(string.Concat("MyNode: Array Variant round trip shares the container = ",
                (arrShared && arrSetOk).ToString()));

            // Dictionary ARGUMENT the engine reads; insertion order survives into the query string.
            Godot.Collections.Dictionary query = new Godot.Collections.Dictionary();
            query["name"] = "dn2cpp";
            query["count"] = 3;
            Godot.HTTPClient http = new Godot.HTTPClient();
            string qs = http.QueryStringFromDict(query);
            bool dictArgOk = qs == "name=dn2cpp&count=3";
            Godot.GD.Print(string.Concat("MyNode: Dictionary argument query string = ", dictArgOk.ToString()));

            // Dictionary as a Variant payload plus the wrapper surface: Count, indexer reads,
            // ContainsKey (engine `has`) and Keys (engine `keys()`, a fresh wrapped Array).
            SetMeta("v_dict", query);
            Godot.Collections.Dictionary dictBack = GetMeta("v_dict", default).AsGodotDictionary();
            Godot.Collections.Array dictKeys = dictBack.Keys;
            bool dictVariantOk = dictBack.Count == 2
                && (string)dictBack["name"] == "dn2cpp" && (long)dictBack["count"] == 3
                && dictBack.ContainsKey("name") && !dictBack.ContainsKey("missing")
                && dictKeys.Count == 2 && (string)dictKeys[0] == "name" && (string)dictKeys[1] == "count";
            Godot.GD.Print(string.Concat("MyNode: Dictionary Variant round trip + Keys = ", dictVariantOk.ToString()));

            // A missing-key read must be non-mutating: NIL back, no slot inserted.
            Godot.Variant missingValue = dictBack["missing_key"];
            bool dictMissOk = missingValue.IsNil && dictBack.Count == 2
                && !dictBack.ContainsKey("missing_key");
            Godot.GD.Print(string.Concat("MyNode: Dictionary missing-key read is non-mutating = ", dictMissOk.ToString()));

            // Values (the engine's values()) plus foreach through the duck-typed struct enumerators.
            Godot.Collections.Array dictValues = dictBack.Values;
            bool valuesOk = dictValues.Count == 2 && (string)dictValues[0] == "dn2cpp"
                && (long)dictValues[1] == 3;
            int enumerated = 0;
            foreach (Godot.Variant unused in dictValues)
            {
                enumerated = enumerated + 1;
            }
            float floatSum = 0f;
            foreach (float fv in floats)
            {
                floatSum = floatSum + fv;
            }
            bool enumOk = enumerated == 2 && floatSum == 1.25f;
            Godot.GD.Print(string.Concat("MyNode: Dictionary.Values + foreach enumerators = ",
                (valuesOk && enumOk).ToString()));

            // Dictionary RETURN computed by the engine: a RegEx's named-group index map.
            Godot.RegEx namedRegex = new Godot.RegEx();
            namedRegex.Compile("(?<word>\\w+)=(?<num>\\d+)", true);
            Godot.RegExMatch namedMatch = namedRegex.Search("count=42", 0, -1);
            Godot.Collections.Dictionary groupNames = namedMatch.Names;
            bool dictRetOk = groupNames.Count == 2
                && (long)groupNames["word"] == 1 && (long)groupNames["num"] == 2;
            Godot.GD.Print(string.Concat("MyNode: Dictionary return (RegExMatch.Names) = ", dictRetOk.ToString()));

            // Typed array of Dictionary: each element is a live wrapper whose entries read back,
            // and the registered extension property must appear.
            Godot.Collections.Array<Godot.Collections.Dictionary> props = GetPropertyList();
            bool foundExportedProp = false;
            for (int pi = 0; pi < props.Count; pi++)
            {
                Godot.Collections.Dictionary prop = props[pi];
                if ((string)prop["name"] == "MyExportedProperty")
                {
                    foundExportedProp = true;
                }
            }
            bool propsOk = props.Count > 0 && foundExportedProp;
            Godot.GD.Print(string.Concat("MyNode: typed Array<Dictionary> property list = ", propsOk.ToString()));

            // Callable and Signal as first-class engine-call values. IsConnected passes another
            // Callable and returns what the engine actually recorded.
            bool cbConnected = IsConnected("MyCustomSignal", new Godot.Callable(this, "set_process"));
            Godot.GD.Print(string.Concat("MyNode: Callable argument IsConnected = ", cbConnected.ToString()));

            // A delegate-backed Callable: the engine invokes the custom callable, whose trampoline
            // decodes the args and re-enters the managed lambda.
            long lambdaScore = -1;
            Connect("ScoreChanged", Godot.Callable.From((Godot.Variant v) => { lambdaScore = (long)v; }), 0);
            EmitSignal("ScoreChanged", 55);
            Godot.GD.Print(string.Concat("MyNode: delegate Callable signal dispatch = ", (lambdaScore == 55).ToString()));

            // Callable.Call routes through the engine's own vararg `call`, for a standard callable
            // and for a zero-arg delegate one.
            string calledName = (string)new Godot.Callable(this, "get_name").Call();
            bool zeroRan = false;
            Godot.Callable.From(() => { zeroRan = true; }).Call();
            Godot.GD.Print(string.Concat("MyNode: Callable.Call standard + zero-arg = ",
                (calledName == "RenamedNode" && zeroRan).ToString()));

            // Callable RETURN from a real engine getter. A delegate-backed callable survives via its
            // custom-callable userdata; a standard one decodes back to target + method, and the
            // wrapped target drives further engine calls.
            Godot.SceneMultiplayer sm = new Godot.SceneMultiplayer();
            long authGot = -1;
            sm.AuthCallback = Godot.Callable.From((Godot.Variant v) => { authGot = (long)v; });
            Godot.Callable authBack = sm.AuthCallback;
            authBack.Call(77);
            bool customRoundTrip = authGot == 77;
            sm.AuthCallback = new Godot.Callable(this, "get_class");
            Godot.Callable classCb = sm.AuthCallback;
            bool standardRoundTrip = classCb.Method == "get_class"
                && classCb.Target is not null
                && classCb.Target.GetInstanceId() == GetInstanceId()
                && (string)classCb.Call() == "MyNode";
            Godot.GD.Print(string.Concat("MyNode: Callable return round trips (custom + standard) = ",
                (customRoundTrip && standardRoundTrip).ToString()));

            // A Variant-carried Callable round-trips through the meta store and still reaches the
            // same managed lambda.
            long metaCbGot = -1;
            SetMeta("v_cb", Godot.Callable.From((Godot.Variant v) => { metaCbGot = (long)v; }));
            Godot.Callable cbBack = GetMeta("v_cb", default).AsCallable();
            cbBack.Call(9);
            Godot.GD.Print(string.Concat("MyNode: Variant Callable round trip invokes = ", (metaCbGot == 9).ToString()));

            // Signal as a Variant payload: the (owner, name) pair rebuilds a real engine Signal.
            SetMeta("v_sig", new Godot.Signal(this, "ScoreChanged"));
            Godot.Signal sigBack = GetMeta("v_sig", default).AsSignal();
            bool vSignalOk = sigBack.Name == "ScoreChanged"
                && sigBack.Owner is not null
                && sigBack.Owner.GetInstanceId() == GetInstanceId();
            Godot.GD.Print(string.Concat("MyNode: Variant Signal round trip = ", vSignalOk.ToString()));

            // Vararg engine methods route through the variant-call interface, not a ptrcall.
            // has_method's verdict depends on the tail argument, so a present vs an absent method
            // proves the tail really reaches the engine rather than being folded to a constant.
            bool hasReal = (bool)Call("has_method", "has_method");
            bool hasBogus = (bool)Call("has_method", "definitely_not_a_method_zzz");
            Godot.GD.Print(string.Concat("MyNode: vararg Object.Call round trip = ",
                (hasReal && !hasBogus).ToString()));

            // Static engine methods (null-instance ptrcall): Stringify covers a Variant argument
            // plus String args/return, ParseString a String argument plus Variant return.
            string jsonInt = Godot.JSON.Stringify(42, "", true, false);
            Godot.Variant parsedStr = Godot.JSON.ParseString("\"hello\"");
            bool staticOk = jsonInt == "42" && (string)parsedStr == "hello";
            Godot.GD.Print(string.Concat("MyNode: static JSON round trip = ", staticOk.ToString()));

            SingletonSection();

            // The generalized engine-virtual reverse dispatch is exercised from test.gd, which
            // instantiates the C# Control subclass MyControl via ClassDB and calls into it.

            // The RefCounted-through-Variant hand-off test lives in MyNode3D: it needs a GC.Collect
            // and only exists in the scene run.

            // Start the Task.Run worker pool from inside the engine: the awaited value resumes on a
            // later frame's scheduler pump, and the running workers arm the unload half —
            // Deinitialize must quiesce them before the engine closes this library. async Task
            // rather than async void: AsyncVoidMethodBuilder's exception path drags in the
            // untranspilable EventSource cascade.
            _ = RunPoolProbeAsync();
        }

        /// <summary>
        /// Engine singletons, and the System.IO round trip they unlock. The asserts check for a
        /// NON-EMPTY result, not merely that two spellings agree: a shim with a null engine
        /// handle silently no-ops, and "" == "" would pass. The whole section is wrapped so a
        /// throw reports as the `= False` line the gate greps for instead of stalling.
        /// </summary>
        private void SingletonSection()
        {
            try
            {
                // The two spellings of one singleton: the static facade real game code writes, and
                // `.Singleton`. Both must reach the same engine object, and both must answer.
                string viaFacade = Godot.ProjectSettings.GlobalizePath("user://");
                string viaInstance = Godot.ProjectSettings.Singleton.GlobalizePath("user://");
                string userDir = Godot.OS.GetUserDataDir();

                bool nonEmpty = !string.IsNullOrEmpty(viaFacade)
                    && !string.IsNullOrEmpty(viaInstance)
                    && !string.IsNullOrEmpty(userDir);
                bool agree = viaFacade == viaInstance;
                // The two singletons must name the same writable root — a cross-check that the
                // receiver is the real engine object, not an echo of our own argument.
                bool crossOk = viaFacade.TrimEnd('/') == userDir.TrimEnd('/');
                Godot.GD.Print(string.Concat("MyNode: singleton both spellings non-empty + agree = ",
                    (nonEmpty && agree && crossOk).ToString()));

                // Scalar returns (int and bool) through a singleton receiver, on two more singletons.
                long ticks = Godot.Time.GetTicksMsec();
                bool editorHint = Godot.Engine.IsEditorHint();
                Godot.GD.Print(string.Concat("MyNode: singleton scalar returns = ",
                    (ticks > 0 && !editorHint).ToString()));

                // A singleton this build does not have must DEGRADE, not crash. EditorInterface is
                // editor-only, so global_get_singleton hands back null here and the runtime caches
                // that, reporting it once. Both halves matter: the object reads null, AND a call
                // through the absent facade no-ops to a default instead of dereferencing null.
                bool absentIsNull = Godot.EditorInterface.Singleton is null;
                double absentCall = Godot.EditorInterface.GetEditorScale();
                bool absentOk = absentIsNull && absentCall == 0.0;
                Godot.GD.Print(string.Concat("MyNode: absent singleton degrades to null = ",
                    absentOk.ToString()));

                // A real System.IO.FileStream under the globalized user:// root, with the incremental
                // GC on (the Godot default), which is what exercises the kernel-write bounce.
                // System.IO takes OS paths, never res:// or user:// — hence GlobalizePath first.
                string path = System.IO.Path.Combine(userDir, "dn2cpp_io_probe.bin");
                byte[] payload = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x00, 0x7F, 0x80, 0xFF };

                using (System.IO.FileStream fs = new System.IO.FileStream(
                    path, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                {
                    fs.Write(payload, 0, payload.Length);
                }

                byte[] readBack = new byte[payload.Length];
                int got = 0;
                using (System.IO.FileStream fs = new System.IO.FileStream(
                    path, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                {
                    got = fs.Read(readBack, 0, readBack.Length);
                }

                bool bytesOk = got == payload.Length;
                for (int i = 0; i < payload.Length; i++)
                {
                    if (readBack[i] != payload[i])
                        bytesOk = false;
                }
                // Delete so a rerun starts from the same state.
                System.IO.File.Delete(path);
                bool gone = !System.IO.File.Exists(path);

                Godot.GD.Print(string.Concat("MyNode: FileStream round trip under user:// = ",
                    (bytesOk && gone).ToString()));
            }
            catch (System.Exception e)
            {
                // Loud, not silent: name the failure and keep the `= False` shape the gate greps for.
                Godot.GD.Print(string.Concat("MyNode: singleton section threw: ", e.GetType().ToString(), ": ", e.Message));
                Godot.GD.Print("MyNode: singleton both spellings non-empty + agree = False");
                Godot.GD.Print("MyNode: singleton scalar returns = False");
                Godot.GD.Print("MyNode: absent singleton degrades to null = False");
                Godot.GD.Print("MyNode: FileStream round trip under user:// = False");
            }
        }

        private async System.Threading.Tasks.Task RunPoolProbeAsync()
        {
            int value = await System.Threading.Tasks.Task.Run(() => 40 + 2);
            Godot.GD.Print(string.Concat("MyNode: Task.Run pool round trip = ", (value == 42).ToString()));
        }

        // A user method sharing the engine virtual's name and arity but not its parameter type:
        // resolution must bind the (double) override below. Declared FIRST so a name+arity
        // match would pick it and fail the first-tick assert.
        public void _Process(int mode)
        {
            Godot.GD.Print(string.Concat("FAIL: _Process(int) dispatched as an engine virtual, mode=", mode.ToString()));
        }

        public override void _Process(double delta)
        {
            _ticks = _ticks + 1;
            if (_ticks == 1)
            {
                string positive = delta >= 0.0 ? "True" : "False";
                Godot.GD.Print(string.Concat("MyNode._Process: first tick, delta >= 0: ", positive));
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            _physicsTicks = _physicsTicks + 1;
            if (_physicsTicks == 1)
            {
                string positive = delta >= 0.0 ? "True" : "False";
                Godot.GD.Print(string.Concat("MyNode._PhysicsProcess: first tick, delta >= 0: ", positive));
            }
        }

        public override void _ExitTree()
        {
            Godot.GD.Print("MyNode._ExitTree: leaving the scene tree");
        }

        // Engine input virtuals. The InputEvent argument is marshalled into a managed shim;
        // IsPressed proves the wrapped handle drives further engine calls.
        public override void _Input(Godot.InputEvent @event)
        {
            if (!_gotInput)
            {
                _gotInput = true;
                bool pressed = @event.IsPressed();
                Godot.GD.Print(string.Concat("MyNode._Input: received InputEvent, IsPressed=", pressed.ToString()));
            }
        }

        public override void _UnhandledInput(Godot.InputEvent @event)
        {
            if (!_gotUnhandledInput)
            {
                _gotUnhandledInput = true;
                bool pressed = @event.IsPressed();
                Godot.GD.Print(string.Concat("MyNode._UnhandledInput: received InputEvent, IsPressed=", pressed.ToString()));
            }
        }

        // The notification virtual, bridged via the GDExtension notification_func slot rather
        // than the virtual-call path. ENTER_TREE=10, EXIT_TREE=11, READY=13.
        public override void _Notification(int what)
        {
            if (what == 10)
            {
                Godot.GD.Print(string.Concat("MyNode._Notification: ENTER_TREE what=", what.ToString()));
            }
            else if (what == 11)
            {
                Godot.GD.Print(string.Concat("MyNode._Notification: EXIT_TREE what=", what.ToString()));
            }
            else if (what == 13)
            {
                Godot.GD.Print(string.Concat("MyNode._Notification: READY what=", what.ToString()));
            }
        }

        public int MultiplyByTwo(int value)
        {
            return value * 2;
        }

        // A leading-underscore user method that is NOT an engine virtual (the `_On...` handler
        // convention): it must register as a normal ClassDB method bind.
        public int _OnScoreHandler(int score)
        {
            return score + 7;
        }
    }
}
