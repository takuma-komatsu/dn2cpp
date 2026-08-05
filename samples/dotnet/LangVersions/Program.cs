// Gate driver for the C# language-version bucket: runs one section per C# major
// version, in order. Each section keeps its own namespace so that the type names
// it prints stay stable.
//
// This file is deliberately written as C# 9 TOP-LEVEL STATEMENTS, and that is not
// a stylistic choice: a compilation may have at most one file with top-level
// statements and that file becomes the entry point, so the driver is the only
// place in this bucket where the feature can be exercised at all. It doubles as
// the assertion that the transpiler finds the synthesized `<Program>$::<Main>$`
// entry point.

using System.Globalization;

// Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

Cs01.Program.__GateEntry();
Cs02.Program.__GateEntry();
Cs03.Program.__GateEntry();
Cs04.Program.__GateEntry();
Cs05.Program.__GateEntry();
Cs06.Program.__GateEntry();
Cs07.Program.__GateEntry();
Cs08.Program.__GateEntry();
Cs09.Program.__GateEntry();
Cs10.Program.__GateEntry();
Cs11.Program.__GateEntry();
Cs12.Program.__GateEntry();
Cs13.Program.__GateEntry();
Cs14.Program.__GateEntry();

// Feature sections belonging to the versions named beside them. They are driven
// from the TAIL rather than from inside their Cs0N section so that the sections
// above keep their output order.
IteratorSubset.Program.__GateEntry();               // C# 2 — yield state machines
NullableValueSubset.Program.__GateEntry();          // C# 2 — Nullable<T> value semantics
TupleSubset.Program.__GateEntry();                  // C# 7 — value tuples
DefaultInterfaceMethodSubset.Program.__GateEntry(); // C# 8 — default interface methods
RecordSubset.Program.__GateEntry();                 // C# 9 — positional records
