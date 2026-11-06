using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DtfICalendarSetterSubset;

internal static class Program
{
    private delegate void Query(DateTimeFormatInfo receiver, Calendar value);
    private static void Direct(DateTimeFormatInfo receiver, Calendar value) => receiver.Calendar = value;
    private static void Virtual(DateTimeFormatInfo receiver, Calendar value) => receiver.Calendar = value;
    private static void Pointer(DateTimeFormatInfo receiver, Calendar value) => receiver.Calendar = value;
    private static Action<Calendar> Address(DateTimeFormatInfo receiver) => value => receiver.Calendar = value;
    private static void DelegateRoute(DateTimeFormatInfo receiver, Calendar value) => Address(receiver)(value);
    private static MethodInfo Metadata() => typeof(DateTimeFormatInfo).GetMethod("set_Calendar")!;
    private static void Reflect(DateTimeFormatInfo receiver, Calendar value) => Metadata().Invoke(receiver, new object[] { value });
    private static Calendar NativeCalendar() => (Calendar)RuntimeHelpers.GetUninitializedObject(typeof(GregorianCalendar));
    private static int trace;

    private static void PinCulture()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }

    private static void Check(string name, bool result)
    {
        Console.WriteLine("dtfi-calendar-setter-" + name + ":" + result);
        if (!result) throw new InvalidOperationException(name);
    }

    private static Exception Observe(Query query, DateTimeFormatInfo receiver, Calendar value, bool reflection)
    {
        Exception observed = null!;
        try { query(receiver, value); }
        catch (Exception error) { observed = error; }
        if (reflection)
        {
            Check("wrapped", observed is TargetInvocationException);
            observed = ((TargetInvocationException)observed).InnerException!;
        }
        return observed;
    }

    private static void Cases(Query query, string label, bool reflection, bool closedDelegate = false)
    {
        var readOnly = DateTimeFormatInfo.InvariantInfo;
        var mutable = (DateTimeFormatInfo)readOnly.Clone();
        mutable.DateSeparator = "*";
        var calendar = NativeCalendar();
        Check(label + "-real-calendar-type", calendar.GetType() == typeof(GregorianCalendar));
        foreach (Calendar value in new Calendar[] { null!, calendar })
        {
            var error = Observe(query, readOnly, value, reflection);
            Check(label + (value is null ? "-readonly-null" : "-readonly-value"),
                error is InvalidOperationException && error.Message == "Instance is read-only.");
        }
        var missing = Observe(query, mutable, null!, reflection);
        Check(label + "-mutable-null", missing is ArgumentNullException argument && argument.ParamName == "value");
        var refused = Observe(query, mutable, calendar, reflection);
        Check(label + "-mutable-value", refused is PlatformNotSupportedException
            && refused.Message == "DateTimeFormatInfo.Calendar is not supported");
        Check(label + "-separator-unchanged", mutable.DateSeparator == "*");
        mutable.DateSeparator = "!";
        Check(label + "-separator-neighbor", mutable.DateSeparator == "!");
        if (!reflection && !closedDelegate)
        {
            Check(label + "-null-null", Observe(query, null!, null!, false) is NullReferenceException);
            Check(label + "-null-value", Observe(query, null!, calendar, false) is NullReferenceException);
        }
    }

    private static DateTimeFormatInfo Receiver()
    {
        trace = trace * 10 + 1;
        return null!;
    }

    private static Calendar Value(bool throws)
    {
        trace = trace * 10 + 2;
        if (throws) throw new FormatException("value producer");
        return null!;
    }

    // Rewritten with producers immediately feeding the exact target, not a wrapper.
    private static void ProducerRoute(bool throws) => Receiver().Calendar = Value(throws);

    private static DateTimeFormatInfo ThrowingReceiver()
    {
        trace = trace * 10 + 1;
        throw new FormatException("receiver producer");
    }

    private static void ProducerReceiverThrows() => ThrowingReceiver().Calendar = Value(false);

    private static void Producers()
    {
        foreach (bool throws in new[] { false, true })
        {
            trace = 0;
            Exception observed = null!;
            try { ProducerRoute(throws); }
            catch (Exception error) { observed = error; }
            Check(throws ? "producer-throws" : "producer-null", trace == 12
                && (throws ? observed is FormatException && observed.Message == "value producer" : observed is NullReferenceException));
        }
        trace = 0;
        Exception receiverError = null!;
        try { ProducerReceiverThrows(); }
        catch (Exception error) { receiverError = error; }
        Check("receiver-producer-throws", trace == 1 && receiverError is FormatException
            && receiverError.Message == "receiver producer");
    }

    public static void RunReflectionChecks()
    {
        PinCulture();
        Cases(Reflect, "reflection", true);
        Check("reflection-null-target", Observe(Reflect, null!, null!, false) is TargetException);
        var method = Metadata();
        Check("reflection-abi", method.ReturnType == typeof(void)
            && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(Calendar));
        Console.WriteLine("dtfi-calendar-setter-reflection-end");
    }

    public static void RunDirectChecks()
    {
        PinCulture();
        Cases(Direct, "direct", false);
        Producers();
        Console.WriteLine("dtfi-calendar-setter-direct-end");
    }

    public static void RunMinimal()
    {
        PinCulture();
        bool observed = false;
        try { DateTimeFormatInfo.InvariantInfo.Calendar = null!; }
        catch (InvalidOperationException error) { observed = error.Message == "Instance is read-only."; }
        Check("minimal", observed);
    }

    public static void Run()
    {
        PinCulture();
        Console.WriteLine("== DTFI Calendar setter native capability ==");
        RunDirectChecks();
        Cases(Virtual, "callvirt", false);
        Cases(Pointer, "calli", false);
        // Closed-null delegate binding is separate from invoking the setter.
        Cases(DelegateRoute, "delegate", false, true);
        var mutable = (DateTimeFormatInfo)DateTimeFormatInfo.InvariantInfo.Clone();
        var method = Address(mutable).Method;
        Check("delegate-abi", method.ReturnType == typeof(void)
            && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(Calendar));
        RunReflectionChecks();
        Console.WriteLine("DTFI Calendar setter native capability end");
    }

    // Public CLR oracle only: successful mutation is deliberately outside native policy.
    public static void RunPublicOracle()
    {
        PinCulture();
        var readOnly = DateTimeFormatInfo.InvariantInfo;
        var mutable = (DateTimeFormatInfo)readOnly.Clone();
        var calendar = new GregorianCalendar();
        Check("public-null-receiver", Observe(Direct, null!, null!, false) is NullReferenceException);
        Check("public-readonly-null", Observe(Direct, readOnly, null!, false) is InvalidOperationException);
        Check("public-readonly-value", Observe(Direct, readOnly, calendar, false) is InvalidOperationException);
        Check("public-mutable-null", Observe(Direct, mutable, null!, false) is ArgumentNullException error && error.ParamName == "value");
        mutable.Calendar = calendar;
        Check("public-mutable-success", ReferenceEquals(mutable.Calendar, calendar));
        mutable.Calendar = calendar;
        Check("public-same-success", ReferenceEquals(mutable.Calendar, calendar));
        Console.WriteLine("DTFI Calendar setter public oracle end");
    }
}
