using System;
using System.Globalization;
using System.Runtime.Intrinsics;

#if (PROBE_X86 && PROBE_ARM) || (PROBE_X86 && PROBE_WASM) || (PROBE_ARM && PROBE_WASM)
#error Select exactly one platform-ISA probe family.
#endif

#if PROBE_X86
using ProbeIsa = System.Runtime.Intrinsics.X86.Sse2;
#elif PROBE_ARM
using ProbeIsa = System.Runtime.Intrinsics.Arm.AdvSimd;
#elif PROBE_WASM
using ProbeIsa = System.Runtime.Intrinsics.Wasm.PackedSimd;
#else
#error Select exactly one platform-ISA probe family.
#endif

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

#if PROBE_X86
const string axis = "x86-sse2";
#elif PROBE_ARM
const string axis = "arm-advsimd";
#else
const string axis = "wasm-packedsimd";
#endif

Console.WriteLine($"platform-isa-header:{axis}:begin");
Vector128<int> result = ProbeIsa.Add(Vector128.Create(1), Vector128.Create(2));
Console.WriteLine($"platform-isa-header:{axis}:{ProbeIsa.IsSupported}:{result.GetElement(0)}");
Console.WriteLine($"platform-isa-header:{axis}:done");
