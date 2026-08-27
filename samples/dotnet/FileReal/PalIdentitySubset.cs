#nullable enable
using System;

// The platform identity PAL beyond file I/O. Nothing on this path is intercepted:
// Environment.UserName runs verbatim real CoreLib IL over either libSystem.Native's
// GetEUid/GetPwUidR or secur32's GetUserNameExW. The Unix symbols lower to dn2cpp's
// shim in runtime/core/platform/posix/dn2cpp_system_native.cpp; the Windows symbol
// direct-links to the OS import library.
//
// A regression here is a C++ LINK error rather than wrong output, so a section that merely
// REACHES these symbols is already an assertion.
//
// Nothing prints a pid or a path: a pid differs per process, and this bucket's driver
// forbids absolute paths outright (the two sides run in separate scratch directories). The
// user NAME is printed because both sides run as the same OS user, so the string is
// exact-diffable — and it is the only assertion available on the 48-byte Passwd layout,
// where a wrong offset makes `Name` a pointer taken from the middle of another field. The
// booleans under it name the failure; they do not cause it.
//
// Environment.GetFolderPath is the other GetPwUidR caller and has its own section
// (FolderPathSubset). The last Process block is a different native module again: macOS
// Interop.libproc.
namespace PalIdentitySubset;

static class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine("-- Environment.ProcessId (SystemNative_GetPid) --");
        int pid = Environment.ProcessId;
        Console.WriteLine("positive: " + (pid > 0));
        Console.WriteLine("stable: " + (pid == Environment.ProcessId));

        Console.WriteLine("-- Environment.UserName (platform identity PAL) --");
        string user = Environment.UserName;
        Console.WriteLine("UserName: " + user);
        Console.WriteLine("non-empty: " + (user.Length > 0));
        Console.WriteLine("stable: " + (user == Environment.UserName));
        // A wrong Passwd offset usually lands mid-field rather than on nothing, and
        // the field two slots along IS a path (HomeDirectory) — so check the shape
        // as well as the bytes: a user name carries no control character and no
        // separator. This is what would name the failure if the diff went quiet
        // because both sides somehow agreed on empty.
        bool clean = user.Length > 0;
        foreach (char c in user)
        {
            if (char.IsControl(c) || c == '/' || c == '\\')
            {
                clean = false;
            }
        }

        Console.WriteLine("printable, no separator: " + clean);

#if DN2CPP_HOST_WINDOWS
        Console.WriteLine("-- System.Diagnostics.Process: the Unix PAL entries, not exercised here --");
