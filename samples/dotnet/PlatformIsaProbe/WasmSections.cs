using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Wasm;

namespace PlatformIsaProbe;

// The one Wasm family; see X86Sections for the probe rule.
internal static class WasmSections
{
    // The PackedSimd exercise is generated (Exercises.g.cs) once the family is Lowered.
    internal static void RegisterExercises(Dictionary<string, Action> exercises)
    {
#if PLATFORM_ISA_SHARD_ALL || PLATFORM_ISA_SHARD_WASM
        Exercises.RegisterWasm(exercises);
#endif
    }

    internal static void ProbePackedSimd() { _ = PackedSimd.Add(Vector128<int>.Zero, Vector128<int>.Zero); }
}
