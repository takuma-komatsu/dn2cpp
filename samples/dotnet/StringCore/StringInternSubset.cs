#nullable disable
using System;

// string.Intern / string.IsInterned over the process-wide pool. Every ldstr literal
// is interned; a runtime-built string with content no literal carries stays out of
// the pool until Intern adds it, and that first Intern returns the argument itself.
// Only reference-identity facts that hold in both runtimes are asserted.
namespace StringInternSubset;

internal static class Program
{
    internal static int Run()
    {
        string lit = "intern_lit_xyz";
        string synth = string.Concat("intern_lit", "_xyz");

        // The synthesized copy is a distinct instance, but interning it lands on
        // the literal's pooled reference.
        Console.WriteLine($"distinct={!ReferenceEquals(synth, lit)}");
        Console.WriteLine($"internHit={ReferenceEquals(string.Intern(synth), lit)}");
        Console.WriteLine($"isInternedLit={ReferenceEquals(string.IsInterned(lit), lit)}");
        Console.WriteLine($"isInternedSynth={ReferenceEquals(string.IsInterned(synth), lit)}");

        // Content that appears in no literal: not pooled until Intern adds it,
        // and first-intern returns the argument itself.
        string fresh = string.Concat("nEvEr_intern", "ed_", 42.ToString());
        Console.WriteLine($"missIsNull={string.IsInterned(fresh) is null}");
        Console.WriteLine($"firstIntern={ReferenceEquals(string.Intern(fresh), fresh)}");
        Console.WriteLine($"nowPooled={ReferenceEquals(string.IsInterned(string.Concat("nEvEr_intern", "ed_", 42.ToString())), fresh)}");

        // The empty string is pooled.
        Console.WriteLine($"empty={string.IsInterned("") is not null}");
        return 0;
    }
}