#else
        // ---- System.Diagnostics.Process: SystemNative_Kill and SystemNative_GetSid ----
        // The two PAL entries no CoreLib-only closure can name (they are declared in
        // System.Diagnostics.Process.dll). This is a real round-trip, not a symbol probe:
        // every line below is diffed against real .NET and the transpile buys none of it
        // with --cut.
        //
        // Which property reaches which:
        //   HasExited  -> ProcessWaitState.GetExited -> Interop.Sys.Kill(pid,
        //                 Signals.None), the "is this pid still alive" probe. The
        //                 signal translation in the PAL matters here for the
        //                 reason written at SystemNative_Kill: PAL_NONE is 0 and
        //                 must stay 0, and a table that passed the managed value
        //                 through would be indistinguishable on this call alone.
        //   SessionId  -> ProcessManager.CreateProcessInfo -> Interop.Sys.GetSid.
        //                 macOS only: on Linux the same property parses
        //                 /proc/self/stat and never enters the PAL, which is why
        //                 the gate script's NAMED assert for GetSid is Darwin-only
        //                 while the value assert below is not.
        //
        // Disposal is explicit and BEFORE the last WriteLine, deliberately. Process
        // derives from Component, whose finalizer runs Dispose -> Close ->
        // StopWatchingForExit, and that path is the whole reason this section could
        // not exist: leaving it to the finalizer would put its output (and its
        // failure) at an indeterminate point of the transcript, or after it.
        Console.WriteLine("-- Process.GetCurrentProcess (SystemNative_Kill + GetSid) --");
        int sessionId;
        using (System.Diagnostics.Process self = System.Diagnostics.Process.GetCurrentProcess())
        {
            // A pid is per-process, so print the AGREEMENT with Environment.ProcessId
            // rather than the number: two independent routes to the same value
            // (Interop.Sys.GetPid vs. Process's own), so a wrong one is not
            // self-consistent.
            Console.WriteLine("Id == Environment.ProcessId: " + (self.Id == pid));
            // False, and it has to be a real answer: Kill(pid, None) returning
            // failure for a live process would print True here.
            Console.WriteLine("HasExited: " + self.HasExited);
            sessionId = self.SessionId;
        }

        // The raw session id IS exact-diffable, unlike the pid. getsid(2) answers
        // the session leader's pid, and the native binary and the real-.NET oracle
        // are launched by the same gate script — same shell, hence the same
        // session, hence the same number. That is the same class of reasoning as
        // UserName above ("both sides run as the same OS user"), and it is what
        // makes this an assertion rather than a shape check: a GetSid that
        // degraded to 0 or to a stub constant would still satisfy any "> 0" test
        // an oracle-free version could make.
        Console.WriteLine("SessionId: " + sessionId);
        Console.WriteLine("SessionId positive: " + (sessionId > 0));

        // ---- Process.ProcessName / StartTime / MainModule: the libproc half ----
        // On macOS these three come from ProcessManager.OSX.CreateProcessInfo over
        // Interop.libproc's proc_pidinfo/proc_pidpath, which lower to direct native calls
        // because /usr/lib/libproc.dylib is in Compilation.IsRuntimeProvidedPInvokeModule.
        // On Linux the same properties parse /proc and enter no native module, so this block
        // is deliberately not Darwin-only: it is a regression test for the property, not for
        // the module.
        //
        // WHY NOT PRINT THE NAME. It is not exact-diffable: the native build IS the process
        // ("FileReal") while the oracle is launched as `dotnet FileReal.dll` and is called
        // "dotnet". So the assertion is the AGREEMENT between two independent routes to the
        // same fact — the OS's idea of this process's name and the basename of
        // Environment.ProcessPath — True on both hosts and False for the "" this section
        // exists to keep out, the same shape as `Id == Environment.ProcessId` above.
        //
        // Caveat: both platforms take the name from a fixed-width comm field (Darwin's
        // proc_bsdinfo.pbi_comm, Linux's /proc/pid/stat) truncated at 15 characters. Rename
        // this sample to something longer and the equality below will fail, rightly.
        Console.WriteLine("-- Process.ProcessName / StartTime / MainModule (libproc on macOS) --");
        using (System.Diagnostics.Process self = System.Diagnostics.Process.GetCurrentProcess())
        {
            string name = self.ProcessName;
            Console.WriteLine("ProcessName non-empty: " + (name.Length > 0));
            Console.WriteLine("ProcessName == executable basename: "
                + (name == System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath)));
            // StartTime is a real timestamp on both sides but never the same one, so assert
            // the interval instead. Reading it AT ALL is half the point: a bounded libproc
            // makes CoreLib raise Win32Exception here.
            DateTime started = self.StartTime;
            Console.WriteLine("StartTime in the past: " + (started <= DateTime.Now));
            Console.WriteLine("StartTime plausible: " + (started.Year >= 2020));
            // MainModule.ModuleName is the same datum ProcessName is, arrived at through
            // proc_pidpath rather than proc_pidinfo, so the agreement below is also the
            // check that the second thunk lowered.
            System.Diagnostics.ProcessModule? main = self.MainModule;
            Console.WriteLine("MainModule present: " + (main is not null));
            Console.WriteLine("MainModule.ModuleName == ProcessName: "
                + (main is not null && main.ModuleName == name));
        }
#endif
    }
}
