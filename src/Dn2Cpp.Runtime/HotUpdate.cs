using System;

namespace Dn2Cpp.Runtime;

/// <summary>The hot-update entry surface. <see cref="Run"/> loads a Baked Patch
/// Image (a managed patch assembly pre-converted by <c>dn2cpp --emit-patch</c>;
/// see docs/BPI-FORMAT.md) and executes its entry method in the runtime's IL
/// interpreter; <see cref="Load"/> does the same with an optional entry method
/// (a registration-only image is a valid deployment unit); and
/// <see cref="LoadDirectory"/> loads every regular <c>*.bpi</c> file whose header
/// pre-validates (magic, format version, base ABI hash — stale patches are
/// skipped with a stderr diagnostic, never blocking startup) in ascending
/// (patchVersion, filename) order, so the highest version registers last and wins
/// name lookups; a missing directory loads nothing.
///
/// The bodies below never run: the transpiler maps this type to runtime
/// intrinsics (<c>dn2cpp_hotupdate_run/load/load_dir</c>), and real .NET has no
/// interpreter to run a BPI against.</summary>
public static class HotUpdate
{
    public static void Run(string bpiPath) =>
        throw new NotSupportedException("Dn2Cpp.Runtime.HotUpdate.Run is a dn2cpp intrinsic (no managed implementation)");

    public static void Load(string bpiPath) =>
        throw new NotSupportedException("Dn2Cpp.Runtime.HotUpdate.Load is a dn2cpp intrinsic (no managed implementation)");

    /// <summary>Returns the number of BPIs actually loaded.</summary>
    public static int LoadDirectory(string dirPath) =>
        throw new NotSupportedException("Dn2Cpp.Runtime.HotUpdate.LoadDirectory is a dn2cpp intrinsic (no managed implementation)");
}
