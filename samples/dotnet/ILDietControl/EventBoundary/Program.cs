using System;
using System.Globalization;
using System.Reflection;

namespace ILDietEventBoundary;

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        var cell = new Cell();
        EventInfo? selected = typeof(Cell).GetEvent("Tick");
        Console.WriteLine("event-found=" + (selected is not null));
        if (selected is not null)
        {
            Action listener = () => Console.WriteLine("tick");
            selected.AddEventHandler(cell, listener);
            cell.Raise();
            selected.RemoveEventHandler(cell, listener);
            cell.Raise();
            Console.WriteLine("event-operations=done");
        }
    }
}

internal sealed class Cell
{
    public event Action? Tick;

    public void Raise() => Tick?.Invoke();
}
