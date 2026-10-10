using System.Globalization;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
#if ARRAY_CLONE_MD
ArrayCloneInterfaceSubset.Program.RunTyped();
ArrayCloneInterfaceSubset.Program.RunMd();
#elif ARRAY_CLONE_REFLECTED
ArrayCloneReflectionOnlySubset.Program.Run();
#elif ARRAY_CLONE_CONSTRAINED
ArrayCloneInterfaceSubset.Program.RunConstrained();
#elif ARRAY_CLONE_ALL
ArrayCloneInterfaceSubset.Program.RunAll();
#elif ARRAY_CLONE_REFERENCE
ArrayCloneInterfaceSubset.Program.RunReferences();
#elif ARRAY_CLONE_ALIASES
ArrayCloneInterfaceSubset.Program.RunAliases();
#elif ARRAY_CLONE_NULL
ArrayCloneInterfaceSubset.Program.RunNull();
#elif ARRAY_CLONE_TYPED
ArrayCloneInterfaceSubset.Program.RunTyped();
#elif ARRAY_CLONE_ERASED
ArrayCloneInterfaceSubset.Program.RunErased();
#elif ARRAY_CLONE_INTERFACE_GROUP
ArrayCloneInterfaceSubset.Program.RunInterfaceGroup();
#elif ARRAY_CLONE_CLASS_GROUP
ArrayCloneInterfaceSubset.Program.RunClassGroup();
#elif CLONE_CLASS_ALIASES
RuntimeHandleRelationSubset.Program.RunDelegateCloneClassAliases();
#elif CLONE_CLASS_INVOKE
RuntimeHandleRelationSubset.Program.RunDelegateCloneClassInvocation();
#elif CLONE_CLASS_NULL
RuntimeHandleRelationSubset.Program.RunDelegateCloneClassNull();
#elif CLONE_CLASS_OVERRIDES
RuntimeHandleRelationSubset.Program.RunDelegateCloneClassOverrides();
#elif CLONE_CLASS_OVERRIDE_ONLY
RuntimeHandleRelationSubset.Program.RunDelegateCloneClassOverrideOnly();
#elif CLONE_CLASS_REFLECTED_GENERIC
RuntimeHandleRelationSubset.Program.RunDelegateCloneClassReflectedGeneric();
#elif CLONE_CLASS_REFLECTED_TYPE
RuntimeHandleRelationSubset.Program.RunDelegateCloneClassReflectedType();
#elif CLONE_CLASS_REFLECTED_FIELD
DelegateCloneFieldTypeSubset.Program.Run();
#elif CLONE_CLASS_NATIVE
RuntimeHandleRelationSubset.Program.RunDelegateCloneClassNative();
#elif CLONE_CLASS_ALL
RuntimeHandleRelationSubset.Program.RunDelegateCloneClassGroups();
#elif CLONE_STRING
RuntimeHandleRelationSubset.Program.RunCloneStringGroup();
#elif CLONE_STRING_CALLS
RuntimeHandleRelationSubset.Program.RunCloneStringCalls();
#elif CLONE_DELEGATE
RuntimeHandleRelationSubset.Program.RunCloneDelegates();
#elif CLONE_GROUP
RuntimeHandleRelationSubset.Program.RunCloneDelegateGroup();
#elif CLONE_ORDINARY
RuntimeHandleRelationSubset.Program.RunCloneOrdinary();
#elif CLONE_ALL
RuntimeHandleRelationSubset.Program.RunCloneInterfaces();
#elif SAFEHANDLE_GROUPS
RuntimeHandleRelationSubset.Program.RunSafeHandleGroups();
#elif SAFEHANDLE_INVOCATION
RuntimeHandleRelationSubset.Program.RunSafeHandleGroupInvocation();
#elif SAFEHANDLE_ORDINARY
RuntimeHandleRelationSubset.Program.RunSafeHandleOrdinaryGroups();
#elif SAFEHANDLE_NULL
RuntimeHandleRelationSubset.Program.RunSafeHandleNullGroups();
#elif SAFEHANDLE_ALL
RuntimeHandleRelationSubset.Program.RunSafeHandleMethodGroups();
#elif LIFETIME_SAFE
RuntimeHandleRelationSubset.Program.RunLifetimeSafeOnly();
#elif LIFETIME_WAIT
RuntimeHandleRelationSubset.Program.RunLifetimeWait();
#elif LIFETIME_EVENT
RuntimeHandleRelationSubset.Program.RunLifetimeEvent();
#elif LIFETIME_TASK
RuntimeHandleRelationSubset.Program.RunLifetimeTask();
#elif LIFETIME_DIRECT
RuntimeHandleRelationSubset.Program.RunLifetimeDirect();
#elif LIFETIME_FACTORY_COMPLETED
RuntimeHandleRelationSubset.Program.RunLifetimeFactoryCompleted();
#elif LIFETIME_FACTORY_RESULT
RuntimeHandleRelationSubset.Program.RunLifetimeFactoryResult();
#elif LIFETIME_FACTORY_SOURCE
RuntimeHandleRelationSubset.Program.RunLifetimeFactorySource();
#elif LIFETIME_FACTORY_GENERIC_SOURCE
RuntimeHandleRelationSubset.Program.RunLifetimeFactoryGenericSource();
#elif LIFETIME_FACTORY_ASYNC
RuntimeHandleRelationSubset.Program.RunLifetimeFactoryAsync();
#elif LIFETIME_FACTORY_COLD
RuntimeHandleRelationSubset.Program.RunLifetimeFactoryCold();
#elif LIFETIME_FACTORY_RUN
RuntimeHandleRelationSubset.Program.RunLifetimeFactoryRun();
#elif LIFETIME_FACTORY_ALL
RuntimeHandleRelationSubset.Program.RunLifetimeFactoryAll();
#else
RuntimeHandleRelationSubset.Program.RunLifetimeInterfaces();
#endif
