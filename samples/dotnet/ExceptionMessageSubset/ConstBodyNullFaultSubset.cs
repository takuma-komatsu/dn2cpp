#nullable disable
using System;
using System.Runtime.CompilerServices;

namespace ExceptionMessageSubset
{
    // A callvirt whose non-virtual callee body is a bare constant (`ldc; ret`). The
    // transpiler folds such a call to its literal, and the callee never dereferences
    // its receiver, so the fold site is the only place left to raise the
    // NullReferenceException .NET raises at the call. Receivers arrive through a
    // NoInlining boundary for the reason EmittedNullFaultSubset states. Type name only.
    internal static class ConstBodyNullFaultSubset
    {
        private sealed class Config
        {
            internal int Version => 3;

            internal long Limit() => 1L << 40;
        }

        [MethodImpl(MethodImplOptions.NoInlining)] private static Config NullConfig() => null;
        [MethodImpl(MethodImplOptions.NoInlining)] private static Config RealConfig() => new Config();

        private static void Catches(string what, Func<object> body)
        {
            try
            {
                Console.WriteLine(what + " -> " + body());
            }
            catch (Exception e)
            {
                Console.WriteLine(what + " -> " + e.GetType().Name);
            }
        }

        internal static void Run()
        {
            Console.WriteLine("-- const-body null receiver --");
            Catches("const getter, real receiver", () => RealConfig().Version);
            Catches("const getter, null receiver", () => NullConfig().Version);
            Catches("const method, real receiver", () => RealConfig().Limit());
            Catches("const method, null receiver", () => NullConfig().Limit());
        }
    }
}
