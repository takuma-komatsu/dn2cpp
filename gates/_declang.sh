#!/usr/bin/env bash

# Resolve only staged DeClang distributions; clang++ on PATH may be ordinary Clang.
ensure_declang_compiler() {
    local candidate selected=""
    if [ -z "${DN2CPP_DECLANG_COMPILER:-}" ]; then
        for candidate in "$PWD"/artifacts/declang-distribution/*/compiler/bin/clang++; do
            [ -f "$candidate" ] && [ -x "$candidate" ] || continue
            if [ -n "$selected" ]; then
                echo "FAIL: multiple DeClang distributions; set DN2CPP_DECLANG_COMPILER explicitly" >&2
                return 1
            fi
            selected="$candidate"
        done
        if [ -z "$selected" ]; then
            gate_skip "set DN2CPP_DECLANG_COMPILER to an existing DeClang C++ driver, or stage one under artifacts/declang-distribution/*/compiler/bin/clang++"
        fi
        DN2CPP_DECLANG_COMPILER="$selected"
    fi
    if [ ! -f "$DN2CPP_DECLANG_COMPILER" ] || [ ! -x "$DN2CPP_DECLANG_COMPILER" ]; then
        echo "FAIL: DeClang compiler is not an executable file: $DN2CPP_DECLANG_COMPILER" >&2
        return 1
    fi
    # The editor gate reads this in child shells and Python processes.
    export DN2CPP_DECLANG_COMPILER
}
