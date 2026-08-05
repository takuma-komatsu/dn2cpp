using System;
using System.Collections.Generic;
using System.IO;

namespace ZipFileCore
{
    // Shared scratch-dir and output helpers. Names are FIXED, not Guids, whose
    // formatting drags unrelated SIMD/hex paths into the reachable set; each is
    // pre-cleaned so a crashed run cannot leak state into either side of the diff.
    //
    // DETERMINISM RULES binding every section here: print only ordinal-sorted
    // relative '/' paths (readdir order is filesystem-dependent), booleans, counts
    // and exception TYPE names — never messages (absolute paths), timestamps
    // (DOS-time is second-granular) or compressed sizes (zlib-ng vs zlib).
    internal static class ZipScratch
    {
        internal static string Fresh(string name)
        {
            string dir = Path.Combine(Path.GetTempPath(), "dn2cpp-zipfilecore-" + name);
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
            Directory.CreateDirectory(dir);
            return dir;
        }

        internal static void Cleanup(string dir)
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        // Root-relative, '/'-separated, directories marked with a trailing '/'.
        internal static List<string> ListTree(string root)
        {
            var list = new List<string>();
            foreach (string p in Directory.EnumerateFileSystemEntries(root, "*", SearchOption.AllDirectories))
            {
                string rel = Relative(root, p);
                if (Directory.Exists(p))
                {
                    rel += "/";
                }
                list.Add(rel);
            }
            SortOrdinal(list);
            return list;
        }

        internal static string Relative(string root, string path)
        {
            string rel = path.Substring(root.Length);
            int start = 0;
            while (start < rel.Length && rel[start] == Path.DirectorySeparatorChar)
                start++;
            rel = rel.Substring(start);
            if (Path.DirectorySeparatorChar != '/')
            {
                char[] chars = rel.ToCharArray();
                for (int i = 0; i < chars.Length; i++)
                    if (chars[i] == Path.DirectorySeparatorChar)
                        chars[i] = '/';
                rel = new string(chars);
            }
            return rel;
        }

        // Ordinal, so the order is culture-free and identical on both sides.
        internal static void SortOrdinal(List<string> list)
        {
            for (int i = 1; i < list.Count; i++)
            {
                string key = list[i];
                int j = i - 1;
                while (j >= 0 && string.CompareOrdinal(list[j], key) > 0)
                {
                    list[j + 1] = list[j];
                    j--;
                }
                list[j + 1] = key;
            }
        }

        internal static void Print(string label, List<string> items)
        {
            string joined = "";
            for (int i = 0; i < items.Count; i++)
                joined = i == 0 ? items[i] : joined + ", " + items[i];
            Console.WriteLine(label + ": [" + joined + "]");
        }
    }
}
