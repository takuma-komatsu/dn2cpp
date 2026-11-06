#!/usr/bin/env bash
# Python fallback, launcher arguments, UTF-8 inheritance and real gate consumers.
set -euo pipefail
cd "$(dirname "$0")/.."
export PYTHONUTF8=0
source gates/_python.sh
# Resolve the executable before the fake commands shadow its name.
DN2CPP_TEST_PYTHON=$(gate_python -c 'import sys; sys.stdout.write(sys.executable.replace(chr(92), "/"))')
export DN2CPP_TEST_PYTHON
unset DN2CPP_PYTHON
fixture=$(mktemp -d "${TMPDIR:-/tmp}/dn2cpp-gate-python.XXXXXX")
trap 'rm -rf "$fixture"' EXIT
printf '\343\201\202' > "$fixture/utf8.txt"
cat > "$fixture/python3" <<'STUB'
#!/usr/bin/env bash
exit 49
STUB
cat > "$fixture/python" <<'STUB'
#!/usr/bin/env bash
exec "$DN2CPP_TEST_PYTHON" "$@"
STUB
cat > "$fixture/py" <<'STUB'
#!/usr/bin/env bash
[ "$1" = -3 ] || exit 41
shift
exec "$DN2CPP_TEST_PYTHON" "$@"
STUB
chmod +x "$fixture/python3" "$fixture/python" "$fixture/py"
mkdir "$fixture/space directory"
cp "$fixture/python" "$fixture/space directory/python executable"
export PATH="$fixture:$PATH"

[ "$(resolve_python)" = python ]
for override in '' 'py -3' "$fixture/space directory/python executable"; do
    DN2CPP_PYTHON="$override" gate_python - "$fixture/utf8.txt" 'argument with spaces' '*.cpp' <<'PY'
import pathlib
import subprocess
import sys

assert sys.flags.utf8_mode == 1
assert pathlib.Path(sys.argv[1]).read_text().encode('utf-8').hex() == 'e38182'
assert sys.argv[2:] == ['argument with spaces', '*.cpp']
subprocess.run([sys.executable, '-c', 'import sys; assert sys.flags.utf8_mode == 1'], check=True)
PY
done
status=0
DN2CPP_PYTHON='py -3' gate_python -c 'raise SystemExit(23)' || status=$?
[ "$status" -eq 23 ]
status=0
DN2CPP_PYTHON=python3 gate_python -c 'print(1)' || status=$?
[ "$status" -eq 49 ]
# A new gate must skip both broken aliases and retain the launcher's -3 argument.
printf '#!/usr/bin/env bash\nexit 49\n' > "$fixture/python"
[ "$(resolve_python)" = 'py -3' ]
for override in '' 'py -3' "$fixture/space directory/python executable"; do
    DN2CPP_PYTHON="$override" bash gates/fixtures/gate-python-consumers.sh "$fixture/consumers"
done
# These SDK-free fixtures exercise actual reflection, ISA and output wrappers.
for consumer in reflection-gate-inputs platform-isa-output pal-output-filter hotupdate-output; do
    DN2CPP_PYTHON='' bash "gates/fixtures/$consumer.sh" > "$fixture/$consumer.log" 2>&1 \
        || { cat "$fixture/$consumer.log" >&2; exit 1; }
done
# The abort-cache consumer used to quote "py -3" as a single executable.
mkdir "$fixture/abort"
printf 'payload\n' > "$fixture/abort/native.input"
cp "$fixture/abort/native.input" "$fixture/abort/oracle.input"
printf '127\n' > "$fixture/abort/native.code"
cp "$fixture/abort/native.code" "$fixture/abort/oracle.code"
printf '%s\n' 'Unhandled exception. System.InvalidOperationException: declared abort' > "$fixture/abort/native.error"
cp "$fixture/abort/native.error" "$fixture/abort/oracle.error"
DN2CPP_PYTHON='' bash gates/test-gate-output-shell.sh --case abort-cache "$fixture/abort" '' \
    > "$fixture/abort.log" 2>&1 || { cat "$fixture/abort.log" >&2; exit 1; }
# No implicit fallback may turn an explicitly selected broken command green.
status=0
DN2CPP_PYTHON=python3 bash gates/fixtures/gate-python-consumers.sh "$fixture/failure" \
    > "$fixture/failure.log" 2>&1 || status=$?
[ "$status" -eq 49 ]
# No viable candidate must fail before a consumer is run.
printf '#!/usr/bin/env bash\nexit 49\n' > "$fixture/py"
status=0
DN2CPP_PYTHON='' bash -c 'source gates/_python.sh; gate_python -c "pass"' \
    > "$fixture/missing.log" 2>&1 || status=$?
[ "$status" -eq 1 ]
grep -Fq 'no working Python 3 interpreter found' "$fixture/missing.log"
printf 'OK: gate Python invocation and consumers\n'
