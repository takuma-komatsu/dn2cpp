using System;
using System.Globalization;
using System.Reflection;
namespace RuntimeIntroducedMethodsSubset;
internal static class Program
{
    private delegate object Factory(Type type);
    internal static void Run()
    {
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;CultureInfo.CurrentUICulture=CultureInfo.InvariantCulture;
        Console.WriteLine("== Introduced method factory contract ==");
        var type=typeof(RuntimeMethodEnumerationSubset.Program.Target);
        var target=new RuntimeMethodEnumerationSubset.Program.Target();
        Console.WriteLine("introduced-factory-public-methods:"+(type.GetMethods(BindingFlags.Public|BindingFlags.Instance|BindingFlags.Static|BindingFlags.DeclaredOnly).Length==2));
        Console.WriteLine("introduced-factory-public-ctors:"+(type.GetConstructors().Length==1));
        Console.WriteLine("introduced-factory-public-invoke:"+((int)type.GetMethod("Read").Invoke(target,null)==37));
        Console.WriteLine("introduced-factory-public-delegate:"+(((Func<int>)type.GetMethod("Read").CreateDelegate(typeof(Func<int>),target))()==37));
        Console.WriteLine("Introduced method factory contract end");
    }
}
