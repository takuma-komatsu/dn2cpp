#!/usr/bin/env bash
# Negative searches must distinguish absence from a search that never completed.
negative_search_assertions() (
    local search_status expected
    check_negative_search() {
        local expected="$1" diagnostic="$2" actual=0 output
        shift 2
        output=$("$@" 2>&1) || actual=$?
        if [ "$actual" != "$expected" ]; then
            echo "FAIL: negative search returned $actual, expected $expected: $*" >&2
            return 1
        fi
        if [[ "$output" != *"$diagnostic"* ]]; then
            echo "FAIL: negative search diagnostic missing '$diagnostic': $output" >&2
            return 1
        fi
    }
    mock_search() (
        grep() { return "$search_status"; }
        assert_no_word_match 'forbidden symbol' 'forbidden'
    )
    for search_status in 0 1 2 127; do
        expected=1
        if [ "$search_status" = 1 ]; then expected=0; fi
        check_negative_search "$expected" '' mock_search
    done

    check_negative_search 0 '' assert_no_word_match 'whole-word absence' 'forbidden' <<< 'not_forbidden'
    check_negative_search 1 'FAIL: whole-word presence' \
        assert_no_word_match 'whole-word presence' 'forbidden' <<< 'forbidden'
    check_negative_search 0 '' \
        assert_no_word_match 'readable file absence' 'negative_search_absent_symbol' gates/_common.sh
    check_negative_search 1 'FAIL: readable file presence' \
        assert_no_word_match 'readable file presence' 'assert_no_word_match' gates/_common.sh
    check_negative_search 1 'search failed with exit 2' \
        assert_no_word_match 'missing input' 'forbidden' "$BASH_SOURCE.missing"
    check_negative_search 1 'search failed with exit 2' \
        assert_no_word_match 'unreadable input' 'forbidden' gates
    check_negative_search 1 'search failed with exit 2' \
        assert_no_word_match 'match and missing input' 'forbidden' - "$BASH_SOURCE.missing" <<< 'forbidden'
    missing_search() (
        PATH=/nonexistent
        hash -r
        assert_no_word_match 'missing grep' 'forbidden'
    )
    check_negative_search 1 'search failed with exit 127' missing_search
    echo 'negative search assertions end'
)

if [ "${BASH_SOURCE[0]}" = "$0" ]; then
    source "$(dirname "$0")/_common.sh"
    negative_search_assertions
fi
