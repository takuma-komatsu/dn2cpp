// Drives the REAL System.Reflection.Metadata byte* read core — PEReader,
// MetadataReader, BlobReader, no shim — over the assemblies named by args, and
// prints only stable sorted facts so native and real .NET diff exactly.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Globalization;

internal static class Program
{
    private static int Main(string[] args)
    {
        // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        string path = args.Length > 0 ? args[0] : "self";
        byte[] bytes = File.ReadAllBytes(path);

        // --- Path A: File.ReadAllBytes -> ImmutableArray<byte> -> PEReader(ia) ---
        ImmutableArray<byte> image = bytes.ToImmutableArray();
        using (var pe = new PEReader(image))
        {
            MetadataReader mr = pe.GetMetadataReader();
            Report("A(immutablearray)", mr);
        }

        // --- Path B: MemoryMappedFile -> AcquirePointer (raw byte*) -> PEReader(byte*, size) ---
        int len = bytes.Length;
        MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(path, FileMode.Open);
        MemoryMappedViewAccessor view = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
        SafeMemoryMappedViewHandle handle = view.SafeMemoryMappedViewHandle;
        unsafe
        {
            byte* p = null;
            handle.AcquirePointer(ref p);
            using (var pe = new PEReader(p, len))
            {
                MetadataReader mr = pe.GetMetadataReader();
                Report("B(mmf-byte*)", mr);
            }
            handle.ReleasePointer();
        }
        view.Dispose();
        mmf.Dispose();

        // A small metadata block uses SRM's stream read path.
        using (FileStream stream = File.OpenRead(path))
        {
            using (var pe = new PEReader(stream))
            {
                Report("C(filestream)", pe.GetMetadataReader());
            }
            Console.WriteLine($"  streamClosed={!stream.CanRead}");
        }

        // SRM maps FileStream metadata requests above its memory-map threshold.
        using (FileStream stream = File.OpenRead(args[1]))
        {
            using (var pe = new PEReader(stream))
            {
                if (pe.PEHeaders.MetadataSize <= 16384)
                {
                    throw new InvalidOperationException("The stream fixture must reach SRM's memory-map path.");
                }
                MetadataReader mr = pe.GetMetadataReader();
                Console.WriteLine($"D(mapped-filestream) typeDefs={mr.TypeDefinitions.Count} methodDefs={mr.MethodDefinitions.Count} mdVersion={mr.MetadataVersion}");
            }
            Console.WriteLine($"  streamClosed={!stream.CanRead}");
        }
        Console.WriteLine("file stream readers complete");

        return 0;
    }

    private static void Report(string tag, MetadataReader mr)
    {
        // Type definitions: full names, sorted.
        List<string> typeNames = new List<string>();
        foreach (TypeDefinitionHandle th in mr.TypeDefinitions)
        {
            TypeDefinition td = mr.GetTypeDefinition(th);
            string ns = mr.GetString(td.Namespace);
            string nm = mr.GetString(td.Name);
            typeNames.Add(ns.Length == 0 ? nm : string.Concat(ns, ".", nm));
        }
        typeNames.Sort(StringComparer.Ordinal);

        // Method definitions, plus the param count decoded from each signature blob
        // — which is what drives BlobReader and its compressed integers.
        List<string> methodNames = new List<string>();
        int totalParams = 0;
        int methodCount = 0;
        foreach (MethodDefinitionHandle mh in mr.MethodDefinitions)
        {
            MethodDefinition md = mr.GetMethodDefinition(mh);
            methodNames.Add(mr.GetString(md.Name));
            methodCount++;
            BlobReader sig = mr.GetBlobReader(md.Signature);
            byte header = sig.ReadByte();
            int paramCount = sig.ReadCompressedInteger();
            totalParams += paramCount;
            // Keeps the header read from being elided.
            if (header == 0xFF)
            {
                totalParams += 1000;
            }
        }
        methodNames.Sort(StringComparer.Ordinal);

        // A user string constant read straight out of the blob heap.
        string tagConst = ReadStringConstant(mr, "Tag");

        // Parsed straight out of the byte* metadata root.
        string mdVersion = mr.MetadataVersion;

        int distinct = DistinctCount(methodNames);
        Console.WriteLine($"{tag} typeDefs={typeNames.Count} mdVersion={mdVersion}");
        Console.WriteLine($"  types=[{string.Join(",", typeNames)}]");
        Console.WriteLine($"  methodDefs={methodCount} distinctMethodNames={distinct} totalSigParams={totalParams}");
        Console.WriteLine($"  methods=[{string.Join(",", methodNames)}]");
        Console.WriteLine($"  tagConst={tagConst}");
    }

    private static int DistinctCount(List<string> sortedNames)
    {
        int n = 0;
        string prev = null;
        foreach (string s in sortedNames)
        {
            if (prev is null || !string.Equals(prev, s, StringComparison.Ordinal))
            {
                n++;
            }
            prev = s;
        }
        return n;
    }

    private static string ReadStringConstant(MetadataReader mr, string fieldName)
    {
        foreach (FieldDefinitionHandle fh in mr.FieldDefinitions)
        {
            FieldDefinition fd = mr.GetFieldDefinition(fh);
            if (!string.Equals(mr.GetString(fd.Name), fieldName, StringComparison.Ordinal))
            {
                continue;
            }
            ConstantHandle ch = fd.GetDefaultValue();
            if (ch.IsNil)
            {
                continue;
            }
            Constant c = mr.GetConstant(ch);
            BlobReader br = mr.GetBlobReader(c.Value);
            // ReadUTF16's argument is a BYTE count, so the whole blob length is right.
            return br.ReadUTF16(br.Length);
        }
        return "<none>";
    }
}
