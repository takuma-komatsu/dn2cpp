#nullable enable
using System;
using System.Reflection;
namespace OrdinaryWideLookupSubset;
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

class PropertyRoot
{
    public int P000 => 0;
    public int P001 => 1;
    public int P002 => 2;
    public int P003 => 3;
    public int P004 => 4;
    public int P005 => 5;
    public int P006 => 6;
    public int P007 => 7;
    public int P008 => 8;
    public int P009 => 9;
    public int P010 => 10;
    public int P011 => 11;
    public int P012 => 12;
    public int P013 => 13;
    public int P014 => 14;
    public int P015 => 15;
    public int P016 => 16;
    public int P017 => 17;
    public int P018 => 18;
    public int P019 => 19;
    public int P020 => 20;
    public int P021 => 21;
    public int P022 => 22;
    public int P023 => 23;
    public int P024 => 24;
    public int P025 => 25;
    public int P026 => 26;
    public int P027 => 27;
    public int P028 => 28;
    public int P029 => 29;
    public int P030 => 30;
    public int P031 => 31;
    public int P032 => 32;
    public int P033 => 33;
    public int P034 => 34;
    public int P035 => 35;
    public int P036 => 36;
    public int P037 => 37;
    public int P038 => 38;
    public int P039 => 39;
    public int P040 => 40;
    public int P041 => 41;
    public int P042 => 42;
    public int P043 => 43;
    public int P044 => 44;
    public int P045 => 45;
    public int P046 => 46;
    public int P047 => 47;
    public int P048 => 48;
    public int P049 => 49;
    public int P050 => 50;
    public int P051 => 51;
    public int P052 => 52;
    public int P053 => 53;
    public int P054 => 54;
    public int P055 => 55;
    public int P056 => 56;
    public int P057 => 57;
    public int P058 => 58;
    public int P059 => 59;
    public int P060 => 60;
    public int P061 => 61;
    public int P062 => 62;
    public int P063 => 63;
    public int P064 => 64;
    public int P065 => 65;
    public int P066 => 66;
    public int P067 => 67;
    public int P068 => 68;
    public int P069 => 69;
    public int P070 => 70;
    public int P071 => 71;
    public int P072 => 72;
    public int P073 => 73;
    public int P074 => 74;
    public int P075 => 75;
    public int P076 => 76;
    public int P077 => 77;
    public int P078 => 78;
    public int P079 => 79;
    public int P080 => 80;
    public int P081 => 81;
    public int P082 => 82;
    public int P083 => 83;
    public int P084 => 84;
    public int P085 => 85;
    public int P086 => 86;
    public int P087 => 87;
    public int P088 => 88;
    public int P089 => 89;
    public int P090 => 90;
    public int P091 => 91;
    public int P092 => 92;
    public int P093 => 93;
    public int P094 => 94;
    public int P095 => 95;
    public int P096 => 96;
    public int P097 => 97;
    public int P098 => 98;
    public int P099 => 99;
    public int P100 => 100;
    public int P101 => 101;
    public int P102 => 102;
    public int P103 => 103;
    public int P104 => 104;
    public int P105 => 105;
    public int P106 => 106;
    public int P107 => 107;
    public int P108 => 108;
    public int P109 => 109;
    public int P110 => 110;
    public int P111 => 111;
    public int P112 => 112;
    public int P113 => 113;
    public int P114 => 114;
    public int P115 => 115;
    public int P116 => 116;
    public int P117 => 117;
    public int P118 => 118;
    public int P119 => 119;
    public int P120 => 120;
    public int P121 => 121;
    public int P122 => 122;
    public int P123 => 123;
    public int P124 => 124;
    public int P125 => 125;
    public int P126 => 126;
    public int P127 => 127;
    public int P128 => 128;
    public int P129 => 129;
    public int P130 => 130;
    public int P131 => 131;
    public int P132 => 132;
    public int P133 => 133;
    public int P134 => 134;
    public int P135 => 135;
    public int P136 => 136;
    public int P137 => 137;
    public int P138 => 138;
    public int P139 => 139;
    public int P140 => 140;
    public int P141 => 141;
    public int P142 => 142;
    public int P143 => 143;
    public int P144 => 144;
    public int P145 => 145;
    public int P146 => 146;
    public int P147 => 147;
    public int P148 => 148;
    public int P149 => 149;
    public int P150 => 150;
    public int P151 => 151;
    public int P152 => 152;
    public int P153 => 153;
    public int P154 => 154;
    public int P155 => 155;
    public int P156 => 156;
    public int P157 => 157;
    public int P158 => 158;
    public int P159 => 159;
    public int P160 => 160;
    public int P161 => 161;
    public int P162 => 162;
    public int P163 => 163;
    public int P164 => 164;
    public int P165 => 165;
    public int P166 => 166;
    public int P167 => 167;
    public int P168 => 168;
    public int P169 => 169;
    public int P170 => 170;
    public int P171 => 171;
    public int P172 => 172;
    public int P173 => 173;
    public int P174 => 174;
    public int P175 => 175;
    public int P176 => 176;
    public int P177 => 177;
    public int P178 => 178;
    public int P179 => 179;
    public int P180 => 180;
    public int P181 => 181;
    public int P182 => 182;
    public int P183 => 183;
    public int P184 => 184;
    public int P185 => 185;
    public int P186 => 186;
    public int P187 => 187;
    public int P188 => 188;
    public int P189 => 189;
    public int P190 => 190;
    public int P191 => 191;
    public int P192 => 192;
    public int P193 => 193;
    public int P194 => 194;
    public int P195 => 195;
    public int P196 => 196;
    public int P197 => 197;
    public int P198 => 198;
    public int P199 => 199;
    public int P200 => 200;
    public int P201 => 201;
    public int P202 => 202;
    public int P203 => 203;
    public int P204 => 204;
    public int P205 => 205;
    public int P206 => 206;
    public int P207 => 207;
    public int P208 => 208;
    public int P209 => 209;
    public int P210 => 210;
    public int P211 => 211;
    public int P212 => 212;
    public int P213 => 213;
    public int P214 => 214;
    public int P215 => 215;
    public int P216 => 216;
    public int P217 => 217;
    public int P218 => 218;
    public int P219 => 219;
    public int P220 => 220;
    public int P221 => 221;
    public int P222 => 222;
    public int P223 => 223;
    public int P224 => 224;
    public int P225 => 225;
    public int P226 => 226;
    public int P227 => 227;
    public int P228 => 228;
    public int P229 => 229;
    public int P230 => 230;
    public int P231 => 231;
    public int P232 => 232;
    public int P233 => 233;
    public int P234 => 234;
    public int P235 => 235;
    public int P236 => 236;
    public int P237 => 237;
    public int P238 => 238;
    public int P239 => 239;
    public int P240 => 240;
    public int P241 => 241;
    public int P242 => 242;
    public int P243 => 243;
    public int P244 => 244;
    public int P245 => 245;
    public int P246 => 246;
    public int P247 => 247;
    public int P248 => 248;
    public int P249 => 249;
    public int P250 => 250;
    public int P251 => 251;
    public int P252 => 252;
    public int P253 => 253;
    public int P254 => 254;
    public int P255 => 255;
    public int P256 => 256;
    public int P257 => 257;
    public int P258 => 258;
    public int P259 => 259;
    public int P260 => 260;
    public int P261 => 261;
    public int P262 => 262;
    public int P263 => 263;
    public int P264 => 264;
    public int P265 => 265;
    public int P266 => 266;
    public int P267 => 267;
    public int P268 => 268;
    public int P269 => 269;
}
class PropertyLeaf : PropertyRoot
{
    public new int P000 => 0;
    public new int P001 => 1;
    public new int P002 => 2;
    public new int P003 => 3;
    public new int P004 => 4;
    public new int P005 => 5;
    public new int P006 => 6;
    public new int P007 => 7;
    public new int P008 => 8;
    public new int P009 => 9;
    public new int P010 => 10;
    public new int P011 => 11;
    public new int P012 => 12;
    public new int P013 => 13;
    public new int P014 => 14;
    public new int P015 => 15;
    public new int P016 => 16;
    public new int P017 => 17;
    public new int P018 => 18;
    public new int P019 => 19;
    public new int P020 => 20;
    public new int P021 => 21;
    public new int P022 => 22;
    public new int P023 => 23;
    public new int P024 => 24;
    public new int P025 => 25;
    public new int P026 => 26;
    public new int P027 => 27;
    public new int P028 => 28;
    public new int P029 => 29;
    public new int P030 => 30;
    public new int P031 => 31;
    public new int P032 => 32;
    public new int P033 => 33;
    public new int P034 => 34;
    public new int P035 => 35;
    public new int P036 => 36;
    public new int P037 => 37;
    public new int P038 => 38;
    public new int P039 => 39;
    public new int P040 => 40;
    public new int P041 => 41;
    public new int P042 => 42;
    public new int P043 => 43;
    public new int P044 => 44;
    public new int P045 => 45;
    public new int P046 => 46;
    public new int P047 => 47;
    public new int P048 => 48;
    public new int P049 => 49;
    public new int P050 => 50;
    public new int P051 => 51;
    public new int P052 => 52;
    public new int P053 => 53;
    public new int P054 => 54;
    public new int P055 => 55;
    public new int P056 => 56;
    public new int P057 => 57;
    public new int P058 => 58;
    public new int P059 => 59;
    public new int P060 => 60;
    public new int P061 => 61;
    public new int P062 => 62;
    public new int P063 => 63;
    public new int P064 => 64;
    public new int P065 => 65;
    public new int P066 => 66;
    public new int P067 => 67;
    public new int P068 => 68;
    public new int P069 => 69;
    public new int P070 => 70;
    public new int P071 => 71;
    public new int P072 => 72;
    public new int P073 => 73;
    public new int P074 => 74;
    public new int P075 => 75;
    public new int P076 => 76;
    public new int P077 => 77;
    public new int P078 => 78;
    public new int P079 => 79;
    public new int P080 => 80;
    public new int P081 => 81;
    public new int P082 => 82;
    public new int P083 => 83;
    public new int P084 => 84;
    public new int P085 => 85;
    public new int P086 => 86;
    public new int P087 => 87;
    public new int P088 => 88;
    public new int P089 => 89;
    public new int P090 => 90;
    public new int P091 => 91;
    public new int P092 => 92;
    public new int P093 => 93;
    public new int P094 => 94;
    public new int P095 => 95;
    public new int P096 => 96;
    public new int P097 => 97;
    public new int P098 => 98;
    public new int P099 => 99;
    public new int P100 => 100;
    public new int P101 => 101;
    public new int P102 => 102;
    public new int P103 => 103;
    public new int P104 => 104;
    public new int P105 => 105;
    public new int P106 => 106;
    public new int P107 => 107;
    public new int P108 => 108;
    public new int P109 => 109;
    public new int P110 => 110;
    public new int P111 => 111;
    public new int P112 => 112;
    public new int P113 => 113;
    public new int P114 => 114;
    public new int P115 => 115;
    public new int P116 => 116;
    public new int P117 => 117;
    public new int P118 => 118;
    public new int P119 => 119;
    public new int P120 => 120;
    public new int P121 => 121;
    public new int P122 => 122;
    public new int P123 => 123;
    public new int P124 => 124;
    public new int P125 => 125;
    public new int P126 => 126;
    public new int P127 => 127;
    public new int P128 => 128;
    public new int P129 => 129;
    public new int P130 => 130;
    public new int P131 => 131;
    public new int P132 => 132;
    public new int P133 => 133;
    public new int P134 => 134;
    public new int P135 => 135;
    public new int P136 => 136;
    public new int P137 => 137;
    public new int P138 => 138;
    public new int P139 => 139;
    public new int P140 => 140;
    public new int P141 => 141;
    public new int P142 => 142;
    public new int P143 => 143;
    public new int P144 => 144;
    public new int P145 => 145;
    public new int P146 => 146;
    public new int P147 => 147;
    public new int P148 => 148;
    public new int P149 => 149;
    public new int P150 => 150;
    public new int P151 => 151;
    public new int P152 => 152;
    public new int P153 => 153;
    public new int P154 => 154;
    public new int P155 => 155;
    public new int P156 => 156;
    public new int P157 => 157;
    public new int P158 => 158;
    public new int P159 => 159;
    public new int P160 => 160;
    public new int P161 => 161;
    public new int P162 => 162;
    public new int P163 => 163;
    public new int P164 => 164;
    public new int P165 => 165;
    public new int P166 => 166;
    public new int P167 => 167;
    public new int P168 => 168;
    public new int P169 => 169;
    public new int P170 => 170;
    public new int P171 => 171;
    public new int P172 => 172;
    public new int P173 => 173;
    public new int P174 => 174;
    public new int P175 => 175;
    public new int P176 => 176;
    public new int P177 => 177;
    public new int P178 => 178;
    public new int P179 => 179;
    public new int P180 => 180;
    public new int P181 => 181;
    public new int P182 => 182;
    public new int P183 => 183;
    public new int P184 => 184;
    public new int P185 => 185;
    public new int P186 => 186;
    public new int P187 => 187;
    public new int P188 => 188;
    public new int P189 => 189;
    public new int P190 => 190;
    public new int P191 => 191;
    public new int P192 => 192;
    public new int P193 => 193;
    public new int P194 => 194;
    public new int P195 => 195;
    public new int P196 => 196;
    public new int P197 => 197;
    public new int P198 => 198;
    public new int P199 => 199;
    public new int P200 => 200;
    public new int P201 => 201;
    public new int P202 => 202;
    public new int P203 => 203;
    public new int P204 => 204;
    public new int P205 => 205;
    public new int P206 => 206;
    public new int P207 => 207;
    public new int P208 => 208;
    public new int P209 => 209;
    public new int P210 => 210;
    public new int P211 => 211;
    public new int P212 => 212;
    public new int P213 => 213;
    public new int P214 => 214;
    public new int P215 => 215;
    public new int P216 => 216;
    public new int P217 => 217;
    public new int P218 => 218;
    public new int P219 => 219;
    public new int P220 => 220;
    public new int P221 => 221;
    public new int P222 => 222;
    public new int P223 => 223;
    public new int P224 => 224;
    public new int P225 => 225;
    public new int P226 => 226;
    public new int P227 => 227;
    public new int P228 => 228;
    public new int P229 => 229;
    public new int P230 => 230;
    public new int P231 => 231;
    public new int P232 => 232;
    public new int P233 => 233;
    public new int P234 => 234;
    public new int P235 => 235;
    public new int P236 => 236;
    public new int P237 => 237;
    public new int P238 => 238;
    public new int P239 => 239;
    public new int P240 => 240;
    public new int P241 => 241;
    public new int P242 => 242;
    public new int P243 => 243;
    public new int P244 => 244;
    public new int P245 => 245;
    public new int P246 => 246;
    public new int P247 => 247;
    public new int P248 => 248;
    public new int P249 => 249;
    public new int P250 => 250;
    public new int P251 => 251;
    public new int P252 => 252;
    public new int P253 => 253;
    public new int P254 => 254;
    public new int P255 => 255;
    public new int P256 => 256;
    public new int P257 => 257;
    public new int P258 => 258;
    public new int P259 => 259;
    public new int P260 => 260;
    public new int P261 => 261;
    public new int P262 => 262;
    public new int P263 => 263;
    public new int P264 => 264;
    public new int P265 => 265;
    public new int P266 => 266;
    public new int P267 => 267;
    public new int P268 => 268;
    public new int P269 => 269;
}
class A00 { }
class A01 { }
class A02 { }
class A03 { }
class A04 { }
class A05 { }
class A06 { }
class A07 { }
class A08 { }
class A09 { }
class A10 { }
class A11 { }
class A12 { }
class A13 { }
class A14 { }
class A15 { }
class A16 { }
class A17 { }
class A18 { }
class A19 { }
class A20 { }
class A21 { }
class A22 { }
class A23 { }
class A24 { }
class A25 { }
class A26 { }
class A27 { }
class A28 { }
class A29 { }
class A30 { }
class A31 { }
class A32 { }
class A33 { }
class A34 { }
class A35 { }
class A36 { }
class A37 { }
class A38 { }
class A39 { }
class ManyConstructors
{
    public string Which;
    public ManyConstructors(object value) => Which = "object";
    public ManyConstructors(A00 value) => Which = "A00";
    public ManyConstructors(A01 value) => Which = "A01";
    public ManyConstructors(A02 value) => Which = "A02";
    public ManyConstructors(A03 value) => Which = "A03";
    public ManyConstructors(A04 value) => Which = "A04";
    public ManyConstructors(A05 value) => Which = "A05";
    public ManyConstructors(A06 value) => Which = "A06";
    public ManyConstructors(A07 value) => Which = "A07";
    public ManyConstructors(A08 value) => Which = "A08";
    public ManyConstructors(A09 value) => Which = "A09";
    public ManyConstructors(A10 value) => Which = "A10";
    public ManyConstructors(A11 value) => Which = "A11";
    public ManyConstructors(A12 value) => Which = "A12";
    public ManyConstructors(A13 value) => Which = "A13";
    public ManyConstructors(A14 value) => Which = "A14";
    public ManyConstructors(A15 value) => Which = "A15";
    public ManyConstructors(A16 value) => Which = "A16";
    public ManyConstructors(A17 value) => Which = "A17";
    public ManyConstructors(A18 value) => Which = "A18";
    public ManyConstructors(A19 value) => Which = "A19";
    public ManyConstructors(A20 value) => Which = "A20";
    public ManyConstructors(A21 value) => Which = "A21";
    public ManyConstructors(A22 value) => Which = "A22";
    public ManyConstructors(A23 value) => Which = "A23";
    public ManyConstructors(A24 value) => Which = "A24";
    public ManyConstructors(A25 value) => Which = "A25";
    public ManyConstructors(A26 value) => Which = "A26";
    public ManyConstructors(A27 value) => Which = "A27";
    public ManyConstructors(A28 value) => Which = "A28";
    public ManyConstructors(A29 value) => Which = "A29";
    public ManyConstructors(A30 value) => Which = "A30";
    public ManyConstructors(A31 value) => Which = "A31";
    public ManyConstructors(A32 value) => Which = "A32";
    public ManyConstructors(A33 value) => Which = "A33";
    public ManyConstructors(A34 value) => Which = "A34";
    public ManyConstructors(A35 value) => Which = "A35";
    public ManyConstructors(A36 value) => Which = "A36";
    public ManyConstructors(A37 value) => Which = "A37";
    public ManyConstructors(A38 value) => Which = "A38";
    public ManyConstructors(A39 value) => Which = "A39";
}
static class Program
{
    internal static void Run()
    {
        Console.WriteLine("== ordinary wide lookups ==");
        const BindingFlags all = BindingFlags.Public | BindingFlags.Instance;
        int baseRows = 0, ownRows = 0;
        foreach (MethodInfo row in typeof(SlotWide).GetMethods(all))
            if (row.Name == "V259")
            {
                if (row.DeclaringType == typeof(SlotWide)) ownRows++;
                else baseRows++;
            }
        Console.WriteLine("wide slots=" + ownRows + "/" + baseRows);
        Console.WriteLine("wide get member=" + typeof(SlotWide).GetMember("V259", all).Length);
        Console.WriteLine("wide get method=" + typeof(SlotWide).GetMethod("V259", all)!.DeclaringType!.Name);
        int wrong = 0;
        foreach (PropertyInfo row in typeof(PropertyLeaf).GetProperties(all))
            if (row.DeclaringType != typeof(PropertyLeaf)) wrong++;
        Console.WriteLine("wide properties=" + typeof(PropertyLeaf).GetProperties(all).Length + "/" + wrong);
        Console.WriteLine("wide constructor=" + ((ManyConstructors)Activator.CreateInstance(typeof(ManyConstructors), new object[] { new A39() })!).Which);
        Console.WriteLine("ordinary wide lookups end");
    }
}

