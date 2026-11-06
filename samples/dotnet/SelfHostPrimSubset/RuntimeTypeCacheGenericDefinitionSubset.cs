using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace RuntimeTypeCacheGenericDefinitionSubset;

public static class DefinitionChecks
{
    public class G<T> { public class N<U> { } }
    public delegate Type Getter();
    delegate Type Route(object cache);
    static object NewCache(Type owner,Type stored)=>throw new InvalidOperationException("Native cache fixture not initialized");
    static Type Stored(object cache)=>throw new InvalidOperationException("Native cache fixture not initialized");
    static Type Direct(object cache)=>throw new InvalidOperationException("Native cache fixture not initialized");
    static Type Virtual(object cache)=>throw new InvalidOperationException("Native cache fixture not initialized");
    static Type Pointer(object cache)=>throw new InvalidOperationException("Native cache fixture not initialized");
    static Getter Address(object cache)=>throw new InvalidOperationException("Native cache fixture not initialized");
    static Type DelegateRoute(object cache)=>Address(cache)();
    static Type ReflectionRoute(object cache)=> (Type)Address(cache??NewCache(typeof(G<int>),null)).Method.Invoke(cache,null);
    static Type Outer(object cache)=>throw new InvalidOperationException("Native cache fixture not initialized");
    static Type[] Inputs()=>new[]{typeof(G<>),typeof(G<int>),typeof(G<string>),typeof(G<int>.N<string>),typeof(G<>.N<>),typeof(Dictionary<int,string>),typeof(List<int>),typeof(int?)};
    static Type[] Definitions()=>new[]{typeof(G<>),typeof(G<>),typeof(G<>),typeof(G<>.N<>),typeof(G<>.N<>),typeof(Dictionary<,>),typeof(List<>),typeof(Nullable<>)};
    static void Culture(){CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;CultureInfo.CurrentUICulture=CultureInfo.InvariantCulture;}
    static void Check(string label,bool value){Console.WriteLine("cache-generic-definition-"+label+":"+value);if(!value)throw new InvalidOperationException(label);}
    public static void Run()
    {
        Culture();Console.WriteLine("== RuntimeType cache generic definition ==");Type[] input=Inputs(),expected=Definitions();
        for(int i=0;i<input.Length;i++)Check("public-"+i,input[i].GetGenericTypeDefinition()==expected[i]);
        Check("public-same-definition",typeof(G<int>).GetGenericTypeDefinition()==typeof(G<string>).GetGenericTypeDefinition());
        Console.WriteLine("RuntimeType cache generic definition end");
    }
    static bool Throws(Func<Type> action,bool nullInput,bool wrapped=false)
    {
        try{action();return false;}catch(Exception ex){if(wrapped){if(ex is not TargetInvocationException)return false;ex=ex.InnerException;}else if(ex is TargetInvocationException)return false;return nullInput?ex is NullReferenceException:ex is InvalidOperationException;}
    }
    static void Routes(string name,Route route)
    {
        Type[] input=Inputs(),expected=Definitions();
        for(int i=0;i<input.Length;i++)
        {
            object cache=NewCache(input[i],typeof(string));Type result=route(cache);
            Check(name+"-"+i,result==expected[i]&&Stored(cache)==expected[i]);
            Check(name+"-repeat-"+i,route(cache)==result&&Stored(cache)==result);
        }
        int index=0;
        foreach(Type owner in new[]{typeof(int),typeof(G<int>[]),typeof(G<>).GetGenericArguments()[0],null})
        {
            object cache=NewCache(owner,typeof(string));
            Check(name+"-preserve-"+(index++),Throws(()=>route(cache),owner==null,name=="reflection")&&Stored(cache)==typeof(string));
        }
        // A null object never provides a valid managed cache receiver.
        if(name=="reflection")
        {
            bool rejected=false;try{route(null);}catch(TargetException){rejected=true;}
            Check(name+"-null-cache",rejected);
        }
        else if(name=="delegate")
        {
            // ldftn + delegate construction rejects a null instance before invocation.
            bool rejected=false;try{Address(null);}catch(ArgumentException){rejected=true;}
            Check(name+"-null-cache",rejected);
        }
        else Check(name+"-null-cache",Throws(()=>route(null),true));
    }
    static void OuterChecks()
    {
        object cache=NewCache(typeof(G<int>),null);Type first=Outer(cache);
        Check("outer-first",first==typeof(G<>)&&Stored(cache)==first&&Outer(cache)==first);
        cache=NewCache(null,typeof(string));
        Check("outer-short-circuit",Outer(cache)==typeof(string)&&Stored(cache)==typeof(string));
        cache=NewCache(typeof(int),typeof(string));
        Check("outer-invalid-short-circuit",Outer(cache)==typeof(string)&&Stored(cache)==typeof(string));
    }
    public static void RunArtifact(){Run();Routes("direct",Direct);Routes("virtual",Virtual);Routes("calli",Pointer);Routes("delegate",DelegateRoute);Routes("reflection",ReflectionRoute);OuterChecks();}
    public static void RunReflectionArtifact(){Culture();Routes("reflection",ReflectionRoute);}
}
