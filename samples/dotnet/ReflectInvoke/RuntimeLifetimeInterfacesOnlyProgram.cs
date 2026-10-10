using System.Globalization;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
#if SAFEHANDLE_GROUPS
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
