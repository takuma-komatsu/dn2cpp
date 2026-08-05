namespace Dn2Cpp;

/// <summary>The register code format's dense opcode table (docs/BPI-FORMAT.md
/// "Register code format (Header.flags bit0)"). The table in docs/BPI-FORMAT.md
/// is NORMATIVE: this enum and the C++ X-macro list
/// (runtime/core/dn2cpp_interp_regops.h) must match it value for value. Values
/// are assigned sequentially in table order starting at 0; the member names
/// keep the C++ spelling so the two mirrors diff cleanly.
///
/// Operand classes (they fix which instruction-record fields are registers):
/// C0 = no registers (a = branch target / EH index / unused); C1 = r0;
/// C2 = r0, r1; C3 = r0, r1, r2 (r2 rides a's low byte). Register operands
/// ride the record's second u16 (low byte = r0, high byte = r1; 0xFF =
/// unused).</summary>
internal enum RegOp : ushort
{
    // invalid / data movement
    R_INVALID = 0,
    R_MOV = 1,
    // constants (r0 = dst)
    R_LDNULL = 2,
    R_LDC_I32 = 3,
    R_LDC_I64 = 4,
    R_LDC_R4 = 5,
    R_LDC_R8 = 6,
    R_LDSTR = 7,
    // binary arithmetic (C3: r0 = dst, r1/r2 = operands)
    R_ADD_I32 = 8,
    R_ADD_I64 = 9,
    R_SUB_I32 = 10,
    R_SUB_I64 = 11,
    R_MUL_I32 = 12,
    R_MUL_I64 = 13,
    R_DIV_I32 = 14,
    R_DIV_I64 = 15,
    R_DIV_UN_I32 = 16,
    R_DIV_UN_I64 = 17,
    R_REM_I32 = 18,
    R_REM_I64 = 19,
    R_REM_UN_I32 = 20,
    R_REM_UN_I64 = 21,
    R_AND_I32 = 22,
    R_AND_I64 = 23,
    R_OR_I32 = 24,
    R_OR_I64 = 25,
    R_XOR_I32 = 26,
    R_XOR_I64 = 27,
    R_ADD_F32 = 28,
    R_ADD_F64 = 29,
    R_SUB_F32 = 30,
    R_SUB_F64 = 31,
    R_MUL_F32 = 32,
    R_MUL_F64 = 33,
    R_DIV_F32 = 34,
    R_DIV_F64 = 35,
    R_REM_F32 = 36,
    R_REM_F64 = 37,
    R_SHL_I32 = 38,
    R_SHL_I64 = 39,
    R_SHR_I32 = 40,
    R_SHR_I64 = 41,
    R_SHR_UN_I32 = 42,
    R_SHR_UN_I64 = 43,
    // unary (C2: r0 = dst, r1 = src)
    R_NEG_I32 = 44,
    R_NEG_I64 = 45,
    R_NEG_F32 = 46,
    R_NEG_F64 = 47,
    R_NOT_I32 = 48,
    R_NOT_I64 = 49,
    // conversions (C2; target-major, source order I32, I64, F)
    R_CONV_I4_FROM_I32 = 50,
    R_CONV_I4_FROM_I64 = 51,
    R_CONV_I4_FROM_F = 52,
    R_CONV_U4_FROM_I32 = 53,
    R_CONV_U4_FROM_I64 = 54,
    R_CONV_U4_FROM_F = 55,
    R_CONV_I8_FROM_I32 = 56,
    R_CONV_I8_FROM_I64 = 57,
    R_CONV_I8_FROM_F = 58,
    R_CONV_U8_FROM_I32 = 59,
    R_CONV_U8_FROM_I64 = 60,
    R_CONV_U8_FROM_F = 61,
    R_CONV_R4_FROM_I32 = 62,
    R_CONV_R4_FROM_I64 = 63,
    R_CONV_R4_FROM_F = 64,
    R_CONV_R8_FROM_I32 = 65,
    R_CONV_R8_FROM_I64 = 66,
    R_CONV_R8_FROM_F = 67,
    // comparisons (C3: r0 = dst i32 0/1, r1/r2 = operands)
    R_CEQ_I32 = 68,
    R_CEQ_I64 = 69,
    R_CEQ_F32 = 70,
    R_CEQ_F64 = 71,
    R_CGT_I32 = 72,
    R_CGT_I64 = 73,
    R_CGT_F32 = 74,
    R_CGT_F64 = 75,
    R_CGT_UN_I32 = 76,
    R_CGT_UN_I64 = 77,
    R_CGT_UN_F32 = 78,
    R_CGT_UN_F64 = 79,
    R_CLT_I32 = 80,
    R_CLT_I64 = 81,
    R_CLT_F32 = 82,
    R_CLT_F64 = 83,
    R_CLT_UN_I32 = 84,
    R_CLT_UN_I64 = 85,
    R_CLT_UN_F32 = 86,
    R_CLT_UN_F64 = 87,
    R_CEQ_REF = 88,
    R_CGT_UN_REF = 89,
    // branches (a = absolute instruction index)
    R_BR = 90,
    R_BRTRUE_I32 = 91,
    R_BRTRUE_I64 = 92,
    R_BRTRUE_REF = 93,
    R_BRFALSE_I32 = 94,
    R_BRFALSE_I64 = 95,
    R_BRFALSE_REF = 96,
    R_BEQ_I32 = 97,
    R_BEQ_I64 = 98,
    R_BEQ_F32 = 99,
    R_BEQ_F64 = 100,
    R_BNE_UN_I32 = 101,
    R_BNE_UN_I64 = 102,
    R_BNE_UN_F32 = 103,
    R_BNE_UN_F64 = 104,
    R_BGE_I32 = 105,
    R_BGE_I64 = 106,
    R_BGE_F32 = 107,
    R_BGE_F64 = 108,
    R_BGT_I32 = 109,
    R_BGT_I64 = 110,
    R_BGT_F32 = 111,
    R_BGT_F64 = 112,
    R_BLE_I32 = 113,
    R_BLE_I64 = 114,
    R_BLE_F32 = 115,
    R_BLE_F64 = 116,
    R_BLT_I32 = 117,
    R_BLT_I64 = 118,
    R_BLT_F32 = 119,
    R_BLT_F64 = 120,
    R_BGE_UN_I32 = 121,
    R_BGE_UN_I64 = 122,
    R_BGE_UN_F32 = 123,
    R_BGE_UN_F64 = 124,
    R_BGT_UN_I32 = 125,
    R_BGT_UN_I64 = 126,
    R_BGT_UN_F32 = 127,
    R_BGT_UN_F64 = 128,
    R_BLE_UN_I32 = 129,
    R_BLE_UN_I64 = 130,
    R_BLE_UN_F32 = 131,
    R_BLE_UN_F64 = 132,
    R_BLT_UN_I32 = 133,
    R_BLT_UN_I64 = 134,
    R_BLT_UN_F32 = 135,
    R_BLT_UN_F64 = 136,
    R_BEQ_REF = 137,
    R_BNE_UN_REF = 138,
    // object / call
    R_CALL = 139,
    R_CALLVIRT = 140,
    R_NEWOBJ = 141,
    R_LDFTN = 142,
    R_LDFLD = 143,
    R_STFLD = 144,
    R_LDSFLD = 145,
    R_STSFLD = 146,
    R_CASTCLASS = 147,
    R_ISINST = 148,
    R_NEWARR = 149,
    R_LDLEN = 150,
    R_LDELEM = 151,
    R_STELEM = 152,
    // control / EH
    R_RET_VOID = 153,
    R_RET = 154,
    R_LEAVE = 155,
    R_ENDFINALLY = 156,
    R_THROW = 157,
    R_RETHROW = 158,
    // reserved (verifier-rejected)
    R_EXT = 159,
}

