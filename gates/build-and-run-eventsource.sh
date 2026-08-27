#!/usr/bin/env bash
# EventSource framework-provider non-void fold gate.
#
# A native build ships the IL2CPP/NativeAOT tracing posture: no EventPipe/ETW/
# EventListener, so the framework's EventSource tracing providers are folded to
# no-ops. Their IsEnabled() guards fold to false and the Log singleton to null
# (its .cctor is skipped), and a call on a provider member yields the method's
# default — the value .NET returns when no listener is attached. The VOID event
# methods discard their args; the interesting case is a NON-VOID member, whose
# return the fold replaces with its type's default. This gate drives the riskiest
# such shape — a STRUCT return, NameResolutionTelemetry.BeforeResolution's
# NameResolutionActivity, reached through Dns.GetHostName() — to prove the fold
# emits the struct's C++ layout, takes its address for the paired stop call, and
# compiles and runs. The scalar/pointer/bool shapes (EnterScope -> 0L,
# ConnectStart -> null Activity, AnyTelemetryEnabled -> false) travel the same
# fold and are exercised by the Thrive corpus.
#
# Frozen snapshot rather than a `dotnet $app` diff: the program prints only the
# NON-EMPTINESS of the host name (machine-specific), not its value, so the two
# lines are deterministic and platform-neutral, but the section INTENTIONALLY
# says nothing about the raw name.
#
# Dns.GetHostName's hostname primitive is per-OS, and both arms are wired, so
# this gate runs everywhere: POSIX bottoms out in libSystem.Native's
# SystemNative_GetHostName (runtime/core/platform/posix/dn2cpp_system_native.cpp),
# Windows in ws2_32 winsock's gethostname behind Interop.Winsock's WSAStartup
# init. Both modules are in the runtime-provided P/Invoke allowlist
# (Compilation.IsRuntimeProvidedPInvokeModule) — ws2_32 direct-links like
# kernel32/ntdll/ole32, needing no dn2cpp-side shim, but unlike them it is
# outside MSVC's default link set, so runtime/CMakeLists.txt links it explicitly
# on the WIN32 arm. The final two lines additionally parse a named IPv6 scope:
# POSIX reaches SystemNative_InterfaceNameToIndex and if_nametoindex(3), while
# Windows direct-links the already-marshalled iphlpapi.dll conversion calls.
source "$(dirname "$0")/_common.sh"

# System.Net.Primitives carries the SocketError enum that the Windows arm's
# Interop.Winsock.gethostname RETURNS. Without it on the reference list its
# ClassInfo does not resolve, IsEnum reads false, and the P/Invoke marshaller —
# which lowers an enum to its underlying integer — reports the return as simply
# unmarshallable (CppTypes.NativeAbiType). The POSIX arm never names it
# (SystemNative_GetHostName returns a plain int32), so it costs nothing there:
# the reference is tree-shaken away.
corelib_freeze_gate EventSourceProbe "$(dirname "$0")/expected/eventsource.txt" \
    System.Net.NameResolution System.Diagnostics.DiagnosticSource System.Net.Primitives
