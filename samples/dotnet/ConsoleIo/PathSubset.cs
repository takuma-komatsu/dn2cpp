#nullable enable
using System;
using System.IO;

// System.IO.Path pure-lexical operations lowered to dn2cpp_path_*
// runtime helpers (Unix '/' separator). Every line is diffed exact vs real .NET
// (the gate runs both from the same working directory, so the GetFullPath cases
// resolve against an identical cwd). Brackets make empty strings visible.
namespace PathSubset;

internal static class Program
{
    static void S(string label, string v) => Console.WriteLine($"{label}=[{v}]");

    internal static void __GateEntry()
    {
        // Combine: rooted second arg wins, no doubled separator, empty is dropped.
        S("comb2", Path.Combine("a", "b"));
        S("comb2sep", Path.Combine("a/", "b"));
        S("comb2rooted", Path.Combine("a", "/b"));
        S("comb2empty1", Path.Combine("", "b"));
        S("comb2empty2", Path.Combine("a", ""));
        S("comb2abs", Path.Combine("/x", "y"));
        S("comb3", Path.Combine("a", "b", "c"));
        S("comb3midroot", Path.Combine("a", "/b", "c"));
        S("comb4", Path.Combine("a", "b", "c", "d.txt"));

        // GetFileName / GetDirectoryName (null for null/empty/root).
        S("fn1", Path.GetFileName("/a/b/c.txt"));
        S("fn2", Path.GetFileName("c.txt"));
        S("fnTrail", Path.GetFileName("/a/b/"));
        S("fnNoDir", Path.GetFileName("/a/b"));
        S("dn1", Path.GetDirectoryName("/a/b/c.txt") ?? "<null>");
        S("dnNoSep", Path.GetDirectoryName("c.txt") ?? "<null>");
        S("dnTrail", Path.GetDirectoryName("/a/b/") ?? "<null>");
        S("dnRoot", Path.GetDirectoryName("/") ?? "<null>");
        S("dnRel", Path.GetDirectoryName("a/b") ?? "<null>");
        S("dnDot", Path.GetDirectoryName("./a") ?? "<null>");

        // GetExtension / GetFileNameWithoutExtension.
        S("ext1", Path.GetExtension("/a/b.txt"));
        S("extNone", Path.GetExtension("/a/b"));
        S("extMulti", Path.GetExtension("/a/b.tar.gz"));
        S("extHidden", Path.GetExtension("/a/.hidden"));
        S("extDotEnd", Path.GetExtension("a."));
        S("fnwe1", Path.GetFileNameWithoutExtension("/a/b.txt"));
        S("fnweMulti", Path.GetFileNameWithoutExtension("b.tar.gz"));

        // GetFullPath: lexical collapse of '.'/'..'/'//' against the cwd (no symlink
        // resolution). Absolute inputs need no cwd; relative inputs share the cwd
        // with real .NET in the gate, so they diff exactly.
        S("fullAbs", Path.GetFullPath("/x/y"));
        S("fullAbsDotDot", Path.GetFullPath("/a/b/../../../d"));
        S("fullAbsTrail", Path.GetFullPath("/a/b/"));
        S("fullAbsDouble", Path.GetFullPath("/a//b"));
        S("fullAbsDot", Path.GetFullPath("/a/./b"));
        S("fullRootUp", Path.GetFullPath("/.."));
        // Relative inputs exercise the getcwd branch (cwd-prefixed, then collapsed).
        S("fullRel", Path.GetFullPath("sub/file.txt"));
        S("fullRelDotDot", Path.GetFullPath("a/../b"));
        S("fullRelDot", Path.GetFullPath("."));

        // IsPathRooted.
        S("rootedRel", Path.IsPathRooted("a/b").ToString());
        S("rootedAbs", Path.IsPathRooted("/a/b").ToString());
    }
}
