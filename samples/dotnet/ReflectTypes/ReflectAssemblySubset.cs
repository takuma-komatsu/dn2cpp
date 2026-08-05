#nullable disable
using System;
using System.Reflection;

// Assembly/Module/AssemblyName identity. Type.AssemblyQualifiedName composes
// "FullName, <assembly display name>" (Name, Version, Culture, PublicKeyToken read
// from the module metadata) and round-trips through Type.GetType(string); Assembly
// FullName/GetName()/GetType/GetModules/IsDynamic and the Module surface follow the
// single-image model (the manifest module IS the assembly). AssemblyName is a real
// managed object (the transpiled BCL class): the parameterless ctor + setters, the
// display-name-parsing ctor, Version (a real System.Version) and FullName all run
// real BCL IL. NAME-based Assembly loading (Load(String/AssemblyName),
// LoadWithPartialName) resolves against the linked-assembly registry and matches
// real .NET on every line here (case-insensitive simple name, whitespace pad,
// display-name form, miss -> FileNotFoundException / null, empty ->
// ArgumentException), including the Newtonsoft DefaultSerializationBinder probe
// shape LoadWithPartialName(name)?.GetType(fullName). The one loud cut left here
// throws the catchable PlatformNotSupportedException: the path/byte-image loads
// (Assembly.LoadFile — no loader in the AOT image; real .NET would probe the
// disk). GetManifestResourceStream is NOT here: it answers over carried blobs and
// matches real .NET exactly, so its coverage lives in the live-diff
// manifest-resources gate — which also keeps this program from carrying every loaded
// module's resource blobs for the sake of one null probe.
namespace ReflectAssemblySubset
{
    public class Widget { }

    static class Program
    {
        internal static void Run()
        {
            Console.WriteLine("== assembly identity ==");

            // AQN for a user type and a BCL type + GetType round-trip.
            Console.WriteLine(typeof(Widget).AssemblyQualifiedName);
            Console.WriteLine(typeof(int).AssemblyQualifiedName);
            Console.WriteLine("roundtrip-user " + (Type.GetType(typeof(Widget).AssemblyQualifiedName) == typeof(Widget)));
            Console.WriteLine("roundtrip-bcl " + (Type.GetType(typeof(int).AssemblyQualifiedName) == typeof(int)));

            // Executing-assembly identity + display names.
            Assembly exec = Assembly.GetExecutingAssembly();
            Console.WriteLine("exec-is-entry " + (exec == Assembly.GetEntryAssembly()));
            Console.WriteLine(exec.FullName);
            Console.WriteLine(typeof(int).Assembly.FullName);
            Console.WriteLine(exec); // WriteLine(object) on an Assembly => FullName

            // Assembly.GetType: hit, miss, wrong assembly, ignoreCase, throwOnError.
            Console.WriteLine("get-hit " + (exec.GetType("ReflectAssemblySubset.Widget")?.Name ?? "null"));
            Console.WriteLine("get-miss " + (exec.GetType("ReflectAssemblySubset.Missing") is null));
            Console.WriteLine("get-wrong-asm " + (typeof(int).Assembly.GetType("ReflectAssemblySubset.Widget") is null));
            Console.WriteLine("get-icase " + (exec.GetType("reflectassemblysubset.widget", false, true)?.Name ?? "null"));
            try { exec.GetType("ReflectAssemblySubset.Missing", true); Console.WriteLine("no-throw"); }
            catch (TypeLoadException) { Console.WriteLine("get-throw TypeLoadException"); }

            // GetName(): a real AssemblyName carrying name + version.
            AssemblyName an = exec.GetName();
            Console.WriteLine("name " + an.Name);
            Console.WriteLine("version " + an.Version);
            Console.WriteLine("fullname " + an.FullName);

            // AssemblyName as a plain object: ctor + setters, display-name parse.
            var built = new AssemblyName();
            built.Name = "Synthetic";
            built.Version = new Version(2, 3);
            Console.WriteLine("built " + built.Name + " / " + built.Version + " / " + built.FullName);
            var parsed = new AssemblyName("Parsed, Version=9.8.7.6, Culture=neutral, PublicKeyToken=null");
            Console.WriteLine("parsed " + parsed.Name + " / " + parsed.Version);

            // Module surface: name forms, identity, single-module model.
            Module mod = typeof(Widget).Module;
            Console.WriteLine("module-name " + mod.Name);
            Console.WriteLine("module-tostring " + mod.ToString());
            Console.WriteLine("module-is-manifest " + (mod == exec.ManifestModule));
            Console.WriteLine("module-assembly " + (mod.Assembly == exec));
            Console.WriteLine("modules " + exec.GetModules().Length);
            Console.WriteLine("dynamic " + exec.IsDynamic);
            Console.WriteLine(mod); // WriteLine(object) on a Module => module name

            // Name-based Assembly loading: a lookup over the linked-assembly
            // registry (every line below matches real .NET, verified against
            // `dotnet run` at capture time). The display-name form carries no
            // Version on purpose: dn2cpp ignores a requested Version where real
            // .NET refuses one above the loaded assembly's — the documented
            // divergence this section deliberately stays off.
            Console.WriteLine("== assembly load ==");
            Console.WriteLine("load-hit " + (Assembly.Load("ReflectTypes") == exec));
            Console.WriteLine("load-icase " + (Assembly.Load("REFLECTTYPES") == exec));
            Console.WriteLine("load-display " + (Assembly.Load("ReflectTypes, Culture=neutral, PublicKeyToken=null") == exec));
            Console.WriteLine("load-pad " + (Assembly.Load(" ReflectTypes ") == exec));
            try { Assembly.Load("NoSuchAssembly"); Console.WriteLine("no-throw"); }
            catch (System.IO.FileNotFoundException) { Console.WriteLine("load-miss FileNotFoundException"); }
            try { Assembly.Load(""); Console.WriteLine("no-throw"); }
            catch (ArgumentException) { Console.WriteLine("load-empty ArgumentException"); }
            Console.WriteLine("load-an " + (Assembly.Load(new AssemblyName("ReflectTypes")) == exec));
#pragma warning disable 618, SYSLIB0018 // LoadWithPartialName is obsolete — that IS the surface under test
            Console.WriteLine("lwpn-hit " + (Assembly.LoadWithPartialName("ReflectTypes") == exec));
            Console.WriteLine("lwpn-miss " + (Assembly.LoadWithPartialName("NoSuchAssembly") is null));
            // The Newtonsoft DefaultSerializationBinder probe shape (Thrive's
            // registry JSON unlock-condition type resolution): partial-name load,
            // then Assembly.GetType over the returned handle.
            Assembly probed = Assembly.LoadWithPartialName("ReflectTypes");
#pragma warning restore 618, SYSLIB0018
            Console.WriteLine("binder " + (probed?.GetType("ReflectAssemblySubset.Widget")?.FullName ?? "null"));

            // Loud cut: catchable PlatformNotSupportedException (INTENTIONAL
            // DIVERGENCE — real .NET probes the loader instead).
            try { Assembly.LoadFile("/nonexistent/x.dll"); Console.WriteLine("no-throw"); }
            catch (PlatformNotSupportedException) { Console.WriteLine("load-file PNSE"); }
            catch (Exception e) { Console.WriteLine("load-file " + e.GetType().Name); }
        }
    }
}
