using System;
using System.Globalization;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
DelegateVirtualIdentitySubset.Program.Run();
#if !DELEGATE_IDENTITY_IL_ONLY
DelegateVirtualIdentitySubset.Program.RunReflected();
#endif
DelegateVirtualIdentitySubset.Program.RunRuntimeOwned();
if (args.Length != 0 && args[0] == "before-type-only-exception")
    return;
Console.WriteLine("== type-only generic exception identity ==");
Console.WriteLine("exception type=" + typeof(ReflectReturnLib.TypeOnlyException<string>).Name);
Func<int, int> identity = Identity;
Console.WriteLine("separate delegate method=" + identity.Method.Name + "/" + identity(7));
Console.WriteLine("type-only generic exception identity end");

static int Identity(int value) => value;
