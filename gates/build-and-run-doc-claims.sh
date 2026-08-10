#!/usr/bin/env bash
# The gate that checks a design doc's factual claims against the tree.
#
# It checks the MECHANICAL half only: a name a doc spells against a file that
# must exist, a count it states against the thing counted, two hand-written
# copies of one set against each other. It cannot check a claim about behaviour.
#
# So the standing rule for the docs this gate covers: **do not write a bare
# number.** Write it so this file can count it, or do not write it — a count
# nothing counts is a claim with a decay rate.
#
# It runs no build, no transpile and no binary, needs no toolchain and finishes
# in well under a second, so it can never `gate_skip`. It deliberately takes no
# result cache: a key that forgot a doc would replay a green over exactly the
# drift this gate exists to catch.
#
# Adding a claim: put the check in the section for its file. Two rules — the
# failure message must print the MEASURED value, so the fix is a copy out of the
# log; and check the doc against the TREE, never against a second copy of the
# number in another doc.
source "$(dirname "$0")/_common.sh"

FAILS=0
CHECKS=0

ok()  { CHECKS=$((CHECKS + 1)); printf '  ok    %s\n' "$1"; }
bad() { CHECKS=$((CHECKS + 1)); FAILS=$((FAILS + 1)); printf '  FAIL  %s\n' "$1" >&2; }

# eq LABEL EXPECTED ACTUAL [REMEDY] — EXPECTED is what the doc says, ACTUAL what
# the tree measures. The message names both, in that order, always.
eq() {
    if [ "$2" = "$3" ]; then
        ok "$1 = $3"
    else
        bad "$1: doc says '$2', tree measures '$3'${4:+ — $4}"
    fi
}

# set_eq LABEL LEFT_NAME LEFT_LIST RIGHT_NAME RIGHT_LIST — both lists
# newline-separated and sorted. Reports each side of the difference BY NAME: the
# two sides fail for opposite reasons (something undocumented vs something
# documented that is gone), so the message has to say which side is which.
set_eq() {
    local label="$1" lname="$2" rname="$4" only_l only_r
    only_l=$(comm -23 <(printf '%s\n' "$3") <(printf '%s\n' "$5") | tr '\n' ' ')
    only_r=$(comm -13 <(printf '%s\n' "$3") <(printf '%s\n' "$5") | tr '\n' ' ')
    if [ -z "${only_l// }" ] && [ -z "${only_r// }" ]; then
        ok "$label (both sides identical)"
    else
        bad "$label: $lname only: [${only_l:-none}]; $rname only: [${only_r:-none}]"
    fi
}

# w2n WORD — the docs spell small counts as English words, which is good prose
# and still a claim. Unknown word yields '?', which can never equal a number.
w2n() {
    case "$(printf '%s' "$1" | tr '[:upper:]' '[:lower:]')" in
        one) echo 1 ;;      two) echo 2 ;;       three) echo 3 ;;
        four) echo 4 ;;     five) echo 5 ;;      six) echo 6 ;;
        seven) echo 7 ;;    eight) echo 8 ;;     nine) echo 9 ;;
        ten) echo 10 ;;     eleven) echo 11 ;;   twelve) echo 12 ;;
        thirteen) echo 13 ;; fourteen) echo 14 ;; fifteen) echo 15 ;;
        sixteen) echo 16 ;; seventeen) echo 17 ;; eighteen) echo 18 ;;
        nineteen) echo 19 ;; twenty) echo 20 ;;
        *) echo '?' ;;
    esac
}

