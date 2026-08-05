using System;

namespace Dn2Cpp.Runtime;

/// <summary>
/// Marks a method (or constructor) as a hot path: the transpiler routes its body
/// into a dedicated translation unit (<c>generated_hot.cpp</c>) compiled with
/// stronger per-file optimization flags (see <c>runtime/CMakeLists.txt</c>; the
/// ISA is widened via the <c>DN2CPP_HOT_ARCH</c> CMake cache variable). The bare
/// attribute changes no semantics: any method qualifies.
///
/// So that the flags actually apply, a marked body is never shared as a generic
/// canonical body (each instantiation is monomorphic) and never promoted into the
/// shared header as an <c>inline</c> definition — either would compile it under
/// some other TU's plain flags.
///
/// Matched by full name only (<c>Dn2Cpp.Runtime.HotPathAttribute</c>), so an
/// assembly that must not reference Dn2Cpp.Runtime may declare its own internal
/// copy. Under a normal .NET runtime the attribute is inert.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
public sealed class HotPathAttribute : Attribute
{
    /// <summary>Opt-in: elide bounds checks in the marked body. Array element
    /// loads, stores and address-taking compile to raw indexing (no index check,
    /// and no null check either — a null array faults like a field access), and a
    /// <c>Span&lt;T&gt;</c>/<c>ReadOnlySpan&lt;T&gt;</c> indexer read compiles to
    /// raw <c>reference + index</c> pointer arithmetic. An out-of-range index is
    /// undefined behavior: this knob declares that every index is in range, it is
    /// not a request for a faster exception. Inert under a normal .NET runtime.
    /// Default false.</summary>
    public bool SkipBoundsChecks { get; set; }

    /// <summary>Opt-in verifying knob: the marked method's direct-call closure
    /// must contain no allocation and no dynamic dispatch. Violations fail the
    /// transpile, naming the offending method and the call chain reaching it.
    /// Dynamic dispatch (virtual, interface, generic-virtual, delegate
    /// <c>Invoke</c>, function-pointer <c>calli</c>) counts because its target is
    /// not statically provable. Allocation on a throwing edge is allowed — a throw
    /// ends the hot path — so checked array access and the checked <c>Span</c>
    /// indexer verify clean and <see cref="SkipBoundsChecks"/> is a speed choice
    /// rather than a NoAlloc requirement. Inert under a normal .NET runtime.
    /// Default false.</summary>
    public bool NoAlloc { get; set; }

    /// <summary>Opt-in: compile the marked body with relaxed floating-point
    /// semantics, in a second dedicated translation unit
    /// (<c>generated_hot_fast.cpp</c>) carrying the hot TU's flags plus
    /// <c>-ffast-math</c> / <c>/fp:fast</c>. Results may differ from IEEE-exact
    /// .NET, so a marked kernel's output is not bit-reproducible and must not be
    /// compared for equality against an unmarked one. A separate TU because
    /// relaxed FP is a translation-unit property — one kernel opting out of exact
    /// arithmetic must not drag the rest of the marked set with it. Inert under a
    /// normal .NET runtime. Default false.</summary>
    public bool FastMath { get; set; }

    /// <summary>Opt-in: declare that the marked body's pointer-like parameters do
    /// not alias one another. Array, multidimensional-array, byref and
    /// unmanaged-pointer parameters are emitted <c>__restrict</c>, and a
    /// <c>Span&lt;T&gt;</c>/<c>ReadOnlySpan&lt;T&gt;</c> parameter's element
    /// pointer is hoisted into a <c>__restrict</c> prologue local — effective only
    /// together with <see cref="SkipBoundsChecks"/>, since the checked indexer
    /// reads the span struct's address rather than that pointer. The receiver and
    /// class-typed parameters are never qualified: two object references may
    /// legitimately alias, so qualifying one would be a correctness claim rather
    /// than a hint. Unqualified is not exempt, though — <c>restrict</c> forbids
    /// memory written through a qualified parameter from being reached by any
    /// other lvalue in the body, a <c>this</c> field or a static included.
    ///
    /// Nothing is verified: passing overlapping buffers is undefined behavior, as
    /// is reseating a span parameter through a <c>ref Span&lt;T&gt;</c> callee (a
    /// plain <c>s = s.Slice(…)</c> assignment is detected and opts that parameter
    /// out of the hoist). Inert under a normal .NET runtime. Default false.</summary>
    public bool NoAlias { get; set; }
}
