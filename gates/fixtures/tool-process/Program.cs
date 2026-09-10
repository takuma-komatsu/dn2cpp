using System.Globalization;
using Dn2Cpp;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
if (args.Length > 0 && args[0] == "--child")
{
    for (int i = 1; i < args.Length; i++)
        Console.WriteLine("child argument " + i + ": [" + args[i] + "]");
    Console.Error.WriteLine("child stderr");
    return 23;
}
if (args.Length != 1)
    throw new ArgumentException("Pass the companion apphost path.");
string[] childArguments = ["--child", "", "two words", "日本語", "a\"b", "end\\", "\\\"", ";$()`&|"];
Console.WriteLine("child exit " + ToolProcess.Run(args[0], childArguments));
try
{
    ToolProcess.Run(args[0] + ".missing", []);
    throw new Exception("The missing executable was accepted.");
}
catch (InvalidOperationException)
{
    Console.WriteLine("launch failure caught");
}
try
{
    ToolProcess.Run(args[0], ["embedded\0nul"]);
    throw new Exception("The NUL argument was accepted.");
}
catch (ArgumentException)
{
    Console.WriteLine("NUL argument rejected");
}
Console.WriteLine("tool process complete");
return 0;
