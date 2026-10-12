using System.Globalization;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
#if PROVIDER_IDENTITY_ONLY
ReflectInvoke.RuntimeHeldInterfacesSubset.Program.RunProviderIdentity();
#elif REFLECTION_ONLY
ReflectInvoke.RuntimeHeldInterfacesSubset.Program.RunReflection();
#elif CONVERTIBLE_PROVIDER_ONLY
ReflectInvoke.RuntimeHeldConvertibleProviderSubset.Run();
#elif REFLECTED_VALUE_ONLY
ReflectInvoke.RuntimeHeldReflectedValueSubset.Run();
#else
ReflectInvoke.RuntimeHeldInterfacesSubset.Program.Run(args.Length == 0 ? "all" : args[0]);
#endif
