#nullable disable
using System;
using System.IO;
using System.Reflection;

// The catalog half of the manifest-resource surface: GetManifestResourceNames,
// GetManifestResourceInfo, the misses (a missing resource is null, NOT an exception)
// and the argument-validation shapes. Also crosses an assembly boundary: CoreLib's own
// embedded resources are carried too, because the answer to "does this assembly have
// resource X" must not depend on which module the question is about.
namespace ResourceCatalogSubset
{
    internal static class Program
    {
        internal static void __GateEntry()
        {
            Console.WriteLine("== resource catalog ==");
            Assembly asm = Assembly.GetExecutingAssembly();

            string[] names = asm.GetManifestResourceNames();
            // Sorted: metadata order is what both sides report, but nothing in .NET
            // specifies it, so the assert is on the SET.
            Array.Sort(names, StringComparer.Ordinal);
            Console.WriteLine("names-count " + names.Length);
            for (int i = 0; i < names.Length; i++)
                Console.WriteLine("name " + names[i]);

            Console.WriteLine("miss-stream " + (asm.GetManifestResourceStream("no.such.resource") == null));
            Console.WriteLine("miss-info " + (asm.GetManifestResourceInfo("no.such.resource") == null));
            // A name that exists only unscoped must NOT resolve through the scoped
            // overload, and vice versa.
            Console.WriteLine("miss-scoped "
                + (asm.GetManifestResourceStream(typeof(Anchor), "hello.txt") == null));

            ManifestResourceInfo info = asm.GetManifestResourceInfo("ManifestResources.hello.txt");
            Console.WriteLine("info-null " + (info == null));
            Console.WriteLine("info-loc " + (int)info.ResourceLocation);
            Console.WriteLine("info-file " + (info.FileName ?? "null"));
            Console.WriteLine("info-refasm " + (info.ReferencedAssembly == null));

            try
            {
                asm.GetManifestResourceStream(null);
                Console.WriteLine("null-name no-throw");
            }
            catch (Exception e)
            {
                Console.WriteLine("null-name " + e.GetType().Name);
            }
            try
            {
                asm.GetManifestResourceStream("");
                Console.WriteLine("empty-name no-throw");
            }
            catch (Exception e)
            {
                Console.WriteLine("empty-name " + e.GetType().Name);
            }

            // Cross-assembly: CoreLib carries its own embedded resources (its SR
            // string set and the ILLink descriptor documents). Each probe goes through
            // Probe(): in this control arm nothing throws, so every line still diffs
            // live against real .NET — and under the resource-drop gate arms
            // (--no-manifest-resources System.Private.CoreLib, with or without a
            // --manifest-resource-root keeping the Strings set) the catchable
            // PlatformNotSupportedException is reported instead, which is what the
            // frozen snapshots pin: a rooted name keeps answering (set-big/info), a
            // miss throws rather than lying with null (illink/miss), and the
            // enumeration throws on the mere ask (any/strings).
            Assembly core = typeof(object).Assembly;
            Probe("corelib-any", () => core.GetManifestResourceNames().Length > 0);
            Probe("corelib-strings", () => Array.IndexOf(core.GetManifestResourceNames(),
                "System.Private.CoreLib.Strings.resources") >= 0);
            Probe("corelib-set-big", () =>
            {
                using (Stream s = core.GetManifestResourceStream("System.Private.CoreLib.Strings.resources"))
                    return s != null && s.Length > 1000;
            });
            Probe("corelib-info", () =>
                core.GetManifestResourceInfo("System.Private.CoreLib.Strings.resources") != null);
            Probe("corelib-illink", () =>
            {
                using (Stream s = core.GetManifestResourceStream("ILLink.Substitutions.xml"))
                    return s != null;
            });
            Probe("corelib-miss", () => core.GetManifestResourceStream("no.such.resource") == null);
        }

        // One CoreLib-facing probe line: the answer, or — when the image was
        // transpiled with --no-manifest-resources System.Private.CoreLib — the
        // catchable PlatformNotSupportedException's message, which must name the
        // assembly and the remedy (asserted verbatim by the frozen gate arms).
        private static void Probe(string label, Func<bool> f)
        {
            try
            {
                Console.WriteLine(label + " " + f());
            }
            catch (PlatformNotSupportedException e)
            {
                Console.WriteLine(label + " PNSE " + e.Message);
            }
        }

        // Lives in ResourceCatalogSubset, a namespace no resource is named for.
        private sealed class Anchor
        {
        }
    }
}