class ObjectLeaf
{
    public string Local() => "local";
}
class ObjectOverride : ObjectLeaf
{
    public override string ToString() => "override";
}
class ObjectNewSlot : ObjectLeaf
{
    public new virtual string ToString() => "newslot";
}
class ObjectNewPlain : ObjectLeaf
{
    public new string ToString() => "newplain";
}
class ObjectOverload : ObjectLeaf
{
    public string ToString(int number) => "overload:" + number;
}
struct ObjectValue
{
    public override string ToString() => "value";
}
struct ObjectPlainValue { }
static class ObjectMethods
{
    internal static void Run()
    {
        Console.WriteLine("== Object family method enumeration ==");
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        Type[] types = { typeof(object), typeof(ObjectLeaf), typeof(ObjectOverride), typeof(ObjectNewSlot), typeof(ObjectNewPlain),
            typeof(ObjectOverload), typeof(ReflectReturnLib.VirtualFactory) };
        BindingFlags[] flags = { BindingFlags.Public | BindingFlags.Instance, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static, all, all | BindingFlags.FlattenHierarchy,
            all | BindingFlags.DeclaredOnly, BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy,
            BindingFlags.Instance | BindingFlags.NonPublic, BindingFlags.Default };
        foreach (Type type in types)
            foreach (BindingFlags flag in flags)
                Dump(type, flag);
        Dump(typeof(ValueType), BindingFlags.Public | BindingFlags.Instance);
        Dump(typeof(ObjectPlainValue), BindingFlags.Public | BindingFlags.Instance);
        Dump(typeof(ObjectValue), BindingFlags.Public | BindingFlags.Instance);
        Dump(typeof(ObjectValue), all | BindingFlags.DeclaredOnly);
        object receiver = new ObjectLeaf();
        MethodInfo? clone = typeof(ObjectLeaf).GetMethod("MemberwiseClone", all);
        MethodInfo? text = typeof(ObjectLeaf).GetMethod("ToString");
        Console.WriteLine("enumerated invocation=" + (clone is not null && clone.Invoke(receiver, null)!.GetType() == receiver.GetType())
            + ":" + (text is not null && (string)text.Invoke(receiver, null)! == receiver.ToString()));
        Console.WriteLine("Object family method enumeration end");
    }

