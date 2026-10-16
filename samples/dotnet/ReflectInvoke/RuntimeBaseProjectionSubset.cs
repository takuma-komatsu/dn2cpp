#nullable enable
using System;

namespace ReflectDelegateSubset;

class ProjectionRoot
{
    public virtual Type GenericSecond<V>() => typeof(object);
}

class ProjectionBase<T, U> : ProjectionRoot
{
    public Type First() => typeof(T);
    public Type Second() => typeof(U);
    public static Type StaticSecond() => typeof(U);
    public Type ForwardSecond() => StaticSecond();
    public override Type GenericSecond<V>() => typeof(U);
}

class ProjectionSwap<T, U> : ProjectionBase<U, T> { }
class ProjectionDrop<T, U, V> : ProjectionSwap<U, V> { }
class ProjectionRepeated<T, U> : ProjectionBase<T, T> { }
class ProjectionConstant<T, U> : ProjectionBase<U, int> { }
class ProjectionConstructed<T, U> : ProjectionBase<SignatureBox<U>, T> { }

class ProjectionLayoutBase<T, U>
{
    public Type? Seen;
    public ProjectionLayoutBase() { }
    public ProjectionLayoutBase(int _) { Seen = typeof(U); }
    public Type First() => typeof(T);
    public static Type StaticFirst() => typeof(T);
    public Type ForwardFirst() => StaticFirst();
}

class ProjectionLayoutSwap<T, U> : ProjectionLayoutBase<U, T> { }

class ProjectionCtorBase<T, U>
{
    public Type? Seen;
    public ProjectionCtorBase() { }
    public ProjectionCtorBase(int _) { Seen = typeof(U); }
}

class ProjectionCtorSwap<T, U> : ProjectionCtorBase<U, T> { }

class ProjectionBodyRoot
{
    public virtual void Trigger() { }
}

class ProjectionBodyBase<T, U> : ProjectionBodyRoot
{
    public virtual void Touch() { }
    public Type Probe() { Touch(); return typeof(T); }
    public Type Second() => typeof(U);
}

class ProjectionBodySwap<T, U> : ProjectionBodyBase<U, T>
{
    public override void Trigger() { _ = Probe(); }
}

class ProjectionBodyDrop<T, U, V> : ProjectionBodyBase<U, V> { }

class ProjectionNullBase<T, U>
{
    public Type First() => typeof(T);
}

class ProjectionNullSwap<T, U> : ProjectionNullBase<U, T>
{
    public Type Forward() => First();
}

class ProjectionNullDrop<T, U, V> : ProjectionNullSwap<U, V>
{
    public Type Again() => Forward();
}

