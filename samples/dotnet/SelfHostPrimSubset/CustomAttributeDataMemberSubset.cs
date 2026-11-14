using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace CustomAttributeDataMemberSubset;

public static class DataChecks
{
    public static int ConstructorCalls;
    [AttributeUsage(AttributeTargets.All, Inherited=true)] public sealed class FirstAttribute : Attribute { public FirstAttribute() { ConstructorCalls++; } }
    [AttributeUsage(AttributeTargets.All, Inherited=true)] public sealed class SecondAttribute : Attribute { public SecondAttribute() { ConstructorCalls++; } }
    [First, Second] public class Tagged
    {
        [First, Second] public Tagged() { }
        [First, Second] public int Field;
        [First, Second] public int Property { get; set; }
        [First, Second] public event Action Changed { add { } remove { } }
        [First, Second] public void Method() { }
        public void Empty() { }
    }
    public class Derived : Tagged { }
    public sealed class CustomMember : MemberInfo
    {
        public int Calls;
        public readonly IList<CustomAttributeData> Rows = new List<CustomAttributeData>();
        public override string Name => "Custom";
        public override Type DeclaringType => null;
        public override Type ReflectedType => null;
        public override MemberTypes MemberType => MemberTypes.Custom;
        public override object[] GetCustomAttributes(bool inherit) => Array.Empty<object>();
        public override object[] GetCustomAttributes(Type type, bool inherit) => Array.Empty<object>();
        public override bool IsDefined(Type type, bool inherit) => false;
        public override IList<CustomAttributeData> GetCustomAttributesData() { Calls++; return Rows; }
    }
    public delegate IList<CustomAttributeData> Reader(MemberInfo target);
    static MemberInfo[] Members() => new MemberInfo[] { typeof(Tagged), typeof(Tagged).GetMethod("Method"),
        typeof(Tagged).GetConstructor(Type.EmptyTypes), typeof(Tagged).GetField("Field"), typeof(Tagged).GetProperty("Property"), typeof(Tagged).GetEvent("Changed") };
    static void Culture() { CultureInfo.CurrentCulture=CultureInfo.InvariantCulture; CultureInfo.CurrentUICulture=CultureInfo.InvariantCulture; }
    static void Check(string label,bool value) { Console.WriteLine("cad-member-"+label+":"+value);if(!value)throw new InvalidOperationException(label); }
    static void Rows(string name, Reader reader)
    {
        int index=0;
        foreach(MemberInfo member in Members())
        {
            IList<CustomAttributeData> data=reader(member);
            Check(name+"-row-"+index,data.Count==2&&data[0].AttributeType==typeof(FirstAttribute)&&data[1].AttributeType==typeof(SecondAttribute));
            Check(name+"-readonly-"+index,data.IsReadOnly);
            int enumerated=0;bool ordered=true;
            foreach(CustomAttributeData attribute in data)
            {
                ordered=ordered&&attribute.AttributeType==(enumerated==0?typeof(FirstAttribute):typeof(SecondAttribute));enumerated++;
            }
            Check(name+"-enumeration-"+index,ordered&&enumerated==2);
            bool set=false,add=false,clear=false,remove=false,removeValue=false;
            try { data[0]=data[0]; } catch(NotSupportedException) { set=true; }
            try { data.Add(data[0]); } catch(NotSupportedException) { add=true; }
            try { data.Clear(); } catch(NotSupportedException) { clear=true; }
            try { data.RemoveAt(0); } catch(NotSupportedException) { remove=true; }
            try { data.Remove(data[0]); } catch(NotSupportedException) { removeValue=true; }
            Check(name+"-immutable-"+(index++),set&&add&&clear&&remove&&removeValue&&data.Count==2
                &&data[0].AttributeType==typeof(FirstAttribute)&&data[1].AttributeType==typeof(SecondAttribute));
        }
        var empty=reader(typeof(Tagged).GetMethod("Empty"));
        Check(name+"-empty",empty.Count==0&&empty.IsReadOnly);
        string emptySetter="<none>";
        try { empty[0]=null; } catch(Exception ex) { emptySetter=ex.GetType().FullName; }
        Console.WriteLine("cad-member-"+name+"-empty-setter:"+emptySetter);
        string negativeSetter="<none>";
        try { empty[-1]=null; } catch(Exception ex) { negativeSetter=ex.GetType().FullName; }
        Check(name+"-empty-negative",negativeSetter==emptySetter);
        bool emptyAdd=false,emptyClear=false,emptyRemoveAt=false,emptyRemove=false;
        try { empty.Add(null); } catch(NotSupportedException) { emptyAdd=true; }
        try { empty.Clear(); } catch(NotSupportedException) { emptyClear=true; }
        try { empty.RemoveAt(0); } catch(NotSupportedException) { emptyRemoveAt=true; }
        try { empty.Remove(null); } catch(NotSupportedException) { emptyRemove=true; }
        Check(name+"-empty-mutations",emptyAdd&&emptyClear&&emptyRemoveAt&&emptyRemove);
        Check(name+"-empty-unchanged",empty.Count==0);
        Check(name+"-not-inherited",reader(typeof(Derived)).Count==0);
        Check(name+"-no-construction",ConstructorCalls==0);
        bool nullTarget=false;
        try { reader(null); } catch(ArgumentNullException ex) when(name!="reflection") { nullTarget=ex.ParamName=="target"; }
        catch(TargetInvocationException ex) when(name=="reflection") { nullTarget=ex.InnerException is ArgumentNullException inner&&inner.ParamName=="target"; }
        Check(name+"-null",nullTarget);
    }
    public static void Run() { Culture();Console.WriteLine("== CustomAttributeData member reader ==");Rows("public",CustomAttributeData.GetCustomAttributes);Console.WriteLine("CustomAttributeData member reader end"); }
}
