using System;

namespace AssemblyScopeCollision;

public static class Who
{
    public static string Name() => "library";
}

public class Box
{
    public long Wide;
    public int Number;
    public Box(int number) { Wide = 9000000000L; Number = number; }
    public int Read() => Number;
}

public sealed class Child : Box
{
    public Child(int number) : base(number) { }
}

public sealed class Holder
{
    public Box Value;
    public Box[] Values;
    public static Box StaticValue;
    public Holder(Box value) { Value = value; Values = new[] { value }; StaticValue = value; }
    public static int Accept(Box value) => value.Read();
    public static Type ReadType() => typeof(Box);
}

public sealed class Generic<T>
{
    public T Value;
    public static int Count;
    public Generic(T value) { Value = value; Count++; }
    public T Read() => Value;
    public static string Label() => "library";
}
