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
    private static void Error(string label, Action body)
    {
        try { body(); Console.WriteLine(label + ":accepted"); }
        catch (Exception error) { Console.WriteLine(label + ":" + error.GetType().Name + ":" + error.Message); }
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static IntPtr RootOnlyToken() => GCHandle<Payload>.ToIntPtr(new GCHandle<Payload>(new Payload { Value = 37 }));
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
        var original = new Payload { Value = 11 };
        var handle = new GCHandle<Payload>(original);
        var copy = handle;
        Console.WriteLine("typed-new:" + (handle.IsAllocated && ReferenceEquals(handle.Target, original)));
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
        handle.Dispose(); handle.Dispose();
        Console.WriteLine("typed-disposed:" + !handle.IsAllocated);
        Console.WriteLine("typed-disposed-zero:" + (handle.Equals(empty) && ((object)handle).Equals((object)empty)
            && handle.GetHashCode() == 0 && GCHandle<Payload>.ToIntPtr(handle) == IntPtr.Zero));
        Error("typed-disposed-get", () => { _ = handle.Target; });
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
        var culture = new GCHandle<CultureInfo>(CultureInfo.InvariantCulture);
        Console.WriteLine("typed-culture:" + ReferenceEquals(culture.Target, CultureInfo.InvariantCulture));
        culture.Target = null;
        Console.WriteLine("typed-culture-null:" + (culture.Target == null));
        culture.Target = CultureInfo.InvariantCulture;
        Console.WriteLine("typed-culture-set:" + ReferenceEquals(culture.Target, CultureInfo.InvariantCulture));
        culture.Dispose();
        var textValue = CultureInfo.InvariantCulture.TextInfo;
        using (var text = new GCHandle<TextInfo>(textValue))
            Console.WriteLine("typed-text-object:" + ReferenceEquals((object)text.Target!, (object)textValue));
        var assemblyValue = typeof(Program).Assembly;
        var assembly = new GCHandle<Assembly>(assemblyValue);
        Console.WriteLine("typed-assembly:" + ReferenceEquals(assembly.Target, assemblyValue));
        assembly.Target = null;
        Console.WriteLine("typed-assembly-null:" + (assembly.Target == null));
        assembly.Target = assemblyValue;
        Console.WriteLine("typed-assembly-set:" + ReferenceEquals(assembly.Target, assemblyValue));
        assembly.Dispose();
        var moduleValue = typeof(Program).Module;
        using (var module = new GCHandle<Module>(moduleValue))
            Console.WriteLine("typed-module-object:" + ReferenceEquals((object)module.Target!, (object)moduleValue));
        var onlyToken = RootOnlyToken();
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        var rooted = GCHandle<Payload>.FromIntPtr(onlyToken);
        Console.WriteLine("typed-token-root:" + rooted.Target!.Value);
        rooted.Dispose();
        Console.WriteLine("Typed GC handles end");
    }
}
