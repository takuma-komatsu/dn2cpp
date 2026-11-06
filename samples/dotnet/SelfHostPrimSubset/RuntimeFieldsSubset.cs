using System;
using System.Globalization;
using System.Reflection;
namespace RuntimeFieldsSubset;
internal static class Program
{
 public class Target { public int Value=17; public static int StaticValue=23; }
 public class Generic<T>{}
 private delegate bool Query(Type type,Span<nint> buffer,ref int count);
 private static bool Direct(Type type,Span<nint> buffer,ref int count)=>throw new InvalidOperationException("fixture missing");
 private static bool Pointer(Type type,Span<nint> buffer,ref int count)=>throw new InvalidOperationException("fixture missing");
 private static Query Address()=>throw new InvalidOperationException("fixture missing");
 private static bool DelegateRoute(Type type,Span<nint> buffer,ref int count)=>Address()(type,buffer,ref count);
 internal static void Run(){CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;CultureInfo.CurrentUICulture=CultureInfo.InvariantCulture;Console.WriteLine("== Runtime handle fields boundary ==");var target=new Target();Console.WriteLine("fields-public-count:"+(typeof(Target).GetFields(BindingFlags.Public|BindingFlags.Instance|BindingFlags.Static|BindingFlags.DeclaredOnly).Length==2));Console.WriteLine("fields-public-value:"+((int)typeof(Target).GetField("Value").GetValue(target)==17));Console.WriteLine("Runtime handle fields boundary end");}
 private static unsafe Type[] Inputs()=>new[]{typeof(Generic<>).GetGenericArguments()[0],typeof(int*),typeof(int).MakeByRefType(),typeof(delegate*<int,void>),typeof(object),typeof(Target),typeof(int[]),typeof(Generic<int>),null};
 private static void Check(string label,Query query)
 {
  var types=Inputs();for(int i=0;i<types.Length;i++)for(int length=0;length<=2;length+=2){nint[] cells={11,22,33};int count=71;bool ok=false;
   try{bool result=query(types[i],cells.AsSpan(0,length),ref count);ok=i<4 && result && count==0;}
   catch(Exception e){ok=count==71 && (i==8?e is NullReferenceException:i>=4 && e is PlatformNotSupportedException && e.Message=="RuntimeTypeHandle.GetFields requires CoreCLR field descriptors. Use Type.GetFields instead.");}
   Console.WriteLine("fields-"+label+"-"+i+"-"+length+":"+(ok && cells[0]==11 && cells[1]==22 && cells[2]==33));
  }
 }
 private static int argumentOrder;
 private static unsafe Type TypeArgument(){argumentOrder=1;return typeof(int*);}
 private static Span<nint> BufferArgument(nint[] cells){argumentOrder=argumentOrder*10+2;return cells.AsSpan();}
 private static ref int CountArgument(ref int count){argumentOrder=argumentOrder*10+3;return ref count;}
 private static void ArgumentChecks()
 {
  argumentOrder=0;int count=71;nint[] cells={11,22};
  bool result=Direct(TypeArgument(),BufferArgument(cells),ref CountArgument(ref count));
  Console.WriteLine("fields-argument-evaluation:"+(result && argumentOrder==123 && count==0 && cells[0]==11 && cells[1]==22));
 }
 private static void ReflectionChecks()
 {
  var method=Address().Method;Console.WriteLine("fields-reflection-shape:"+(method.ReturnType==typeof(bool) && method.GetParameters().Length==3 && method.GetParameters()[1].ParameterType==typeof(Span<nint>)));
  try{method.Invoke(null,new object[]{typeof(int).MakePointerType(),null,71});Console.WriteLine("fields-reflection-refusal:accepted");}
  catch(Exception e){Console.WriteLine("fields-reflection-refusal:"+e.GetType().Name+":"+e.Message);}
 }
 internal static void RunArtifactChecks(string[] args){Run();Check("direct",Direct);Check("calli",Pointer);Check("delegate",DelegateRoute);ArgumentChecks();ReflectionChecks();}
 internal static void RunReflectionChecks(string[] args){Run();ReflectionChecks();}
}
