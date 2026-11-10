using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Reflection;
using System.Collections.Generic;

namespace TypedGCHandleSubset;

internal static class Program
{
    private sealed class Payload { internal int Value; }
    private enum Choice { First = 3, Second = 7 }
    private static void Error(string label, Action body)
    {
        try { body(); Console.WriteLine(label + ":accepted"); }
        catch (Exception error) { Console.WriteLine(label + ":" + error.GetType().Name + ":" + error.Message); }
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static IntPtr RootOnlyToken() => GCHandle<Payload>.ToIntPtr(new GCHandle<Payload>(new Payload { Value = 37 }));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DisposeConstrained<T>(ref T value) where T : struct, IDisposable => value.Dispose();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool EqualsConstrained<T>(ref T left, T right) where T : struct, IEquatable<T> => left.Equals(right);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool EqualsObject<T>(GCHandle<T> left, object right) where T : class => left.Equals(right);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool EqualsObjectConstrained<T>(T left, object right) where T : struct => left.Equals(right);

    private static void InterfaceCalls()
    {
        var empty = default(GCHandle<Payload>);
        DisposeConstrained(ref empty);
        DisposeConstrained(ref empty);
        Console.WriteLine("typed-constrained-default:" + (!empty.IsAllocated
            && EqualsConstrained(ref empty, default(GCHandle<Payload>))));
        object emptyBox = empty;
        ((IDisposable)emptyBox).Dispose();
        ((IDisposable)emptyBox).Dispose();
        Console.WriteLine("typed-interface-default:" + !((GCHandle<Payload>)emptyBox).IsAllocated);

        var handle = new GCHandle<Payload>(new Payload { Value = 53 });
        var copy = handle;
        Console.WriteLine("typed-constrained-equals:" + (EqualsConstrained(ref handle, copy)
            && !EqualsConstrained(ref handle, empty)));
        Console.WriteLine("typed-generic-object-payload:" + (EqualsObject(handle, (object)copy)
            && !EqualsObject(handle, (object)default(GCHandle<string>))
            && !EqualsObject(handle, null)
            && EqualsObjectConstrained(handle, (object)copy)));
        object boxed = handle;
        var equatable = (IEquatable<GCHandle<Payload>>)boxed;
        Console.WriteLine("typed-interface-equals:" + (equatable.Equals(copy) && !equatable.Equals(empty)));
        Console.WriteLine("typed-interface-wrong-closed:" + !(boxed is IEquatable<GCHandle<string>>));
        DisposeConstrained(ref handle);
        DisposeConstrained(ref handle);
        Console.WriteLine("typed-constrained-dispose:" + (!handle.IsAllocated && copy.IsAllocated));
        // Copies retain their token after another copy releases it; never dereference or free them.
        Error("typed-constrained-disposed-get", () => { _ = handle.Target; });

        var original = new GCHandle<Payload>(new Payload { Value = 61 });
        object disposableBox = original;
        var disposable = (IDisposable)disposableBox;
        disposable.Dispose();
        disposable.Dispose();
        var disposed = (GCHandle<Payload>)disposableBox;
        Console.WriteLine("typed-interface-dispose:" + (!disposed.IsAllocated
            && GCHandle<Payload>.ToIntPtr(disposed) == IntPtr.Zero
            && disposableBox.Equals((object)empty)
            && ((IEquatable<GCHandle<Payload>>)disposableBox).Equals(empty)));
        Console.WriteLine("typed-interface-copy-allocated:" + original.IsAllocated);
        Error("typed-interface-disposed-get", () => { _ = disposed.Target; });
        Error("typed-interface-disposed-set", () => disposed.Target = null);

        using (var text = new GCHandle<string>("generic"))
            Console.WriteLine("typed-generic-object-string:" + (EqualsObject(text, (object)text)
                && EqualsObjectConstrained(text, (object)text)
                && !EqualsObject(text, (object)empty)));
        using (var array = new GCHandle<int[]>(new[] { 3 }))
            Console.WriteLine("typed-generic-object-array:" + (EqualsObject(array, (object)array)
                && EqualsObjectConstrained(array, (object)array)
                && !EqualsObject(array, (object)empty)));
        using (var culture = new GCHandle<CultureInfo>(CultureInfo.InvariantCulture))
            Console.WriteLine("typed-generic-object-culture:" + (EqualsObject(culture, (object)culture)
                && EqualsObjectConstrained(culture, (object)culture)
                && !EqualsObject(culture, (object)empty)));
        return;
    }

    internal static void Run()
    {
        Console.WriteLine("== Typed GC handles ==");
        var empty = default(GCHandle<Payload>);
        Console.WriteLine("typed-default:" + empty.IsAllocated);
        Error("typed-default-get", () => { _ = empty.Target; });
        Error("typed-default-set", () => empty.Target = null);
        empty.Dispose(); empty.Dispose();
        var zero = GCHandle<Payload>.FromIntPtr(IntPtr.Zero);
        Console.WriteLine("typed-zero:" + (!zero.IsAllocated && zero.Equals(empty) && zero.GetHashCode() == 0));
        Console.WriteLine("typed-zero-boxed:" + (object.Equals((object)zero, (object)empty) && ((object)zero).GetHashCode() == 0));
        Console.WriteLine("typed-zero-wrong-closed:" + (!zero.Equals((object)default(GCHandle<object>))
            && !((object)zero).Equals((object)default(GCHandle<object>))));
        var original = new Payload { Value = 11 };
        var handle = new GCHandle<Payload>(original);
        var copy = handle;
        Console.WriteLine("typed-new:" + (handle.IsAllocated && ReferenceEquals(handle.Target, original)));
        using (var distinct = new GCHandle<Payload>(original))
            Console.WriteLine("typed-distinct-handles:" + (ReferenceEquals(handle.Target, distinct.Target)
                && !handle.Equals(distinct) && !handle.Equals((object)distinct)));
        Console.WriteLine("typed-equality:" + (handle.Equals(copy) && handle.Equals((object)copy) && handle.GetHashCode() == copy.GetHashCode()));
        object boxed = handle;
        Console.WriteLine("typed-boxed:" + (boxed.Equals((object)copy) && boxed.GetHashCode() == handle.GetHashCode()
            && !boxed.Equals((object)default(GCHandle<object>)) && !boxed.Equals(null)));
        Console.WriteLine("typed-wrong-equality:" + (!handle.Equals((object)default(GCHandle<object>)) && !handle.Equals(null)));
        handle.Target = new Payload { Value = 23 };
        Console.WriteLine("typed-copy-update:" + copy.Target!.Value);
        var token = GCHandle<Payload>.ToIntPtr(handle);
        var roundtrip = GCHandle<Payload>.FromIntPtr(token);
        Console.WriteLine("typed-roundtrip:" + (roundtrip.Equals(handle) && roundtrip.Target!.Value == 23));
        var otherType = GCHandle<object>.FromIntPtr(token);
        Console.WriteLine("typed-cross-type-token:" + (GCHandle<object>.ToIntPtr(otherType) == token
            && !handle.Equals((object)otherType) && !((object)handle).Equals((object)otherType)));
        handle.Dispose(); handle.Dispose();
        Console.WriteLine("typed-disposed:" + !handle.IsAllocated);
        Console.WriteLine("typed-disposed-zero:" + (handle.Equals(empty) && ((object)handle).Equals((object)empty)
            && handle.GetHashCode() == 0 && GCHandle<Payload>.ToIntPtr(handle) == IntPtr.Zero));
        Error("typed-disposed-get", () => { _ = handle.Target; });
        Error("typed-disposed-set", () => handle.Target = null);
        var nullHandle = new GCHandle<string>(null);
        Console.WriteLine("typed-null:" + (nullHandle.IsAllocated && nullHandle.Target == null));
        nullHandle.Target = "text";
        Console.WriteLine("typed-string:" + nullHandle.Target);
        nullHandle.Dispose();
        int invoked = 0;
        using (var action = new GCHandle<Action>(() => invoked++)) action.Target!();
        Console.WriteLine("typed-action:" + invoked);
        using (var array = new GCHandle<int[]>(new[] { 4, 9 }))
        {
            Console.WriteLine("typed-array:" + array.Target![1]);
            int sum = 0;
            foreach (int value in (IEnumerable<int>)array.Target!) sum += value;
            Console.WriteLine("typed-array-enumeration:" + sum);
        }
        var enumArray = new GCHandle<Choice[]>(new[] { Choice.Second });
        Console.WriteLine("typed-enum-array-new:" + (enumArray.IsAllocated && enumArray.Target![0] == Choice.Second));
        enumArray.Target = new[] { Choice.First };
        Console.WriteLine("typed-enum-array-set:" + (enumArray.Target![0] == Choice.First));
        enumArray.Dispose();
        var culture = new GCHandle<CultureInfo>(CultureInfo.InvariantCulture);
        Console.WriteLine("typed-culture:" + ReferenceEquals(culture.Target, CultureInfo.InvariantCulture));
        culture.Target = null;
        Console.WriteLine("typed-culture-null:" + (culture.Target == null));
        culture.Target = CultureInfo.InvariantCulture;
        Console.WriteLine("typed-culture-set:" + ReferenceEquals(culture.Target, CultureInfo.InvariantCulture));
        culture.Dispose();
        var textValue = CultureInfo.InvariantCulture.TextInfo;
        var text = new GCHandle<TextInfo>(textValue);
        Console.WriteLine("typed-text-object:" + ReferenceEquals((object)text.Target!, (object)textValue));
        text.Target = null;
        Console.WriteLine("typed-text-null:" + (text.Target == null));
        text.Target = textValue;
        Console.WriteLine("typed-text-set:" + ReferenceEquals(text.Target, textValue));
        text.Dispose();
        var assemblyValue = typeof(Program).Assembly;
        var assembly = new GCHandle<Assembly>(assemblyValue);
        Console.WriteLine("typed-assembly:" + ReferenceEquals(assembly.Target, assemblyValue));
        assembly.Target = null;
        Console.WriteLine("typed-assembly-null:" + (assembly.Target == null));
        assembly.Target = assemblyValue;
        Console.WriteLine("typed-assembly-set:" + ReferenceEquals(assembly.Target, assemblyValue));
        assembly.Dispose();
        var moduleValue = typeof(Program).Module;
        var module = new GCHandle<Module>(moduleValue);
        Console.WriteLine("typed-module-object:" + ReferenceEquals((object)module.Target!, (object)moduleValue));
        module.Target = null;
        Console.WriteLine("typed-module-null:" + (module.Target == null));
        module.Target = moduleValue;
        Console.WriteLine("typed-module-set:" + ReferenceEquals(module.Target, moduleValue));
        module.Dispose();
        var onlyToken = RootOnlyToken();
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        var rooted = GCHandle<Payload>.FromIntPtr(onlyToken);
        Console.WriteLine("typed-token-root:" + rooted.Target!.Value);
        rooted.Dispose();
        InterfaceCalls();
        Console.WriteLine("Typed GC handles end");
        return;
    }
}
