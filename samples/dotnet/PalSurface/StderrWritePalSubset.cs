using System;
using System.Diagnostics;

namespace StderrWritePalSubset
{
    // The PAL's raw fd write. A Debug write reaches DebugProvider.WriteCore, whose two
    // sinks are Interop.Sys.SysLog (the debugger arm, once Debugger.IsLogging const-folds
    // false) and Interop.Sys.Write (the stderr arm) -- so this section is what puts
    // SystemNative_Write in the emitted call set beside SystemNative_SysLog. The bucket
    // defines DEBUG for it: Debug.Write is [Conditional("DEBUG")], and without the define
    // the call site is gone before the transpiler sees it.
    //
    // Debug, not Trace: DefaultTraceListener can log to a FILE, so a Trace write drags the
    // whole file-I/O P/Invoke closure in behind it -- and that closure is exactly what the
    // wasm build excludes.
    //
    // Neither sink may reach stdout on either side of the diff, so the section prints a
    // marker: a section whose subject prints nothing cannot prove it ran.
    internal static class Program
    {
        internal static void __GateEntry()
        {
            Console.WriteLine("-- StderrWritePalSubset --");
            Debug.WriteLine("must not reach stdout");
            Debug.Write("must not reach stdout");
            Console.WriteLine("debugWritten=True");
        }
    }
}
