using System.Globalization;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
#if POINTER_ARRAY_FROM_ONLY
ReflectInvokeValidationSubset.Program.RunPointerArrayFromArrayType();
#elif POINTER_ARRAY_CREATE_ONLY
ReflectInvokeValidationSubset.Program.RunPointerArrayCreateInstance();
#elif POINTER_ARRAY_JAGGED_ONLY
ReflectInvokeValidationSubset.Program.RunPointerArrayJagged();
#else
ReflectInvokeValidationSubset.Program.RunPointerArrays();
#endif
