#nullable enable
// SUBJECT: GetMethods, GetMember and GetMethod report each method once over a
// wide hierarchy, as .NET does: every generic virtual override of a class with
// many of them hides the base method it overrides, and an override hides the
// definition of its slot below a class that introduces many virtual slots.
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ReflectWideHierarchySubset;

class GvmWideBase
{
    public virtual string M00<T>() => "b0"; public virtual string M01<T>() => "b1"; public virtual string M02<T>() => "b2";
    public virtual string M03<T>() => "b3"; public virtual string M04<T>() => "b4"; public virtual string M05<T>() => "b5";
    public virtual string M06<T>() => "b6"; public virtual string M07<T>() => "b7"; public virtual string M08<T>() => "b8";
    public virtual string M09<T>() => "b9"; public virtual string M10<T>() => "b10"; public virtual string M11<T>() => "b11";
    public virtual string M12<T>() => "b12"; public virtual string M13<T>() => "b13"; public virtual string M14<T>() => "b14";
    public virtual string M15<T>() => "b15"; public virtual string M16<T>() => "b16"; public virtual string M17<T>() => "b17";
    public virtual string M18<T>() => "b18"; public virtual string M19<T>() => "b19"; public virtual string M20<T>() => "b20";
    public virtual string M21<T>() => "b21"; public virtual string M22<T>() => "b22"; public virtual string M23<T>() => "b23";
    public virtual string M24<T>() => "b24"; public virtual string M25<T>() => "b25"; public virtual string M26<T>() => "b26";
    public virtual string M27<T>() => "b27"; public virtual string M28<T>() => "b28"; public virtual string M29<T>() => "b29";
    public virtual string M30<T>() => "b30"; public virtual string M31<T>() => "b31"; public virtual string M32<T>() => "b32";
    public virtual string M33<T>() => "b33"; public virtual string M34<T>() => "b34"; public virtual string M35<T>() => "b35";
    public virtual string M36<T>() => "b36"; public virtual string M37<T>() => "b37"; public virtual string M38<T>() => "b38";
    public virtual string M39<T>() => "b39";
}

class GvmWideLeaf : GvmWideBase
{
    public override string M00<T>() => "l0"; public override string M01<T>() => "l1"; public override string M02<T>() => "l2";
    public override string M03<T>() => "l3"; public override string M04<T>() => "l4"; public override string M05<T>() => "l5";
    public override string M06<T>() => "l6"; public override string M07<T>() => "l7"; public override string M08<T>() => "l8";
    public override string M09<T>() => "l9"; public override string M10<T>() => "l10"; public override string M11<T>() => "l11";
    public override string M12<T>() => "l12"; public override string M13<T>() => "l13"; public override string M14<T>() => "l14";
    public override string M15<T>() => "l15"; public override string M16<T>() => "l16"; public override string M17<T>() => "l17";
    public override string M18<T>() => "l18"; public override string M19<T>() => "l19"; public override string M20<T>() => "l20";
    public override string M21<T>() => "l21"; public override string M22<T>() => "l22"; public override string M23<T>() => "l23";
    public override string M24<T>() => "l24"; public override string M25<T>() => "l25"; public override string M26<T>() => "l26";
    public override string M27<T>() => "l27"; public override string M28<T>() => "l28"; public override string M29<T>() => "l29";
    public override string M30<T>() => "l30"; public override string M31<T>() => "l31"; public override string M32<T>() => "l32";
    public override string M33<T>() => "l33"; public override string M34<T>() => "l34"; public override string M35<T>() => "l35";
    public override string M36<T>() => "l36"; public override string M37<T>() => "l37"; public override string M38<T>() => "l38";
    public override string M39<T>() => "l39";
}

class SlotRoot
{
    public virtual int Last() => -1;
}

class SlotMid : SlotRoot
{
    public override int Last() => -2;
}

