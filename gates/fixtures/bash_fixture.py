"""Run extracted adapters in the invoking Bash, not a Windows WSL launcher."""
import os
import subprocess


def run_bash(script, arguments, **kwargs):
    executable = os.environ.get('DN2CPP_TEST_BASH')
    if not executable:
        if os.name == 'nt':
            raise AssertionError('Set DN2CPP_TEST_BASH to the invoking Git/MSYS Bash executable')
        executable = 'bash'
    if os.name == 'nt':
        # cygpath can spell /usr/bin/bash without its on-disk .exe suffix.
        if not os.path.isfile(executable) and os.path.isfile(executable + '.exe'):
            executable += '.exe'
        if not os.path.isfile(executable):
            raise AssertionError(f'Explicit Bash executable does not exist: {executable!r}')
    # A successful launch is mandatory even for tests expecting adapter failure.
    # Otherwise an unavailable WSL launcher could make every negative test pass.
    canary = b'dn2cpp-fixture-bash-entered\n'
    try:
        result = subprocess.run([executable, '-c', "printf 'dn2cpp-fixture-bash-entered\\n'\n" + script,
                                 *arguments], capture_output=True, **kwargs)
    except OSError as error:
        raise AssertionError(f'Cannot launch explicit Bash {executable!r}: {error}') from error
    if not result.stdout.startswith(canary):
        raise AssertionError(f'Bash fixture did not enter: executable={executable!r}, '
                             f'exit={result.returncode}, stdout={result.stdout!r}, stderr={result.stderr!r}')
    result.stdout = result.stdout[len(canary):]
    return result
