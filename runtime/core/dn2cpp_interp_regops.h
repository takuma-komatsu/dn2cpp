// dn2cpp_interp_regops.h — the register code format's dense opcode table
// (docs/BPI-FORMAT.md "Register code format (Header.flags bit0)").
//
// The table in docs/BPI-FORMAT.md is NORMATIVE: this X-macro list and the
// converter's C# mirror (RegOps.cs) must match it value for value. Values are
// assigned sequentially in table order starting at 0 (asserted below), so the
// list order is load-bearing.
//
// Each entry is X(name, value, operandClass). The operand class fixes which
// instruction-record fields are registers and which are payloads — it drives
// both the dispatch decode and the load-time verifier:
//
//   C0: no registers;      a = branch target / EH index / unused, b = payload
//   C1: r0;                a, b = payloads
//   C2: r0, r1;            a, b = payloads
//   C3: r0, r1, r2 (= a & 0xFF); b = payload
//
// Register operands ride the record's second u16 (low byte = r0, high byte =
// r1; 0xFF = unused) — the field the stack encoding calls `pfx`.
//
// Standalone header, included by dn2cpp_interp.cpp only.
#pragma once

#include <cstdint>

// Operand classes (the third X-macro column pastes onto kRegClass).
enum Dn2CppRegOpClass : uint8_t
{
    kRegClassC0 = 0,
    kRegClassC1 = 1,
    kRegClassC2 = 2,
    kRegClassC3 = 3,
};

// Register-model limits (docs/BPI-FORMAT.md "Register model"): args + locals
// and the eval-temp region are each capped at 64, so a frame never exceeds
// 128 registers and every register operand fits a u8.
enum : uint32_t
{
    kRegSlotLimit = 64, // argCount + localCount
    kRegTempLimit = 64, // maxStack = the simulated maximum eval depth
};

