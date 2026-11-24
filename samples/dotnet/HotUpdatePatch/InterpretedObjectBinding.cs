using HotUpdateBase;

namespace HotUpdatePatch;

public sealed class InterpretedObjectBinding : Slate
{
    public static string Run() => new InterpretedObjectBinding().Read();

    private string Read() => CheckBaseText(base.ToString);
}
