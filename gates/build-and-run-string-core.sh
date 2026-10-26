#!/usr/bin/env bash
# Consolidated string-core gate. Merges the former per-feature string instance/
# static method subset gates into one multi-section program, transpiled once
# against the tree-shaken real CoreLib and diffed exactly against real .NET.
# Covers String methods (IndexOf/Contains/Replace/Substring/Trim/case/etc.),
# comparison/StartsWith/EndsWith, padding, Split, Join, string<->span,
# string.CopyTo(Span<char>), misc string constructors (char*, char[],
# ReadOnlySpan<char>), the broad string method surface, building strings from
# chars, and String.Create.
# Also covers string.Join/Concat over a List<T> that arrives as a CALL RESULT or
# a FIELD rather than a local (JoinCallResultSubset), and the
# StringComparer.CurrentCulture -> ordinal interception
# (OrdinalCultureComparerSubset).
# StringJoinSubset.RunSequences asserts string.Join and string.Concat over
# IEnumerable<string>: the ArgumentNullException a null List, collection or bare
# interface raises, Concat(object) formatting a collection argument as itself
# rather than joining its elements, and the enumerator disposed once the join
# ends; then the ArgumentNullException, with .NET's parameter name, that a null
# array raises in the array overloads of Join, Concat and StringBuilder.AppendJoin.
# StringJoinSubset.RunOperandShapes asserts that Join, Concat and AppendJoin take
# an IEnumerable<T> operand of any static type: a null literal's
# ArgumentNullException raised when the call runs, a conditional over an array
# and a list, a LINQ grouping, and a set seen through ISet<T> or IReadOnlySet<T>.
# AppendJoin preserves the partial builder on formatting failure, checks its
# receiver before values, and detects a List modified during enumeration.
# This project's reference set is deliberately NARROW: android-gdext,
# emit-order-stability, ios-sim-console and wasm-console each re-transpile
# StringCore with their own hand-written copy of it, so a fourth `-r` here is a
# fourth edit there and two of those are cross-compiles. The Join-over-a-concrete-
# IEnumerable section that would have needed System.Collections lives in
# DictCollections, which already carries it.
# Former gates: string-method, string-compare, string-pad, string-split,
# string-join, string-span, string-ctor-misc, string-surface, string-from-chars,
# string-create, join-callresult-subset, ordinal-culture-comparer-subset, plus
# the String-as-interface sections (LINQ over a string / CharEnumerator /
# IComparable-family dispatch / Intern), which need System.Linq.
# Indexed culture comparisons validate clamped windows before null ordering and options.
# Array CopyTo preserves the rejected Int32 and the bound in its argument faults.
# Insert and ToCharArray preserve unsigned start faults and signed window precedence.
# Array string constructors copy UTF-16, validate windows and share String.Empty.
# Remove overloads preserve distinct fault fields and padding rejects named widths.
# Empty repeat/span string constructors and char-span ToString share String.Empty.
# StringComparison faults preserve named messages and each overload's null precedence.
# Equals guards callvirt receivers while a direct IL call enters the real method.
# Search windows preserve named faults, empty-source precedence and unchecked counts.
# String argument validation preserves named faults, values and validation order.
# Composite format faults preserve UTF-16 positions, grammar reasons and argument effects.
# Runtime argument messages retain named/boxed operands across Web collections.
source "$(dirname "$0")/_common.sh"
string_python=$(resolve_python) || gate_skip "no working Python 3 interpreter for string call fixtures"

call_app="gates/fixtures/string-comparison-call/bin/$CONFIG/$TFM/StringComparisonCall.dll"
build_gate_proj gates/fixtures/string-comparison-call/StringComparisonCall.csproj
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} $call_app ${call_app%.dll}.runtimeconfig.json ${call_app%.dll}.deps.json gates/fixtures/string-comparison-call/patch-call.py"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|string-comparison-call|cli:$(_gate_cli_hash)"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|format-diagnostics-prefix-argv:before-format-diagnostics|argument-roots-prefix-argv:before-argument-roots"