    private static void Dump(Type type, BindingFlags flags)
    {
        MethodInfo[] methods = flags == (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            ? type.GetMethods() : type.GetMethods(flags);
        string[] rows = new string[methods.Length];
        string[] order = new string[methods.Length];
        bool same = true;
        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo method = methods[i];
            Type[] parameters = new Type[method.GetParameters().Length];
            for (int j = 0; j < parameters.Length; j++) parameters[j] = method.GetParameters()[j].ParameterType;
            if (type != typeof(ObjectNewSlot) && type != typeof(ObjectNewPlain))
                same &= ReferenceEquals(method, type.GetMethod(method.Name, flags, null, parameters, null));
            same &= method.ReflectedType == type;
            order[i] = method.Name + "/" + parameters.Length;
            rows[i] = method + ":decl=" + method.DeclaringType!.Name + ":virtual=" + method.IsVirtual
                + ":attrs=" + (int)method.Attributes + ":base=" + method.GetBaseDefinition().DeclaringType!.Name;
        }
        if ((type == typeof(object) || type == typeof(ObjectLeaf) || type == typeof(ReflectReturnLib.VirtualFactory)
                || type == typeof(ValueType) || type == typeof(ObjectPlainValue))
            && (flags == (BindingFlags.Public | BindingFlags.Instance)
                || flags == (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                || flags == (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)))
            Console.WriteLine("method order " + type.Name + "/" + (int)flags + "=" + string.Join("|", order));
        Array.Sort(rows, StringComparer.Ordinal);
        Console.WriteLine("methods " + type.Name + "/" + (int)flags + "=" + rows.Length + ":same=" + same);
        foreach (string row in rows) Console.WriteLine("  " + row);
    }
}
