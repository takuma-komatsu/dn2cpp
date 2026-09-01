using System.Globalization;
using System.Runtime.InteropServices;

namespace PInvokeFrameworkQCallBad;

internal static class Program
{
    [DllImport("QCall", EntryPoint = "Dn2CppFrameworkQCallAdmissionProbe")]
    private static extern int Probe();

    private static int Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        return Probe();
    }
}
