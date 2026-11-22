"""Declare the selected implementation pack's managed files, not all DLLs we ship."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import sys
import tempfile
import xml.etree.ElementTree as ET


FRAMEWORK = "Microsoft.NETCore.App"
CORELIB = "System.Private.CoreLib.dll"
CROSS_RID = "linux-x64"
HEADER = "dn2cpp-bcl-v1"
SIDECAR = HEADER + ".tsv"


def unique_object(pairs):
    obj = {}
    for key, value in pairs:
        if key in obj:
            raise ValueError("duplicate deps.json key: " + key)
        obj[key] = value
    return obj


def windows_rid(rid):
    return re.match(r"win(?:[0-9]+(?:\.[0-9]+)*)?(?:-|$)", rid) is not None


def posix_only(rid):
    return ValueError("ref-posix cannot use the " + rid + " runtime; point DN2CPP_CORELIB_CROSS at "
                      "a .NET 11 " + CROSS_RID + " CoreLib or unset the override")


def admit_rid(rid, kind):
    if kind == "host" or rid == CROSS_RID:
        return True
    # A different POSIX override is not inventoried, but only positive
    # evidence of a Windows runtime invalidates a custom selection.
    if kind == "override" and not windows_rid(rid):
        return False
    raise posix_only(rid)


def evident_rids(src):
    """Runtime identifiers the directory names; no version or pack shape is validated."""
    if len(src.parts) >= 4 and src.parts[-4] == "runtimes" and src.parts[-2] == "lib":
        yield src.parts[-3]
    try:
        with (src / (FRAMEWORK + ".deps.json")).open(encoding="utf-8-sig") as reader:
            target = json.load(reader)["runtimeTarget"]["name"]
    except (OSError, ValueError, KeyError, TypeError):
        return
    if isinstance(target, str) and "/" in target:
        yield target.rpartition("/")[2]


def deps_assets(path, version):
    with path.open(encoding="utf-8-sig") as reader:
        deps = json.load(reader, object_pairs_hook=unique_object)
    target = deps["runtimeTarget"]["name"]
    match = re.fullmatch(r"\.NETCoreApp,Version=v11\.0/([a-z0-9.-]+)", target)
    if not match:
        raise ValueError("invalid net11 runtime target: " + target)
    rid = match.group(1)
    key = FRAMEWORK + ".Runtime." + rid + "/" + version
    assets = deps["targets"][target][key]["runtime"]
    if deps["libraries"][key]["type"] != "package" or not isinstance(assets, dict):
        raise ValueError("not a runtime pack: " + key)
    return rid, [name for name in assets if name.lower().endswith(".dll")]


def nuget_assets(root, package, version, rid):
    # NuGet's global-packages folder contains the normalized lowercase nuspec.
    spec = ET.parse(root / (package.lower() + ".nuspec")).getroot()
    ns = spec.tag[:-len("package")]
    if (spec.tag != ns + "package"
            or (spec.findtext(ns + "metadata/" + ns + "id") or "").lower() != package.lower()
            or spec.findtext(ns + "metadata/" + ns + "version") != version):
        raise ValueError("NuGet implementation pack identity does not match " + str(root))
    listing = ET.parse(root / "data" / "RuntimeList.xml").getroot()
    if (listing.tag != "FileList" or listing.get("FrameworkName", FRAMEWORK) != FRAMEWORK
            or listing.get("TargetFrameworkIdentifier", ".NETCoreApp") != ".NETCoreApp"
            or listing.get("TargetFrameworkVersion", "11.0") not in ("11.0", "v11.0")
            or listing.get("RuntimeIdentifier", rid) != rid):
        raise ValueError("not a runtime file list for " + package)
    names = []
    for file in listing.findall("File"):
        if file.get("Type") != "Managed":
            continue
        parts = file.attrib["Path"].replace("\\", "/").split("/")
        if any(part in ("", ".", "..") or ":" in part for part in parts):
            raise ValueError("invalid NuGet managed asset path: " + file.attrib["Path"])
        if parts[:-1] == ["runtimes", rid, "lib", "net11.0"]:
            names.append(parts[-1])
    return names


def selected_assets(corelib, kind):
    src = corelib.parent
    # A framework left unverified below is still shipped, so the Windows
    # refusal precedes every test of its name, layout and version.
    if kind != "host":
        for rid in evident_rids(src):
            if windows_rid(rid):
                raise posix_only(rid)
    if corelib.name != CORELIB:
        return None
    if src.parent.name == FRAMEWORK and src.parent.parent.name == "shared":
        version = src.name
        layout = "shared"
    elif (kind != "host" and len(src.parts) >= 6 and src.parts[-4] == "runtimes"
          and src.parts[-2:] == ("lib", "net11.0")):
        runtime_rid = src.parts[-3]
        if runtime_rid != CROSS_RID and not windows_rid(runtime_rid):
            return None
        root = src.parents[3]
        package = FRAMEWORK + ".Runtime." + runtime_rid
        if root.parent.name.lower() != package.lower():
            return None
        version = root.name
        layout = "nuget"
    else:
        return None
    if not re.fullmatch(r"11\.0\.[0-9]+(?:[-+][a-zA-Z0-9.+-]+)?", version):
        return None
    deps = src / (FRAMEWORK + ".deps.json")
    if deps.is_file():
        rid, names = deps_assets(deps, version)
        if layout == "nuget" and rid != runtime_rid:
            raise ValueError("runtime target " + rid + " does not match NuGet runtime directory "
                             + runtime_rid + ": " + str(deps))
        return names if admit_rid(rid, kind) else None
    if layout == "nuget" and (root / "data" / "RuntimeList.xml").is_file():
        if kind != "override" or (root / (package.lower() + ".nuspec")).is_file():
            names = nuget_assets(root, package, version, runtime_rid)
            return names if admit_rid(runtime_rid, kind) else None
    if kind == "override":
        return None
    raise ValueError("missing runtime pack metadata for " + str(corelib))


def digest(directory, name):
    asset = directory / name
    if not asset.is_file() or asset.resolve().parent != directory.resolve():
        raise ValueError("missing or external runtime asset: " + str(asset))
    sha = hashlib.sha256()
    with asset.open("rb") as reader:
        for block in iter(lambda: reader.read(1024 * 1024), b""):
            sha.update(block)
    return sha.hexdigest()


def declaration(names, staged, source=None):
    if CORELIB not in names:
        raise ValueError("runtime pack does not declare " + CORELIB)
    if len(set(name.lower() for name in names)) != len(names):
        raise ValueError("duplicate runtime DLL basename")
    lines = [HEADER]
    for name in sorted(names):
        if not re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9_.-]*\.dll", name):
            raise ValueError("invalid runtime DLL basename: " + name)
        sha = digest(staged, name)
        if source is not None and sha != digest(source, name):
            raise ValueError("staged DLL differs from its source: " + name)
        lines.append(sha + "\t" + name)
    return ("\n".join(lines) + "\n").encode("ascii")


def write_inventory(corelib, staged, kind):
    sidecar = staged / SIDECAR
    names = selected_assets(corelib, kind)
    if names is None:
        # Unknown overrides still ship their DLLs; never reuse a former declaration.
        if sidecar.exists():
            sidecar.unlink()
        print("warning: no BCL inventory for unverified framework: " + str(corelib), file=sys.stderr)
        return
    data = declaration(names, staged, corelib.parent)
    fd, name = tempfile.mkstemp(dir=str(staged), prefix="." + SIDECAR)
    try:
        with os.fdopen(fd, "wb") as tmp:
            tmp.write(data)
        os.chmod(name, 0o644)
        os.replace(name, str(sidecar))
    finally:
        if os.path.exists(name):
            os.unlink(name)


def verify_inventory(staged):
    data = (staged / SIDECAR).read_bytes()
    lines = data.decode("ascii").split("\n")
    if lines[0] != HEADER or lines[-1] != "":
        raise ValueError("invalid BCL inventory header or line ending")
    names = [row.partition("\t")[2] for row in lines[1:-1]]
    if data != declaration(names, staged):
        raise ValueError("BCL inventory differs from staged DLLs: " + str(staged))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("staged", type=Path)
    parser.add_argument("--source-corelib", type=Path, help="generate; otherwise verify the declaration")
    parser.add_argument("--kind", choices=("host", "cross", "override"), default="host")
    args = parser.parse_args()
    try:
        if args.source_corelib:
            write_inventory(args.source_corelib, args.staged, args.kind)
        else:
            verify_inventory(args.staged)
    except (ValueError, KeyError, TypeError, OSError, ET.ParseError) as err:
        sys.exit("error: BCL inventory: " + str(err))
