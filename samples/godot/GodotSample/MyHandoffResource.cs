namespace GodotSample
{
    /// <summary>A registered Resource proving engine dispatch still reaches a C#
    /// override after every C# reference is dropped, as long as the engine itself still
    /// references the instance. GDScript triggers the notification directly, so the flag
    /// can only be observed set if object_set_instance still resolves to this shim.</summary>
    public class MyHandoffResource : Godot.Resource
    {
        public const int ProbeNotification = 9001;

        private bool _notified;

        public override void _Notification(int what)
        {
            if (what == ProbeNotification)
                _notified = true;
        }

        public bool WasNotified()
        {
            return _notified;
        }
    }
}
