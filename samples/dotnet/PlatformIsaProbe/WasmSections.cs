using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Wasm;

namespace PlatformIsaProbe;

// The one Wasm family; see X86Sections for the probe rule.
internal static class WasmSections
{
    // The PackedSimd exercise lands with its lowering; see X86Sections.
    internal static void RegisterExercises(Dictionary<string, Action> exercises)
    {
    }

    internal static void ProbePackedSimd() { _ = PackedSimd.Add(Vector128<int>.Zero, Vector128<int>.Zero); }
}
