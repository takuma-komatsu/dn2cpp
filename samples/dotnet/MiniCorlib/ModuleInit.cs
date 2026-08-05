using System.Runtime.CompilerServices;

namespace MiniBcl
{
    // A [ModuleInitializer] in a REFERENCED library (this assembly is pulled in with -r).
    // Nothing calls Init(): the C# compiler hangs it off this assembly's `<Module>` .cctor,
    // and no call edge from the app ever reaches it. A reference assembly's .cctor is not a
    // root — it is pulled in when its declaring type is ALLOCATED — and `<Module>` is never
    // allocated, so without an explicit root the library's initializer would silently never
    // run. The app's Main asserts Ready is true and fails loudly (non-zero exit) if not.
    public static class Boot
    {
        public static bool Ready;

        public static string Banner = "<unset>";

        // CA2255 warns that [ModuleInitializer] is meant for application code — but a
        // library self-registering before anyone touches it is exactly the shape under
        // test, and exactly the shape a call-graph tree-shaker cannot see.
#pragma warning disable CA2255
        [ModuleInitializer]
        internal static void Init()
#pragma warning restore CA2255
        {
            Ready = true;
            Banner = "minicorlib-booted";
        }
    }
}
