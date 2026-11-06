using System;
using System.Globalization;
using System.Reflection;
namespace RuntimeInterfacesSubset;
internal static class Program
{
    public interface IA {} public interface IB:IA {} public interface IG<T> {}
    public class Base:IB {} public class Closed<T>:Base,IA,IG<T> {}
    public class Constrained<T> where T:IB {}
    private delegate Type[] Factory(Type type);
    private static Type[] Direct(Type type)=>throw new InvalidOperationException("fixture missing");
    private static Type[] Pointer(Type type)=>throw new InvalidOperationException("fixture missing");
    private static Factory Address()=>throw new InvalidOperationException("fixture missing");
    private static Type[] DelegateRoute(Type type)=>Address()(type);
    private static Type[] Reflect(Type type)=>(Type[])Address().Method.Invoke(null,new object[]{type});
    private static string Rows(Type[] rows){string s="";for(int i=0;i<rows.Length;i++){if(i!=0)s+=";";s+=rows[i].ToString();}return s;}
    private static unsafe Type[] Inputs()=>new[]{typeof(object),typeof(Closed<int>),typeof(Closed<>),typeof(IB),typeof(int[]),typeof(int[,]),typeof(int*),typeof(int).MakeByRefType(),typeof(delegate*<int,void>)};
    internal static void Run()
    {
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;CultureInfo.CurrentUICulture=CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime handle interfaces ==");
        Console.WriteLine("interfaces-public-empty:"+(typeof(object).GetInterfaces().Length==0));
        Type[] parents=typeof(IB).GetInterfaces();
        Console.WriteLine("interfaces-public-parent:"+(parents.Length==1 && parents[0]==typeof(IA)));
        Console.WriteLine("Runtime handle interfaces end");
    }
    private static void RunSequence()
    {
        var inputs=Inputs();for(int i=0;i<inputs.Length;i++)Console.WriteLine("interfaces-sequence-"+i+":"+Rows(inputs[i].GetInterfaces()));
        Console.WriteLine("interfaces-sequence-formal:"+Rows(typeof(Constrained<>).GetGenericArguments()[0].GetInterfaces()));
    }
    internal static void RunOracle(string[] args){Run();RunSequence();}
    private static void Check(string route,Func<Type,Type[]> call)
    {
        var inputs=Inputs();
        for(int i=0;i<inputs.Length;i++)
        {
            Type[] a=call(inputs[i]),b=call(inputs[i]);bool empty=a.Length==0;
            bool shape=empty?ReferenceEquals(a,Array.Empty<Type>()) && a.GetType()==typeof(Type[]):
                a.GetType().GetElementType()==((object)typeof(object)).GetType() && !ReferenceEquals(a,b);
            bool sequence=Rows(a)==Rows(inputs[i].GetInterfaces());
            bool isolated=true;if(!empty){Type first=b[0];a[0]=null;isolated=b[0]==first && call(inputs[i])[0]==first;}
            Console.WriteLine("interfaces-"+route+"-"+i+":"+(shape&&sequence&&isolated));
        }
        var formal=typeof(Constrained<>).GetGenericArguments()[0];
        Console.WriteLine("interfaces-"+route+"-formal:"+ReferenceEquals(call(formal),Array.Empty<Type>()));
        bool nullOk=false;try{call(null);}catch(Exception e){bool wrapped=e is TargetInvocationException;nullOk=wrapped==(route=="reflection") && (wrapped?e.InnerException:e) is NullReferenceException;}
        Console.WriteLine("interfaces-"+route+"-null:"+nullOk);
        Console.WriteLine("interfaces-"+route+"-empty-unmutated:"+(Array.Empty<Type>().GetType()==typeof(Type[]) && Array.Empty<Type>().Length==0));
    }
    internal static void RunArtifactChecks(string[] args){Run();RunSequence();Check("direct",Direct);Check("calli",Pointer);Check("delegate",DelegateRoute);Check("reflection",Reflect);}
    internal static void RunReflectionChecks(string[] args){Run();RunSequence();Check("reflection",Reflect);}
}
