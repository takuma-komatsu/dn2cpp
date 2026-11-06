using System;
using System.Globalization;
using System.Reflection;

namespace CustomAttributeConstructorSubset;

public static class ConstructorChecks
{
    public static int ConstructorCalls;
    [AttributeUsage(AttributeTargets.All,AllowMultiple=true)] public sealed class MarkAttribute:Attribute
    {
        public MarkAttribute(int value) { ConstructorCalls++; }
        public MarkAttribute(string value) { ConstructorCalls++; }
    }
    [Mark(3),Mark("x")] public class Tagged { }
    public class BaseData:CustomAttributeData { }
    public class OverrideData:CustomAttributeData
    {
        public override ConstructorInfo Constructor=>Chosen;
        public virtual int UnrelatedVirtual()=>91;
    }
    // This declaration is visible to planning, but no allocation or reflection root uses it.
    public class UnallocatedData:CustomAttributeData
    {
        public override ConstructorInfo Constructor=>throw new InvalidOperationException("Unallocated constructor override was rooted");
    }
    public class FurtherData:OverrideData { public override ConstructorInfo Constructor=>StringChosen; }
    static readonly ConstructorInfo StringChosen=typeof(MarkAttribute).GetConstructor(new[]{typeof(string)});
    static readonly ConstructorInfo Chosen=typeof(MarkAttribute).GetConstructor(new[]{typeof(int)});
    public delegate ConstructorInfo Bound();
    static IntPtr BaseFunction()=>IntPtr.Zero;
    static ConstructorInfo BaseDirect(CustomAttributeData data)=>null;
    static ConstructorInfo BasePointer(CustomAttributeData data)=>null;
    static Bound BaseAddress(CustomAttributeData data)=>()=>null;
    static ConstructorInfo ReboundPointer(CustomAttributeData selected,CustomAttributeData receiver)=>selected.Constructor;
    static ConstructorInfo Virtual(CustomAttributeData data)=>data.Constructor;
    static ConstructorInfo VirtualPointer(CustomAttributeData data)=>data.Constructor;
    static Bound VirtualAddress(CustomAttributeData data)=>()=>data.Constructor;
    static MethodInfo Getter()=>typeof(CustomAttributeData).GetProperty("Constructor").GetMethod;
    static void Culture() { CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;CultureInfo.CurrentUICulture=CultureInfo.InvariantCulture; }
    static void Check(string label,bool value) {Console.WriteLine("cad-constructor-"+label+":"+value);if(!value)throw new InvalidOperationException(label);}
    public static void Run()
    {
        Culture();Console.WriteLine("== CustomAttributeData constructor boundary ==");var rows=CustomAttributeData.GetCustomAttributes(typeof(Tagged));
        Check("data-types",rows.Count==2&&rows[0].AttributeType==typeof(MarkAttribute)&&rows[1].AttributeType==typeof(MarkAttribute));
        Check("data-no-construction",ConstructorCalls==0);Console.WriteLine("CustomAttributeData constructor boundary end");
    }
    public static void RunConstructorOracle()
    {
        Culture();var rows=CustomAttributeData.GetCustomAttributes(typeof(Tagged));int i=0;
        foreach(CustomAttributeData row in rows)
        {
            var expected=typeof(MarkAttribute).GetConstructor(new[]{i==0?typeof(int):typeof(string)});
            Check("clr-exact-"+i,row.Constructor.MetadataToken==expected.MetadataToken&&row.Constructor.GetParameters()[0].ParameterType==(i==0?typeof(int):typeof(string)));
            Check("clr-invoke-"+i,((ConstructorInfo)Getter().Invoke(row,null)).MetadataToken==expected.MetadataToken);i++;
        }
        Check("clr-no-construction",ConstructorCalls==0);CustomChecks();
    }
    static void ReflectionNull()
    {
        bool rejected=false;
        try { Getter().Invoke(null,null); } catch(TargetException) { rejected=true; }
        Check("reflection-null-target",rejected);
    }
    static void CustomChecks()
    {
        ReflectionNull();var empty=new BaseData();var custom=new OverrideData();
        Check("custom-base",Virtual(empty)==null&&VirtualPointer(empty)==null&&VirtualAddress(empty)()==null);
        Check("custom-override",ReferenceEquals(Virtual(custom),Chosen)&&ReferenceEquals(VirtualPointer(custom),Chosen)&&ReferenceEquals(VirtualAddress(custom)(),Chosen));
        Check("custom-reflection",Getter().Invoke(empty,null)==null&&ReferenceEquals(Getter().Invoke(custom,null),Chosen));
        Check("custom-method",VirtualAddress(custom).Method.DeclaringType==typeof(OverrideData));
        SelectionChecks();
    }
    static void SelectionChecks()
    {
        var custom=new OverrideData();var further=new FurtherData();
        Check("selection-base",ReboundPointer(new BaseData(),custom)==null);
        Check("selection-override",ReferenceEquals(ReboundPointer(custom,further),Chosen));
        Check("selection-control-string",ReferenceEquals(Virtual(further),StringChosen));
        bool nullSelected=false;
        try { ReboundPointer(null,custom); } catch(NullReferenceException) { nullSelected=true; }
        Check("selection-null",nullSelected);
    }
    public static void RunBaseOracle()
    {
        Culture();BaseChecks();
    }
    static void BaseChecks()
    {
        var canonical=CustomAttributeData.GetCustomAttributes(typeof(Tagged))[0];var custom=new OverrideData();
        Check("base-direct",BaseDirect(canonical)==null&&BaseDirect(custom)==null&&BaseDirect(null)==null);
        Check("base-calli",BasePointer(canonical)==null&&BasePointer(custom)==null&&BasePointer(null)==null);
        Check("base-delegate",BaseAddress(canonical)()==null&&BaseAddress(custom)()==null);
        Check("base-function",BaseFunction()!=IntPtr.Zero);
        Check("base-method",BaseAddress(canonical).Method.MetadataToken==Getter().MetadataToken&&BaseAddress(custom).Method.MetadataToken==Getter().MetadataToken);
        Check("base-dynamic",BaseAddress(canonical).DynamicInvoke(null)==null&&BaseAddress(custom).DynamicInvoke(null)==null);
        var closedNull=(Bound)Getter().CreateDelegate(typeof(Bound),null);
        Check("closed-null-base",closedNull()==null&&closedNull.DynamicInvoke(null)==null);
    }
    static bool Refused(Func<ConstructorInfo> action,bool wrapped=false)
    {
        try {action();return false;}
        catch(PlatformNotSupportedException ex) {return !wrapped&&ex.Message=="CustomAttributeData.Constructor: constructor identity is not retained";}
        catch(TargetInvocationException ex) {return wrapped&&ex.InnerException is PlatformNotSupportedException&&ex.InnerException.Message=="CustomAttributeData.Constructor: constructor identity is not retained";}
    }
    static void Policy()
    {
        var rows=CustomAttributeData.GetCustomAttributes(typeof(Tagged));int i=0;
        foreach(CustomAttributeData row in rows)
        {
            const string argumentsMessage="CustomAttributeData exposes AttributeType only in AOT-compiled code (constructor/named arguments are not recorded)";
            bool ctorArgs=false,namedArgs=false;
            try { _=row.ConstructorArguments; } catch(PlatformNotSupportedException ex) { ctorArgs=ex.Message==argumentsMessage; }
            try { _=row.NamedArguments; } catch(PlatformNotSupportedException ex) { namedArgs=ex.Message==argumentsMessage; }
            Check("existing-data-"+i,row.AttributeType==typeof(MarkAttribute)&&row.ToString().Contains("MarkAttribute")&&ctorArgs&&namedArgs);
            bool methodRefused=false;
            try { _=VirtualAddress(row).Method; }
            catch(PlatformNotSupportedException ex) { methodRefused=ex.Message=="Delegate.Method: the runtime attribute-data constructor override is not retained"; }
            Check("canonical-method-"+i,methodRefused);
            Check("canonical-virtual-"+i,Refused(()=>Virtual(row)));
            Check("canonical-calli-"+i,Refused(()=>VirtualPointer(row)));
            Check("canonical-delegate-"+i,Refused(()=>VirtualAddress(row)()));
            Check("canonical-dynamic-"+i,Refused(()=>(ConstructorInfo)VirtualAddress(row).DynamicInvoke(null),true));
            Check("canonical-reflection-"+i,Refused(()=>(ConstructorInfo)Getter().Invoke(row,null),true));i++;
        }
        Check("selection-canonical-custom",Refused(()=>ReboundPointer(rows[0],new OverrideData())));
        bool direct=false,pointer=false,address=false;
        try{Virtual(null);}catch(NullReferenceException){direct=true;}
        try{VirtualPointer(null);}catch(NullReferenceException){pointer=true;}
        try{VirtualAddress(null);}catch(NullReferenceException){address=true;}
        Check("null-virtual",direct&&pointer&&address);Check("policy-no-construction",ConstructorCalls==0);
    }
    public static void RunArtifact() {Run();BaseChecks();CustomChecks();Policy();}
    public static void RunReflectionArtifact()
    {
        Culture();ReflectionNull();var rows=CustomAttributeData.GetCustomAttributes(typeof(Tagged));int i=0;
        foreach(CustomAttributeData row in rows)Check("canonical-reflection-"+(i++),Refused(()=>(ConstructorInfo)Getter().Invoke(row,null),true));
        Check("custom-reflection",Getter().Invoke(new BaseData(),null)==null&&ReferenceEquals(Getter().Invoke(new OverrideData(),null),Chosen));
    }
}
