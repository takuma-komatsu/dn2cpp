namespace ReflectReturnLib;

public interface ILibraryResult
{
    string Label();
}

public struct LibraryResult : ILibraryResult
{
    public int Number;

    public string Label() => "result:" + Number;
    public override string ToString() => "result:" + Number;
}

public struct VirtualResult : ILibraryResult
{
    public int Number;

    public string Label() => "virtual:" + Number;
}

public struct UnusedResult : ILibraryResult
{
    public string Label() => "unused";
    public override string ToString() => "unused";
}

public static class LibraryFactory
{
    public static LibraryResult Make() => new LibraryResult { Number = 17 };
    public static UnusedResult Unused() => new UnusedResult();
}

public class VirtualFactory
{
    public virtual VirtualResult MakeVirtual(int number) => new VirtualResult { Number = number };
}

public struct ValueResult : ILibraryResult
{
    public int Number;

    public string Label() => "value:" + Number;
}

public struct ValueFactory
{
    public ValueResult MakeValue(int number) => new ValueResult { Number = number };
}

public struct LateResult : ILibraryResult
{
    public string Label() => "late";
}

public struct GenericResult : ILibraryResult
{
    public int Number;

    public string Label() => "generic:" + Number;
}

public static class LateFactory
{
    public static LateResult MakeLate() => new LateResult();
}

public struct DeadResult : ILibraryResult
{
    public string Label() => "dead";
    public override string ToString() => "dead";
}

public static class DeadFactory
{
    public static DeadResult MakeDead() => new DeadResult();
}
