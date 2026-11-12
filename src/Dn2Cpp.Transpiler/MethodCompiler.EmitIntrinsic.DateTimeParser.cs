namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    // Native DateTime arithmetic has no host leap-second table.
    private void EmitDateTimeLeapSecondSupport() => Push(StackKind.I4, "int32_t", "0");

    private void EmitDateTimeManagedConstructor(MethodInfo constructor)
    {
        var arguments = PopArgs(constructor, hasThis: false);
        string value = NewTemp("Dn2CppDateTime");
        Emit($"{value} = {{}};");
        arguments.Insert(0, $"&{value}");
        Emit($"{GuardEnteredCall(constructor, DirectCall(constructor, arguments))};");
        EmitByRefSlotFixups();
        Push(StackKind.Struct, "Dn2CppDateTime", value);
    }
}
