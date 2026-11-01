using System.Runtime.Intrinsics;

public static class EmitterInput
{
    public static int Good(int value) => value + 1;
    public static int Three(int one, int two, int three) => one + two + three;
    public static int Bad(int value)
    {
        _ = Vector128.MaxNumber(default(Vector128<decimal>), default);
        return value;
    }
    public static int Later() => Bad(1);
}

public class LifetimeInput
{
    public void Dispose() { }
    protected virtual void Dispose(bool disposing) { }
}
