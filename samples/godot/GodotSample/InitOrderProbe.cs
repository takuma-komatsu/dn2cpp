namespace GodotSample
{
    /// <summary>What the precise-init exports below record from their initializers. A type
    /// of its own, so reading it triggers neither initializer.</summary>
    internal static class InitOrderWitness
    {
        internal static bool StaticBindInitialized;
        internal static bool ConstructedInitialized;
    }

    /// <summary>A precise-init export that GDScript enters first through a static bind,
    /// which runs the initializer before the bound method's body.</summary>
    public static class InitOrderApi
    {
        static InitOrderApi() => InitOrderWitness.StaticBindInitialized = true;

        public static bool InitializedBeforeBody() => InitOrderWitness.StaticBindInitialized;
    }

    /// <summary>A precise-init registered Resource that GDScript enters first through
    /// ClassDB construction, which runs the initializer before the constructor's body.
    /// Calling the instance method afterwards triggers nothing.</summary>
    public class MyInitOrderResource : Godot.Resource
    {
        private readonly bool _initializedFirst;

        static MyInitOrderResource() => InitOrderWitness.ConstructedInitialized = true;

        public MyInitOrderResource() => _initializedFirst = InitOrderWitness.ConstructedInitialized;

        public bool InitializedBeforeConstructor() => _initializedFirst;
    }
}
