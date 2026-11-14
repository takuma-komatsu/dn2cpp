#!/usr/bin/env python3
"""Compile the fork's opaque storage and matrix callback declarations against native probes."""
import os
from pathlib import Path
import re
import subprocess
import sys
import tempfile

fork = Path(sys.argv[1]).resolve()
with tempfile.TemporaryDirectory(prefix='unreal-storage-abi-') as temporary:
    root = Path(temporary)
    storage = (fork / 'Managed/UnrealSharp/UnrealSharp/NativeStructHandle.cs').read_text(encoding='utf-8-sig')
    declaration = re.search(r'\[StructLayout\(LayoutKind.Sequential, Size = 64\)\]\s*public struct NativeStructHandleData\s*\{.*?\}', storage, re.S)
    assert declaration
    (root / 'Storage.cs').write_text('using System.Runtime.InteropServices;\nnamespace UnrealSharp;\n' + declaration[0])
    (root / 'Bind.cs').write_text((fork / 'Managed/UnrealSharp/UnrealSharp/Interop/Bind_FRotator.cs').read_text(encoding='utf-8-sig'))
    native = (fork / 'Source/UnrealSharpCore/Private/Binds/Bind_UScriptStruct.cpp').read_text(encoding='utf-8-sig')
    union = re.search(r'union FNativeStructData\s*\{.*?\};', native, re.S)[0]
    body = (fork / 'Source/UnrealSharpCore/Private/Binds/Bind_FRotator.cpp').read_text(encoding='utf-8-sig')
    body = re.search(r'void FromMatrix\(.*?\n\t\}', body, re.S)[0]
    (root / 'probe.cpp').write_text('''#include <array>
#include <cstddef>
#include <type_traits>
struct FRotator { double Pitch, Yaw, Roll; };
struct FMatrix { double Data[16]; FRotator Rotator() const { return {Data[0], Data[5], Data[10]}; } };
''' + union + '''
struct Embedding { char Prefix; FNativeStructData Data; };
static_assert(sizeof(FNativeStructData) == 64);
static_assert(offsetof(Embedding, Data) == alignof(void*));
extern "C" {
''' + body + '\n}\nstatic_assert(std::is_same_v<decltype(&FromMatrix), void(*)(FRotator*, const FMatrix&)>);\n')
    (root / 'CMakeLists.txt').write_text('cmake_minimum_required(VERSION 3.20)\nproject(StorageABI LANGUAGES CXX)\nadd_library(probe SHARED probe.cpp)\ntarget_compile_features(probe PRIVATE cxx_std_17)\n')
    configure = ['cmake', '-S', str(root), '-B', str(root / 'build'), '-G', 'Ninja']
    if sys.platform == 'darwin':
        for variable, query in [('CMAKE_OSX_SYSROOT', ['--show-sdk-path']), ('CMAKE_CXX_COMPILER', ['--find', 'clang++'])]:
            value = os.environ.get('SDKROOT' if variable == 'CMAKE_OSX_SYSROOT' else variable) or subprocess.check_output(['xcrun', '--sdk', 'macosx', *query], text=True).strip()
            configure.append(f'-D{variable}={value}')
    subprocess.run(configure, check=True)
    subprocess.run(['cmake', '--build', str(root / 'build')], check=True)
    (root / 'Probe.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><AllowUnsafeBlocks>true</AllowUnsafeBlocks></PropertyGroup></Project>')
    (root / 'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    (root / 'Program.cs').write_text('''using System;
using System.Globalization;
using System.Runtime.InteropServices;
using UnrealSharp;
using UnrealSharp.CoreUObject;
using UnrealSharp.Interop;
namespace UnrealSharp.Binds { public sealed class NativeCallbacksAttribute : Attribute {} }
namespace UnrealSharp.CoreUObject {
 public struct FRotator { public double Pitch, Yaw, Roll; }
 public unsafe struct FMatrix { public fixed double Data[16]; }
}
[StructLayout(LayoutKind.Sequential)] struct Embedding { public byte Prefix; public NativeStructHandleData Data; }
static unsafe class Program {
 static int Main(string[] args) {
  CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
  CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
  if (Marshal.SizeOf<NativeStructHandleData>() != 64 || Marshal.OffsetOf<Embedding>(nameof(Embedding.Data)).ToInt64() != IntPtr.Size) return 1;
  nint library = NativeLibrary.Load(args[0]);
  Bind_FRotator.FromMatrix = (delegate* unmanaged<ref FRotator, ref FMatrix, void>)NativeLibrary.GetExport(library, "FromMatrix");
  FMatrix matrix = default; matrix.Data[0] = 13; matrix.Data[5] = 29; matrix.Data[10] = 47;
  FRotator result = default; Bind_FRotator.FromMatrix(ref result, ref matrix);
  if (result.Pitch != 13 || result.Yaw != 29 || result.Roll != 47 || matrix.Data[5] != 29) return 2;
  NativeLibrary.Free(library); Console.WriteLine("UNREALSHARP_STORAGE_MATRIX_ABI_OK"); return 0;
 }
}
''')
    library = root / 'build' / ('libprobe.dylib' if sys.platform == 'darwin' else 'libprobe.so')
    env = dict(os.environ, DOTNET_CLI_HOME='/tmp/dn2cpp-unreal-dotnet')
    subprocess.run(['dotnet', 'run', '--project', str(root / 'Probe.csproj'), '-c', 'Release', '--', str(library)], env=env, check=True)
