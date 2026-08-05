using Dn2Cpp.Godot;
using System.Globalization;

// Focused native self-host root for the Godot binding-generation path. Calls the
// REAL Dn2Cpp.Godot.BindingGenerator.Run, which deserializes extension_api.json
// through GodotApi.Load (a System.Text.Json source-gen JsonSerializerContext, not
// the reflection Deserialize<T>) and emits the engine shim C# into the output path.
//
// Transpiled to native C++ by dn2cpp itself and run on the real ~6.4 MiB
// extension_api.json, this exercises the actual binding-generation path on real
// System.Text.Json IL end to end (deserialize + StringBuilder emit + File write),
// not just a deserialize probe. The emitted shim file is diffed against the managed
// (`dotnet`) output of this same driver to prove byte-identical generation — the
// self-host fixpoint for the `--generate-bindings` subtree.

public static class Program
{
    public static int Main(string[] args)
    {
        // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        if (args.Length < 2)
        {
            System.Console.Error.WriteLine("usage: GodotBindGen <extension_api.json> <out.g.cs>");
            return 1;
        }

        BindingGenerator.Run(args[0], args[1]);
        System.Console.WriteLine("generated " + args[1]);
        return 0;
    }
}
