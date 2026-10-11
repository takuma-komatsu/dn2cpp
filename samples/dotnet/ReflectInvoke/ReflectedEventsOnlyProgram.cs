using System;
using System.Globalization;

internal static class ReflectedEventsOnlyProgram
{
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        if (args.Length > 0 && args[0] == "generic-event-others")
        {
            EventSubset.Program.RunGenericOtherReflection();
            return;
        }
        if (args.Length > 0 && args[0] == "stripped-events")
        {
            var type = TrimReflectLib.Factory.MakeEvent().GetType();
            for (int i = 0; i < 2; i++)
            {
                try { Console.WriteLine("stripped event query: " + (type.GetEvent("Tick") is not null)); }
                catch (System.Exception exception) { Console.WriteLine("stripped event query: " + exception.GetType().Name); }
                try { Console.WriteLine("stripped events query: " + type.GetEvents().Length); }
                catch (System.Exception exception) { Console.WriteLine("stripped events query: " + exception.GetType().Name); }
            }
            return;
        }
        EventSubset.Program.RunReflection();
    }
}