// Its own slots come first in a derived-to-base walk, ahead of SlotMid's override.
class SlotWide : SlotMid
{
    public virtual int V000() => 0; public virtual int V001() => 1; public virtual int V002() => 2; public virtual int V003() => 3;
    public virtual int V004() => 4; public virtual int V005() => 5; public virtual int V006() => 6; public virtual int V007() => 7;
    public virtual int V008() => 8; public virtual int V009() => 9; public virtual int V010() => 10; public virtual int V011() => 11;
    public virtual int V012() => 12; public virtual int V013() => 13; public virtual int V014() => 14; public virtual int V015() => 15;
    public virtual int V016() => 16; public virtual int V017() => 17; public virtual int V018() => 18; public virtual int V019() => 19;
    public virtual int V020() => 20; public virtual int V021() => 21; public virtual int V022() => 22; public virtual int V023() => 23;
    public virtual int V024() => 24; public virtual int V025() => 25; public virtual int V026() => 26; public virtual int V027() => 27;
    public virtual int V028() => 28; public virtual int V029() => 29; public virtual int V030() => 30; public virtual int V031() => 31;
    public virtual int V032() => 32; public virtual int V033() => 33; public virtual int V034() => 34; public virtual int V035() => 35;
    public virtual int V036() => 36; public virtual int V037() => 37; public virtual int V038() => 38; public virtual int V039() => 39;
    public virtual int V040() => 40; public virtual int V041() => 41; public virtual int V042() => 42; public virtual int V043() => 43;
    public virtual int V044() => 44; public virtual int V045() => 45; public virtual int V046() => 46; public virtual int V047() => 47;
    public virtual int V048() => 48; public virtual int V049() => 49; public virtual int V050() => 50; public virtual int V051() => 51;
    public virtual int V052() => 52; public virtual int V053() => 53; public virtual int V054() => 54; public virtual int V055() => 55;
    public virtual int V056() => 56; public virtual int V057() => 57; public virtual int V058() => 58; public virtual int V059() => 59;
    public virtual int V060() => 60; public virtual int V061() => 61; public virtual int V062() => 62; public virtual int V063() => 63;
    public virtual int V064() => 64; public virtual int V065() => 65; public virtual int V066() => 66; public virtual int V067() => 67;
    public virtual int V068() => 68; public virtual int V069() => 69; public virtual int V070() => 70; public virtual int V071() => 71;
    public virtual int V072() => 72; public virtual int V073() => 73; public virtual int V074() => 74; public virtual int V075() => 75;
    public virtual int V076() => 76; public virtual int V077() => 77; public virtual int V078() => 78; public virtual int V079() => 79;
    public virtual int V080() => 80; public virtual int V081() => 81; public virtual int V082() => 82; public virtual int V083() => 83;
    public virtual int V084() => 84; public virtual int V085() => 85; public virtual int V086() => 86; public virtual int V087() => 87;
    public virtual int V088() => 88; public virtual int V089() => 89; public virtual int V090() => 90; public virtual int V091() => 91;
    public virtual int V092() => 92; public virtual int V093() => 93; public virtual int V094() => 94; public virtual int V095() => 95;
    public virtual int V096() => 96; public virtual int V097() => 97; public virtual int V098() => 98; public virtual int V099() => 99;
    public virtual int V100() => 100; public virtual int V101() => 101; public virtual int V102() => 102; public virtual int V103() => 103;
    public virtual int V104() => 104; public virtual int V105() => 105; public virtual int V106() => 106; public virtual int V107() => 107;
    public virtual int V108() => 108; public virtual int V109() => 109; public virtual int V110() => 110; public virtual int V111() => 111;
    public virtual int V112() => 112; public virtual int V113() => 113; public virtual int V114() => 114; public virtual int V115() => 115;
    public virtual int V116() => 116; public virtual int V117() => 117; public virtual int V118() => 118; public virtual int V119() => 119;
    public virtual int V120() => 120; public virtual int V121() => 121; public virtual int V122() => 122; public virtual int V123() => 123;
    public virtual int V124() => 124; public virtual int V125() => 125; public virtual int V126() => 126; public virtual int V127() => 127;
    public virtual int V128() => 128; public virtual int V129() => 129; public virtual int V130() => 130; public virtual int V131() => 131;
    public virtual int V132() => 132; public virtual int V133() => 133; public virtual int V134() => 134; public virtual int V135() => 135;
    public virtual int V136() => 136; public virtual int V137() => 137; public virtual int V138() => 138; public virtual int V139() => 139;
    public virtual int V140() => 140; public virtual int V141() => 141; public virtual int V142() => 142; public virtual int V143() => 143;
    public virtual int V144() => 144; public virtual int V145() => 145; public virtual int V146() => 146; public virtual int V147() => 147;
    public virtual int V148() => 148; public virtual int V149() => 149; public virtual int V150() => 150; public virtual int V151() => 151;
    public virtual int V152() => 152; public virtual int V153() => 153; public virtual int V154() => 154; public virtual int V155() => 155;
    public virtual int V156() => 156; public virtual int V157() => 157; public virtual int V158() => 158; public virtual int V159() => 159;
    public virtual int V160() => 160; public virtual int V161() => 161; public virtual int V162() => 162; public virtual int V163() => 163;
    public virtual int V164() => 164; public virtual int V165() => 165; public virtual int V166() => 166; public virtual int V167() => 167;
    public virtual int V168() => 168; public virtual int V169() => 169; public virtual int V170() => 170; public virtual int V171() => 171;
    public virtual int V172() => 172; public virtual int V173() => 173; public virtual int V174() => 174; public virtual int V175() => 175;
    public virtual int V176() => 176; public virtual int V177() => 177; public virtual int V178() => 178; public virtual int V179() => 179;
    public virtual int V180() => 180; public virtual int V181() => 181; public virtual int V182() => 182; public virtual int V183() => 183;
    public virtual int V184() => 184; public virtual int V185() => 185; public virtual int V186() => 186; public virtual int V187() => 187;
    public virtual int V188() => 188; public virtual int V189() => 189; public virtual int V190() => 190; public virtual int V191() => 191;
    public virtual int V192() => 192; public virtual int V193() => 193; public virtual int V194() => 194; public virtual int V195() => 195;
    public virtual int V196() => 196; public virtual int V197() => 197; public virtual int V198() => 198; public virtual int V199() => 199;
    public virtual int V200() => 200; public virtual int V201() => 201; public virtual int V202() => 202; public virtual int V203() => 203;
    public virtual int V204() => 204; public virtual int V205() => 205; public virtual int V206() => 206; public virtual int V207() => 207;
    public virtual int V208() => 208; public virtual int V209() => 209; public virtual int V210() => 210; public virtual int V211() => 211;
    public virtual int V212() => 212; public virtual int V213() => 213; public virtual int V214() => 214; public virtual int V215() => 215;
    public virtual int V216() => 216; public virtual int V217() => 217; public virtual int V218() => 218; public virtual int V219() => 219;
    public virtual int V220() => 220; public virtual int V221() => 221; public virtual int V222() => 222; public virtual int V223() => 223;
    public virtual int V224() => 224; public virtual int V225() => 225; public virtual int V226() => 226; public virtual int V227() => 227;
    public virtual int V228() => 228; public virtual int V229() => 229; public virtual int V230() => 230; public virtual int V231() => 231;
    public virtual int V232() => 232; public virtual int V233() => 233; public virtual int V234() => 234; public virtual int V235() => 235;
    public virtual int V236() => 236; public virtual int V237() => 237; public virtual int V238() => 238; public virtual int V239() => 239;
    public virtual int V240() => 240; public virtual int V241() => 241; public virtual int V242() => 242; public virtual int V243() => 243;
    public virtual int V244() => 244; public virtual int V245() => 245; public virtual int V246() => 246; public virtual int V247() => 247;
    public virtual int V248() => 248; public virtual int V249() => 249; public virtual int V250() => 250; public virtual int V251() => 251;
    public virtual int V252() => 252; public virtual int V253() => 253; public virtual int V254() => 254; public virtual int V255() => 255;
    public virtual int V256() => 256; public virtual int V257() => 257; public virtual int V258() => 258; public virtual int V259() => 259;
}

