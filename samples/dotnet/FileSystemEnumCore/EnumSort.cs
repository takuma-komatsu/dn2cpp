using System;
using System.Collections.Generic;
using System.IO;

namespace FileSystemEnumCore
{
    // Shared output helpers. readdir order is filesystem-dependent, so every
    // section sorts (ordinal) before printing, and prints root-relative paths
    // with '/' separators only.
    internal static class EnumSort
    {
        internal static List<string> Relative(string root, IEnumerable<string> paths)
        {
            var list = new List<string>();
            foreach (string p in paths)
            {
                string rel = p.Substring(root.Length);
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
                list.Add(rel);
            }
            SortOrdinal(list);
            return list;
        }

        // Insertion sort on string.CompareOrdinal — culture-free and identical
        // between real .NET and the transpiled binary.
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