#define DN2CPP_REGOPS(X) \
    /* invalid / data movement */ \
    X(R_INVALID, 0, C0) \
    X(R_MOV, 1, C2) \
    /* constants (r0 = dst) */ \
    X(R_LDNULL, 2, C1) \
    X(R_LDC_I32, 3, C1) \
    X(R_LDC_I64, 4, C1) \
    X(R_LDC_R4, 5, C1) \
    X(R_LDC_R8, 6, C1) \
    X(R_LDSTR, 7, C1) \
    /* binary arithmetic (r0 = dst, r1/r2 = operands) */ \
    X(R_ADD_I32, 8, C3) \
    X(R_ADD_I64, 9, C3) \
    X(R_SUB_I32, 10, C3) \
    X(R_SUB_I64, 11, C3) \
    X(R_MUL_I32, 12, C3) \
    X(R_MUL_I64, 13, C3) \
    X(R_DIV_I32, 14, C3) \
    X(R_DIV_I64, 15, C3) \
    X(R_DIV_UN_I32, 16, C3) \
    X(R_DIV_UN_I64, 17, C3) \
    X(R_REM_I32, 18, C3) \
    X(R_REM_I64, 19, C3) \
    X(R_REM_UN_I32, 20, C3) \
    X(R_REM_UN_I64, 21, C3) \
    X(R_AND_I32, 22, C3) \
    X(R_AND_I64, 23, C3) \
    X(R_OR_I32, 24, C3) \
    X(R_OR_I64, 25, C3) \
    X(R_XOR_I32, 26, C3) \
    X(R_XOR_I64, 27, C3) \
    X(R_ADD_F32, 28, C3) \
    X(R_ADD_F64, 29, C3) \
    X(R_SUB_F32, 30, C3) \
    X(R_SUB_F64, 31, C3) \
    X(R_MUL_F32, 32, C3) \
    X(R_MUL_F64, 33, C3) \
    X(R_DIV_F32, 34, C3) \
    X(R_DIV_F64, 35, C3) \
    X(R_REM_F32, 36, C3) \
    X(R_REM_F64, 37, C3) \
    X(R_SHL_I32, 38, C3) \
    X(R_SHL_I64, 39, C3) \
    X(R_SHR_I32, 40, C3) \
    X(R_SHR_I64, 41, C3) \
    X(R_SHR_UN_I32, 42, C3) \
    X(R_SHR_UN_I64, 43, C3) \
    /* unary (r0 = dst, r1 = src) */ \
    X(R_NEG_I32, 44, C2) \
    X(R_NEG_I64, 45, C2) \
    X(R_NEG_F32, 46, C2) \
    X(R_NEG_F64, 47, C2) \
    X(R_NOT_I32, 48, C2) \
    X(R_NOT_I64, 49, C2) \
    /* conversions (target-major; source order I32, I64, F) */ \
    X(R_CONV_I4_FROM_I32, 50, C2) \
    X(R_CONV_I4_FROM_I64, 51, C2) \
    X(R_CONV_I4_FROM_F, 52, C2) \
    X(R_CONV_U4_FROM_I32, 53, C2) \
    X(R_CONV_U4_FROM_I64, 54, C2) \
    X(R_CONV_U4_FROM_F, 55, C2) \
    X(R_CONV_I8_FROM_I32, 56, C2) \
    X(R_CONV_I8_FROM_I64, 57, C2) \
    X(R_CONV_I8_FROM_F, 58, C2) \
    X(R_CONV_U8_FROM_I32, 59, C2) \
    X(R_CONV_U8_FROM_I64, 60, C2) \
    X(R_CONV_U8_FROM_F, 61, C2) \
    X(R_CONV_R4_FROM_I32, 62, C2) \
    X(R_CONV_R4_FROM_I64, 63, C2) \
    X(R_CONV_R4_FROM_F, 64, C2) \
    X(R_CONV_R8_FROM_I32, 65, C2) \
    X(R_CONV_R8_FROM_I64, 66, C2) \
    X(R_CONV_R8_FROM_F, 67, C2) \
    /* comparisons (r0 = dst i32 0/1, r1/r2 = operands) */ \
    X(R_CEQ_I32, 68, C3) \
    X(R_CEQ_I64, 69, C3) \
    X(R_CEQ_F32, 70, C3) \
    X(R_CEQ_F64, 71, C3) \
    X(R_CGT_I32, 72, C3) \
    X(R_CGT_I64, 73, C3) \
    X(R_CGT_F32, 74, C3) \
    X(R_CGT_F64, 75, C3) \
    X(R_CGT_UN_I32, 76, C3) \
    X(R_CGT_UN_I64, 77, C3) \
    X(R_CGT_UN_F32, 78, C3) \
    X(R_CGT_UN_F64, 79, C3) \
    X(R_CLT_I32, 80, C3) \
    X(R_CLT_I64, 81, C3) \
    X(R_CLT_F32, 82, C3) \
    X(R_CLT_F64, 83, C3) \
    X(R_CLT_UN_I32, 84, C3) \
    X(R_CLT_UN_I64, 85, C3) \
    X(R_CLT_UN_F32, 86, C3) \
    X(R_CLT_UN_F64, 87, C3) \
    X(R_CEQ_REF, 88, C3) \
    X(R_CGT_UN_REF, 89, C3) \
    /* branches (a = absolute instruction index) */ \
    X(R_BR, 90, C0) \
    X(R_BRTRUE_I32, 91, C1) \
    X(R_BRTRUE_I64, 92, C1) \
    X(R_BRTRUE_REF, 93, C1) \
    X(R_BRFALSE_I32, 94, C1) \
    X(R_BRFALSE_I64, 95, C1) \
    X(R_BRFALSE_REF, 96, C1) \
    X(R_BEQ_I32, 97, C2) \
    X(R_BEQ_I64, 98, C2) \
    X(R_BEQ_F32, 99, C2) \
    X(R_BEQ_F64, 100, C2) \
    X(R_BNE_UN_I32, 101, C2) \
    X(R_BNE_UN_I64, 102, C2) \
    X(R_BNE_UN_F32, 103, C2) \
    X(R_BNE_UN_F64, 104, C2) \
    X(R_BGE_I32, 105, C2) \
    X(R_BGE_I64, 106, C2) \
    X(R_BGE_F32, 107, C2) \
    X(R_BGE_F64, 108, C2) \
    X(R_BGT_I32, 109, C2) \
    X(R_BGT_I64, 110, C2) \
    X(R_BGT_F32, 111, C2) \
    X(R_BGT_F64, 112, C2) \
    X(R_BLE_I32, 113, C2) \
    X(R_BLE_I64, 114, C2) \
    X(R_BLE_F32, 115, C2) \
    X(R_BLE_F64, 116, C2) \
    X(R_BLT_I32, 117, C2) \
    X(R_BLT_I64, 118, C2) \
    X(R_BLT_F32, 119, C2) \
    X(R_BLT_F64, 120, C2) \
    X(R_BGE_UN_I32, 121, C2) \
    X(R_BGE_UN_I64, 122, C2) \
    X(R_BGE_UN_F32, 123, C2) \
    X(R_BGE_UN_F64, 124, C2) \
    X(R_BGT_UN_I32, 125, C2) \
    X(R_BGT_UN_I64, 126, C2) \
    X(R_BGT_UN_F32, 127, C2) \
    X(R_BGT_UN_F64, 128, C2) \
    X(R_BLE_UN_I32, 129, C2) \
    X(R_BLE_UN_I64, 130, C2) \
    X(R_BLE_UN_F32, 131, C2) \
    X(R_BLE_UN_F64, 132, C2) \
    X(R_BLT_UN_I32, 133, C2) \
    X(R_BLT_UN_I64, 134, C2) \
    X(R_BLT_UN_F32, 135, C2) \
    X(R_BLT_UN_F64, 136, C2) \
    X(R_BEQ_REF, 137, C2) \
    X(R_BNE_UN_REF, 138, C2) \
    /* object / call */ \
    X(R_CALL, 139, C1) \
    X(R_CALLVIRT, 140, C1) \
    X(R_NEWOBJ, 141, C1) \
    X(R_LDFTN, 142, C1) \
    X(R_LDFLD, 143, C2) \
    X(R_STFLD, 144, C2) \
    X(R_LDSFLD, 145, C1) \
    X(R_STSFLD, 146, C1) \
    X(R_CASTCLASS, 147, C2) \
    X(R_ISINST, 148, C2) \
    X(R_NEWARR, 149, C2) \
    X(R_LDLEN, 150, C2) \
    X(R_LDELEM, 151, C3) \
    X(R_STELEM, 152, C3) \
    /* control / EH */ \
    X(R_RET_VOID, 153, C0) \
    X(R_RET, 154, C1) \
    X(R_LEAVE, 155, C0) \
    X(R_ENDFINALLY, 156, C0) \
    X(R_THROW, 157, C1) \
    X(R_RETHROW, 158, C0) \
    /* reserved (verifier-rejected) */ \
    X(R_EXT, 159, C0)

