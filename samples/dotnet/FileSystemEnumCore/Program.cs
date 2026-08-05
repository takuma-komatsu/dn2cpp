using System;
using System.IO;
using System.Globalization;

namespace FileSystemEnumCore
{
    // Consolidated gate driver: runs each section's __GateEntry() in order.
    // Each section keeps its own namespace so namespace-sensitive output stays
    // identical to a standalone build. args[0] is a fresh scratch directory the
    // gate script created (mktemp -d) and cleans up; every section prints only
    // root-relative paths and booleans — never the absolute scratch path — so
    // the native run and the real-.NET run diff exactly across different roots.
    internal static class Program
    {
        private static void Main(string[] args)
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            string root = args[0];
            CreateFixture(root);
            DirectoryEnumerateSubset.Program.__GateEntry(root);
            DirectoryInfoEnumerateSubset.Program.__GateEntry(root);
            CreateDirectoryInfoSubset.Program.__GateEntry(root);
            DirectoryDeleteSubset.Program.__GateEntry(root);
            DirectoryCreateSubset.Program.__GateEntry(root);   // was directory-subset
            FileIoSubset.Program.__GateEntry(root);            // was file-subset
        }

        // Known tree the enumeration sections walk:
        //   alpha.txt  beta.log  gamma.txt
        //   sub1/delta.txt  sub1/eps.log  sub1/nested/zeta.txt
        //   sub2/eta.txt
        //   emptydir/
        private static void CreateFixture(string root)
        {
            Directory.CreateDirectory(Path.Combine(root, "sub1", "nested"));
            Directory.CreateDirectory(Path.Combine(root, "sub2"));
            Directory.CreateDirectory(Path.Combine(root, "emptydir"));
            File.WriteAllText(Path.Combine(root, "alpha.txt"), "a");
            File.WriteAllText(Path.Combine(root, "beta.log"), "b");
            File.WriteAllText(Path.Combine(root, "gamma.txt"), "c");
            File.WriteAllText(Path.Combine(root, "sub1", "delta.txt"), "d");
            File.WriteAllText(Path.Combine(root, "sub1", "eps.log"), "e");
            File.WriteAllText(Path.Combine(root, "sub1", "nested", "zeta.txt"), "z");
            File.WriteAllText(Path.Combine(root, "sub2", "eta.txt"), "h");
        }
    }
}
