#!/usr/bin/env bash
# Consolidated char/text gate: System.Char classification & casing, the remaining
# char predicates (including every indexed-string classification sibling),
# char-in-concat lowering, UTF-16 surrogate predicates, sub-word ToString,
# primitive ToString, and Encoding.GetString. Diffed exactly vs real .NET.
# Former gates: char, char-rest, char-concat, char-issurrogate, subword-tostring,
# tostring, encoding-getstring.
source "$(dirname "$0")/_common.sh"

corelib_diff_gate CharText