enum Dn2CppRegOp : uint16_t
{
#define DN2CPP_REGOP_ENUM_ENTRY(name, value, cls) name = (value),
    DN2CPP_REGOPS(DN2CPP_REGOP_ENUM_ENTRY)
#undef DN2CPP_REGOP_ENUM_ENTRY
};

enum : uint32_t
{
    kRegOpCount = 0
#define DN2CPP_REGOP_COUNT_ENTRY(name, value, cls) + 1
        DN2CPP_REGOPS(DN2CPP_REGOP_COUNT_ENTRY)
#undef DN2CPP_REGOP_COUNT_ENTRY
};

static_assert(kRegOpCount == 160, "the register opcode table is 160 entries (values 0-159)");
static_assert(R_INVALID == 0 && R_MOV == 1 && R_EXT == 159,
    "the register opcode table anchors moved");

// The list must stay sequential in value: positional lookups (the operand-class
// table in dn2cpp_interp.cpp) index by opcode value.
constexpr bool dn2cpp_regops_sequential()
{
    uint32_t next = 0;
    bool ok = true;
#define DN2CPP_REGOP_SEQ_ENTRY(name, value, cls) \
    if ((value) != next++) \
        ok = false;
    DN2CPP_REGOPS(DN2CPP_REGOP_SEQ_ENTRY)
#undef DN2CPP_REGOP_SEQ_ENTRY
    return ok;
}
static_assert(dn2cpp_regops_sequential(),
    "DN2CPP_REGOPS entries must be listed in value order from 0");
