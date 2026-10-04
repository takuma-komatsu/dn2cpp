#pragma warning disable CA2022 // These probes exercise short reads and invalid arguments.
using System;
using System.IO;
using System.Text;
using System.Threading;

namespace ConsoleStandardStreamSubset;

internal static class Program
{
    internal static void Run()
    {
        Console.WriteLine("== standard console streams ==");
        using Stream input = Console.OpenStandardInput();
        using Stream output = Console.OpenStandardOutput();
        using Stream error = Console.OpenStandardError();
        using Stream bufferedInput = Console.OpenStandardInput(0);
        using Stream bufferedOutput = Console.OpenStandardOutput(1);
        using Stream bufferedError = Console.OpenStandardError(4096);
        Func<Stream> open = Console.OpenStandardOutput;
        using Stream delegateOutput = open();
        foreach (Stream stream in new[] { input, output, error, bufferedInput, bufferedOutput, bufferedError })
        {
            Console.WriteLine("capabilities: " + stream.CanRead + ":" + stream.CanWrite + ":" + stream.CanSeek);
            Fault("length", () => { _ = stream.Length; });
            Fault("position get", () => { _ = stream.Position; });
            Fault("position set", () => stream.Position = 0);
            Fault("seek", () => stream.Seek(0, SeekOrigin.Begin));
            Fault("set length", () => stream.SetLength(0));
        }
        Fault("input buffer size", () => Console.OpenStandardInput(-1));
        Fault("output buffer size", () => Console.OpenStandardOutput(-1));
        Fault("error buffer size", () => Console.OpenStandardError(-1));
        Fault("read null", () => input.Read(null, 0, 0));
        Fault("read offset", () => input.Read(new byte[1], -1, 0));
        Fault("read count", () => input.Read(new byte[1], 0, -1));
        Fault("read range", () => input.Read(new byte[1], 1, 1));
        Fault("read direction", () => output.Read(Array.Empty<byte>(), 0, 0));
        Fault("read span direction", () => output.Read(Span<byte>.Empty));
        Fault("write null", () => output.Write(null, 0, 0));
        Fault("write offset", () => output.Write(new byte[1], -1, 0));
        Fault("write count", () => output.Write(new byte[1], 0, -1));
        Fault("write range", () => output.Write(new byte[1], 1, 1));
        Fault("write direction", () => input.Write(Array.Empty<byte>(), 0, 0));
        Fault("write span direction", () => input.Write(new byte[1].AsSpan()));
        byte[] bytes = Encoding.UTF8.GetBytes("raw stdout: 日本語\0\n");
        output.Write(bytes, 0, bytes.Length);
        bufferedOutput.Write(Encoding.UTF8.GetBytes("span stdout\n").AsSpan());
        output.WriteByte((byte)'!');
        output.WriteByte(10);
        bytes = Encoding.UTF8.GetBytes("async array stdout\n");
        output.WriteAsync(bytes, 0, bytes.Length).GetAwaiter().GetResult();
        bufferedOutput.WriteAsync(Encoding.UTF8.GetBytes("async memory stdout\n").AsMemory()).GetAwaiter().GetResult();
        error.Write(Encoding.UTF8.GetBytes("raw stderr\n").AsSpan());
        bufferedError.WriteAsync(Encoding.UTF8.GetBytes("async stderr\n").AsMemory()).GetAwaiter().GetResult();
        Fault("canceled write", () => output.WriteAsync(new byte[1].AsMemory(), new CancellationToken(true)).GetAwaiter().GetResult());
        Fault("canceled read", () => input.ReadAsync(new byte[1].AsMemory(), new CancellationToken(true)).GetAwaiter().GetResult());
        output.Flush();
        output.FlushAsync().GetAwaiter().GetResult();
        output.Dispose();
        Console.WriteLine("disposed capabilities: " + output.CanRead + ":" + output.CanWrite + ":" + output.CanSeek);
        Fault("disposed array write", () => output.Write(Array.Empty<byte>(), 0, 0));
        Fault("disposed span write", () => output.Write(new byte[1].AsSpan()));
        output.Write(ReadOnlySpan<byte>.Empty);
        Fault("disposed flush", () => output.Flush());
        input.Dispose();
        Fault("disposed array read", () => input.Read(Array.Empty<byte>(), 0, 0));
        Fault("disposed span read", () => input.Read(Span<byte>.Empty));
        bufferedOutput.Write(Encoding.UTF8.GetBytes("second wrapper survives\n").AsSpan());
        Console.WriteLine("standard console streams end");
    }

    internal static void ReadInput()
    {
        using Stream first = Console.OpenStandardInput();
        using Stream second = Console.OpenStandardInput(1);
        Console.WriteLine("empty read: " + first.Read(Span<byte>.Empty));
        Console.WriteLine("byte: " + first.ReadByte());
        first.Dispose();
        byte[] buffer = new byte[2];
        Show("array", second.Read(buffer, 0, 2), buffer);
        Show("span", second.Read(buffer.AsSpan()), buffer);
        Show("async array", second.ReadAsync(buffer, 0, 2).GetAwaiter().GetResult(), buffer);
        Show("async memory", second.ReadAsync(buffer.AsMemory()).GetAwaiter().GetResult(), buffer);
        Console.WriteLine("eof: " + second.ReadByte());
        Console.WriteLine("standard input end");
    }

    internal static void WritePipe()
    {
        using Stream output = Console.OpenStandardOutput();
        byte[] bytes = new byte[131072];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = (byte)(i % 251);
        output.Write(bytes.AsSpan());
        Console.Error.WriteLine("standard pipe end");
    }

    private static void Show(string label, int count, byte[] buffer)
    {
        Console.WriteLine(label + ": " + count + ":" + buffer[0] + ":" + buffer[1]);
    }

    private static void Fault(string label, Action action)
    {
        try
        {
            action();
            Console.WriteLine(label + ": success");
        }
        catch (Exception exception)
        {
            Console.WriteLine(label + ": " + exception.GetType().Name + ":"
                + (exception is ArgumentException argument ? argument.ParamName : "")
                + (exception is IOException || exception is UnauthorizedAccessException ? ":" + exception.HResult : ""));
        }
    }
}
