using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal enum OperandKind
{
    None,
    Int8,        // ldc.i4.s
    UInt8,       // ldarg.s / ldloc.s / starg.s / stloc.s
    UInt16,      // ldarg / ldloc (fat)
    Int32,       // ldc.i4
    Int64,       // ldc.i8
    Float32,     // ldc.r4
    Float64,     // ldc.r8
    BranchTarget8,
    BranchTarget32,
    Token,       // call / ldstr / newobj / ldfld / ...
    Switch,      // jump table
}

internal readonly struct Instruction
{
    public required int Offset { get; init; }
    public required int NextOffset { get; init; }
    public required ILOpCode OpCode { get; init; }
    public long Operand { get; init; }       // numeric operands / branch target offset
    public double FloatOperand { get; init; }
    public int Token { get; init; }
    public int[]? SwitchTargets { get; init; }
}

internal static class ILDecoder
{
    public static List<Instruction> Decode(ImmutableArray<byte> il)
    {
        var result = new List<Instruction>();
        int pos = 0;
        while (pos < il.Length)
        {
            int offset = pos;
            int b = il[pos++];
            ILOpCode op = b == 0xFE ? (ILOpCode)(0xFE00 | il[pos++]) : (ILOpCode)b;

            long operand = 0;
            double fop = 0;
            int token = 0;
            int[]? switchTargets = null;
            switch (GetOperandKind(op))
            {
                case OperandKind.None:
                    break;
                case OperandKind.Int8:
                    operand = unchecked((sbyte)il[pos]); pos += 1;
                    break;
                case OperandKind.UInt8:
                    operand = il[pos]; pos += 1;
                    break;
                case OperandKind.UInt16:
                    operand = ReadU16(il, pos); pos += 2;
                    break;
                case OperandKind.Int32:
                    operand = ReadI32(il, pos); pos += 4;
                    break;
                case OperandKind.Int64:
                    operand = ReadI64(il, pos); pos += 8;
                    break;
                case OperandKind.Float32:
                    fop = BitConverter.Int32BitsToSingle(ReadI32(il, pos)); pos += 4;
                    break;
                case OperandKind.Float64:
                    fop = BitConverter.Int64BitsToDouble(ReadI64(il, pos)); pos += 8;
                    break;
                case OperandKind.BranchTarget8:
                    operand = pos + 1 + unchecked((sbyte)il[pos]); pos += 1;
                    break;
                case OperandKind.BranchTarget32:
                    operand = pos + 4 + ReadI32(il, pos); pos += 4;
                    break;
                case OperandKind.Token:
                    token = ReadI32(il, pos); pos += 4;
                    break;
                case OperandKind.Switch:
                {
                    int n = ReadI32(il, pos); pos += 4;
                    int end = pos + 4 * n;
                    switchTargets = new int[n];
                    for (int i = 0; i < n; i++)
                    {
                        switchTargets[i] = end + ReadI32(il, pos);
                        pos += 4;
                    }
                    break;
                }
            }

            result.Add(new Instruction
            {
                Offset = offset,
                NextOffset = pos,
                OpCode = op,
                Operand = operand,
                FloatOperand = fop,
                Token = token,
                SwitchTargets = switchTargets,
            });
        }
        return result;
    }

    private static int ReadU16(ImmutableArray<byte> il, int p) => il[p] | (il[p + 1] << 8);
    private static int ReadI32(ImmutableArray<byte> il, int p) =>
        il[p] | (il[p + 1] << 8) | (il[p + 2] << 16) | (il[p + 3] << 24);
    private static long ReadI64(ImmutableArray<byte> il, int p) =>
        (uint)ReadI32(il, p) | ((long)ReadI32(il, p + 4) << 32);

    public static bool IsBranch(ILOpCode op) => op is
        ILOpCode.Leave or ILOpCode.Leave_s or
        ILOpCode.Br or ILOpCode.Br_s or
        ILOpCode.Brtrue or ILOpCode.Brtrue_s or ILOpCode.Brfalse or ILOpCode.Brfalse_s or
        ILOpCode.Beq or ILOpCode.Beq_s or ILOpCode.Bne_un or ILOpCode.Bne_un_s or
        ILOpCode.Bge or ILOpCode.Bge_s or ILOpCode.Bge_un or ILOpCode.Bge_un_s or
        ILOpCode.Bgt or ILOpCode.Bgt_s or ILOpCode.Bgt_un or ILOpCode.Bgt_un_s or
        ILOpCode.Ble or ILOpCode.Ble_s or ILOpCode.Ble_un or ILOpCode.Ble_un_s or
        ILOpCode.Blt or ILOpCode.Blt_s or ILOpCode.Blt_un or ILOpCode.Blt_un_s;

