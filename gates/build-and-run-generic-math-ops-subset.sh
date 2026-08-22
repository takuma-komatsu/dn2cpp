#!/usr/bin/env bash
# Generic-math static-abstract interface members lowered to scalar primitive ops:
# the constants (INumberBase<T>.Zero/One), arithmetic operators (op_Addition/
# op_Subtraction/op_Multiply/op_Division/op_UnaryNegation), comparison operators
# (op_LessThan/op_GreaterThan) and the interface-routed conversion
# (T.CreateChecked -> INumberBase<T>.TryConvertFromChecked). These are exactly the
# shapes the real System.Linq Sum/Average/Min/Max/Range reach once T is closed to a
# primitive; MethodCompiler.TryEmitGenericMathIntrinsic emits the primitive op and
# the constrained `call` to a static-abstract member devirtualizes to the concrete
# impl. The app drives the patterns through generic methods; output diffs exact vs
# real .NET. CoreLib only (no Linq shim). FloatingPointMath covers the floating-
# point interfaces (IRootFunctions/ITrigonometricFunctions/IExponentialFunctions/
# ILogarithmicFunctions/IPowerFunctions/IHyperbolicFunctions/IFloatingPointIeee754/
# IFloatingPoint) with TSelf closed to double/float/Half. ShiftModulusOps covers
# op_Modulus (IModulusOperators; integers/float/double/decimal), the IShiftOperators
# trio (<< / >> / >>>, storage-width amount masking) and op_CheckedDivision.
# ClassOperators covers reference-type TSelf: class operator implementors (base-
# class-provided impls included) plus the canonical-shared fallback to
# per-instantiation expansion. ConformanceSweep covers the remaining
# INumber<T>-family surface (Clamp/CopySign/Max/Min/Sign, Abs + the magnitude
# selectors, Radix + the category predicates, storage-width Min/MaxValue,
# IsPow2/Log2/AllBitsSet, the bit counts/rotates/DivRem, ~) and the
# IParsable/ISpanParsable constrained parse route, with decimal/Half spot-checks
# and the IEEE NaN / negative-zero edges pinned. OutSlotWidth is not about generic
# math at all: it pins the STORE WIDTH of the `out` those conversions write through —
# a packed array element and a struct field must get a storage-width store, and a
# promoted local slot must additionally be re-normalized — for the all-primitive arm
# and for the intrinsic-decimal one. It is also the corpus's only direct caller of the
# `protected` static-abstract TryConvert* members, reached through a helper interface
# deriving from INumberBase<byte>, which is the whole reason that interface exists.
# StaticAbstractGenericMethod covers a static abstract GENERIC method on a generic
# interface (IPackable<TSelf>.Pack<TSink>) dispatched through a constrained call:
# the explicit implementation's .override row names an open generic MemberRef, and
# the impl is template-matched and instantiated at the caller's method args.
# InterfaceStaticImpl covers an INTERFACE as the constrained type argument: the
# interface's own explicit static impls resolve the static abstract members, and
# an unimplemented static-virtual default still binds the default body.
source "$(dirname "$0")/_common.sh"

corelib_diff_gate GenericMathOpsSubset System.Runtime
