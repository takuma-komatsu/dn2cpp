using System;
using System.Globalization;
using System.Reflection;
using TrimReflectLib;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

Console.WriteLine("== isolated stripped member metadata ==");
object widget = Factory.Make();
ILibThing thing = (ILibThing)widget;
Console.WriteLine("direct interface=" + thing.Twice(21) + "/" + thing.Tag);
Read("widget methods", () => "any=" + (widget.GetType().GetMethods().Length > 0));
Read("widget field", () => "found=" + (widget.GetType().GetField("Amount") != null));
Read("widget property", () => "found=" + (widget.GetType().GetProperty("Tag") != null));
Read("widget name bind", () => ((Func<int, int>)Delegate.CreateDelegate(typeof(Func<int, int>), widget, "Twice"))(21).ToString());
object gadget = Factory.MakeGadget();
Read("gadget methods", () => "any=" + (gadget.GetType().GetMethods().Length > 0));
Read("gadget field", () => "found=" + (gadget.GetType().GetField("Size") != null));
object box = Factory.MakeBox();
Read("box methods", () => "any=" + (box.GetType().GetMethods().Length > 0));
Read("box field", () => "found=" + (box.GetType().GetField("Value") != null));
Console.WriteLine("isolated stripped member metadata end");

static void Read(string label, Func<string> read)
{
    try
    {
        Console.WriteLine(label + " -> " + read());
    }
    catch (PlatformNotSupportedException exception)
    {
        Console.WriteLine(label + " -> PNSE: " + exception.Message);
    }
}
