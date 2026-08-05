namespace GodotSample
{
    /// <summary>A registered Resource whose constructor always throws. The engine object
    /// is constructed and the shim bound before the ctor body runs, so the finalizer must
    /// be registered first — otherwise the constructed engine object leaks.</summary>
    public class MyThrowingResource : Godot.Resource
    {
        public MyThrowingResource()
        {
            throw new System.InvalidOperationException("MyThrowingResource ctor throws by design");
        }
    }
}
