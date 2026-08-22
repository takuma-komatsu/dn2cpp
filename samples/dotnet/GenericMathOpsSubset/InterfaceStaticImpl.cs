using System;

namespace GenericMathInterfaceStaticImpl;

// An INTERFACE as the static-abstract-constrained type argument (the serializer
// union-registration shape): C# admits it only when the interface itself carries
// the explicit static impls, so the constrained call must resolve against the
// interface's own .override rows — and a static-virtual DEFAULT the interface
// leaves unimplemented must still bind the member's default body.
internal interface IRegister
{
    static abstract string Register();
    static virtual string Describe() => "default";
}

internal interface IWidget : IRegister
{
    static string IRegister.Register() => "widget";
}

internal interface IGadget : IRegister
{
    static string IRegister.Register() => "gadget";
    static string IRegister.Describe() => "gadget-desc";
}

internal static class InterfaceStaticImpl
{
    static string RegOf<T>() where T : IRegister => T.Register();
    static string DescOf<T>() where T : IRegister => T.Describe();

    internal static void __GateEntry()
    {
        Console.WriteLine("== Interface as static-abstract type argument ==");
        Console.WriteLine($"widget reg   {RegOf<IWidget>()}");
        Console.WriteLine($"gadget reg   {RegOf<IGadget>()}");
        Console.WriteLine($"widget desc  {DescOf<IWidget>()}");
        Console.WriteLine($"gadget desc  {DescOf<IGadget>()}");
    }
}
