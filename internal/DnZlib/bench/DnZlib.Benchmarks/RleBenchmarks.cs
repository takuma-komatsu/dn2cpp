using BenchmarkDotNet.Attributes;
using DnZlib;

namespace DnZlib.Benchmarks;

/// <summary>
/// CompressionStrategy.Rle throughput — not exercised by CompressBenchmarks (which always uses
/// CompressionStrategy.Default), so the SIMD-accelerated DeflateRle run scan has no dedicated
/// coverage without this.
/// </summary>
[MemoryDiagnoser]
public class RleBenchmarks
{
    [Params("binary", "text")]
    public string Input = "";

    private byte[] _data = [];

    [GlobalSetup]
    public void Setup() => _data = BenchData.Get(Input);

    [Benchmark(Baseline = true)]
    public byte[] Default() => Zlib.Compress(_data, 6, ZlibFormat.Zlib, CompressionStrategy.Default);

    [Benchmark]
    public byte[] Rle() => Zlib.Compress(_data, 6, ZlibFormat.Zlib, CompressionStrategy.Rle);
}
