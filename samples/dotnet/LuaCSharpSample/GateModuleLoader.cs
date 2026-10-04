using System;
using System.Threading;
using System.Threading.Tasks;
using Lua;

namespace Dn2Cpp;

internal sealed class GateModuleLoader : ILuaModuleLoader
{
    internal int LoadCount { get; private set; }

    public bool Exists(string moduleName) => moduleName == "gate.module";

    public ValueTask<LuaModule> LoadAsync(string moduleName, CancellationToken cancellationToken = default)
    {
        if (!Exists(moduleName))
            throw new InvalidOperationException("Unknown gate module: " + moduleName);
        LoadCount++;
        return new ValueTask<LuaModule>(new LuaModule(moduleName,
            "return { answer = 42, add = function(a, b) return a + b end }"));
    }
}
