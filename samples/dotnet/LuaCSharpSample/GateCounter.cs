using Lua;

namespace Dn2Cpp;

[LuaObject]
public partial class GateCounter
{
    [LuaMember("value")]
    public int Value { get; set; }

    [LuaMember("create")]
    public static GateCounter Create(int value) => new GateCounter { Value = value };

    [LuaMember("add")]
    public int Add(int delta)
    {
        Value += delta;
        return Value;
    }

    [LuaMetamethod(LuaObjectMetamethod.Add)]
    public static GateCounter Combine(GateCounter a, GateCounter b) =>
        new GateCounter { Value = a.Value + b.Value };
}