    public static bool IsUnconditionalTransfer(ILOpCode op) =>
        op is ILOpCode.Br or ILOpCode.Br_s or ILOpCode.Ret or ILOpCode.Throw or
        ILOpCode.Leave or ILOpCode.Leave_s or ILOpCode.Rethrow;

    private static OperandKind GetOperandKind(ILOpCode op) => op switch
    {
        ILOpCode.Nop or ILOpCode.Ret or ILOpCode.Dup or ILOpCode.Pop or
        ILOpCode.Ldc_i4_m1 or ILOpCode.Ldc_i4_0 or ILOpCode.Ldc_i4_1 or ILOpCode.Ldc_i4_2 or
        ILOpCode.Ldc_i4_3 or ILOpCode.Ldc_i4_4 or ILOpCode.Ldc_i4_5 or ILOpCode.Ldc_i4_6 or
        ILOpCode.Ldc_i4_7 or ILOpCode.Ldc_i4_8 or
        ILOpCode.Ldarg_0 or ILOpCode.Ldarg_1 or ILOpCode.Ldarg_2 or ILOpCode.Ldarg_3 or
        ILOpCode.Ldloc_0 or ILOpCode.Ldloc_1 or ILOpCode.Ldloc_2 or ILOpCode.Ldloc_3 or
        ILOpCode.Stloc_0 or ILOpCode.Stloc_1 or ILOpCode.Stloc_2 or ILOpCode.Stloc_3 or
        ILOpCode.Ldnull or
        ILOpCode.Add or ILOpCode.Sub or ILOpCode.Mul or ILOpCode.Div or ILOpCode.Div_un or
        ILOpCode.Rem or ILOpCode.Rem_un or ILOpCode.Neg or ILOpCode.Not or
        ILOpCode.And or ILOpCode.Or or ILOpCode.Xor or
        ILOpCode.Shl or ILOpCode.Shr or ILOpCode.Shr_un or
        ILOpCode.Ceq or ILOpCode.Cgt or ILOpCode.Cgt_un or ILOpCode.Clt or ILOpCode.Clt_un or
        ILOpCode.Conv_i1 or ILOpCode.Conv_u1 or ILOpCode.Conv_i2 or ILOpCode.Conv_u2 or
        ILOpCode.Conv_i4 or ILOpCode.Conv_u4 or ILOpCode.Conv_i8 or ILOpCode.Conv_u8 or
        ILOpCode.Conv_r4 or ILOpCode.Conv_r8 or ILOpCode.Conv_i or ILOpCode.Conv_u or
        ILOpCode.Conv_r_un or
        ILOpCode.Add_ovf or ILOpCode.Add_ovf_un or ILOpCode.Sub_ovf or ILOpCode.Sub_ovf_un or
        ILOpCode.Mul_ovf or ILOpCode.Mul_ovf_un or
        ILOpCode.Conv_ovf_i1 or ILOpCode.Conv_ovf_u1 or ILOpCode.Conv_ovf_i2 or ILOpCode.Conv_ovf_u2 or
        ILOpCode.Conv_ovf_i4 or ILOpCode.Conv_ovf_u4 or ILOpCode.Conv_ovf_i8 or ILOpCode.Conv_ovf_u8 or
        ILOpCode.Conv_ovf_i or ILOpCode.Conv_ovf_u or
        ILOpCode.Conv_ovf_i1_un or ILOpCode.Conv_ovf_u1_un or ILOpCode.Conv_ovf_i2_un or ILOpCode.Conv_ovf_u2_un or
        ILOpCode.Conv_ovf_i4_un or ILOpCode.Conv_ovf_u4_un or ILOpCode.Conv_ovf_i8_un or ILOpCode.Conv_ovf_u8_un or
        ILOpCode.Conv_ovf_i_un or ILOpCode.Conv_ovf_u_un or
        ILOpCode.Ckfinite or
        ILOpCode.Ldlen or
        ILOpCode.Ldelem_i4 or ILOpCode.Stelem_i4 or
        ILOpCode.Ldelem_ref or ILOpCode.Stelem_ref or
        ILOpCode.Ldelem_i1 or ILOpCode.Ldelem_u1 or ILOpCode.Ldelem_i2 or ILOpCode.Ldelem_u2 or
        ILOpCode.Ldelem_u4 or ILOpCode.Ldelem_i8 or ILOpCode.Ldelem_i or
        ILOpCode.Ldelem_r4 or ILOpCode.Ldelem_r8 or
        ILOpCode.Stelem_i1 or ILOpCode.Stelem_i2 or ILOpCode.Stelem_i8 or ILOpCode.Stelem_i or
        ILOpCode.Stelem_r4 or ILOpCode.Stelem_r8 or
        ILOpCode.Ldind_i1 or ILOpCode.Ldind_u1 or ILOpCode.Ldind_i2 or ILOpCode.Ldind_u2 or
        ILOpCode.Ldind_i4 or ILOpCode.Ldind_u4 or ILOpCode.Ldind_i8 or ILOpCode.Ldind_i or
        ILOpCode.Ldind_r4 or ILOpCode.Ldind_r8 or ILOpCode.Ldind_ref or
        ILOpCode.Stind_i1 or ILOpCode.Stind_i2 or ILOpCode.Stind_i4 or ILOpCode.Stind_i8 or
        ILOpCode.Stind_r4 or ILOpCode.Stind_r8 or ILOpCode.Stind_ref or ILOpCode.Stind_i or
        ILOpCode.Throw or ILOpCode.Rethrow or ILOpCode.Endfinally or ILOpCode.Endfilter or
        ILOpCode.Localloc or
        // Block memory ops: cpblk copies <size> bytes dest<-src; initblk
        // fills <size> bytes at addr with a byte value. Both take no inline operand.
        ILOpCode.Cpblk or ILOpCode.Initblk or
        // Prefixes we model as no-ops (memory ordering / tail call hints) — the
        // following instruction is decoded normally (BCL bodies reached by
        // iterator/async state machines use `volatile.`).
        ILOpCode.Volatile or ILOpCode.Readonly or ILOpCode.Tail
            => OperandKind.None,

        // unaligned. <alignment:u8> — a one-byte hint we ignore.
        ILOpCode.Unaligned => OperandKind.UInt8,

        ILOpCode.Ldc_i4_s => OperandKind.Int8,
        ILOpCode.Ldc_i4 => OperandKind.Int32,
        ILOpCode.Ldc_i8 => OperandKind.Int64,
        ILOpCode.Ldc_r4 => OperandKind.Float32,
        ILOpCode.Ldc_r8 => OperandKind.Float64,

        ILOpCode.Ldarg_s or ILOpCode.Starg_s or ILOpCode.Ldloc_s or ILOpCode.Stloc_s or
        ILOpCode.Ldarga_s or ILOpCode.Ldloca_s
            => OperandKind.UInt8,
        ILOpCode.Ldarg or ILOpCode.Starg or ILOpCode.Ldloc or ILOpCode.Stloc or
        ILOpCode.Ldarga or ILOpCode.Ldloca
            => OperandKind.UInt16,

        ILOpCode.Switch => OperandKind.Switch,

        ILOpCode.Leave_s or
        ILOpCode.Br_s or ILOpCode.Brtrue_s or ILOpCode.Brfalse_s or
        ILOpCode.Beq_s or ILOpCode.Bne_un_s or ILOpCode.Bge_s or ILOpCode.Bge_un_s or
        ILOpCode.Bgt_s or ILOpCode.Bgt_un_s or ILOpCode.Ble_s or ILOpCode.Ble_un_s or
        ILOpCode.Blt_s or ILOpCode.Blt_un_s
            => OperandKind.BranchTarget8,
        ILOpCode.Leave or
        ILOpCode.Br or ILOpCode.Brtrue or ILOpCode.Brfalse or
        ILOpCode.Beq or ILOpCode.Bne_un or ILOpCode.Bge or ILOpCode.Bge_un or
        ILOpCode.Bgt or ILOpCode.Bgt_un or ILOpCode.Ble or ILOpCode.Ble_un or
        ILOpCode.Blt or ILOpCode.Blt_un
            => OperandKind.BranchTarget32,

        ILOpCode.Ldstr or ILOpCode.Call or ILOpCode.Callvirt or ILOpCode.Newobj or
        // calli's inline operand is a 4-byte StandAloneSig token (the call site
        // signature), decoded like any other metadata token.
        ILOpCode.Calli or
        ILOpCode.Ldfld or ILOpCode.Stfld or ILOpCode.Ldsfld or ILOpCode.Stsfld or
        ILOpCode.Ldflda or ILOpCode.Ldsflda or
        ILOpCode.Ldobj or ILOpCode.Stobj or ILOpCode.Initobj or
        ILOpCode.Newarr or ILOpCode.Castclass or ILOpCode.Isinst or
        ILOpCode.Ldelem or ILOpCode.Stelem or ILOpCode.Ldelema or
        // unbox (0x79) — unlike unbox.any, which COPIES the value out, this pushes a
        // managed pointer INTO the box. It is what Roslyn emits for `((T)obj).Field` on a
        // value type (`unbox; ldfld`), which is the shape of a hand-written struct's
        // `Equals(object)` — so a field walk that dispatches one lands here.
        ILOpCode.Box or ILOpCode.Unbox or ILOpCode.Unbox_any or
        ILOpCode.Ldftn or ILOpCode.Ldvirtftn or
        ILOpCode.Constrained or ILOpCode.Ldtoken or
        ILOpCode.Sizeof
            => OperandKind.Token,

        _ => throw new NotSupportedException($"Unsupported IL opcode: {op} (0x{(int)op:X})"),
    };
}