# md_table_rows FILE ANCHOR — the DATA rows of the first markdown table at or
# after the line beginning with ANCHOR. The anchor is either a `## ` heading (the
# table follows it) or the table's own header row, for a table that has no
# heading of its own. A section with no table yields nothing rather than reaching
# into the NEXT section's table, which is what the `^## ` exit is for.
md_table_rows() {
    awk -v h="$2" '
        !f {
            if (index($0, h) != 1) next
            f = 1
            if ($0 ~ /^\|/) { t = 1; n = 1 }
            next
        }
        !t { if ($0 ~ /^\|/) t = 1; else if ($0 ~ /^## /) exit; else next }
        { if ($0 !~ /^\|/) exit; n++; if (n > 2) print }
    ' "$1"
}

# md_col FILE HEADING_PREFIX N — column N of every data row, trimmed.
md_col() {
    md_table_rows "$1" "$2" | awk -F'|' -v c="$3" '{ f = $(c + 1); gsub(/^[ \t]+|[ \t]+$/, "", f); print f }'
}

# check_paths FILE — every repo-relative path the file cites in backticks must
# exist, so a renamed or deleted gate script / sample directory turns the citing
# doc red instead of leaving it reading plausibly.
#
# A GITIGNORED path is exempt, and that is not a loophole — it is the difference
# between citing tree content and citing an OUTPUT. `gates/out-*/gaps.txt` is a
# thing a harness produces; requiring it to exist would fail on every clean
# checkout, i.e. the check would be red for the one reason that says nothing.
#
# One consequence to know rather than to discover, since `docs/STATUS.md` is in
# scope: a backlog row naming a file the ticket proposes to CREATE fails here.
# That is the correct trade; spell such a path without the backticked form.
check_paths() {
    local f="$1" p n=0
    for p in $(grep -oE '`(gates|samples|src|runtime|docs|dist|internal|third_party)/[A-Za-z0-9_./+-]*`' "$f" \
               | tr -d '`' | sed 's#/$##' | sort -u); do
        [ -e "$p" ] && { n=$((n + 1)); continue; }
        git check-ignore -q "$p" 2>/dev/null && { n=$((n + 1)); continue; }
        bad "$f cites '$p', which does not exist and is not a gitignored output"
    done
    ok "$f: all $n cited repo paths exist"
}

echo "== 1/11 docs/PINVOKE-MARSHALLING.md — rows against the PInvokeNative bucket =="
PM=docs/PINVOKE-MARSHALLING.md
BUCKET=samples/dotnet/PInvokeNative

# Every section of this bucket is named `*Subset`, and the doc names each one in
# backticks. Set equality both ways is the strongest cheap statement available:
# a section folded in and not documented fails, a documented section renamed or
# deleted fails, and neither can be answered by a count.
doc_sections=$(grep -o '`[A-Za-z0-9_]*Subset`' "$PM" | tr -d '`' | sort -u)
tree_sections=$(ls "$BUCKET"/*.cs | xargs -n1 basename | sed 's/\.cs$//' | grep -v '^Program$' | sort -u)
set_eq "PInvokeNative sections: doc vs bucket" "named by the doc" "$doc_sections" "present in the bucket" "$tree_sections"

# A section that exists and is not driven asserts nothing, and the doc's row
# would be attributing a claim to dead code: the driver line could be missing and
# the bucket's diff would still be exact.
driven=$(grep -o '^[[:space:]]*[A-Za-z0-9_]*\.Program\.__GateEntry();' "$BUCKET/Program.cs" \
         | sed 's/^[[:space:]]*//; s/\.Program\.__GateEntry();$//' | sort -u)
set_eq "PInvokeNative sections: bucket vs driver" "present in the bucket" "$tree_sections" "driven by Program.cs" "$driven"

echo "== 2/11 AGENTS.md — the default-reference wiring =="
# The conditional default references. This is the one claim in AGENTS.md that
# says out loud that nothing checks it — "Nothing references these assemblies at
# build time, so nothing fails when a wiring is dropped", and of the destinations
# only the toolchain bundle is self-checking. So the shim set comes from
# `internal/` (the source of truth: a fourth shim is a fourth directory) and is
# demanded of every wiring file the destinations table names.
shims_tree=$(ls -d internal/*/ | xargs -n1 basename | sort)
shims_doc=$(md_table_rows AGENTS.md '| shim | trigger assembly |' | awk -F'|' '{print $2}' | tr -d ' `' | sort)
set_eq "conditional default references" "named by AGENTS.md's shim table" "$shims_doc" "present under internal/" "$shims_tree"

flat=$(tr '\n' ' ' < AGENTS.md)

# The wiring files are read out of the destinations table's own last column, so
# the check cannot drift from the table by holding a second copy of the list.
dest_rows=$(md_table_rows AGENTS.md '| # | destination | wired in |')
wiring=$(printf '%s\n' "$dest_rows" | grep -oE '`(src|dist|gates)/[A-Za-z0-9_./+-]+`' | tr -d '`' | sort -u)

for f in $wiring; do
    if [ ! -f "$f" ]; then
        bad "AGENTS.md's destinations table names wiring file '$f', which does not exist"
        continue
    fi
    missing=""
    for s in $shims_tree; do
        grep -q "$s" "$f" || missing="$missing $s"
    done
    if [ -z "${missing// }" ]; then
        ok "every default-ref shim is wired in $f"
    else
        bad "$f names no wiring for:$missing (AGENTS.md: nothing fails at build time when this is dropped)"
    fi
done

# The two smoke tests are the PROOF that a shipped artifact carries the siblings,
# and AGENTS.md says their lists are hand-written too — so they get the same
# demand. Their paths are read out of AGENTS.md rather than spelled here.
for f in $(printf '%s' "$flat" | grep -oE '`dist/[a-z-]*smoke-test\.sh`' | tr -d '`' | sort -u); do
    if [ ! -f "$f" ]; then
        bad "AGENTS.md names smoke test '$f', which does not exist"
        continue
    fi
    missing=""
    for s in $shims_tree Dn2Cpp.Runtime; do
        grep -q "$s" "$f" || missing="$missing $s"
    done
    if [ -z "${missing// }" ]; then
        ok "every shipped sibling is asserted by $f"
    else
        bad "$f asserts no sibling named:$missing"
    fi
done

echo "== 3/11 docs/PORTING.md — the PAL seam's declared contract =="
# The seam is a contract between this repository and somebody porting to a target
# it has never seen. The SOURCE of the classification is the `// PAL-CONTRACT:`
# marker on each declaration in the header, not the table in the doc, so this
# section derives from the header and diffs the doc against it — never the other
# way round.
pal_h=runtime/core/platform/dn2cpp_pal.h

# Every declaration carries a marker. This is the fail-CLOSED half and has to come
# first: without it, a new seam entry added with no marker would simply be absent
# from both derived sets below, and every other check here would still pass.
pal_decls=$(grep -cE '^[A-Za-z_].*\bdn2cpp_pal_[a-z_0-9]+\(' "$pal_h")
pal_marks=$(grep -c '^// PAL-CONTRACT: ' "$pal_h")
eq "$pal_h — declarations carrying a PAL-CONTRACT marker" "$pal_decls" "$pal_marks" \
   "every seam declaration needs a '// PAL-CONTRACT: MUST' or 'MAY-DEGRADE <sentinel>' line directly above it; that marker is what docs/PORTING.md §2.1 restates"

# The doc's stated total, as an English word, against the header.
# Captured then matched from a here-string rather than piped into `head`/`sed …q`:
# under `set -o pipefail` a pipeline into an early-exiting consumer reports the
# consumer's status, and the pipeline-shape section of this very file bans it.
porting_total=$(w2n "$(sed -nE '1s/.*\*\*([a-z]+)\*\*.*/\1/p' \
    <<<"$(grep -oE 'The seam declares \*\*[a-z]+\*\* functions' docs/PORTING.md)")")
eq "docs/PORTING.md §2.1 'the seam declares N functions'" "$porting_total" "$pal_decls"

# The MAY-DEGRADE set: header markers vs the doc's table rows. A set diff and not a
# count, because the two sides go wrong for opposite reasons — a degrade the header
# permits and the doc omits sends a porter hunting for an answer that does not
# exist, while one the doc lists and the header does not tells them a correctness
# obligation is optional.
pal_degrade=$(awk '/^\/\/ PAL-CONTRACT: MAY-DEGRADE/{k=1; next}
                   k && match($0, /dn2cpp_pal_[a-z_0-9]+/) { print substr($0, RSTART, RLENGTH); k=0 }' \
              "$pal_h" | sort -u)
doc_degrade=$(md_col docs/PORTING.md '| may degrade |' 1 | tr -d '`' | sort -u)
set_eq "docs/PORTING.md §2.1 may-degrade table vs the header's markers" \
       "header" "$pal_degrade" "doc" "$doc_degrade"

# The doc's MUST arithmetic. Stated as "N of the M must answer truly", and it is
# the one number here that is a subtraction rather than a count — which is the kind
# that stays wrong longest, because both operands look right.
porting_must=$(w2n "$(sed -nE '1s/\*\*([A-Za-z]+) of.*/\1/p' \
    <<<"$(grep -oE '\*\*[A-Za-z]+ of the [a-z]+ must answer truly' docs/PORTING.md)")")
eq "docs/PORTING.md §2.1 'N of the M must answer truly'" \
   "$porting_must" "$((pal_decls - $(printf '%s\n' "$pal_degrade" | grep -c .)))"

# Every platform/*/ implementation defines the whole seam. The pal-reference gate
# asserts this too, and the duplication is deliberate: that gate builds a runtime
# and this one runs in milliseconds with no toolchain, so on a machine where the
# build cannot happen this is still what says a target has fallen behind the seam.
pal_required=$(grep -E '^[A-Za-z_].*\bdn2cpp_pal_[a-z_0-9]+\(' "$pal_h" \
               | grep -oE 'dn2cpp_pal_[a-z_0-9]+' | sort -u)
for impl in runtime/core/platform/*/dn2cpp_pal_*.cpp; do
    have=$(grep -E '^[A-Za-z_].*\bdn2cpp_pal_[a-z_0-9]+\(' "$impl" \
           | grep -v '^static' | grep -oE 'dn2cpp_pal_[a-z_0-9]+' | sort -u)
    set_eq "$impl vs the seam" "seam" "$pal_required" "impl" "$have"
done

echo "== 4/11 the generated culture table's own header =="
# It states a row count and a provenance, and it is a "do not edit by hand" file —
# which is why the count is worth checking: a hand-added row leaves the header
# describing a table that no longer exists. The candidate list is the generator's
# input, so a name added there without a regeneration is the same drift one step
# upstream; the two may legitimately differ only by candidates the generator
# refuses, and it refuses none today.
CT_INC=runtime/core/intrinsics/dn2cpp_culture_table.inc
CT_CAND=tools/gen-culture-table/culture-candidates.txt
# A `#if`-guarded platform split writes one culture as two rows; count the
# distinct culture NAMES, which is what "rows" means to a reader of the header.
ct_rows=$(grep -oE '^    \{ u"[A-Za-z-]+"' "$CT_INC" | sort -u | grep -c . || true)
ct_claimed=$(sed -n 's|^//   rows      : \([0-9]*\) .*|\1|p' "$CT_INC")
eq "$CT_INC header 'rows: N'" "${ct_claimed:-?}" "$ct_rows" \
   "regenerate: dotnet run tools/gen-culture-table/gen-culture-table.cs"
ct_cands=$(grep -cE '^[^#[:space:]]' "$CT_CAND" || true)
eq "$CT_CAND names one candidate per emitted row" "$ct_cands" "$ct_rows" \
   "a candidate the generator REFUSES is legitimately absent from the table — it prints the reason; otherwise regenerate"

echo "== 5/11 the BCL exception-message key set, written twice =="
# The transpiler emits the texts (Dn2Cpp.BclMessages) and the runtime asks for
# them by name (the DN2CPP_SR_* constants). The lookup is by key, so drift can
# only LOSE a message — the runtime asks for a key nothing emitted, reads null,
# and quietly falls back to "Exception of type 'X' was thrown."
sr_emitted=$(sed -n 's/^ *"\([A-Za-z0-9_]*\)",$/\1/p' src/Dn2Cpp.Transpiler/BclMessages.cs | sort)
sr_asked=$(sed -n 's/^inline constexpr const char\* DN2CPP_SR_[A-Z0-9_]* = "\([A-Za-z0-9_]*\)";$/\1/p' \
    runtime/core/dn2cpp_core.h | sort)
set_eq "the BCL message key set" \
    "src/Dn2Cpp.Transpiler/BclMessages.cs" "$sr_emitted" \
    "runtime/core/dn2cpp_core.h DN2CPP_SR_*" "$sr_asked"

echo "== 6/11 the banned pipeline shape — an early-exiting consumer =="
# gates/_common.sh's `set -o pipefail` note claims the tree builds no pipeline
# into a consumer that can quit before its producer is done. It is the one claim
# here whose subject is a shape rather than a number, and it has two failure modes
# that nothing downstream reports:
#
#   * Silent and fail-OPEN. `if X | grep -q "$forbidden"; then FAIL` stops firing
#     the moment the forbidden text appears — an assert that only fires when it
#     should not.
#   * Loud in the wrong place. `sort … | head -10` SIGPIPEs `sort`, and `set -e`
#     takes the whole script down past its verdict.
#
# So the ban covers `grep -q`/`-m`, `head`, an `awk` that `exit`s and a `sed` that
# `q`s, with no size below which it is safe; `_common.sh`'s `first_line` exists so
# those sites have a spelling.
#
# The strip below is what makes the count honest: a full-line comment, a
# backticked mention and a trailing ` # …` note are removed before the pattern
# runs, so writing ABOUT the form stays legal and only writing it is not. `||` is
# excluded by the leading `[^|]`, or every `x || grep -q y FILE` would read as a
# pipeline. A comment is BLANKED, never deleted, so `grep -n` still reports the
# real line number — which is the whole value of the failure message.
# END is every character that can terminate a word here; `head)` and `sed 2q)`
# are as much the shape as `head ` is.
_EE_END="[[:space:];}')\"]"
_EARLY_EXIT_RE="(^|[^|])\\|[[:space:]]*(head($_EE_END|\$)|(grep|egrep|fgrep)[[:space:]]+[^|]*(-[A-Za-z]*[qm][0-9]*($_EE_END|\$)|--quiet|--silent|--max-count)|awk[^|]*[^a-zA-Z_]exit[^a-zA-Z_]|sed[^|]*([[:space:];{]|[0-9])q($_EE_END|\$))"
badpipe=$(git ls-files '*.sh' '.github/workflows/*.yml' \
    | grep -v '^third_party/' \
    | while read -r f; do
        sed -e 's/^[[:space:]]*#.*$//' -e 's/`[^`]*`//g' -e 's/[[:space:]]#[^"'"'"'`]*$//' "$f" \
            | grep -nE "$_EARLY_EXIT_RE" \
            | sed "s|^|$f:|" || true
      done)
n_badpipe=$(grep -c . <<<"$badpipe" || true)
eq "gates/_common.sh 'none of which builds a pipeline at all' — pipelines into an early-exiting consumer" \
   "0" "$n_badpipe" \
   "each is: ${badpipe:-none}. Rewrite as \`grep -q P <<<\"\$(X)\"\`, \`grep -q P FILE\`, \`head -N <<<\"\$(X)\"\`, \`\${x%%\$'\''\\n'\''*}\` or \`first_line \"\$(X)\"\`; see the note beside \`set -o pipefail\` in gates/_common.sh"

echo "== 7/11 docs/EDITOR-EXPORT-DESIGN.md — the release assets and the bundle layout =="
# A release asset is exactly a dist/ script that writes the `<lane>.metadata`
# dist/release-github.sh consumes; §11's table is the hand-written copy of that
# set. Both directions matter: a lane packaged and undocumented leaves a release
# nobody can reproduce from the doc, and a row whose script was renamed sends a
# release cut to a file that is not there. dist/package-toolchain.sh writes no
# lane metadata and is absent from both sides for the same reason.
EED=docs/EDITOR-EXPORT-DESIGN.md
doc_pkg=$(md_col "$EED" '| asset | script |' 2 | tr -d '`' | sort -u)
tree_pkg=$(grep -lE 'cat > "\$OUT[A-Z_]*/[a-z-]+\.metadata"' dist/package-*.sh | sort -u)
set_eq "$EED §11 asset table vs the lane packagers" \
       "named by the table" "$doc_pkg" "writing a lane metadata" "$tree_pkg"

# §4's bundle-contents bullets each open with the layout entry they describe, and
# dist/package-toolchain.sh writes that layout under $LAYOUT. Both directions
# matter and neither is visible in a green export: a directory staged and
# undocumented ships with nobody having said what it is, and a documented one
# that stopped being staged sends a reader looking through the archive for it.
doc_top=$(awk '/^\*\*Bundle contents\*\*/{b=1} b && /^\*\*The prebuilt cache/{b=0} b' "$EED" \
          | grep -oE '^- `[A-Za-z0-9_.-]+' | sed 's/^- `//' | sort -u)
tree_top=$(grep -oE '\$LAYOUT/[A-Za-z0-9_.-]+' dist/package-toolchain.sh \
           | sed 's|^\$LAYOUT/||' | grep -vE '^\.' | sort -u)
set_eq "$EED §4 bundle contents vs the layout dist/package-toolchain.sh stages" \
       "named by §4" "$doc_top" "staged under \$LAYOUT" "$tree_top"

echo "== 8/11 dist/release-notes-template.md — bound by its renderer =="
# The template is rendered on a packaging host at release time, and that is the
# only machine its two failure modes reach: an @@KEY@@ nothing binds dies mid-cut,
# and a comment that is not a lane marker publishes verbatim onto the release
# page. Both are decidable here, against the renderer rather than a second list.
TPL=dist/release-notes-template.md
RG=dist/release-github.sh

# The bound set: the unconditional map_put calls, plus the `PLACEHOLDER:key`
# tokens in the lane rows with editor_row's `$1` expanded to the suffixes it is
# actually called with.
sfx=$(grep -oE 'editor_row _[A-Z]+' "$RG" | sed 's/^editor_row //' | sort -u)
bound=$( { grep -oE '^map_put +[A-Z][A-Z0-9_]*' "$RG" | awk '{print $2}'
           for s in $sfx; do
               grep -oE '\b[A-Z][A-Z0-9_]*(\$1[A-Z0-9_]*)?:[a-z_]+' "$RG" \
                   | sed "s/:.*\$//" | sed "s/[\$]1/$s/"
           done
         } | sort -u)
used=$(grep -oE '@@[A-Z0-9_]+@@' "$TPL" | tr -d '@' | sort -u)
unbound=$(comm -23 <(printf '%s\n' "$used") <(printf '%s\n' "$bound") | tr '\n' ' ')
eq "$TPL placeholders no lane row in $RG binds" "none" "${unbound:-none}" \
   "add the binding to that lane's row, or keep the line out of a cut without the lane"

# A marker is consumed only at the start of a line; one further in survives the
# render into the published notes, which is exactly what $RG's own stray-comment
# check refuses — but it sees only the lanes that cut is publishing.
stray_c=$(grep -n '<!--' "$TPL" | grep -vE '^[0-9]+:<!--(lane:!?[a-z0-9-]+|/lane)-->' \
          | tr '\n' ' ' || true)
eq "$TPL HTML comments that are not a line-initial lane marker" "none" "${stray_c:-none}"

tpl_lanes=$(grep -oE '<!--lane:!?[a-z0-9-]+-->' "$TPL" | sed -E 's/^<!--lane:!?//; s/-->$//' | sort -u)
known_lanes=$(sed -nE 's/^LANES_KNOWN="(.*)"$/\1/p' "$RG" | tr ' ' '\n' | sort -u)
set_eq "$TPL lane markers vs LANES_KNOWN" "the template" "$tpl_lanes" "$RG" "$known_lanes"

# The notes carry only what changes per release; the standing text lives in a
# guide the notes link at a pinned sha. Three ways that split rots, none of them
# visible in a green cut.

# (A) A renamed guide turns EVERY past release's links into 404s, silently — the
# links are pinned to a sha, so nothing revisits them. The linked path therefore
# has to exist AND be the one $RG states, which is what a cut checks for itself.
tpl_guide=$(grep -oE 'blob/@@DOCS_REF@@/[A-Za-z0-9_./+-]+' "$TPL" \
            | sed 's|^blob/@@DOCS_REF@@/||' | sort -u)
guide_files=""
for g in $tpl_guide; do
    if [ -f "$g" ]; then
        ok "$TPL links '$g', which exists"
        guide_files="$guide_files $g"
    else
        bad "$TPL links '$g', which is not a file — a renamed guide 404s every past release's links; restore the path or re-point the notes"
    fi
done
rg_guide=$(sed -nE 's/^GUIDE=["'"'"']?([^"'"'"']*)["'"'"']?$/\1/p' "$RG" | sort -u)
set_eq "$TPL guide links vs $RG's GUIDE=" "linked by the template" "$tpl_guide" "GUIDE= in $RG" "$rg_guide"

# (B) and (C) read the linked files, so they have a subject only once (A) holds.
if [ -n "${guide_files// }" ]; then

    # (B) A renamed guide SECTION is worse than a 404: the link still returns 200
    # and lands the reader at the top of the page with no sign anything is wrong.
    # The slug rule below approximates GitHub's (lowercase, spaces to `-`, ASCII
    # punctuation dropped, non-ASCII kept) — which is why the guide's headings are
    # written without punctuation, keeping the two in agreement by construction.
    # These sets are the only ones here that are not ASCII, and sort/comm must
    # agree on collation or comm answers nonsense: under a UTF-8 locale BSD
    # strcoll ranks every Japanese heading equal, which reads as "every anchor
    # found" — the fail-open direction. Byte order on both sides, always.
    tpl_anchors=$(grep -oE 'blob/@@DOCS_REF@@/[A-Za-z0-9_./+-]+#[^)]+' "$TPL" \
                  | sed 's/^[^#]*#//' | LC_ALL=C sort -u)
    guide_slugs=$(sed -n 's/^#\{2,6\} //p' $guide_files \
                  | LC_ALL=C tr 'A-Z' 'a-z' \
                  | LC_ALL=C sed -e 's/[]!"#$%&'"'"'()*+,./:;<=>?@[\^`{|}~]//g' -e 's/ /-/g' \
                  | LC_ALL=C sort -u)
    lost_anchors=$(LC_ALL=C comm -23 <(printf '%s\n' "$tpl_anchors") <(printf '%s\n' "$guide_slugs") | tr '\n' ' ')
    eq "$TPL link anchors that name no heading in the guide" "none" "${lost_anchors:-none}" \
       "a link to a missing anchor still returns 200 and lands at the top of the page; rename the anchor with the heading, or restore the heading"

    # (C) The one way the split itself rots is a section written on both sides and
    # corrected on one. Sharing a `## ` heading is that state, whichever copy is
    # stale, so the two top-level heading sets must be disjoint.
    tpl_h2=$(sed -n 's/^## //p' "$TPL" | LC_ALL=C sort -u)
    guide_h2=$(sed -n 's/^## //p' $guide_files | LC_ALL=C sort -u)
    shared_h2=$(LC_ALL=C comm -12 <(printf '%s\n' "$tpl_h2") <(printf '%s\n' "$guide_h2") | tr '\n' ' ')
    eq "sections written in both $TPL and the guide" "none" "${shared_h2:-none}" \
       "standing text belongs in the guide and per-release text in the notes; a section in both gets corrected in one"
fi

echo "== 9/11 the pinned toolchains a bundle ships, and their keep lists =="
# Everything here holds for BOTH pins — the Emscripten SDK and the cmake+ninja
# pair — because both are the same thing: an upstream archive per host, unpacked,
# trimmed to a keep list, staged into the bundle.
#
# What this section cannot reach: whether the cmake entries a keep list holds
# still satisfy the fork's Dn2CppToolchain.IsBuildToolsLayout, which is what an
# editor probes a staged buildtools/ with. That is a claim about two trees, one
# of them outside this repository, and this gate builds nothing —
# gates/build-and-run-godot-editor-export-web-hermetic.sh is its oracle: it
# exports through the staged pair with no other on PATH.
EMSDK_TRIM=dist/emsdk-trim.txt
BUILDTOOLS_TRIM=dist/buildtools-trim.txt

# Hand-written copies of one set. A host dn2cpp_host_tag can name and a pin has
# no archive row for is a machine that resolves a directory nothing fills; a
# pinned archive no tag names is one nothing will ever fetch.
#
# The host column is per pin and cannot be inferred: emsdk-pin.txt and
# node-pin.txt carry one archive per host, buildtools-pin.txt one per (tool,
# host). Each pin's header spells its own row grammar.
tag_hosts=$(awk '/^dn2cpp_host_tag\(\) \{/,/^\}/' gates/_common.sh \
            | grep -oE "printf '[a-z0-9-]+" | sed "s/^printf '//" | sort -u)
for spec in "$EMSDK_PIN:2" "$NODE_PIN:2" "$BUILDTOOLS_PIN:3"; do
    pin="${spec%:*}"
    pin_hosts=$(awk -v c="${spec##*:}" '$1 == "archive" { print $c }' "$pin" | sort -u)
    set_eq "$pin archive rows vs dn2cpp_host_tag" "the pin" "$pin_hosts" "gates/_common.sh" "$tag_hosts"
done

# A version is written once. A doc that spells it holds a second copy with
# nothing to reconcile it against; the release notes take each from the release
# metadata as a placeholder instead. README.md's `cmake ≥ 3.20` is a different
# claim and stays legal — it is the floor the REPOSITORY's own build needs, not
# the exact version a bundle carries.
#
# dist/licenses/ is deliberately outside the doc set: a vendored licence records
# the tag it was fetched at, and that provenance is the one place the version
# must be spelled.
for spec in "$EMSDK_PIN:version:Emscripten:EMSDK_VERSION" \
            "$NODE_PIN:version:Node.js:NODE_VERSION_*" \
            "$BUILDTOOLS_PIN:cmake_version:cmake:CMAKE_VERSION_*" \
            "$BUILDTOOLS_PIN:ninja_version:ninja:NINJA_VERSION_*"; do
    pin="${spec%%:*}"; rest="${spec#*:}"
    key="${rest%%:*}"; rest="${rest#*:}"
    what="${rest%%:*}"; ph="${rest#*:}"
    ver=$(pin_field "$pin" "$key")
    spelled=$(grep -lF "$ver" docs/*.md README.md CLAUDE.md AGENTS.md dist/*.md 2>/dev/null \
              | tr '\n' ' ' || true)
    eq "prose spelling the pinned $what version instead of deriving it" "none" "${spelled:-none}" \
       "$pin is the single source; the notes bind @@$ph@@ from the lane metadata"
done

# ninja's release archives hold the executable and nothing else, so the licence
# is vendored and its text names the tag it came from. Both halves are the
# obligation: the file, and a provenance that moves with the pin.
[ -f dist/licenses/ninja-COPYING.txt ] \
    && ok "dist/licenses/ninja-COPYING.txt exists" \
    || bad "dist/licenses/ninja-COPYING.txt is missing — ninja ships with no licence text of its own"
ninja_ver=$(pin_field "$BUILDTOOLS_PIN" ninja_version)
grep -qF "$ninja_ver" dist/licenses/README.md \
    && ok "dist/licenses/README.md records the pinned ninja version ($ninja_ver)" \
    || bad "dist/licenses/README.md does not name ninja $ninja_ver — re-fetch COPYING at the pinned tag and record its sha256"

# The SDK archive holds no licence file for LLVM or Binaryen — its only texts sit
# under emscripten/ — so no keep list can reach one and these two files are the
# bundle's whole copy of those terms.
for lic in llvm-LICENSE.txt binaryen-LICENSE.txt; do
    [ -f "dist/licenses/$lic" ] \
        && ok "dist/licenses/$lic exists" \
        || bad "dist/licenses/$lic is missing — the Emscripten SDK archive carries no licence text for it"
done
# Neither revision is a field of any pin, so the release_hash is the only thread
# tying these texts to the SDK build they were read off: it moving is what tells
# a reader to re-measure clang --version and wasm-opt --version and re-fetch.
emsdk_rel=$(pin_field "$EMSDK_PIN" release_hash)
grep -qF "$emsdk_rel" dist/licenses/README.md \
    && ok "dist/licenses/README.md records the pinned SDK release hash ($emsdk_rel)" \
    || bad "dist/licenses/README.md does not name the pinned SDK release hash $emsdk_rel — re-measure the LLVM and Binaryen revisions off the newly unpacked SDK and re-fetch both texts"

# A recorded hash nothing recomputes decays into decoration. Over the directory
# rather than a list, so a text vendored later is checked by having been added.
for lic in dist/licenses/*.txt; do
    lic_sha=$(shasum -a 256 "$lic" | awk '{print $1}')
    grep -qF "$lic_sha" dist/licenses/README.md \
        && ok "dist/licenses/README.md records $(basename "$lic")'s own sha256" \
        || bad "dist/licenses/README.md does not record $(basename "$lic")'s sha256 ($lic_sha) — a vendored text whose hash is unrecorded cannot be checked against upstream on a refresh"
done

# A keep list is the only statement of what the bundle carries of its archive, so
# it has to read as one: a repeated entry and an entry a directory above it
# already keeps both read as a deliberate narrowing that is not there. An OS tag
# (`windows: `/`posix: `) is part of the entry — bundle_trim's rule exactly — and
# a prefix that is neither is a typo the trim would refuse.
for TRIM in "$EMSDK_TRIM" "$BUILDTOOLS_TRIM"; do
    # `node-closure` / `node-drop` are directives rather than paths, and only the
    # SDK list has them; dropping them costs the other list nothing.
    trim_keep=$(grep -vE '^[[:space:]]*(#|$)' "$TRIM" \
                | grep -vE '^((windows|posix): )?node-(closure|drop)\b' || true)
    badtag=$(printf '%s\n' "$trim_keep" | grep -oE '^[A-Za-z0-9_-]+:' \
             | grep -vE '^(windows|posix):' | sort -u | tr '\n' ' ' || true)
    eq "$TRIM OS-tag prefixes that are not windows:/posix:" "none" "${badtag:-none}"
    dupes=$(printf '%s\n' "$trim_keep" | LC_ALL=C sort | uniq -d | tr '\n' ' ')
    eq "$TRIM repeated keep entries" "none" "${dupes:-none}"
    # Subsumption holds only where both entries apply: within one tag, and an
    # untagged keep (both OSes) over a tagged one. A tagged directory does not
    # cover an untagged entry under it — the other OS still needs that line.
    subsumed=$(printf '%s\n' "$trim_keep" | LC_ALL=C sort | awk '
        { e[NR] = $0; t[NR] = ""
          if (e[NR] ~ /^windows: /) { t[NR] = "windows"; sub(/^windows: /, "", e[NR]) }
          else if (e[NR] ~ /^posix: /) { t[NR] = "posix"; sub(/^posix: /, "", e[NR]) }
        }
        END {
            for (i = 1; i <= NR; i++) for (j = 1; j <= NR; j++) {
                if (i == j || index(e[i], "*") || index(e[j], "*")) continue
                if (t[i] != t[j] && t[i] != "") continue
                if (index(e[j], e[i] "/") == 1) printf "%s (under %s) ", e[j], e[i]
            }
        }')
    eq "$TRIM entries a broader keep already covers" "none" "${subsumed:-none}" \
       "a kept directory keeps its whole subtree, so naming something inside one narrows nothing"
done

echo "== 10/11 README.md — the vendored set against third_party/ =="
# README's License section is the only inventory of what this repository vendors
# and under what terms, so a tree added without a row ships with its license
# unstated — and one whose row outlived it credits a license nothing carries.
# The tree side is every directory under third_party/, NOT the ones carrying a
# DN2CPP-VENDORED.md: highway's marker is named README.dn2cpp.md, and a
# marker-keyed set would quietly exempt it.
tree_vendored=$(ls -d third_party/*/ | xargs -n1 basename | sort)
# `## License` only — Repository layout and the prose name these directories too,
# and neither states a license.
doc_vendored=$(awk '/^## /{s = ($0 == "## License")} s' README.md \
               | grep -oE '`third_party/[A-Za-z0-9_.+-]+/`' | tr -d '`' \
               | sed 's|^third_party/||; s|/$||' | sort -u)
set_eq "README.md's vendored license list vs third_party/" \
       "licensed by README.md" "$doc_vendored" "present under third_party/" "$tree_vendored"

echo "== 11/11 dangling path references in every doc =="
for f in docs/*.md README.md CLAUDE.md AGENTS.md CONTRIBUTING.md; do
    check_paths "$f"
done

echo
if [ "$FAILS" -ne 0 ]; then
    printf 'error: %d of %d doc claims are false. Each line above names what the doc says and what the tree measures.\n' \
        "$FAILS" "$CHECKS" >&2
    exit 1
fi
printf '== OK — %d doc claims checked, all true ==\n' "$CHECKS"
