using System;
using System.Collections;
using System.Collections.Generic;

namespace DelegateVirtualIdentitySubset;

interface IFirst { int First(); }
interface ISecond { int Second(); }
interface IAlias { int First(); }
interface IOut<out T> { int First(); }

class Base : IFirst, ISecond
{
    public virtual int First() => 7;
    public virtual int Second() => 7;
}

class Child : Base, IAlias, IOut<string>
{
    public override int First() => 7;
    public override int Second() => 7;
}

class Hider : Base
{
    public new virtual int First() => 7;
}

class Explicit : IFirst, IAlias
{
    int IFirst.First() => 7;
    int IAlias.First() => 7;
}

class Generic<T> : Base
{
    public override int First() => 7;
    public override int Second() => 7;
    public virtual int One<U>() => 7;
    public virtual int Two<U>() => 7;
}

class ObjectMethods
{
    public override int GetHashCode() => 7;
    public virtual int OtherHash() => 7;
}

class Runtime<T> : Base, IAlias
{
    public override int First() => 7;
    public override int Second() => 7;
}

struct Boxed : IFirst, IAlias
{
    public int First() => 7;
    int IAlias.First() => 7;
}

public static class Program
{
    static Base NewChild() => new Child();

    static void Pair(string label, Func<int> first, Func<int> second)
    {
        var combined = first + second;
        var remaining = Delegate.Remove(combined, first).GetInvocationList();
        Console.WriteLine($"virtual identity {label}: {first == second}/{(first != second || first.GetHashCode() == second.GetHashCode())}/{ReferenceEquals(remaining[0], second)}/{first()}/{second()}");
    }

    static void StringPair(string label, Func<object> first, Func<object> second)
    {
        var remaining = Delegate.Remove(first + second, first).GetInvocationList();
        Console.WriteLine($"virtual identity {label}: {first == second}/{(first != second || first.GetHashCode() == second.GetHashCode())}/{ReferenceEquals(remaining[0], second)}/{first().GetType().Name}/{second().GetType().Name}");
    }

    static void StringFormatPair(string label, Func<IFormatProvider, string> first,
        Func<IFormatProvider, string> second)
    {
        var remaining = Delegate.Remove(first + second, first).GetInvocationList();
        Console.WriteLine($"virtual identity {label}: {first == second}/{(first != second || first.GetHashCode() == second.GetHashCode())}/{ReferenceEquals(remaining[0], second)}/{first(null)}/{second(null)}");
    }

    static void ArrayPair(string label, Func<IEnumerator> first, Func<IEnumerator> second)
    {
        var remaining = Delegate.Remove(first + second, first).GetInvocationList();
        Console.WriteLine($"virtual identity {label}: {first == second}/{(first != second || first.GetHashCode() == second.GetHashCode())}/{ReferenceEquals(remaining[0], second)}/{first().MoveNext()}/{second().MoveNext()}");
    }

    public static void Run()
    {
        Console.WriteLine("== virtual delegate selected methods ==");
#if DELEGATE_IDENTITY_IL_ONLY
        Base child = NewChild();
#else
        var child = new Child();
#endif
        Pair("distinct overrides", child.First, child.Second);
        Pair("base override", ((Base)child).First, child.First);
        Pair("class interface", child.First, ((IFirst)child).First);
        Pair("interface aliases", ((IFirst)child).First, ((IAlias)child).First);
        Pair("variant interface", ((IOut<string>)child).First, ((IOut<object>)child).First);
        var hider = new Hider();
        Pair("new slots", ((Base)hider).First, hider.First);
        var impl = new Explicit();
        Pair("explicit methods", ((IFirst)impl).First, ((IAlias)impl).First);
        var generic = new Generic<string>();
        Pair("closed owner", generic.First, generic.Second);
        Pair("generic virtuals", generic.One<string>, generic.Two<string>);
        Pair("generic arguments", generic.One<string>, generic.One<object>);
        Pair("generic same", generic.One<string>, generic.One<string>);
        Pair("closed base override", ((Base)generic).First, generic.First);
        var objectMethods = new ObjectMethods();
        Pair("Object distinct", ((object)objectMethods).GetHashCode, objectMethods.OtherHash);
        Pair("Object override", ((object)objectMethods).GetHashCode, objectMethods.GetHashCode);
        object boxed = new Boxed();
        Pair("boxed methods", ((IFirst)boxed).First, ((IAlias)boxed).First);
        Pair("boxed same", ((IFirst)boxed).First, ((IFirst)boxed).First);
        string text = "same";
        Console.WriteLine($"virtual identity String dispatch: {((IEnumerable<char>)text).GetEnumerator().MoveNext()}/{((IConvertible)text).ToString(null)}");
        StringPair("String enumerators", ((IEnumerable)text).GetEnumerator,
            ((IEnumerable<char>)text).GetEnumerator);
        StringPair("String same", ((IEnumerable<char>)text).GetEnumerator,
            ((IEnumerable<char>)text).GetEnumerator);
        StringFormatPair("String interface", text.ToString, ((IConvertible)text).ToString);
        object array = new string[] { "same" };
        Console.WriteLine($"virtual identity array dispatch: {((IEnumerable<string>)array).GetEnumerator().MoveNext()}");
        ArrayPair("array enumerators", ((IEnumerable)array).GetEnumerator,
            ((IEnumerable<string>)array).GetEnumerator);
        ArrayPair("array variant", ((IEnumerable<string>)array).GetEnumerator,
            ((IEnumerable<object>)array).GetEnumerator);
        ArrayPair("array same", ((IEnumerable<string>)array).GetEnumerator,
            ((IEnumerable<string>)array).GetEnumerator);
        Console.WriteLine("virtual delegate selected methods end");
    }

    public static void RunReflected()
    {
        Console.WriteLine("== runtime delegate selected methods ==");
        var child = new Child();
        var first = typeof(Base).GetMethod("First");
        var second = typeof(Base).GetMethod("Second");
        Pair("reflected distinct", (Func<int>)first.CreateDelegate(typeof(Func<int>), child),
            (Func<int>)second.CreateDelegate(typeof(Func<int>), child));
        Pair("reflected override", (Func<int>)first.CreateDelegate(typeof(Func<int>), child),
            (Func<int>)typeof(Child).GetMethod("First").CreateDelegate(typeof(Func<int>), child));
        var runtime = (Base)Activator.CreateInstance(typeof(Runtime<>).MakeGenericType(typeof(Guid)));
        Pair("runtime distinct", runtime.First, runtime.Second);
        Pair("runtime interface", runtime.First, ((IAlias)runtime).First);
        Pair("runtime same", runtime.First, runtime.First);
        Console.WriteLine("runtime delegate selected methods end");
    }
}
