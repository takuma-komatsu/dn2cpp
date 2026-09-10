using System.ComponentModel;
using System.Diagnostics;

namespace Dn2Cpp;

/// <summary>Runs a companion tool synchronously with inherited standard streams.</summary>
internal static class ToolProcess
{
    public static int Run(string executable, string[] arguments)
    {
        ArgumentNullException.ThrowIfNull(executable);
        ArgumentNullException.ThrowIfNull(arguments);
        if (executable.Length == 0 || executable.Contains('\0'))
            throw new ArgumentException("Executable must be a nonempty path without NUL.", nameof(executable));
        var start = new ProcessStartInfo(executable) { UseShellExecute = false };
        foreach (string argument in arguments)
        {
            ArgumentNullException.ThrowIfNull(argument);
            if (argument.Contains('\0'))
                throw new ArgumentException("Tool arguments cannot contain NUL.", nameof(arguments));
            start.ArgumentList.Add(argument);
        }
        Process child;
        try
        {
            child = Process.Start(start)
                ?? throw new InvalidOperationException($"Could not start companion tool '{executable}'.");
        }
        catch (Win32Exception ex)
        {
            throw new InvalidOperationException($"Could not start companion tool '{executable}': {ex.Message}", ex);
        }
        using (child)
        {
            child.WaitForExit();
            return child.ExitCode;
        }
    }
}
