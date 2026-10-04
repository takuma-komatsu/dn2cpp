import os
import subprocess
import sys
import threading


def run(command, broken):
    reader, writer = os.pipe()
    os.set_blocking(writer, False)
    if broken:
        os.close(reader)
    try:
        process = subprocess.Popen(command + ["standard-pipe"], stdout=writer, stderr=subprocess.PIPE)
    finally:
        os.close(writer)
    payload = []
    def drain():
        with os.fdopen(reader, "rb") as pipe:
            payload.append(pipe.read())
    consumer = None if broken else threading.Thread(target=drain, daemon=True)
    if consumer is not None:
        consumer.start()
    try:
        _, error = process.communicate(timeout=30)
    except subprocess.TimeoutExpired:
        process.kill()
        process.communicate()
        raise
    finally:
        if consumer is not None:
            consumer.join(timeout=5)
            assert not consumer.is_alive(), command
    assert process.returncode == 0, (command, process.returncode, error)
    assert error == b"standard pipe end\n", (command, error)
    return b"" if broken else payload[0]


native, oracle = [sys.argv[1]], ["dotnet", sys.argv[2]]
expected = bytes(index % 251 for index in range(131072))
for broken in (False, True):
    reference = run(oracle, broken)
    actual = run(native, broken)
    assert reference == (b"" if broken else expected)
    assert actual == reference
print("standard pipe parity end")
