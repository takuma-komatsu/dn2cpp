"""Export selected Android methods with both IL preprocessing modes and inspect APKs."""
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import zipfile

root, project, output, editor = map(Path, sys.argv[1:])
compiler = Path(os.environ["DN2CPP_DECLANG_COMPILER"]).resolve()
ndk = Path(os.environ["ANDROID_NDK_ROOT"])
nm = next(ndk.glob("toolchains/llvm/prebuilt/*/bin/llvm-nm" + (".exe" if os.name == "nt" else "")))
presets = project / "export_presets.cfg"
original = presets.read_text(encoding="utf-8")
attribute = project / "ObfuscateAttribute.cs"
probe = project / "DeClangProbe.cs"
game = project / "ExportProbe.cs"
original_game = game.read_text(encoding="utf-8")
# Android discards stdout; route the device assertions through the engine logger.
game.write_text(original_game.replace("Console.WriteLine(", "Godot.GD.Print("), encoding="utf-8")
attribute.write_text((root / "src/Dn2Cpp.Runtime/ObfuscateAttribute.cs").read_text(encoding="utf-8"), encoding="utf-8")
probe.write_text('''using System;
public partial class ExportProbe {
    static ExportProbe() {
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
        int n = Environment.TickCount & 63;
        bool correct = DeClangSelected(n) == n * (n - 1) / 2 + (n % 2) * n;
        Godot.GD.Print($"DN2CPP_DECLANG_SELECTED correct={correct}");
    }
    [Dn2Cpp.Runtime.Obfuscate]
    private static int DeClangSelected(int n) {
        int result = 0;
        for (int i = 0; i < n; i++) {
            if ((i & 1) == 0) result += n + i;
            else result -= n - i;
        }
        return result;
    }
}
''', encoding="utf-8")


def configure(compiler_path, seed, prestrip=False):
    settings = {"declang_path": json.dumps(str(compiler_path)), "declang_seed": json.dumps(seed),
                "il_prestripping": str(prestrip).lower()}
    block = original.split("[preset.2.options]", 1)
    assert len(block) == 2
    tail = block[1]
    for name, value in settings.items():
        tail = re.sub(r"^dotnet/dn2cpp/" + name + r"=.*\n", "", tail, flags=re.M)
    presets.write_text(block[0] + "[preset.2.options]\n" +
                       "".join(f"dotnet/dn2cpp/{key}={value}\n" for key, value in settings.items()) + tail, encoding="utf-8")


def export(label, diagnostic=None):
    print(f"Checking Android DeClang export: {label}", flush=True)
    apk = (output / f"declang-{label}.apk").resolve()
    apk.unlink(missing_ok=True)
    result = subprocess.run([str(editor.resolve()), "--headless", "--path", str(project.resolve()),
                             "--export-debug", "dn2cpp-android", str(apk)],
                            text=True, encoding="utf-8", stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=3600)
    (output / f"declang-{label}.log").write_text(result.stdout, encoding="utf-8")
    if diagnostic:
        assert result.returncode != 0 and diagnostic in result.stdout, result.stdout
        assert "dn2cpp: transpiling" not in result.stdout
        if apk.exists():
            with zipfile.ZipFile(apk) as archive:
                assert "lib/arm64-v8a/libEditorExportSample.so" not in archive.namelist(), "rejected export packaged a stale game library"
        return
    assert result.returncode == 0 and apk.is_file(), result.stdout
    assert "ERROR: Export .NET Project" not in result.stdout, result.stdout
    with zipfile.ZipFile(apk) as archive:
        names = archive.namelist()
        assert not any(re.search(r"\.dll|hostfxr|coreclr|libmonosgen|System\..*\.so", n, re.I) for n in names)
        assert not any(Path(n).name in ("declang-config.json", "obfuscation-targets.json") or "/declang/" in n.lower() or "/.declang/" in n.lower() or "/.godot/mono/dn2cpp/" in n for n in names)
        assert any(n.startswith("assets/.godot/mono/publish/arm64/") for n in names)
        binary = archive.read("lib/arm64-v8a/libEditorExportSample.so")
    assert binary[:5] == b"\x7fELF\x02" and int.from_bytes(binary[18:20], "little") == 183
    so = output / f"declang-{label}.so"
    so.write_bytes(binary)
    symbols = subprocess.check_output([str(nm), "-D", "--defined-only", str(so)], text=True)
    assert re.search(r"\bgodotsharp_game_main_init\b", symbols)
    builds = list((project / ".godot/mono/dn2cpp/build").glob("*android*declang"))
    assert len(builds) == 1, builds
    build = builds[0]
    libraries = list(build.rglob("libEditorExportSample.so"))
    assert any(path.read_bytes() == binary for path in libraries), "APK does not contain the DeClang build"
    subprocess.run([sys.executable, str(root / "gates/fixtures/declang-export-checks.py"),
                    "snapshot", str(build), str(output / "declang-state.json")], check=True)
    logs = sorted((project / ".godot/mono/temp/bin/dn2cpp/logs").glob("export-*.log"),
                  key=lambda p: p.stat().st_mtime_ns)
    assert logs
    log = logs[-1].read_text(encoding="utf-8")
    if label.startswith("prestrip-"):
        if label.endswith("True"):
            assert re.search(r"ILDiet: removed [0-9]+ types and [0-9]+ methods", log), log
            assert "--no-ildiet" not in log
        else:
            assert "--no-ildiet" in log and "ILDiet:" not in log, log
    assert "using the prebuilt runtime" not in log
    return build


try:
    configure(compiler, "")
    export("empty-seed", "declang_seed")
    configure("/missing/declang", "android-export-gate")
    export("missing-compiler", "existing compiler executable")
    configure(next(ndk.glob("toolchains/llvm/prebuilt/*/bin/clang++" + (".exe" if os.name == "nt" else ""))), "android-export-gate")
    export("ordinary-clang", "DeClang produced no application log")
    for prestrip in (False, True):
        configure(compiler, "android-export-gate", prestrip)
        build = export(f"prestrip-{prestrip}")
    configure(compiler, "android-export-gate-changed", True)
    previous = output / "declang-before-seed.json"
    previous.write_bytes((output / "declang-state.json").read_bytes())
    export("seed-changed")
    checks = [sys.executable, str(root / "gates/fixtures/declang-export-checks.py")]
    subprocess.run([*checks, "check-seed", str(build), str(previous)], check=True)
    subprocess.run([*checks, "delete-result", str(build), str(previous)], check=True)
    subprocess.run(["cmake", "--build", str(build)], check=True, timeout=3600)
    subprocess.run([*checks, "check-rebuild", str(build), str(previous)], check=True)
finally:
    game.write_text(original_game, encoding="utf-8")
    presets.write_text(original, encoding="utf-8")
    attribute.unlink()
    probe.unlink()
print("Android DeClang APK checks passed; no device execution performed")
