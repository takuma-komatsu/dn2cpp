// This is a backend contract fixture, not the UnrealSharp implementation.
using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnrealSharp.Plugins;

public struct PluginsCallbacks
{
    public nint LoadPlugin;
    public nint UnloadPlugin;
}

public static unsafe class Dn2CppBootstrap
{
    private static delegate* unmanaged<int, int> _callback;
    private static int _registered;
    private static bool _preparedPlugins, _preparedCore, _preparedGame;

    public static void Initialize(nint workingDirectory, nint pluginCallbacks, nint bindsCallbacks, nint managedCallbacks)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        if (sizeof(PluginsCallbacks) != 2 * sizeof(nint) || sizeof(UnrealSharp.Core.ManagedCallbacks) != 9 * sizeof(nint))
            throw new InvalidOperationException("fixture callback layout mismatch");
        if (!UnrealSharp.Interop.Bind_Utf8Fixture.Probe(((PluginsCallbacks*)pluginCallbacks)->UnloadPlugin)
            || !UnrealSharp.Core.Interop.Bind_Utf8Fixture.Probe(((PluginsCallbacks*)pluginCallbacks)->UnloadPlugin)
            || !UnrealSharp.Log.Bind_Utf8Fixture.Probe(((PluginsCallbacks*)pluginCallbacks)->UnloadPlugin))
            throw new InvalidOperationException("native UTF-8 calli marshalling mismatch");
        if (!UnrealSharp.Interop.Bind_FScriptSet.Probe(((nint*)managedCallbacks)[8])
            || !UnrealSharp.Interop.Bind_FScriptSet.ProbeCapacity()
            || !UnrealSharp.Interop.Bind_BoolFixture.Probe(((nint*)managedCallbacks)[7]))
            throw new InvalidOperationException("native callback calli marshalling mismatch");
        ((PluginsCallbacks*)pluginCallbacks)->LoadPlugin = (nint)(delegate* unmanaged<void>)&ThrowCallback;
        ((UnrealSharp.Core.ManagedCallbacks*)managedCallbacks)->InvokeManagedMethod =
            (nint)(delegate* unmanaged<nint, nint, nint, nint, nint, int>)&UnrealSharp.Core.UnmanagedCallbacks.InvokeManagedMethod;
        _callback = (delegate* unmanaged<int, int>)bindsCallbacks;
        if (_callback(10) != 0)
            throw new InvalidOperationException("fixture bootstrap failure");
    }

    [UnmanagedCallersOnly]
    private static void ThrowCallback()
    {
        throw new InvalidOperationException("fixture reverse callback failure");
    }

    public static void PrepareAssembly(Assembly assembly)
    {
        string name = assembly.GetName().Name!;
        if (name == "UnrealSharp.Plugins") _preparedPlugins = true;
        else if (name == "UnrealSharp.Core") _preparedCore = true;
        else if (name == "RegistrationFixture") _preparedGame = true;
        else throw new InvalidOperationException("unexpected prepared assembly");
    }

    public static void RegisterAssembly(Assembly assembly)
    {
        if (!_preparedPlugins || !_preparedCore || !_preparedGame)
            throw new InvalidOperationException("all assembly records must precede registration");
        string name = assembly.GetName().Name!;
        int marker = name == "UnrealSharp.Plugins" ? 30 : name == "RegistrationFixture" ? 40 : -1;
        if (marker < 0)
            throw new InvalidOperationException("unexpected fixture assembly");
        _callback(marker);
        _registered++;
    }

    public static void NotifyInitializer()
    {
        if (_callback == null)
            throw new InvalidOperationException("initializer preceded callback setup");
        if (_callback(20) != 0)
            throw new InvalidOperationException("fixture initializer failure");
    }

    public static void CompleteAssembly(Assembly assembly)
    {
        _callback(assembly.GetName().Name == "UnrealSharp.Plugins" ? 31 : 41);
    }

    public static void Tick(float deltaTime)
    {
        if (_registered != 2 || deltaTime != 0.25f)
            throw new InvalidOperationException("fixture tick state mismatch");
        int status = _callback(50);
        if (status == 2)
            UnrealSharp.Interop.Bind_FScriptSet.ThrowCallback();
        else if (status != 0)
            throw new InvalidOperationException("fixture tick failure");
    }

    public static void ReleaseHandles()
    {
        _callback(70);
    }

    public static void Shutdown()
    {
        if (_callback(60) != 0)
            throw new InvalidOperationException("fixture shutdown failure");
    }
}
