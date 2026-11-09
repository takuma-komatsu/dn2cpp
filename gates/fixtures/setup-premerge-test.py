#!/usr/bin/env python3
"""Offline checks for hosted prerequisite integrity and shell/native paths."""
import hashlib
import io
import json
import os
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

from bash_fixture import run_bash

ROOT = Path(__file__).resolve().parents[2]
HELPER = ROOT / 'gates/_setup-premerge.sh'
WINDOWS_SETUP = (ROOT / 'gates/setup-windows-premerge.sh').read_text(encoding='utf-8')
CROSS_SETUP = WINDOWS_SETUP[WINDOWS_SETUP.index("IFS=$'\\t' read"):].split('\npremerge_prepare_compilers', 1)[0]
CROSS_METADATA = WINDOWS_SETUP.split('> "$PREMERGE_TOOLS/cross-runtime-input.txt" <<\'PY\'\n', 1)[1].split('\nPY\n', 1)[0]


class PremergeSetupTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='premerge setup space ')
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.environment = dict(os.environ, RUNNER_TEMP=self.root.as_posix(),
                                GITHUB_ENV=(self.root / 'env').as_posix(),
                                GITHUB_PATH=(self.root / 'path').as_posix(),
                                GITHUB_RUN_ID='12', GITHUB_RUN_ATTEMPT='3',
                                DN2CPP_PREMERGE_LOGROOT=self.root.as_posix(),
                                FIXTURE_PYTHON=Path(sys.executable).as_posix())

    def invoke(self, script, *arguments, **environment):
        return run_bash('set -euo pipefail\nsource "$1"\nshift\n'
                        'python() { "$FIXTURE_PYTHON" "$@"; }\n' + script,
                        ['fixture', HELPER.as_posix(), *arguments],
                        env=dict(self.environment, **environment), timeout=20)

    def test_rejects_corrupt_archives_for_each_digest(self):
        path = self.root / 'archive space.zip'
        data = b'controlled fixture bytes\x00\xff\r\n'
        path.write_bytes(data)
        for algorithm in ('sha1', 'sha256', 'sha512'):
            digest = hashlib.new(algorithm, data).hexdigest()
            with self.subTest(algorithm=algorithm):
                result = self.invoke('premerge_verify_archive "$@"', path.as_posix(), algorithm, digest)
                self.assertEqual(result.returncode, 0, result.stderr)
                result = self.invoke('premerge_verify_archive "$@"', path.as_posix(), algorithm, '0' * len(digest))
                self.assertNotEqual(result.returncode, 0)
                self.assertIn(b'mismatch', result.stderr)

    def test_bad_download_never_records_success_or_continues(self):
        result = self.invoke('''
curl() { printf corrupt > "$6"; }
premerge_download https://example.invalid/archive "$RUNNER_TEMP/archive" sha256 "$1"
touch "$RUNNER_TEMP/continued"
''', '0' * 64)
        self.assertNotEqual(result.returncode, 0)
        self.assertFalse((self.root / 'continued').exists())
        self.assertFalse((self.root / 'prerequisite-files.sha256').exists())
        self.assertIn('https://example.invalid/archive', (self.root / 'prerequisite-origins.tsv').read_text(encoding='utf-8'))

    def download(self, url, canonical='fail', fallback='good'):
        return self.invoke('''
curl() {
    local output url retry
    while [ "$#" -gt 0 ]; do
        case "$1" in
            --output) output="$2"; shift 2 ;;
            --retry) retry="$2"; shift 2 ;;
            --*) shift ;;
            *) url="$1"; shift ;;
        esac
    done
    printf '%s\\t%s\\n' "$url" "$retry" >> "$RUNNER_TEMP/requests"
    local result="$FIXTURE_CANONICAL"
    [ "$url" = "$FIXTURE_URL" ] || result="$FIXTURE_FALLBACK"
    case "$result" in
        good) printf fixture > "$output" ;;
        corrupt) printf corrupt > "$output" ;;
        fail) printf partial > "$output"; return 56 ;;
    esac
}
premerge_download "$FIXTURE_URL" "$RUNNER_TEMP/archive" sha256 "$1"
touch "$RUNNER_TEMP/continued"
''', hashlib.sha256(b'fixture').hexdigest(), FIXTURE_URL=url,
                           FIXTURE_CANONICAL=canonical, FIXTURE_FALLBACK=fallback)

    def test_release_download_falls_back_once_and_verifies_archive(self):
        url = 'https://github.com/owner/repository/releases/download/v1/archive.zip'
        result = self.download(url)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual((self.root / 'archive').read_bytes(), b'fixture')
        self.assertEqual((self.root / 'requests').read_text(encoding='utf-8').splitlines(),
                         [f'{url}\t3', f'{url}?download=1\t0'])
        origins = (self.root / 'prerequisite-origins.tsv').read_text(encoding='utf-8').splitlines()
        self.assertEqual([line.split('\t')[0] for line in origins], [url, f'{url}?download=1'])
        self.assertEqual(origins[0].split('\t')[1:], origins[1].split('\t')[1:])
        self.assertIn(hashlib.sha256(b'fixture').hexdigest(),
                      (self.root / 'prerequisite-files.sha256').read_text(encoding='utf-8'))
        self.assertTrue((self.root / 'continued').exists())

    def test_release_download_rejects_corrupt_fallback(self):
        result = self.download('https://github.com/owner/repository/releases/download/v1/archive.zip',
                               fallback='corrupt')
        self.assertNotEqual(result.returncode, 0)
        self.assertIn(b'mismatch', result.stderr)
        self.assertEqual(len((self.root / 'requests').read_text(encoding='utf-8').splitlines()), 2)
        self.assertFalse((self.root / 'continued').exists())
        self.assertFalse((self.root / 'prerequisite-files.sha256').exists())

    def test_release_download_preserves_failed_fallback_status(self):
        result = self.download('https://github.com/owner/repository/releases/download/v1/archive.zip',
                               fallback='fail')
        self.assertEqual(result.returncode, 56)
        self.assertEqual(len((self.root / 'requests').read_text(encoding='utf-8').splitlines()), 2)
        self.assertFalse((self.root / 'continued').exists())
        self.assertFalse((self.root / 'prerequisite-files.sha256').exists())

    def test_successful_release_download_keeps_canonical_url(self):
        url = 'https://github.com/owner/repository/releases/download/v1/archive.zip'
        result = self.download(url, canonical='good')
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual((self.root / 'requests').read_text(encoding='utf-8'), f'{url}\t3\n')

    def test_release_checksum_mismatch_does_not_fetch_again(self):
        url = 'https://github.com/owner/repository/releases/download/v1/archive.zip'
        result = self.download(url, canonical='corrupt')
        self.assertNotEqual(result.returncode, 0)
        self.assertIn(b'mismatch', result.stderr)
        self.assertEqual((self.root / 'requests').read_text(encoding='utf-8'), f'{url}\t3\n')
        self.assertFalse((self.root / 'continued').exists())
        self.assertFalse((self.root / 'prerequisite-files.sha256').exists())

    def test_other_download_urls_do_not_fall_back(self):
        for url in ('https://example.invalid/archive.zip',
                    'https://github.com/owner/repository/archive/v1.zip',
                    'https://github.com/owner?query/repository/releases/download/v1/archive.zip',
                    'https://github.com/owner/repository#fragment/releases/download/v1/archive.zip',
                    'https://github.com/owner/repository/releases/download/v1/archive.zip?existing=1'):
            with self.subTest(url=url):
                (self.root / 'requests').unlink(missing_ok=True)
                result = self.download(url)
                self.assertEqual(result.returncode, 56)
                self.assertEqual((self.root / 'requests').read_text(encoding='utf-8'), f'{url}\t3\n')
                self.assertFalse((self.root / 'continued').exists())
                self.assertFalse((self.root / 'prerequisite-files.sha256').exists())

    def test_windows_path_uses_shell_path_and_native_export(self):
        directory = self.root / 'bin space'
        directory.mkdir()
        tool = directory / 'premerge-fixture-tool'
        tool.write_text('#!/usr/bin/env bash\nprintf available', encoding='utf-8', newline='\n')
        tool.chmod(0o755)
        result = self.invoke('''
PREMERGE_HOST=windows
fixture_bin="$FIXTURE_BIN"
if command -v cygpath >/dev/null 2>&1; then
    fixture_bin="$(cygpath -u "$fixture_bin")"
fi
cygpath() { test "$1" = -u; printf '%s\\n' "$fixture_bin"; }
premerge_add_path 'C:/runner/tool space/bin'
premerge-fixture-tool
''', FIXTURE_BIN=directory.as_posix())
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout, b'available')
        self.assertEqual((self.root / 'path').read_text(encoding='utf-8'), 'C:/runner/tool space/bin\n')

    def test_init_refuses_reused_tool_root(self):
        result = self.invoke('''
uname() { case "$1" in -s) echo Linux ;; -m) echo x86_64 ;; esac; }
premerge_setup_init linux
touch "$PREMERGE_TOOLS/retained"
premerge_setup_init linux
''')
        self.assertNotEqual(result.returncode, 0)
        self.assertIn(b'refusing to reuse', result.stderr)
        self.assertTrue((self.root / 'dn2cpp-premerge-tools-12-3/retained').is_file())

    def test_init_refuses_wrong_host_before_installing(self):
        result = self.invoke('''
uname() { case "$1" in -s) echo Darwin ;; -m) echo x86_64 ;; esac; }
premerge_setup_init macos
''')
        self.assertNotEqual(result.returncode, 0)
        self.assertIn(b'unsupported pre-merge host', result.stderr)
        self.assertFalse((self.root / 'dn2cpp-premerge-tools-12-3').exists())

    def test_windows_native_cross_metadata_preserves_hash_and_isolates_corelib(self):
        runtime = '10.0.14'
        digest = 'ab' * 64
        (self.root / 'cross-runtime-input.txt').write_bytes(
            f'{runtime}\thttps://example.invalid/runtime.tar.gz\t{digest}\r\n'.encode('ascii'))
        (self.root / 'downloads').mkdir()
        result = self.invoke('''
PREMERGE_TOOLS="$RUNNER_TEMP"
cygpath() { test "$1" = -u; printf '%s\\n' "$2"; }
premerge_download() {
    test "$3" = sha512
    printf '%s' "$4" > "$RUNNER_TEMP/received-hash"
    touch "$2"
}
tar() {
    mkdir -p "$PREMERGE_TOOLS/cross-runtime/shared/Microsoft.NETCore.App/$cross_version"
    touch "$PREMERGE_TOOLS/cross-runtime/shared/Microsoft.NETCore.App/$cross_version/System.Private.CoreLib.dll"
}
''' + CROSS_SETUP)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual((self.root / 'received-hash').read_bytes(), digest.encode('ascii'))
        exports = (self.root / 'env').read_text(encoding='utf-8')
        self.assertIn('DN2CPP_CORELIB_CROSS=', exports)
        self.assertIn(f'/cross-runtime/shared/Microsoft.NETCore.App/{runtime}/System.Private.CoreLib.dll', exports)
        self.assertNotIn('DOTNET_ROOT=', exports)

    def test_cross_runtime_metadata_selects_runtime_after_apphost(self):
        version = '10.0.14'
        files = [
            {'rid': 'linux-x64', 'name': 'dotnet-apphost-pack-linux-x64.tar.gz',
             'url': 'https://example.invalid/apphost.tar.gz', 'hash': 'a' * 128},
            {'rid': 'linux-x64', 'name': 'dotnet-runtime-linux-x64.tar.gz',
             'url': 'https://example.invalid/runtime.tar.gz', 'hash': 'b' * 128},
        ]
        metadata = {'releases': [{'runtime': {'version': version, 'files': files}}]}
        output = io.StringIO()
        with patch('runpy.run_path', return_value={'PROFILES': {'ci': ('10.0.403', version)}}), \
                patch('urllib.request.urlopen', return_value=io.BytesIO(json.dumps(metadata).encode('utf-8'))), \
                patch.object(sys, 'argv', ['metadata', str(self.root)]), patch.object(sys, 'stdout', output):
            exec(compile(CROSS_METADATA, 'windows-cross-runtime-metadata', 'exec'), {})
        self.assertEqual(output.getvalue().split('\t'),
                         [version, 'https://example.invalid/runtime.tar.gz', 'b' * 128 + '\n'])
        self.assertEqual(json.loads((self.root / 'dotnet-release-metadata.json').read_bytes()), metadata)


if __name__ == '__main__':
    unittest.main()