/// <summary>Operand-role predicates over <see cref="RegOp"/>, used by the
/// converter's copy-propagation peephole: which record fields an opcode reads
/// as source registers, and whether its r0 is a plain destination that can be
/// retargeted. The call family (R_CALL/R_CALLVIRT/R_NEWOBJ) intentionally
/// reports neither — its r0 is a contiguous-temp window base that the runtime
/// both reads through and writes the result to, never a free register
/// field.</summary>
internal static class RegOpFold
{
    /// <summary>r0 is a source register: value stores, ret/throw, and every
    /// conditional branch (for compare-branches r0 is the first
    /// operand).</summary>
    public static bool ReadsR0(RegOp op) =>
        op is RegOp.R_STFLD or RegOp.R_STSFLD or RegOp.R_STELEM
            or RegOp.R_RET or RegOp.R_THROW
        || (op >= RegOp.R_BRTRUE_I32 && op <= RegOp.R_BNE_UN_REF);

    /// <summary>r1 is a source register.</summary>
    public static bool ReadsR1(RegOp op) =>
        op == RegOp.R_MOV
        || (op >= RegOp.R_ADD_I32 && op <= RegOp.R_CGT_UN_REF) // arith/unary/conv/cmp
        || (op >= RegOp.R_BEQ_I32 && op <= RegOp.R_BNE_UN_REF) // compare-branches
        || op is RegOp.R_LDFLD or RegOp.R_STFLD or RegOp.R_CASTCLASS or RegOp.R_ISINST
            or RegOp.R_NEWARR or RegOp.R_LDLEN or RegOp.R_LDELEM or RegOp.R_STELEM;

    /// <summary>The `a` field's low byte is the source register r2 — exactly
    /// the C3 operand class (binary arithmetic, comparisons,
    /// ldelem/stelem).</summary>
    public static bool ReadsR2(RegOp op) =>
        (op >= RegOp.R_ADD_I32 && op <= RegOp.R_SHR_UN_I64)
        || (op >= RegOp.R_CEQ_I32 && op <= RegOp.R_CGT_UN_REF)
        || op is RegOp.R_LDELEM or RegOp.R_STELEM;

    /// <summary>r0 is a plain destination register (written after every
    /// source read and every possible throw, never read): safe to retarget
    /// from an eval temp to an arg/local slot.</summary>
    public static bool WritesR0(RegOp op) =>
        (op >= RegOp.R_MOV && op <= RegOp.R_CGT_UN_REF) // mov/constants/arith/unary/conv/cmp
        || op is RegOp.R_LDFTN or RegOp.R_LDFLD or RegOp.R_LDSFLD
            or RegOp.R_CASTCLASS or RegOp.R_ISINST
            or RegOp.R_NEWARR or RegOp.R_LDLEN or RegOp.R_LDELEM;
}

/// <summary>Register-model limits (docs/BPI-FORMAT.md "Register model"),
/// mirroring the C++ kRegSlotLimit/kRegTempLimit: args + locals and the
/// eval-temp region are each capped at 64, so a frame never exceeds 128
/// registers and every register operand fits a u8. Both are converter-enforced
/// (a method over either is a conversion-time rejection).</summary>
internal static class RegFormat
{
    public const int SlotLimit = 64; // argCount + localCount
    public const int TempLimit = 64; // maxStack = the simulated maximum eval depth
}
