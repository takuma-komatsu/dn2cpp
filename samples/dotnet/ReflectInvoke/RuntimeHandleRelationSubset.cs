#nullable enable
using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// SUBJECT: the CLR relations of objects whose type-info the runtime writes by hand —
// the reflection objects, Assembly and Module, StringBuilder, Exception and the
// exceptions the runtime raises itself, the synchronization handles, Thread, Task and
// the culture wrappers — and of System.Array's emitted type-info. The type test,
// IsAssignableFrom, BaseType, the interface enumerators and the invoke receiver and
// argument checks read one base chain and one set of interface rows, so each relation
// is asked through several of them.
//
// The runtime-raised exceptions come from real faults: constructing one binds its
// handle to the emitted class, and the runtime's own type-info would go unasked.
//
// An interface whose members no dispatch map serves on these objects
// (ICustomAttributeProvider, IReflect, ISerializable, IAsyncResult, ICloneable,
// IDeserializationCallback, and Task's IDisposable) is tested and passed, never
// called. Assembly, Module and the culture wrappers are headerless handles until
// they escape to object, so every test reaches them through object.
namespace RuntimeHandleRelationSubset;

class Holder
{
    public int Field;

    public int Property { get; set; }

    public void Method(int value) => Field = value;
}

class DisposableHolder
{
    public DisposableHolder(IDisposable value) => Held = value is not null;

    public bool Held { get; }
}

static class Sink
{
    public static IDisposable? Disposable { get; set; }
    public static string Attributes(ICustomAttributeProvider value) => "attributes";
    public static string Reflect(IReflect value) => "reflect";
    public static string Member(MemberInfo value) => "member:" + value.Name;
    public static string Serializable(ISerializable value) => "serializable";
    public static string Disposes(IDisposable value) => "disposable";
    public static string Async(IAsyncResult value) => "async";
    public static string Remote(MarshalByRefObject value) => "remote";
    public static string Critical(CriticalFinalizerObject value) => "critical";
    public static string SystemFault(SystemException value) => "system:" + value.GetType().Name;
    public static string Arithmetic(ArithmeticException value) => "arithmetic:" + value.GetType().Name;
    public static string Cloneable(ICloneable value) => "cloneable";
    public static string Provider(IFormatProvider value) => "provider";
}

static class Program
{
    private static int[]? s_null;
    private static int[] s_one = new int[1];
    private static int s_index = 5;
    private static int s_zero;
    private static long s_wide = long.MaxValue;
    private static object s_boxed = 5;

    private static void Test(string label, bool value) => Console.WriteLine(label + ": " + value);

    private static void Try(string label, Func<object?> invoke)
    {
        string text;
        try
        {
            text = invoke()?.ToString() ?? "null";
        }
        catch (Exception ex)
        {
            text = ex.GetType().Name
                + (ex.InnerException is null ? "" : "/" + ex.InnerException.GetType().Name);
        }
        Console.WriteLine(label + ": " + text);
    }

    private static Exception Fault(Action fault)
    {
        try
        {
            fault();
        }
        catch (Exception ex)
        {
            return ex;
        }
        throw new InvalidOperationException("no fault");
    }

    private static string Chain(Type type)
    {
        string text = type.Name;
        for (Type? b = type.BaseType; b is not null; b = b.BaseType)
            text += " > " + b.Name;
        return text;
    }

    private static bool Lists(Type type, Type itf)
    {
        foreach (Type i in type.GetInterfaces())
            if (i == itf)
                return true;
        return false;
    }

    // A receiver typed only as IDisposable: no call site can devirtualize it, so the
    // call goes through the handle's IDisposable map.
    private static string Release(IDisposable value)
    {
        value.Dispose();
        return "disposed";
    }