gate_extra_asserts() {
    local out="$1" native before prefix line oracle fixture expected actual
    gate_capture_ok "$out/assert-native-1" run_bounded "./$out/StringCore"
    native="$out/assert-native-1.stdout"

    gate_capture_ok "$out/assert-before-2" run_bounded dotnet "$_CG_APP" before-join-sequences
    before="$out/assert-before-2.stdout"

    prefix="$out/assert-prefix-21.stdout"

    gate_output select "$native" "$prefix" --before '== string sequences ==' --policy crlf
    gate_output compare "$prefix" "$before"
    for line in '== string sequences ==' \
        '== string sequence operands ==' \
        'disposed: w1++w3 w1w3 2' \
        'sets: x,y pq 7|8 9 p+q' \
        '== AppendJoin failures ==' \
        'array: InvalidOperationException prefix:one,' \
        'span: InvalidOperationException prefix:one/' \
        'ienum array: InvalidOperationException prefix:one+' \
        'list: InvalidOperationException prefix:one-' \
        'changed list: InvalidOperationException prefix:first' \
        'null receiver strings: NullReferenceException' \
        'null receiver empty span: NullReferenceException'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: sequence witness missing: $line" >&2; exit 1; }
    done
    gate_capture_ok "$out/assert-before-3" run_bounded dotnet "$_CG_APP" before-indexed-compare
    before="$out/assert-before-3.stdout"

    prefix="$out/assert-prefix-22.stdout"

    gate_output select "$native" "$prefix" --before '== indexed culture compare ==' --policy crlf
    gate_output compare "$prefix" "$before"
    for line in '== indexed culture compare ==' 'bool fold=0' 'bool clamp=-1' \
        'options ordinal=-32' 'options ordinal-ci=0' \
        'bool evaluation=AIBJLFC' 'options evaluation=AIBJLCO' \
        'throwing culture evaluation=AIBJLC' 'indexed culture compare end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: indexed compare witness missing: $line" >&2; exit 1; }
    done
    gate_capture_ok "$out/assert-before-4" run_bounded dotnet "$_CG_APP" before-copyto-faults
    before="$out/assert-before-4.stdout"

    prefix="$out/assert-prefix-23.stdout"

    gate_output select "$native" "$prefix" --before '== string array copy faults ==' --policy crlf
    gate_output compare "$prefix" "$before"
    for line in '== string array copy faults ==' 'source-window actual=Int32:4' \
        'source-past-end actual=Int32:0' 'destination-window actual=Int32:1' \
        'destination-room-before-sign actual=Int32:-1' \
        'count-minimum actual=Int32:-2147483648' \
        'source-window destination=....' 'copy-window destination=.bcd.' \
        'empty-at-end copied' 'null receiver evaluation=SIDJC' \
        'throwing count evaluation=SIDJC' 'string array copy faults end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: array CopyTo witness missing: $line" >&2; exit 1; }
    done
    gate_capture_ok "$out/assert-before-5" run_bounded dotnet "$_CG_APP" before-window-faults
    before="$out/assert-before-5.stdout"

    prefix="$out/assert-prefix-24.stdout"

    gate_output select "$native" "$prefix" --before '== string window faults ==' --policy crlf
    gate_output compare "$prefix" "$before"
    for line in '== string window faults ==' 'insert-negative actual=UInt32:4294967295' \
        'insert-minimum actual=UInt32:2147483648' 'insert-null-before-index param=value' \
        'array-negative-start actual=UInt32:4294967295' 'array-window actual=Int32:1' \
        'array-minimum-length actual=Int32:0' 'array-negative-length actual=Int32:-1' \
        'insert-empty-identity result=True' 'insert-empty-source-identity result=True' \
        'array-empty-at-end result=0' 'independent array=abc:Qbc' \
        'empty array identities=True:True:True' 'empty array after GC=True:True' \
        'insert null evaluation=SIV' 'insert throwing value evaluation=SIV' \
        'array null evaluation=SIL' 'array throwing length evaluation=SIL' \
        'string window faults end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: string window witness missing: $line" >&2; exit 1; }
    done
    gate_capture_ok "$out/assert-before-6" run_bounded dotnet "$_CG_APP" before-array-ctors
    before="$out/assert-before-6.stdout"

    prefix="$out/assert-prefix-25.stdout"

    gate_output select "$native" "$prefix" --before '== string array constructors ==' --policy crlf
    gate_output compare "$prefix" "$before"
    for line in '== string array constructors ==' \
        'whole-utf16 result=0041 0000 D800 DC00 005A ' \
        'slice-utf16 result=0000 D800 DC00 ' \
        'whole empties=True:True:True:True:True' 'slice empties=True:True:True' \
        'ctor empty after GC=True:True' 'ctor fresh=False:False' \
        'ctor independent=abc:bc:aQc' 'null-before-bounds param=value' \
        'start-before-length actual=Int32:-1' 'start-minimum actual=Int32:-2147483648' \
        'length-before-window param=length' 'length-minimum actual=Int32:-2147483648' \
        'start-maximum actual=Int32:2147483647' 'length-maximum actual=Int32:0' \
        'slice-window param=startIndex' 'empty-bad-start actual=Int32:1' \
        'whole null evaluation=A' 'slice null evaluation=AIL' \
        'slice copy evaluation=AIL' 'slice throwing length evaluation=AIL' \
        'slice throwing array evaluation=A' 'string array constructors end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: string array constructor witness missing: $line" >&2; exit 1; }
    done
    gate_capture_ok "$out/assert-before-7" run_bounded dotnet "$_CG_APP" before-remove-padding
    before="$out/assert-before-7.stdout"

    prefix="$out/assert-prefix-26.stdout"

    gate_output select "$native" "$prefix" --before '== string remove and padding faults ==' --policy crlf
    gate_output compare "$prefix" "$before"
    for line in '== string remove and padding faults ==' \
        'remove-tail-negative actual=null' 'remove-tail-past-end param=startIndex' \
        'remove-start-before-count actual=Int32:-1' \
        'remove-start-minimum actual=Int32:-2147483648' \
        'remove-count-before-window param=count' 'remove-window actual=Int32:2' \
        'remove-past-end-zero actual=Int32:0' 'remove-count-maximum actual=Int32:2147483647' \
        'pad-left-default param=totalWidth' 'pad-left-explicit actual=Int32:-2147483648' \
        'pad-right-default param=totalWidth' 'pad-right-explicit actual=Int32:-1' \
        'remove unchanged=True:True:True' 'remove empty identities=True:True:True:True' \
        'constructed empty 0=True:True' 'constructed empty 1=True:True' \
        'pad unchanged=True:True:True:True' 'remove-copy result=ac' 'remove-fresh result=False' \
        'remove-utf16 result=0041 0000 DC00 005A ' \
        'remove identity after GC=True:True' 'remove null evaluation=SIC' \
        'remove copy evaluation=SIC' 'remove throwing count evaluation=SIC' \
        'pad null evaluation=SWP' 'pad throwing padding evaluation=SWP' \
        'string remove and padding faults end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: string remove/padding witness missing: $line" >&2; exit 1; }
    done
    gate_capture_ok "$out/assert-before-8" run_bounded dotnet "$_CG_APP" before-empty-char-sources
    before="$out/assert-before-8.stdout"

    prefix="$out/assert-prefix-27.stdout"

    gate_output select "$native" "$prefix" --before '== empty string char sources ==' --policy crlf
    gate_output compare "$prefix" "$before"
    for line in '== empty string char sources ==' \
        'repeat empties=True:True:True:True' \
        'span ctor empties=True:True:True:True:True:True:True' \
        'span tostring empties=True:True:True:True' \
        'repeat units=D800 D800 D800 ' 'span units=0000 D800 DC00 ' \
        'char source fresh=False:False:False' 'span string fresh=False' \
        'span independent=0000 D800 DC00 :0000 D800 DC00 ' \
        'char source empty after GC=True:True:True' \
        'repeat negative param=count' 'repeat negative actual=Int32:-1' \
        'repeat minimum actual=Int32:-2147483648' \
        'repeat empty evaluation=CN' 'repeat negative evaluation=CN' \
        'repeat throwing count evaluation=CN' 'repeat throwing char evaluation=C' \
        'span empty evaluation=S' 'empty string char sources end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: empty char source witness missing: $line" >&2; exit 1; }
    done
    gate_capture_ok "$out/assert-before-9" run_bounded dotnet "$_CG_APP" before-comparison-faults
    before="$out/assert-before-9.stdout"

    prefix="$out/assert-prefix-28.stdout"

    gate_output select "$native" "$prefix" --before '== string comparison faults ==' --policy crlf
    gate_output compare "$prefix" "$before"
    for line in '== string comparison faults ==' \
        'starts -1 param=comparisonType' 'char index 6 param=comparisonType' \
        'index start -2147483648 param=comparisonType' \
        'last start 2147483647 param=comparisonType' \
        'null starts -1 type=ArgumentNullException' 'null starts -1 param=value' \
        'null index start 6 param=value' 'null last start 6 param=value' \
        'receiver char 6 type=NullReferenceException' \
        'equals receiver 6 type=NullReferenceException' \
        'valid equals receiver type=NullReferenceException' \
        'compare null 6 param=comparisonType' 'equals null 6 param=comparisonType' \
        'replace null 6 param=comparisonType' 'hash empty 6 param=comparisonType' \
        'span equals 6 param=comparisonType' 'plain contains param=value' \
        'valid 0=True:True:True:1:1' 'valid 5=True:True:True:1:1' \
        'empty 5=True:True:True:0:0' 'ordinal unicode=0:-1' \
        'null evaluation=SVC' 'throwing comparison evaluation=SVC' \
        'equals evaluation=SVC' 'equals throwing comparison evaluation=SVC' \
        'string comparison faults end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: string comparison fault witness missing: $line" >&2; exit 1; }
    done
    gate_capture_ok "$out/assert-before-10" run_bounded dotnet "$_CG_APP" before-search-range-faults
    before="$out/assert-before-10.stdout"

    prefix="$out/assert-prefix-29.stdout"

    gate_output select "$native" "$prefix" --before '== string search range faults ==' --policy crlf
    gate_output compare "$prefix" "$before"
    for line in '== string search range faults ==' \
        'index char start 0:-2147483648 param=startIndex' \
        'last char start 0:2147483647 param=startIndex' \
        'index char range 0:0:-1 param=count' \
        'index char range 0:0:-1 actual=null' \
        'last string range 0:4:-1 param=count' \
        'last string range 0:5:0 param=startIndex' \
        'last empty range 1:0:-2147483648 result=0' \
        'last char start 1:2147483647 result=-1' \
        'index null set param=anyOf' 'index null set range param=anyOf' \
        'last null set param=anyOf' 'last null set range param=anyOf' \
        'empty last null set param=anyOf' 'empty last set result=-1' \
        'empty index bad start param=startIndex' \
        'index null value param=value' 'last null value param=value' \
        'range utf16=2:1' 'range null evaluation=SAIC' \
        'range throwing count evaluation=SAIC' 'string search range faults end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: string search range fault witness missing: $line" >&2; exit 1; }
    done
    oracle="$out/direct-call-oracle"
    fixture="$out/direct-call"
    mkdir -p "$oracle"
    cp "$call_app" "$oracle/StringComparisonCall.dll"
    cp "${call_app%.dll}.runtimeconfig.json" "${call_app%.dll}.deps.json" "$oracle/"
    $string_python gates/fixtures/string-comparison-call/patch-call.py "$oracle/StringComparisonCall.dll"
    invoke_cli "$oracle/StringComparisonCall.dll" -r "$_CG_CORELIB" -o "$fixture"
    compile_console "$fixture" StringComparisonCall
    gate_capture_ok "$out/assert-expected-11" run_bounded dotnet "$oracle/StringComparisonCall.dll"
    expected="$out/assert-expected-11.stdout"
    gate_capture_ok "$out/assert-actual-12" run_bounded "./$fixture/StringComparisonCall"
    actual="$out/assert-actual-12.stdout"

    gate_output compare "$actual" "$expected"
    for line in 'direct both null valid=True' 'direct ignore case=True' \
        "direct both null invalid=ArgumentException:The string comparison type passed in is currently not supported. (Parameter 'comparisonType')" \
        "direct same invalid=ArgumentException:The string comparison type passed in is currently not supported. (Parameter 'comparisonType')" \
        'virtual both null valid=NullReferenceException:Object reference not set to an instance of an object.' \
        'virtual both null invalid=NullReferenceException:Object reference not set to an instance of an object.'; do
        gate_output byte-witness "$actual" "$line" --count any --policy crlf \
            || { echo "FAIL: direct comparison call witness missing: $line" >&2; exit 1; }
    done
    gate_capture_ok "$out/assert-native-13" run_bounded "./$out/StringCore"
    native="$out/assert-native-13.stdout"

    gate_capture_ok "$out/assert-before-14" run_bounded dotnet "$_CG_APP" before-span-flag-faults
    before="$out/assert-before-14.stdout"

    prefix="$out/assert-prefix-30.stdout"

    gate_output select "$native" "$prefix" --before '== String span and flag faults ==' --policy crlf
    gate_output compare "$prefix" "$before"
    for line in '== String span and flag faults ==' \
        'span copy:0 param=destination' 'span copy:2 param=destination' \
        'empty span copy success' 'split char flag:4 param=options' \
        'split string flag:4 param=options' 'String span and flag faults end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: span and flag fault witness missing: $line" >&2; exit 1; }
    done

    gate_capture_ok "$out/assert-before-15" run_bounded dotnet "$_CG_APP" before-argument-fields

    before="$out/assert-before-15.stdout"

    prefix="$out/assert-prefix-31.stdout"

    gate_output select "$native" "$prefix" --before '== String argument fields ==' --policy crlf
    gate_output compare "$prefix" "$before"
    for line in '== String argument fields ==' \
        'replace:0:0:0 param=oldValue' \
        'replace:0:1:0 param=oldValue' \
        'split:0:-1:-1:char param=count' \
        'split:0:-1:-1:char actual=Int32:-1' \
        'split:0:0:-1:char param=options' \
        'normalize:0:-1 param=normalizationForm' \
        'is normalized:0:-1 param=normalizationForm' \
        'join:0:0:-1:-1 param=value' \
        'join:2:0:-1:-1 param=startIndex' \
        'join:2:0:0:-1 param=count' \
        'intern null param=str' \
        'is interned null param=str' \
        'fault after GC:1 actual=Int32:-7' \
        'String argument fields end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: String argument field witness missing: $line" >&2; exit 1; }
    done

    gate_capture_ok "$out/assert-before-16" run_bounded dotnet "$_CG_APP" before-boolean-compare

    before="$out/assert-before-16.stdout"
    prefix="$out/assert-prefix-32.stdout"
    gate_output select "$native" "$prefix" --before '== Boolean string comparison equality ==' --policy crlf
    gate_output compare "$prefix" "$before"
    for line in '== Boolean string comparison equality ==' \
        'Name:name:True=True:True' 'Name:name:False=False:False' \
        '<null>::True=False:False' '名前:名前:False=True:True' \
        'Boolean string comparison equality end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: Boolean comparison witness missing: $line" >&2; exit 1; }
    done

    gate_capture_ok "$out/assert-before-17" run_bounded dotnet "$_CG_APP" before-format-diagnostics

    before="$out/assert-before-17.stdout"
    prefix="$out/assert-prefix-33.stdout"
    gate_output select "$native" "$prefix" --before '== String composite format diagnostics ==' --policy crlf
    gate_output compare "$prefix" "$before"
    gate_capture_ok "$out/assert-before-18" run_bounded "./$out/StringCore$EXE_EXT" before-format-diagnostics
    before="$out/assert-before-18.stdout"
    gate_output compare "$prefix" "$before"
    for line in '== String composite format diagnostics ==' \
        'valid-one result=[7] trace= calls=0' \
        'valid-null-spec result=[[]] trace= calls=0' \
        'valid-two result=[7-11] trace= calls=0' \
        'valid-three result=[7-11-13] trace= calls=0' \
        'valid-array result=[13/7/11] trace= calls=0' \
        'valid-spaces result=[[7   ][  0B]] trace= calls=0' \
        'valid-provider-one result=[1.25] trace= calls=0' \
        'valid-provider-span result=[1.25/11] trace= calls=0' \
        'valid-span result=[7/11/13/17] trace= calls=0' \
        'value-repeated result=[value1/value1] trace=T1T1 calls=2' \
        'evaluation-order result=[value3/value1/value2] trace=F123T3T1T2 calls=3' \
        'argument-throw-before-grammar fault=InvalidOperationException:argument-fault trace=F1X calls=0' \
        'array-evaluation result=[value1] trace=FAT1 calls=1' \
        'provider-evaluation result=[value1] trace=PF1T1 calls=1' \
        'String composite format diagnostics end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: composite format witness missing: $line" >&2; exit 1; }
    done
    gate_capture_ok "$out/assert-before-19" run_bounded dotnet "$_CG_APP" before-argument-roots
    before="$out/assert-before-19.stdout"
    prefix="$out/assert-prefix-34.stdout"
    gate_output select "$native" "$prefix" --before '== Collected runtime argument operands ==' --policy crlf
    gate_output compare "$prefix" "$before"
    gate_capture_ok "$out/assert-before-20" run_bounded "./$out/StringCore$EXE_EXT" before-argument-roots
    before="$out/assert-before-20.stdout"
    gate_output compare "$prefix" "$before"
    for line in '== Collected runtime argument operands ==' 'Collected runtime argument operands end'; do
        gate_output witness "$native" "$line" --count 1 --policy crlf \
            || { echo "FAIL: runtime argument root block missing or repeated: $line" >&2; exit 1; }
    done
    gate_output view "$native" "$out/argument-roots.lf" --policy crlf
    for line in 'roots signed' 'roots unsigned' 'roots float' 'roots double' \
        'roots managed message' 'roots monitor message' 'roots boxed actual'; do
        [ "$(grep -Ec "^$line=Argument(OutOfRange)?Exception:" "$out/argument-roots.lf")" = 1 ] \
            || { echo "FAIL: collected runtime argument witness missing or repeated: $line" >&2; exit 1; }
    done
}

corelib_diff_gate StringCore System.Linq