static class Program
{
    private static void Try(string label, Func<object?> invoke)
    {
        string text;
        try
        {
            text = invoke()?.ToString() ?? "null";
        }
        catch (Exception ex)
        {
            text = ex.GetType().Name;
        }
        Console.WriteLine(label + ": " + text);
    }

    // How many methods of the name GetMethods reports, and how many on `below`.
    private static string Reported(Type type, Func<string, bool> named, Type below)
    {
        int count = 0, inherited = 0;
        foreach (var method in type.GetMethods())
            if (named(method.Name))
            {
                count++;
                if (method.DeclaringType == below)
                    inherited++;
            }
        return count + "/" + inherited;
    }

    private static string Members(MemberInfo[] members) =>
        members.Length == 0 ? "none" : members.Length + "/" + members[0].DeclaringType!.Name;

    internal static void Run()
    {
        Console.WriteLine("== wide hierarchies ==");
        // The calls put each closed instantiation reflected below in the image.
        GvmWideBase wide = new GvmWideLeaf();
        Console.WriteLine("direct: "
            + wide.M00<int>() + wide.M01<int>() + wide.M02<int>() + wide.M03<int>() + wide.M04<int>()
            + wide.M05<int>() + wide.M06<int>() + wide.M07<int>() + wide.M08<int>() + wide.M09<int>()
            + wide.M10<int>() + wide.M11<int>() + wide.M12<int>() + wide.M13<int>() + wide.M14<int>()
            + wide.M15<int>() + wide.M16<int>() + wide.M17<int>() + wide.M18<int>() + wide.M19<int>()
            + wide.M20<int>() + wide.M21<int>() + wide.M22<int>() + wide.M23<int>() + wide.M24<int>()
            + wide.M25<int>() + wide.M26<int>() + wide.M27<int>() + wide.M28<int>() + wide.M29<int>()
            + wide.M30<int>() + wide.M31<int>() + wide.M32<int>() + wide.M33<int>() + wide.M34<int>()
            + wide.M35<int>() + wide.M36<int>() + wide.M37<int>() + wide.M38<int>() + wide.M39<int>());
        Try("GetMethods, generic virtual overrides", () =>
            Reported(typeof(GvmWideLeaf), name => name.Length == 3 && name[0] == 'M', typeof(GvmWideBase)));
        Try("GetMember, generic virtual override", () => Members(typeof(GvmWideLeaf).GetMember("M39")));
        Try("GetMethod, generic virtual override", () => typeof(GvmWideLeaf).GetMethod("M39")!.DeclaringType!.Name);
        SlotRoot slots = new SlotWide();
        Console.WriteLine("direct: " + slots.Last() + "/" + ((SlotWide)slots).V259());
        Try("GetMethods, slot override", () => Reported(typeof(SlotWide), name => name == "Last", typeof(SlotRoot)));
        Try("GetMember, slot override", () => Members(typeof(SlotWide).GetMember("Last")));
        Try("GetMethod, slot override", () => typeof(SlotWide).GetMethod("Last")!.DeclaringType!.Name);
        Console.WriteLine("wide hierarchies end");
    }
}