    internal static void Run()
    {
        Console.WriteLine("== runtime handle relations ==");

        // -- The reflection objects, Assembly and Module. --
        object type = typeof(int);
        object method = typeof(Holder).GetMethod("Method")!;
        object ctor = typeof(Holder).GetConstructor(Type.EmptyTypes)!;
        object field = typeof(Holder).GetField("Field")!;
        object property = typeof(Holder).GetProperty("Property")!;
        object parameter = typeof(Holder).GetMethod("Method")!.GetParameters()[0];
        object assembly = typeof(Program).Assembly;
        object module = typeof(Program).Module;
        Test("Type is ICustomAttributeProvider", type is ICustomAttributeProvider);
        Test("Type is IReflect", type is IReflect);
        Test("Type is MemberInfo", type is MemberInfo);
        Test("MethodInfo is ICustomAttributeProvider", method is ICustomAttributeProvider);
        Test("MethodInfo is MethodBase", method is MethodBase);
        Test("ConstructorInfo is ICustomAttributeProvider", ctor is ICustomAttributeProvider);
        Test("FieldInfo is ICustomAttributeProvider", field is ICustomAttributeProvider);
        Test("PropertyInfo is ICustomAttributeProvider", property is ICustomAttributeProvider);
        Test("ParameterInfo is ICustomAttributeProvider", parameter is ICustomAttributeProvider);
        Test("Assembly is ICustomAttributeProvider", assembly is ICustomAttributeProvider);
        Test("Assembly is ISerializable", assembly is ISerializable);
        Test("Module is ICustomAttributeProvider", module is ICustomAttributeProvider);
        Test("Module is ISerializable", module is ISerializable);
        Test("ICustomAttributeProvider <- Type", typeof(ICustomAttributeProvider).IsAssignableFrom(typeof(Type)));
        Test("ICustomAttributeProvider <- MemberInfo", typeof(ICustomAttributeProvider).IsAssignableFrom(typeof(MemberInfo)));
        Test("ICustomAttributeProvider <- MethodInfo", typeof(ICustomAttributeProvider).IsAssignableFrom(typeof(MethodInfo)));
        Test("ICustomAttributeProvider <- ConstructorInfo", typeof(ICustomAttributeProvider).IsAssignableFrom(typeof(ConstructorInfo)));
        Test("ICustomAttributeProvider <- FieldInfo", typeof(ICustomAttributeProvider).IsAssignableFrom(typeof(FieldInfo)));
        Test("ICustomAttributeProvider <- PropertyInfo", typeof(ICustomAttributeProvider).IsAssignableFrom(typeof(PropertyInfo)));
        Test("ICustomAttributeProvider <- ParameterInfo", typeof(ICustomAttributeProvider).IsAssignableFrom(typeof(ParameterInfo)));
        Test("ICustomAttributeProvider <- Assembly", typeof(ICustomAttributeProvider).IsAssignableFrom(typeof(Assembly)));
        Test("ICustomAttributeProvider <- Module", typeof(ICustomAttributeProvider).IsAssignableFrom(typeof(Module)));
        Test("ICustomAttributeProvider <- the assembly's type", typeof(ICustomAttributeProvider).IsAssignableFrom(assembly.GetType()));
        Test("IReflect <- Type", typeof(IReflect).IsAssignableFrom(typeof(Type)));
        Test("MemberInfo <- Type", typeof(MemberInfo).IsAssignableFrom(typeof(Type)));
        Test("ISerializable <- Assembly", typeof(ISerializable).IsAssignableFrom(typeof(Assembly)));
        Test("ISerializable <- Module", typeof(ISerializable).IsAssignableFrom(typeof(Module)));
        Test("Type lists IReflect", Lists(typeof(Type), typeof(IReflect)));
        Test("Assembly lists ISerializable", Lists(typeof(Assembly), typeof(ISerializable)));
        Console.WriteLine("Type GetInterface: " + (typeof(Type).GetInterface("ICustomAttributeProvider")?.Name ?? "null"));
        Console.WriteLine("Type chain: " + Chain(typeof(Type)));
        Console.WriteLine("MethodInfo chain: " + Chain(typeof(MethodInfo)));
        Console.WriteLine("ConstructorInfo chain: " + Chain(typeof(ConstructorInfo)));
        Console.WriteLine("ParameterInfo chain: " + Chain(typeof(ParameterInfo)));

        // -- Exceptions: the runtime's own raises, a plain Exception, and StringBuilder. --
        Exception nullRef = Fault(() => { _ = s_null![0]; });
        Exception index = Fault(() => { _ = s_one[s_index]; });
        Exception cast = Fault(() => { _ = (string)s_boxed; });
        Exception overflow = Fault(() => { _ = checked((int)s_wide); });
        Exception divide = Fault(() => { _ = s_index / s_zero; });
        Console.WriteLine("runtime faults: " + nullRef.GetType().Name + " " + index.GetType().Name + " "
            + cast.GetType().Name + " " + overflow.GetType().Name + " " + divide.GetType().Name);
        Test("runtime NullReferenceException is SystemException", nullRef is SystemException);
        Test("runtime NullReferenceException is ISerializable", nullRef is ISerializable);
        Test("SystemException <- runtime NullReferenceException", typeof(SystemException).IsAssignableFrom(nullRef.GetType()));
        Test("runtime IndexOutOfRangeException is SystemException", index is SystemException);
        Test("SystemException <- runtime IndexOutOfRangeException", typeof(SystemException).IsAssignableFrom(index.GetType()));
        Test("runtime InvalidCastException is SystemException", cast is SystemException);
        Test("runtime OverflowException is ArithmeticException", overflow is ArithmeticException);
        Test("runtime OverflowException is SystemException", overflow is SystemException);
        Test("ArithmeticException <- runtime OverflowException", typeof(ArithmeticException).IsAssignableFrom(overflow.GetType()));
        Test("runtime DivideByZeroException is SystemException", divide is SystemException);
        Test("runtime NullReferenceException lists ISerializable", Lists(nullRef.GetType(), typeof(ISerializable)));
        Console.WriteLine("runtime NullReferenceException chain: " + Chain(nullRef.GetType()));
        Console.WriteLine("runtime IndexOutOfRangeException chain: " + Chain(index.GetType()));
        Console.WriteLine("runtime InvalidCastException chain: " + Chain(cast.GetType()));
        Console.WriteLine("runtime OverflowException chain: " + Chain(overflow.GetType()));
        Console.WriteLine("runtime DivideByZeroException chain: " + Chain(divide.GetType()));
        object plain = new Exception("plain");
        object builder = new StringBuilder("text");
        Test("Exception is ISerializable", plain is ISerializable);
        Test("ISerializable <- Exception", typeof(ISerializable).IsAssignableFrom(typeof(Exception)));
        Test("StringBuilder is ISerializable", builder is ISerializable);
        Test("ISerializable <- StringBuilder", typeof(ISerializable).IsAssignableFrom(typeof(StringBuilder)));

        // -- The synchronization handles, Thread and Task. --
        var source = new CancellationTokenSource();
        var semaphore = new SemaphoreSlim(1);
        var manual = new ManualResetEvent(false);
        var auto = new AutoResetEvent(false);
        var slim = new ManualResetEventSlim(false);
        var timer = new Timer(_ => { }, null, Timeout.Infinite, Timeout.Infinite);
        object sourceObject = source, semaphoreObject = semaphore, manualObject = manual;
        object autoObject = auto, slimObject = slim, timerObject = timer;
        object task = Task.CompletedTask;
        object result = Task.FromResult(1);
        object thread = Thread.CurrentThread;
        Test("CancellationTokenSource is IDisposable", sourceObject is IDisposable);
        Test("SemaphoreSlim is IDisposable", semaphoreObject is IDisposable);
        Test("ManualResetEvent is IDisposable", manualObject is IDisposable);
        Test("ManualResetEvent is MarshalByRefObject", manualObject is MarshalByRefObject);
        Test("AutoResetEvent is IDisposable", autoObject is IDisposable);
        Test("AutoResetEvent is MarshalByRefObject", autoObject is MarshalByRefObject);
        Test("ManualResetEventSlim is IDisposable", slimObject is IDisposable);
        Test("ManualResetEventSlim is MarshalByRefObject", slimObject is MarshalByRefObject);
        Test("Timer is MarshalByRefObject", timerObject is MarshalByRefObject);
        Test("Timer is IAsyncDisposable", timerObject is IAsyncDisposable);
        Test("Task is IDisposable", task is IDisposable);
        Test("Task is IAsyncResult", task is IAsyncResult);
        Test("Task<int> is IAsyncResult", result is IAsyncResult);
        Test("Thread is CriticalFinalizerObject", thread is CriticalFinalizerObject);
        Test("IDisposable <- CancellationTokenSource", typeof(IDisposable).IsAssignableFrom(typeof(CancellationTokenSource)));
        Test("IDisposable <- SemaphoreSlim", typeof(IDisposable).IsAssignableFrom(typeof(SemaphoreSlim)));
        Test("IDisposable <- WaitHandle", typeof(IDisposable).IsAssignableFrom(typeof(WaitHandle)));
        Test("IDisposable <- ManualResetEvent", typeof(IDisposable).IsAssignableFrom(typeof(ManualResetEvent)));
        Test("IDisposable <- AutoResetEvent", typeof(IDisposable).IsAssignableFrom(typeof(AutoResetEvent)));
        Test("IDisposable <- ManualResetEventSlim", typeof(IDisposable).IsAssignableFrom(typeof(ManualResetEventSlim)));
        Test("IDisposable <- Task", typeof(IDisposable).IsAssignableFrom(typeof(Task)));
        Test("IAsyncResult <- Task", typeof(IAsyncResult).IsAssignableFrom(typeof(Task)));
        Test("MarshalByRefObject <- WaitHandle", typeof(MarshalByRefObject).IsAssignableFrom(typeof(WaitHandle)));
        Test("MarshalByRefObject <- Timer", typeof(MarshalByRefObject).IsAssignableFrom(typeof(Timer)));
        Test("CriticalFinalizerObject <- Thread", typeof(CriticalFinalizerObject).IsAssignableFrom(typeof(Thread)));
        Test("ManualResetEvent lists IDisposable", Lists(typeof(ManualResetEvent), typeof(IDisposable)));
        Console.WriteLine("SemaphoreSlim GetInterface: " + (typeof(SemaphoreSlim).GetInterface("IDisposable")?.Name ?? "null"));
        Console.WriteLine("WaitHandle chain: " + Chain(typeof(WaitHandle)));
        Console.WriteLine("ManualResetEvent chain: " + Chain(typeof(ManualResetEvent)));
        Console.WriteLine("AutoResetEvent chain: " + Chain(typeof(AutoResetEvent)));
        Console.WriteLine("Timer chain: " + Chain(typeof(Timer)));
        Console.WriteLine("Thread chain: " + Chain(typeof(Thread)));

        // -- The culture wrappers, the synthesized DateTimeFormatInfo, and System.Array. --
        object culture = CultureInfo.InvariantCulture;
        object numbers = NumberFormatInfo.InvariantInfo;
        object text = CultureInfo.InvariantCulture.TextInfo;
        object dates = DateTimeFormatInfo.InvariantInfo;
        Test("CultureInfo is ICloneable", culture is ICloneable);
        Test("CultureInfo is IFormatProvider", culture is IFormatProvider);
        Test("NumberFormatInfo is ICloneable", numbers is ICloneable);
        Test("NumberFormatInfo is IFormatProvider", numbers is IFormatProvider);
        Test("TextInfo is ICloneable", text is ICloneable);
        Test("TextInfo is IDeserializationCallback", text is IDeserializationCallback);
        Test("DateTimeFormatInfo is ICloneable", dates is ICloneable);
        Test("DateTimeFormatInfo is DateTimeFormatInfo", dates is DateTimeFormatInfo);
        Test("ICloneable <- CultureInfo", typeof(ICloneable).IsAssignableFrom(typeof(CultureInfo)));
        Test("ICloneable <- NumberFormatInfo", typeof(ICloneable).IsAssignableFrom(typeof(NumberFormatInfo)));
        Test("ICloneable <- TextInfo", typeof(ICloneable).IsAssignableFrom(typeof(TextInfo)));
        Test("ICloneable <- DateTimeFormatInfo", typeof(ICloneable).IsAssignableFrom(typeof(DateTimeFormatInfo)));
        Test("IDeserializationCallback <- TextInfo", typeof(IDeserializationCallback).IsAssignableFrom(typeof(TextInfo)));
        Test("IFormatProvider <- CultureInfo", typeof(IFormatProvider).IsAssignableFrom(typeof(CultureInfo)));
        Test("IFormatProvider <- NumberFormatInfo", typeof(IFormatProvider).IsAssignableFrom(typeof(NumberFormatInfo)));
        Test("TextInfo lists IDeserializationCallback", Lists(typeof(TextInfo), typeof(IDeserializationCallback)));
        Test("ICloneable <- Array", typeof(ICloneable).IsAssignableFrom(typeof(Array)));
        Test("IList <- Array", typeof(IList).IsAssignableFrom(typeof(Array)));
        Test("IStructuralEquatable <- Array", typeof(IStructuralEquatable).IsAssignableFrom(typeof(Array)));

        // -- Argument checks: a parameter typed as one of those relations. --
        MethodInfo Sunk(string name) => typeof(Sink).GetMethod(name)!;
        Try("attributes from Type", () => Sunk("Attributes").Invoke(null, new object[] { typeof(int) }));
        Try("attributes from MethodInfo", () => Sunk("Attributes").Invoke(null, new object[] { typeof(Holder).GetMethod("Method")! }));
        Try("attributes from ConstructorInfo", () => Sunk("Attributes").Invoke(null, new object[] { typeof(Holder).GetConstructor(Type.EmptyTypes)! }));
        Try("attributes from FieldInfo", () => Sunk("Attributes").Invoke(null, new object[] { typeof(Holder).GetField("Field")! }));
        Try("attributes from PropertyInfo", () => Sunk("Attributes").Invoke(null, new object[] { typeof(Holder).GetProperty("Property")! }));
        Try("attributes from ParameterInfo", () => Sunk("Attributes").Invoke(null, new object[] { typeof(Holder).GetMethod("Method")!.GetParameters()[0] }));
        Try("attributes from Assembly", () => Sunk("Attributes").Invoke(null, new object[] { typeof(Program).Assembly }));
        Try("attributes from Module", () => Sunk("Attributes").Invoke(null, new object[] { typeof(Program).Module }));
        Try("reflect from Type", () => Sunk("Reflect").Invoke(null, new object[] { typeof(int) }));
        Try("member from Type", () => Sunk("Member").Invoke(null, new object[] { typeof(int) }));
        Try("serializable from Assembly", () => Sunk("Serializable").Invoke(null, new object[] { typeof(Program).Assembly }));
        Try("serializable from StringBuilder", () => Sunk("Serializable").Invoke(null, new object[] { builder }));
        Try("serializable from Exception", () => Sunk("Serializable").Invoke(null, new object[] { plain }));
        Try("serializable from runtime NullReferenceException", () => Sunk("Serializable").Invoke(null, new object[] { nullRef }));
        Try("disposable from CancellationTokenSource", () => Sunk("Disposes").Invoke(null, new object[] { source }));
        Try("disposable from SemaphoreSlim", () => Sunk("Disposes").Invoke(null, new object[] { semaphore }));
        Try("disposable from ManualResetEvent", () => Sunk("Disposes").Invoke(null, new object[] { manual }));
        Try("disposable from AutoResetEvent", () => Sunk("Disposes").Invoke(null, new object[] { auto }));
        Try("disposable from ManualResetEventSlim", () => Sunk("Disposes").Invoke(null, new object[] { slim }));
        Try("disposable from Timer", () => Sunk("Disposes").Invoke(null, new object[] { timer }));
        Try("disposable from Task", () => Sunk("Disposes").Invoke(null, new object[] { task }));
        Try("async from Task", () => Sunk("Async").Invoke(null, new object[] { task }));
        Try("remote from ManualResetEvent", () => Sunk("Remote").Invoke(null, new object[] { manual }));
        Try("remote from Timer", () => Sunk("Remote").Invoke(null, new object[] { timer }));
        Try("remote from ManualResetEventSlim", () => Sunk("Remote").Invoke(null, new object[] { slim }));
        Try("critical from Thread", () => Sunk("Critical").Invoke(null, new object[] { thread }));
        Try("critical from SemaphoreSlim", () => Sunk("Critical").Invoke(null, new object[] { semaphore }));
        Try("system from runtime NullReferenceException", () => Sunk("SystemFault").Invoke(null, new object[] { nullRef }));
        Try("system from runtime IndexOutOfRangeException", () => Sunk("SystemFault").Invoke(null, new object[] { index }));
        Try("system from runtime InvalidCastException", () => Sunk("SystemFault").Invoke(null, new object[] { cast }));
        Try("system from Exception", () => Sunk("SystemFault").Invoke(null, new object[] { plain }));
        Try("arithmetic from runtime OverflowException", () => Sunk("Arithmetic").Invoke(null, new object[] { overflow }));
        Try("arithmetic from runtime DivideByZeroException", () => Sunk("Arithmetic").Invoke(null, new object[] { divide }));
        Try("cloneable from CultureInfo", () => Sunk("Cloneable").Invoke(null, new object[] { CultureInfo.InvariantCulture }));
        Try("cloneable from NumberFormatInfo", () => Sunk("Cloneable").Invoke(null, new object[] { NumberFormatInfo.InvariantInfo }));
        Try("cloneable from TextInfo", () => Sunk("Cloneable").Invoke(null, new object[] { CultureInfo.InvariantCulture.TextInfo }));
        Try("cloneable from DateTimeFormatInfo", () => Sunk("Cloneable").Invoke(null, new object[] { DateTimeFormatInfo.InvariantInfo }));
        Try("provider from CultureInfo", () => Sunk("Provider").Invoke(null, new object[] { CultureInfo.InvariantCulture }));
        Try("provider from NumberFormatInfo", () => Sunk("Provider").Invoke(null, new object[] { NumberFormatInfo.InvariantInfo }));
        Try("constructor from SemaphoreSlim", () => ((DisposableHolder)typeof(DisposableHolder)
            .GetConstructor(new[] { typeof(IDisposable) })!.Invoke(new object[] { semaphore })).Held);
        Try("property from CancellationTokenSource", () =>
        {
            typeof(Sink).GetProperty("Disposable")!.SetValue(null, source);
            return Sink.Disposable == source;
        });

        // -- Receiver checks: an interface-declared member invoked on the handles. The
        // runtime-held bases (Exception, WaitHandle, MarshalByRefObject) carry no member
        // rows, so IDisposable.Dispose is the member reflection can reach. --
        MethodInfo dispose = typeof(IDisposable).GetMethod("Dispose")!;
        Try("Dispose on Holder", () => dispose.Invoke(new Holder(), null));
        Try("Dispose on SemaphoreSlim", () => dispose.Invoke(semaphore, null));
        Try("Dispose on ManualResetEventSlim", () => dispose.Invoke(slim, null));
        Try("Dispose on CancellationTokenSource", () => dispose.Invoke(source, null));
        Try("Dispose on Timer", () => dispose.Invoke(timer, null));
        Try("Dispose on ManualResetEvent", () => dispose.Invoke(manual, null));
        Try("ManualResetEvent after Dispose", () => manual.WaitOne(0));
        Try("Dispose on AutoResetEvent", () => dispose.Invoke(auto, null));
        Try("AutoResetEvent after Dispose", () => auto.WaitOne(0));

        // -- `using` and an IDisposable-typed Dispose. WaitOne after either proves the
        // WaitHandle family ran its real Dispose rather than a no-op. --
        Try("using SemaphoreSlim", () =>
        {
            using (var s = new SemaphoreSlim(1))
            {
                s.Wait();
                s.Release();
            }
            return "disposed";
        });
        Try("using ManualResetEventSlim", () =>
        {
            using (var e = new ManualResetEventSlim(false))
                e.Set();
            return "disposed";
        });
        Try("using CancellationTokenSource", () =>
        {
            using (var c = new CancellationTokenSource())
                c.Cancel();
            return "disposed";
        });
        var usedManual = new ManualResetEvent(true);
        Try("using ManualResetEvent", () =>
        {
            using (usedManual)
                return usedManual.WaitOne(0);
        });
        Try("ManualResetEvent after using", () => usedManual.WaitOne(0));
        var usedAuto = new AutoResetEvent(true);
        Try("using AutoResetEvent", () =>
        {
            using (usedAuto)
                return usedAuto.WaitOne(0);
        });
        Try("AutoResetEvent after using", () => usedAuto.WaitOne(0));
        Try("IDisposable SemaphoreSlim", () => Release(new SemaphoreSlim(1)));
        Try("IDisposable ManualResetEventSlim", () => Release(new ManualResetEventSlim(true)));
        Try("IDisposable CancellationTokenSource", () => Release(new CancellationTokenSource()));
        var typedManual = new ManualResetEvent(true);
        Try("IDisposable ManualResetEvent", () => Release(typedManual));
        Try("ManualResetEvent after IDisposable", () => typedManual.WaitOne(0));
        var typedAuto = new AutoResetEvent(true);
        Try("IDisposable AutoResetEvent", () => Release(typedAuto));
        Try("AutoResetEvent after IDisposable", () => typedAuto.WaitOne(0));
        Console.WriteLine("runtime handle relations end");
    }
}
