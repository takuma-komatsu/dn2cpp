using System.Diagnostics;

static class CutRequestCapture
{
    public static bool Run(string[] args)
    {
        string? python = Environment.GetEnvironmentVariable("CUT_CAPTURE_PYTHON");
        if (python is null)
            return false;
        if (args.Length != 4 || args[0] != "--request" || args[2] != "--result")
            throw new InvalidOperationException("cut capture requires request and result arguments");
        if (!Path.IsPathFullyQualified(python) || !File.Exists(python))
            throw new InvalidOperationException("cut capture requires an absolute Python executable path");
        string script = Path.Combine(AppContext.BaseDirectory, "cut_request_capture.py");
        if (!File.Exists(script))
            throw new InvalidOperationException("cut capture recorder is missing");
        var start = new ProcessStartInfo(python) { UseShellExecute = false };
        start.ArgumentList.Add(script);
        foreach (string argument in args)
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start)
            ?? throw new InvalidOperationException("cut capture recorder did not start");
        process.WaitForExit();
        Environment.ExitCode = process.ExitCode;
        return true;
    }
}
