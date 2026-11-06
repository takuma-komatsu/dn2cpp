#!/usr/bin/env bash
# Interpreter selection is independent of SDK, compiler and host setup.
# Gate sources, generated files and child Python processes use UTF-8.
export PYTHONUTF8=1

_gate_python_resolve() {
    local override="${DN2CPP_PYTHON:-}" candidate found=0
    if [ "${_GATE_PYTHON_READY:-0}" = 1 ] \
        && [ "${_GATE_PYTHON_OVERRIDE:-}" = "$override" ] \
        && [ "${_GATE_PYTHON_PATH:-}" = "$PATH" ]; then
        return 0
    fi
    _GATE_PYTHON_READY=0
    if [ -n "$override" ]; then
        # A whole executable path may contain spaces. Otherwise the override
        # is a whitespace-separated command, such as the Windows launcher.
        if [ -x "$override" ] || command -v "$override" >/dev/null 2>&1; then
            _GATE_PYTHON_ARGV=("$override")
        else
            IFS=$' \t\n' read -r -a _GATE_PYTHON_ARGV <<< "$override"
        fi
        [ -n "${_GATE_PYTHON_ARGV[0]:-}" ] || return 127
        _GATE_PYTHON_COMMAND="$override"
    else
        for candidate in python3 python 'py -3'; do
            IFS=' ' read -r -a _GATE_PYTHON_ARGV <<< "$candidate"
            # App-execution aliases can exist and launch without being Python.
            if "${_GATE_PYTHON_ARGV[@]}" -c 'import sys; sys.exit(sys.version_info.major != 3)' >/dev/null 2>&1; then
                _GATE_PYTHON_COMMAND="$candidate"
                found=1
                break
            fi
        done
        if [ "$found" != 1 ]; then
            echo 'error: no working Python 3 interpreter found (tried: python3, python, py -3)' >&2
            return 1
        fi
    fi
    _GATE_PYTHON_OVERRIDE="$override"
    _GATE_PYTHON_PATH="$PATH"
    _GATE_PYTHON_READY=1
}

# Legacy command-string lookup for tools that need an interpreter description.
# Gate execution goes through gate_python so launcher arguments stay separate.
resolve_python() {
    _GATE_PYTHON_READY=0
    _gate_python_resolve || return $?
    printf '%s\n' "$_GATE_PYTHON_COMMAND"
}

gate_python() {
    _gate_python_resolve || return $?
    "${_GATE_PYTHON_ARGV[@]}" "$@"
}

# Use only in a child shell: the job PID must belong to Python for cleanup.
gate_python_exec() {
    _gate_python_resolve || return $?
    exec "${_GATE_PYTHON_ARGV[@]}" "$@"
}
