using System;
using System.Globalization;
using System.Reflection;
namespace RuntimeIntroducedMethodsSubset;
internal static class Program
{
    private delegate object Factory(Type type);
    private static object Direct(Type type) => throw new InvalidOperationException("fixture missing");
    private static object Pointer(Type type) => throw new InvalidOperationException("fixture missing");
    private static Factory Address() => throw new InvalidOperationException("fixture missing");
    private static object DelegateRoute(Type type) => Address()(type);
    private static void Discard(Type type) => throw new InvalidOperationException("fixture missing");
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
    private static void Refusal(string route,Type type)
    {
        try
        {
            if(route=="direct") Direct(type);
            else if(route=="calli") Pointer(type);
            else if(route=="delegate") DelegateRoute(type);
            else if(route=="discard") Discard(type);
            else Address().Method.Invoke(null,new object[]{type});
            Console.WriteLine("introduced-factory-"+route+"-"+(type==null?"null":"type")+":accepted");
        }
        catch(Exception error)
        {
            bool wrapped=error is TargetInvocationException;
            Exception leaf=wrapped?error.InnerException:error;
            Console.WriteLine("introduced-factory-"+route+"-"+(type==null?"null":"type")+":"+
                (wrapped==(route=="reflection") && leaf is PlatformNotSupportedException && leaf.Message=="System.RuntimeTypeHandle.GetFirstIntroducedMethod requires CoreCLR method descriptors. Use Type.GetMethods or Type.GetConstructors instead."));
        }
    }
    internal static void RunArtifactChecks(string[] args)
    {
        Run();foreach(string route in new[]{"direct","calli","delegate","discard","reflection"})
        {Refusal(route,typeof(RuntimeMethodEnumerationSubset.Program.Target));Refusal(route,null);}
    }
    // Separate entry body ensures reflection mode cannot accidentally retain direct roots.
    internal static void RunReflectionChecks(string[] args)
    {
        Run();
        foreach(Type type in new Type[]{typeof(RuntimeMethodEnumerationSubset.Program.Target),null})
        {
            bool ok=false;
            try{Address().Method.Invoke(null,new object[]{type});}
            catch(TargetInvocationException e){ok=e.InnerException is PlatformNotSupportedException && e.InnerException.Message=="System.RuntimeTypeHandle.GetFirstIntroducedMethod requires CoreCLR method descriptors. Use Type.GetMethods or Type.GetConstructors instead.";}
            Console.WriteLine("introduced-factory-reflection-"+(type==null?"null":"type")+":"+ok);
        }
    }
}
