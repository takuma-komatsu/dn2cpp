using Godot;

// A Node2D covering the full engine-virtual set plus headless input injection,
// mirroring the GDExtension lane's MyNode so the two samples cover one surface.
//
// This node also ends the run: the scene quits only after the Player flow AND
// every parity condition here have completed, so no marker races the quit. The
// cross-script static flag doubles as proof that managed statics are shared
// across script instances in the module.
public partial class ParityNode : Node2D
{
    private static bool _playerFlowDone;

    private int _processTicks;
    private int _physicsTicks;
    private bool _gotInput;
    private bool _gotUnhandledInput;
    private bool _quitRequested;

    // Called by Player once its timer-dependent markers are all out.
    public static void NotifyPlayerFlowDone()
    {
        _playerFlowDone = true;
    }

    public override void _EnterTree()
    {
        GD.Print("DN2CPP_DM_ENTER_TREE");
    }

    public override void _Ready()
    {
        // Reverse-direction engine calls through the generated NativeCalls ptrcall
        // bindings: bool/String/NodePath returns, Vector2 and String property round
        // trips, and an Object return whose wrapper drives a further engine call.
        bool inTree = IsInsideTree();
        string klass = GetClass();
        Position = new Vector2(30.5f, 40.5f);
        Vector2 pos = Position;
        EditorDescription = "hello from dm";
        bool descOk = EditorDescription == "hello from dm";
        Node parent = GetParent();
        bool pathOk = GetPath().ToString().EndsWith("/Parity");
        bool hasSelf = HasNode(".");
        bool selfAncestor = IsAncestorOf(this);
        GD.Print($"DN2CPP_DM_ENGINE inTree={inTree} class={klass} pos=({pos.X}, {pos.Y}) desc={descOk} parent={parent.Name} path={pathOk} hasSelf={hasSelf} selfAncestor={selfAncestor}");
    }

    public override void _Process(double delta)
    {
        _processTicks++;
        if (_processTicks == 1)
        {
            GD.Print($"DN2CPP_DM_PARITY_PROCESS deltaOk={delta >= 0.0}");
            InjectKeyEvent();
        }
        if (!_quitRequested && _playerFlowDone && _gotInput && _gotUnhandledInput
            && _physicsTicks > 0)
        {
            _quitRequested = true;
            GD.Print("DN2CPP_DM_PARITY_DONE");
            GetTree().Quit();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        _physicsTicks++;
        if (_physicsTicks == 1)
        {
            GD.Print($"DN2CPP_DM_PHYSICS deltaOk={delta > 0.0}");
        }
    }

    // C#-constructed engine class: ctor through ClassDB, property sets/gets
    // through ptrcalls, values asserted before the event is handed over.
    // push_input dispatches _Input/_UnhandledInput synchronously.
    private void InjectKeyEvent()
    {
        var ev = new InputEventKey();
        ev.Keycode = Key.A;
        ev.PhysicalKeycode = Key.A;
        ev.Pressed = true;
        bool roundtrip = ev.Keycode == Key.A && ev.PhysicalKeycode == Key.A
            && ev.Pressed && !ev.Echo;
        GD.Print($"DN2CPP_DM_EVKEY roundtrip={roundtrip}");
        GetViewport().PushInput(ev);
    }

    // The borrowed engine-owned event arrives as a typed managed wrapper; the
    // engine call plus the property read prove the handle and the payload both
    // survived the dispatch.
    public override void _Input(InputEvent @event)
    {
        if (_gotInput || @event is not InputEventKey key)
        {
            return;
        }
        _gotInput = true;
        GD.Print($"DN2CPP_DM_INPUT pressed={key.IsPressed()} keycodeOk={key.Keycode == Key.A}");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_gotUnhandledInput || @event is not InputEventKey key)
        {
            return;
        }
        _gotUnhandledInput = true;
        GD.Print($"DN2CPP_DM_UNHANDLED pressed={key.IsPressed()} keycodeOk={key.Keycode == Key.A}");
    }

    public override void _Notification(int what)
    {
        if (what == NotificationEnterTree)
        {
            GD.Print($"DN2CPP_DM_NOTIFY_ENTER_TREE what={what}");
        }
        else if (what == NotificationReady)
        {
            GD.Print($"DN2CPP_DM_NOTIFY_READY what={what}");
        }
        else if (what == NotificationExitTree)
        {
            GD.Print($"DN2CPP_DM_NOTIFY_EXIT_TREE what={what}");
        }
    }

    public override void _ExitTree()
    {
        GD.Print("DN2CPP_DM_EXIT_TREE");
    }
}
