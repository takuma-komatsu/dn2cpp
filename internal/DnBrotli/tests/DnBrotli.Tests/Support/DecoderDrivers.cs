using System.Buffers;

namespace DnBrotli.Tests.Support;

/// <summary>One <c>Decompress</c> call's observable result — status plus the (consumed, written)
/// pair. Sequences of these are compared call-for-call between DnBrotli and the BCL.</summary>
internal readonly record struct DecodeStep(OperationStatus Status, int Consumed, int Written);

/// <summary>
/// Chunk-schedule drivers for the idiomatic decoder surface. <see cref="DriveDn"/> and
/// <see cref="DriveBcl"/> run the exact same feeding protocol over DnBrotli's
/// <see cref="BrotliDecoder"/> and the BCL's <c>System.IO.Compression.BrotliDecoder</c>
/// (real native brotli), so their step lists are directly comparable. The shared protocol
/// enforces the hardening invariants: bounded iterations with a forward-progress check
/// (never hangs) and canary bytes around the destination window (never writes past the span).
/// </summary>
internal static class DecoderDrivers
{
    private const int CanaryPad = 32;
    private const byte CanaryByte = 0xC5;

    internal delegate OperationStatus StepFn(
        ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten);

    /// <summary>Wraps the struct decoder in a class so a single instance (not per-call copies)
    /// is mutated across delegate invocations, and is reliably disposed.</summary>
    private sealed class DnStepper : IDisposable
    {
        private BrotliDecoder _decoder;

        public OperationStatus Step(
            ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten) =>
            _decoder.Decompress(source, destination, out bytesConsumed, out bytesWritten);

        public void Dispose() => _decoder.Dispose();
    }

    private sealed class BclStepper : IDisposable
    {
        private System.IO.Compression.BrotliDecoder _decoder;

        public OperationStatus Step(
            ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten) =>
            _decoder.Decompress(source, destination, out bytesConsumed, out bytesWritten);

        public void Dispose() => _decoder.Dispose();
    }

    internal static (List<DecodeStep> Steps, byte[] Output) DriveDn(
        byte[] input, int inChunk, int outChunk, long maxOutput = 64L << 20)
    {
        using var stepper = new DnStepper();
        return Drive(stepper.Step, input, inChunk, outChunk, maxOutput);
    }

    internal static (List<DecodeStep> Steps, byte[] Output) DriveBcl(
        byte[] input, int inChunk, int outChunk, long maxOutput = 64L << 20)
    {
        using var stepper = new BclStepper();
        return Drive(stepper.Step, input, inChunk, outChunk, maxOutput);
    }

    private static (List<DecodeStep> Steps, byte[] Output) Drive(
        StepFn step, byte[] input, int inChunk, int outChunk, long maxOutput)
    {
        var steps = new List<DecodeStep>();
        var output = new MemoryStream();
        byte[] outer = new byte[outChunk + 2 * CanaryPad];
        outer.AsSpan().Fill(CanaryByte);

        int offset = 0;
        int remaining = 0;  // unconsumed bytes of the current window: input[offset .. offset+remaining)
        int zeroProgress = 0;
        while (true)
        {
            if (remaining == 0 && offset < input.Length)
            {
                remaining = Math.Min(inChunk, input.Length - offset);
            }
            OperationStatus status = step(
                input.AsSpan(offset, remaining), outer.AsSpan(CanaryPad, outChunk),
                out int consumed, out int written);
            steps.Add(new DecodeStep(status, consumed, written));

            Assert.InRange(consumed, 0, remaining);
            Assert.InRange(written, 0, outChunk);
            AssertCanariesIntact(outer, outChunk);

            output.Write(outer, CanaryPad, written);
            Assert.True(output.Length <= maxOutput, "decoder produced excessive output");
            offset += consumed;
            remaining -= consumed;

            if (status is OperationStatus.Done or OperationStatus.InvalidData)
            {
                break;
            }
            if (status == OperationStatus.NeedMoreData && remaining == 0 && offset >= input.Length)
            {
                break;  // truncated input: nothing more to feed
            }
            Assert.True(status is OperationStatus.NeedMoreData or OperationStatus.DestinationTooSmall,
                $"unexpected OperationStatus {status}");
            zeroProgress = (consumed == 0 && written == 0) ? zeroProgress + 1 : 0;
            Assert.True(zeroProgress <= 4, "decoder made no forward progress (hang)");
        }
        return (steps, output.ToArray());
    }

    private static void AssertCanariesIntact(byte[] outer, int outChunk)
    {
        for (int i = 0; i < CanaryPad; i++)
        {
            Assert.True(outer[i] == CanaryByte, $"canary before destination overwritten at {i}");
            Assert.True(outer[CanaryPad + outChunk + i] == CanaryByte,
                $"canary after destination overwritten at {i}");
        }
    }
}