internal static class RuntimeBaseProjectionSubset
{
    internal static void Run()
    {
        Console.WriteLine("== runtime generic base projections ==");
        Type swapped = typeof(ProjectionSwap<,>).MakeGenericType(typeof(Guid), typeof(int));
        Type swappedBase = swapped.BaseType!;
        object receiver = Activator.CreateInstance(swapped)!;
        var first = swapped.GetMethod("First")!.CreateDelegate<Func<Type>>(receiver);
        var second = swapped.GetMethod("Second")!.CreateDelegate<Func<Type>>(receiver);
        var forwarded = swapped.GetMethod("ForwardSecond")!.CreateDelegate<Func<Type>>(receiver);
        Console.WriteLine($"projection swapped => {first().Name}/{second().Name}/{forwarded().Name}");
        Console.WriteLine($"projection AOT base => {swappedBase == typeof(ProjectionBase<int, Guid>)}/{first.Method.DeclaringType == swappedBase}/{first.Method.ReflectedType == swappedBase}");
        var root = (ProjectionRoot)receiver;
        Func<Type> generic = root.GenericSecond<string>;
        Console.WriteLine($"projection generic virtual => {generic().Name}/{generic.Method.DeclaringType == swappedBase}/{root.GenericSecond<string>().Name}");
        Type standalone = typeof(ProjectionBase<,>).MakeGenericType(typeof(Guid), typeof(string));
        object standaloneReceiver = Activator.CreateInstance(standalone)!;
        var standaloneFirst = standalone.GetMethod("First")!.CreateDelegate<Func<Type>>(standaloneReceiver);
        Func<Type> standaloneGeneric = ((ProjectionRoot)standaloneReceiver).GenericSecond<string>;
        Console.WriteLine($"projection standalone => {standaloneFirst().Name}/{standaloneGeneric().Name}/{standaloneGeneric.Method.DeclaringType == standalone}");
        Type dropped = typeof(ProjectionDrop<,,>).MakeGenericType(typeof(decimal), typeof(string), typeof(Guid));
        Type middle = dropped.BaseType!;
        Type bottom = middle.BaseType!;
        receiver = Activator.CreateInstance(dropped)!;
        first = dropped.GetMethod("First")!.CreateDelegate<Func<Type>>(receiver);
        second = dropped.GetMethod("Second")!.CreateDelegate<Func<Type>>(receiver);
        forwarded = dropped.GetMethod("ForwardSecond")!.CreateDelegate<Func<Type>>(receiver);
        generic = ((ProjectionRoot)receiver).GenericSecond<string>;
        Console.WriteLine($"projection chained => {first().Name}/{second().Name}/{forwarded().Name}/{generic().Name}");
        Type[] arguments = middle.GetGenericArguments();
        Console.WriteLine($"projection local arguments => {arguments[0].Name}/{arguments[1].Name}/{bottom == standalone}/{generic.Method.DeclaringType == bottom}");
        Type reverse = typeof(ProjectionSwap<,>).MakeGenericType(typeof(Guid), typeof(string));
        Type reverseBase = typeof(ProjectionBase<,>).MakeGenericType(typeof(string), typeof(Guid));
        Console.WriteLine($"projection interning => {reverse.BaseType == reverseBase}/{typeof(ProjectionDrop<,,>).MakeGenericType(typeof(decimal), typeof(string), typeof(Guid)) == dropped}/{middle == typeof(ProjectionSwap<,>).MakeGenericType(typeof(string), typeof(Guid))}");
        Type owner = typeof(SignatureOverloadReordered<,>).MakeGenericType(typeof(Guid), typeof(int));
        var target = (NameRef)Delegate.CreateDelegate(typeof(NameRef), owner, "Ref");
        int value = 10;
        Console.WriteLine($"projection inherited overload => {target(ref value)}/{value}/{target.Method.DeclaringType == owner.BaseType}");
        Type layoutBase = typeof(ProjectionLayoutBase<,>).MakeGenericType(typeof(Guid), typeof(string));
        Type layoutSwap = typeof(ProjectionLayoutSwap<,>).MakeGenericType(typeof(string), typeof(Guid));
        object layoutReceiver = Activator.CreateInstance(layoutSwap)!;
        var layoutFirst = layoutSwap.GetMethod("First")!.CreateDelegate<Func<Type>>(layoutReceiver);
        var layoutForward = layoutSwap.GetMethod("ForwardFirst")!.CreateDelegate<Func<Type>>(layoutReceiver);
        Console.WriteLine($"projection constructor layouts => {layoutFirst().Name}/{layoutForward().Name}/{layoutSwap.BaseType == layoutBase}/{layoutFirst.Method.DeclaringType == layoutBase}");
        Type ctorSwap = typeof(ProjectionCtorSwap<,>).MakeGenericType(typeof(string), typeof(Guid));
        Type ctorBase = typeof(ProjectionCtorBase<,>).MakeGenericType(typeof(Guid), typeof(string));
        object constructed = Activator.CreateInstance(ctorBase, new object[] { 7 })!;
        Console.WriteLine($"projection constructor source => {((Type)ctorBase.GetField("Seen")!.GetValue(constructed)!).Name}/{ctorSwap.BaseType == ctorBase}");
        Type bodySwap = typeof(ProjectionBodySwap<,>).MakeGenericType(typeof(string), typeof(Guid));
        var bodyReceiver = (ProjectionBodyRoot)Activator.CreateInstance(bodySwap)!;
        bodyReceiver.Trigger();
        Type bodyBase = typeof(ProjectionBodyBase<,>).MakeGenericType(typeof(Guid), typeof(string));
        Type bodyDrop = typeof(ProjectionBodyDrop<,,>).MakeGenericType(typeof(decimal), typeof(Guid), typeof(string));
        var swapSecond = bodySwap.GetMethod("Second")!.CreateDelegate<Func<Type>>(bodyReceiver);
        var dropSecond = bodyDrop.GetMethod("Second")!.CreateDelegate<Func<Type>>(Activator.CreateInstance(bodyDrop)!);
        Console.WriteLine($"projection body layouts => {swapSecond().Name}/{dropSecond().Name}/{bodySwap.BaseType == bodyBase}/{bodyDrop.BaseType == bodyBase}");
        foreach (Type[] argumentsForNull in new[] { new[] { typeof(string), typeof(Guid) }, new[] { typeof(Guid), typeof(string) } })
        {
            Type nullSwap = typeof(ProjectionNullSwap<,>).MakeGenericType(argumentsForNull);
            Type nullDrop = typeof(ProjectionNullDrop<,,>).MakeGenericType(typeof(decimal), argumentsForNull[0], argumentsForNull[1]);
            foreach (var entry in new[] { (Owner: nullSwap, Name: "Forward"), (Owner: nullDrop, Name: "Again") })
            {
                var nullCall = entry.Owner.GetMethod(entry.Name)!.CreateDelegate<Func<Type>>(null);
                string outcome;
                try
                {
                    outcome = nullCall().Name;
                }
                catch (NullReferenceException)
                {
                    outcome = nameof(NullReferenceException);
                }
                Console.WriteLine($"projection null {entry.Name}/{argumentsForNull[0].Name} => {outcome}");
            }
        }
        Console.WriteLine("runtime generic base projections end");
    }

    // The arguments arrive as another method's result, so the transpile mints no
    // instantiation for them and these shapes reach the runtime-template refusal.
    private static Type[] BoundaryArguments() => new[] { typeof(Guid), typeof(string) };

    internal static void RunBoundaries()
    {
        foreach (Type definition in new[] { typeof(ProjectionRepeated<,>), typeof(ProjectionConstant<,>), typeof(ProjectionConstructed<,>) })
        {
            string outcome;
            try
            {
                outcome = definition.MakeGenericType(BoundaryArguments()).IsGenericType ? "created" : "missing";
            }
            catch (NotSupportedException)
            {
                outcome = "unsupported";
            }
            Console.WriteLine("projection unsupported " + definition.Name + " => " + outcome);
        }
    }
}
