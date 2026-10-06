namespace ReflectionUnusedSignature;

public static class Subject
{
    public static int Used(int value) => value + 1;
    public static U Supported<U>(U value) => value;
    public static void Unused<U>(Ext.Box<U> value) { }
}

public class Base
{
    public Ext.Box<int>? Value;
}

public interface IMethodSignature<T>
{
    Ext.Box<T>? Get();
}

public class VirtualBase<T>
{
    public virtual void M(Ext.Box<T> value) { }
}

public sealed class VirtualDerived<T> : VirtualBase<T>
{
    public override void M(Ext.Box<T> value) { }
}

public class NameGateBase<T>
{
    public virtual void M(int value) { }
    public void Other(Ext.Box<T> value) { }
}

public class VirtualSibling<T> : NameGateBase<T>
{
    public override void M(int value) { }
}

public interface IDefaultSignature<T>
{
    Ext.Box<T>? Get() => null;
}

public sealed class DefaultHolder<T> : IDefaultSignature<T> { }

public struct SDefault<T> : IDefaultSignature<T> { }

public interface ISupportedDefault<T>
{
    int Get() => 42;
}

public sealed class SupportedDefaultHolder<T> : ISupportedDefault<T> { }
