"""Offline packaging contracts; fixture bytes are not actual Windows runtime DLLs."""
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest


WRITER = Path(__file__).resolve().parents[1] / "dist" / "bcl-inventory.py"
CORE = "System.Private.CoreLib.dll"
PEER = "Microsoft.CSharp.dll"
SIDE = "dn2cpp-bcl-v1.tsv"
FW = "Microsoft.NETCore.App"
VERSION = "11.0.1"


def deps(rid):
    target = ".NETCoreApp,Version=v11.0/" + rid
    pack = FW + ".Runtime." + rid + "/" + VERSION
    assets = {CORE: {}, PEER: {}}
    return {"runtimeTarget": {"name": target}, "targets": {
        target: {pack: {"runtime": assets, "native": {"coreclr.dll": {}}},
                 "Foreign/1.0": {"runtime": {"System.Extra.dll": {}}}},
        ".NETCoreApp,Version=v11.0/unused": {pack: {"runtime": {"System.Extra.dll": {}}}}},
        "libraries": {pack: {"type": "package"}}}


class Inventory(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        self.shared = self.root / "shared" / FW / VERSION
        self.ref = self.root / "bundle" / "ref"
        self.shared.mkdir(parents=True)
        self.ref.mkdir(parents=True)
        self.meta = self.shared / (FW + ".deps.json")
        self.meta.write_text(json.dumps(deps("win-x64")), encoding="utf-8")
        self.stage(self.shared, self.ref)

    @staticmethod
    def stage(source, dest, flavor=b"WIN"):
        for name in (CORE, PEER, "coreclr.dll", "System.Extra.dll"):
            (source / name).write_bytes(flavor + name.encode("ascii"))
            shutil.copy(source / name, dest / name)

    def tool(self, staged=None, source=None, kind="host", fail=False):
        cmd = [sys.executable, str(WRITER), str(staged or self.ref)]
        if source is not None:
            cmd += ["--source-corelib", str(source / CORE), "--kind", kind]
        result = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        self.assertEqual(result.returncode == 0, not fail, result.stderr)
        return result.stderr

    def expected(self, stage):
        rows = [hashlib.sha256((stage / n).read_bytes()).hexdigest() + "\t" + n for n in (PEER, CORE)]
        return ("dn2cpp-bcl-v1\n" + "\n".join(rows) + "\n").encode("ascii")

    def test_selected_target_and_staged_bytes(self):
        side = self.ref / SIDE
        for _ in range(2):
            self.tool(source=self.shared)
            self.assertEqual(side.read_bytes(), self.expected(self.ref))
        self.tool()
        (self.ref / PEER).write_bytes(b"stale")
        self.tool(fail=True)
        for missing in (False, True):
            if missing:
                (self.ref / PEER).unlink()
            previous = side.read_bytes()
            self.tool(source=self.shared, fail=True)
            self.assertEqual(previous, side.read_bytes())  # no partial replacement

    def test_deps_identity_and_unsafe_rows(self):
        original = self.meta.read_text(encoding="utf-8")
        self.tool(source=self.shared)
        for old, new in [('Runtime.win-x64/11.0.1', 'Runtime.linux-x64/11.0.1'),
                         ('/11.0.1', '/11.0.0'),
                         ('"' + CORE + '"', '"Not.CoreLib.dll"'),
                         ('"' + PEER + '"', '"../escape.dll"'),
                         ('"' + PEER + '"', '"' + CORE.lower() + '"'),
                         ('"' + PEER + '": {}', '"' + PEER + '": {}, "' + PEER + '": {}')]:
            with self.subTest(new=new):
                self.meta.write_text(original.replace(old, new), encoding="utf-8")
                self.tool(source=self.shared, fail=True)
        self.meta.write_text(original, encoding="utf-8")
        self.tool(source=self.shared, kind="cross", fail=True)

    def test_cross_archive_and_unrecognized_override(self):
        self.meta.write_text(json.dumps(deps("linux-x64")), encoding="utf-8")
        self.tool(source=self.shared, kind="override")
        self.assertEqual((self.ref / SIDE).read_bytes(), self.expected(self.ref))
        custom = self.root / "loose"
        self.shared.rename(custom)  # metadata alone must not bless an arbitrary override
        self.assertIn(b"unverified", self.tool(source=custom, kind="override"))
        self.assertFalse((self.ref / SIDE).exists())
        self.assertTrue((self.ref / CORE).is_file())

    def test_windows_archive_override_refused(self):
        staged = self.ref.parent / "ref-posix"
        staged.mkdir()
        self.stage(self.shared, staged)
        self.assertIn(b"ref-posix cannot use the win-x64 runtime",
                      self.tool(staged, self.shared, "override", fail=True))
        self.assertFalse((staged / SIDE).exists())

        # The fallback still accepts an unverified POSIX override.
        self.meta.write_text(json.dumps(deps("osx-arm64")), encoding="utf-8")
        self.assertIn(b"unverified", self.tool(staged, self.shared, "override"))
        self.assertFalse((staged / SIDE).exists())

        # Neither verdict depends on the framework being the supported major.
        other = self.shared.rename(self.shared.with_name("10.0.1"))
        for rid, verdict in (("win-x64", b"ref-posix cannot use the win-x64 runtime"),
                             ("linux-x64", b"unverified")):
            with self.subTest(rid=rid):
                (other / self.meta.name).write_text(
                    json.dumps(deps(rid)).replace("11.0", "10.0"), encoding="utf-8")
                self.assertIn(verdict, self.tool(staged, other, "override", fail=rid != "linux-x64"))
                self.assertFalse((staged / SIDE).exists())

    def nuget_fixture(self, rid):
        package = FW + ".Runtime." + rid
        root = self.root / package.lower() / VERSION
        src = root / "runtimes" / rid / "lib/net11.0"
        staged = self.ref.parent / "ref-posix"
        src.mkdir(parents=True)
        staged.mkdir()
        self.stage(src, staged, b"LINUX")
        spec = root / (package.lower() + ".nuspec")
        spec.write_text('<package xmlns="urn:nuspec"><metadata><id>%s</id><version>%s</version>'
                        '</metadata></package>' % (package, VERSION), encoding="utf-8")
        runtime_list = root / "data/RuntimeList.xml"
        runtime_list.parent.mkdir()
        rows = [("Managed", "runtimes/" + rid + "/lib/net11.0/" + n) for n in (CORE, PEER)]
        rows.extend([("Native", "runtimes/" + rid + "/native/coreclr.dll"),
                     ("Managed", "runtimes/linux-arm64/lib/net11.0/System.Extra.dll"),
                     ("Managed", "runtimes/" + rid + "/lib/net9.0/System.Extra.dll")])
        xml = '<FileList FrameworkName="%s">%s</FileList>' % (
            FW, ''.join('<File Type="%s" Path="%s"/>' % row for row in rows))
        runtime_list.write_text(xml, encoding="utf-8")
        return src, staged, spec, runtime_list

    def test_windows_nuget_override_refused(self):
        src, staged, _, _ = self.nuget_fixture("win-x64")
        self.assertIn(b"ref-posix cannot use the win-x64 runtime",
                      self.tool(staged, src, "override", fail=True))
        self.assertFalse((staged / SIDE).exists())
        other = src.rename(src.with_name("net10.0"))  # another major's pack layout
        self.assertIn(b"ref-posix cannot use the win-x64 runtime",
                      self.tool(staged, other, "override", fail=True))
        self.assertFalse((staged / SIDE).exists())

    def test_cross_nuget_managed_rid_and_tfm(self):
        src, staged, spec, runtime_list = self.nuget_fixture("linux-x64")
        xml = runtime_list.read_text(encoding="utf-8")
        self.tool(staged, src, "cross")
        self.assertEqual((staged / SIDE).read_bytes(), self.expected(staged))
        self.assertNotEqual((staged / SIDE).read_bytes(), self.expected(self.ref))
        runtime_list.write_text(xml.replace("net11.0/" + PEER, "net11.0/../" + PEER), encoding="utf-8")
        self.tool(staged, src, "cross", fail=True)
        runtime_list.write_text(xml, encoding="utf-8")
        spec.write_text(spec.read_text(encoding="utf-8").replace(VERSION, "11.0.0"), encoding="utf-8")
        self.tool(staged, src, "cross", fail=True)


if __name__ == "__main__":
    unittest.main()
