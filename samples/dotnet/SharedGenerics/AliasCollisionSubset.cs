#nullable enable
// Alias-row collision policy: a class implementing BOTH IDescribe<string> and
// IDescribe<object> collapses both onto the canonical IDescribe<CnRef> row, so a
// shared body dispatching through the canonical handle would be ambiguous. Every
// such body is therefore tainted to per-instantiation, where the dispatch uses
// the REAL interface handles. Real System.Private.CoreLib (-r), run vs .NET.
using System;
namespace AliasCollisionSubset;

interface IDescribe<T>
{
    string Describe(T value);
}

class Dual : IDescribe<string>, IDescribe<object>
{
    public string Describe(string value) => "str:" + value;
    public string Describe(object value) => "obj:" + value;
}

class Single : IDescribe<string>
{
    public string Describe(string value) => "single:" + value;
}

// The collision consumer: a generic class whose body dispatches through
// IDescribe<T>.
class Caller<T>
{
    public string Call(IDescribe<T> d, T value) => d.Describe(value);
}

class Program
{
    internal static void __GateEntry()
    {
        var dual = new Dual();
        Console.WriteLine("dual str=" + new Caller<string>().Call(dual, "a"));
        Console.WriteLine("dual obj=" + new Caller<object>().Call(dual, "b"));
        Console.WriteLine("dual obj int=" + new Caller<object>().Call(dual, 7));
        Console.WriteLine("single str=" + new Caller<string>().Call(new Single(), "c"));
        // Direct (non-generic) dispatch keeps the real rows exact too.
        IDescribe<string> ds = dual;
        IDescribe<object> dobj = dual;
        Console.WriteLine("direct str=" + ds.Describe("d"));
        Console.WriteLine("direct obj=" + dobj.Describe("e"));
    }
}
