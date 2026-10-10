using System.Globalization;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
DelegateVirtualIdentitySubset.Program.Run();
#if !DELEGATE_IDENTITY_IL_ONLY
DelegateVirtualIdentitySubset.Program.RunReflected();
#endif
