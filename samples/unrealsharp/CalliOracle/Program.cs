using System;
using System.Globalization;
using System.Runtime.InteropServices;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
nint library = NativeLibrary.Load(args[0]);
if (!UnrealSharp.Interop.Bind_Utf8Fixture.Probe(NativeLibrary.GetExport(library, "unrealsharp_fixture_utf8"))
            || !UnrealSharp.Core.Interop.Bind_Utf8Fixture.Probe(NativeLibrary.GetExport(library, "unrealsharp_fixture_utf8"))
            || !UnrealSharp.Log.Bind_Utf8Fixture.Probe(NativeLibrary.GetExport(library, "unrealsharp_fixture_utf8")))
    throw new InvalidOperationException("native UTF-8 calli marshalling mismatch");
if (!UnrealSharp.Interop.Bind_FScriptSet.Probe(NativeLibrary.GetExport(library, "unrealsharp_fixture_delegates")))
    throw new InvalidOperationException("native delegate calli marshalling mismatch");
if (!UnrealSharp.Interop.Bind_BoolFixture.Probe(NativeLibrary.GetExport(library, "unrealsharp_fixture_bool")))
    throw new InvalidOperationException("native bool calli marshalling mismatch");
NativeLibrary.Free(library);
Console.WriteLine("unrealsharp-bootstrap utf8 OK");
