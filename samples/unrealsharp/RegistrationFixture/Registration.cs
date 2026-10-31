using System.Runtime.CompilerServices;
using UnrealSharp.Plugins;

namespace RegistrationFixture;

internal static class Registration
{
    [ModuleInitializer]
    internal static void Initialize() => Dn2CppBootstrap.NotifyInitializer();
}
