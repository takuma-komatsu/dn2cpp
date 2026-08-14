#!/usr/bin/env bash
# Shared helpers for the godot-dotnet (mono-module) gates. Sourced after
# _common.sh by gates/build-and-run-godot-dotnet-{lib,handshake,sample}.sh,
# gates/build-and-run-gdtask.sh and gates/dotnet-measure.sh — all need the same
# pipeline: real-Godot.NET.Sdk sample build -> --dotnet-module transpile
# against the real GodotSharp -> mono-module shared library. Requires the
# artifacts of gates/setup-godot-dotnet.sh; DN2CPP_GODOT_DOTNET_ROOT overrides
# where to look, defaulting to the setup script's own default root — artifacts
# built there are found with no env var set. (The engine-launch watchdog is
# run_with_watchdog in gates/_common.sh.)

GODOT_DOTNET_SAMPLE_DIR=samples/godot-dotnet/DotnetSample
GODOT_DOTNET_ROOT="${DN2CPP_GODOT_DOTNET_ROOT:-$HOME/.cache/dn2cpp-godot-dotnet}"
GODOT_DOTNET_GODOTSHARP="$GODOT_DOTNET_ROOT/GodotSharp/Api/Release/GodotSharp.dll"

# The two engine binaries setup-godot-dotnet.sh links into the artifact root.
# Both are *executed* (the editor directly, the template after a cp), so they
# carry EXE_EXT — an extension-less PE image is not launchable on Windows, while
# `test -x` says yes to one under MSYS, which is how a guard that spells the name
# by hand fails to skip and dies at exec instead.
#
# Both are symlinks here (this root still links; the fork root records paths
# instead — see gates/_godot_fork.sh), and the four gates that key on them —
# godot-dotnet-{sample,handshake,trim} and gdtask — must sign them with
# file_sig_deref, never with `readlink`. A readlink term is the TARGET PATH, and
# the path does not move when the engine is rebuilt at it: measured on a live
# root, the term is byte-identical across a rebuild, so those four gates would
# hit a warm key and replay a green over an engine they never ran.
GODOT_DOTNET_EDITOR="$GODOT_DOTNET_ROOT/editor_bin$EXE_EXT"
GODOT_DOTNET_TEMPLATE="$GODOT_DOTNET_ROOT/template_bin$EXE_EXT"

# This file's CONTENT is in every gate's cache key (_gate_helpers_hash in
# _common.sh hashes the whole gates/_*.sh set), and that is load-bearing here
# rather than incidental: godot_dotnet_link_lib below links the shared library
# the engine is run against, and the link sits BELOW gate_cache_check, so a warm
# hit does not build it at all — nothing downstream can notice that the green it
# replays was recorded against a library this file no longer produces. Every
# other input to that library was already keyed (the generated C++, runtime/ +
# third_party/, the compiler, every build-axis env var, compile_dotnet_module's
# own definition), leaving the compile INVOCATION and GODOT_DOTNET_PLATFORM —
# which picks the data_<Assembly>_<platform>_<arch> directory the engine gates
# stage that library into. Both live here.
#
# Keying the whole helper SET is what leaves no gate anything to remember:
# per-gate wiring is one forgotten argument away from leaving a consumer of this
# file unkeyed, and nothing says so.

# GODOT_DOTNET_PLATFORM — the engine's own name for this platform, as it appears
# in the data_<Assembly>_<platform>_<arch> directory try_load_native_aot_library
# reads (modules/mono/godotsharp_dirs.cpp: exe_dir/data_<appname>_<platform>_<arch>).
#
# This is NOT $DN2CPP_OS and must not be derived from it: the engine's
# platform_name_map (godotsharp_dirs.cpp, "the equivalent of
# GodotTools.Utils.OS.PlatformNameMap") maps Linux to "linuxbsd", not "linux".
# Substituting $DN2CPP_OS would work on macOS and Windows and put the library
# somewhere the engine never looks on Linux — where the failure is a missing
# dlopen deep inside the engine, not a missing file the gate could name.
case "$DN2CPP_OS" in
    macos)   GODOT_DOTNET_PLATFORM=macos ;;
    windows) GODOT_DOTNET_PLATFORM=windows ;;
    linux)   GODOT_DOTNET_PLATFORM=linuxbsd ;;
    *)       GODOT_DOTNET_PLATFORM=unknown ;;
esac

# godot_dotnet_host_arch [MACHINE] — echo the host architecture as
# Engine::get_architecture_name spells it for data_<Assembly>_<platform>_<arch>.
godot_dotnet_host_arch() {
    local machine="${1:-$(uname -m)}"
    case "$machine" in
        aarch64|arm64) printf 'arm64\n' ;;
        amd64|x86_64)  printf 'x86_64\n' ;;
        *)             printf '%s\n' "$machine" ;;
    esac
}

# godot_dotnet_root_ok — true when the setup artifacts a *build-only* gate needs
# (the GodotSharp API assembly + the local Godot.NET.Sdk feed) are present.
godot_dotnet_root_ok() {
    [ -n "$GODOT_DOTNET_ROOT" ] && [ -f "$GODOT_DOTNET_GODOTSHARP" ] \
        && compgen -G "$GODOT_DOTNET_ROOT/nuget/Godot.NET.Sdk.*.nupkg" >/dev/null
}

# godot_dotnet_nuget_config DIR — write DIR/nuget.config (gitignored).
#
# It is generated rather than checked in because nuget.config cannot expand
# environment variables, and the MSBuild NuGet SDK resolver — which resolves
# Sdk="Godot.NET.Sdk/4.7.1" before any restore flag can apply — discovers config
# by walking up from the project directory. So the machine-specific feed path has
# to be *in the file*, and the file has to be *next to the project*.
#
# Two sources, and both are load-bearing:
#   - the local feed built by gates/setup-godot-dotnet.sh, which carries the
#     engine's own packages (Godot.NET.Sdk / GodotSharp / Godot.SourceGenerators)
#     packed from the pinned clone — these MUST come from there, not from
#     nuget.org, or the sample would build against a different engine than the
#     one the gate then runs it in;
#   - nuget.org, for anything else a sample PackageReferences. That is how
#     GDTaskSample acquires the real GDTask 3.1.0: a plain PackageReference, no
#     vendored DLL. (The <clear/> above them means these two are the only
#     sources, whatever the user's machine-wide NuGet config says.)
godot_dotnet_nuget_config() {
    local dir="$1"
    # NuGet is a native Windows program: it reads this file, not bash. An MSYS
    # path ("/c/Users/...") would be taken as drive-relative and resolve to
    # C:\c\Users\... — the feed then simply has no packages in it, and what
    # surfaces is a restore error about Godot.NET.Sdk, naming nothing about the
    # path. cygpath -m gives the C:/... form: a real Windows path, but with
    # forward slashes, so it needs no XML/backslash escaping.
    local feed="$GODOT_DOTNET_ROOT/nuget"
    [ "$DN2CPP_OS" = windows ] && feed="$(cygpath -m "$feed")"
    cat > "$dir/nuget.config" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<!-- GENERATED by gates/_godot_dotnet.sh — do not edit, do not commit. Points the
     Godot.NET.Sdk resolver + restore at the local feed built by
     gates/setup-godot-dotnet.sh; nuget.org supplies everything else a sample
     PackageReferences (GDTaskSample: the real GDTask). -->
<configuration>
  <packageSources>
    <clear />
    <add key="dn2cpp-godot-dotnet-local" value="$feed" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
EOF
}

# The pipeline is TWO halves, and the split is the cache boundary.
#
# godot_dotnet_transpile OUT — the key-producing half: generate the sample's
# nuget.config, build the sample with the real Godot.NET.Sdk, and transpile it
# with --dotnet-module against the real GodotSharp + the net10 CoreLib into OUT.
# The caller must have verified godot_dotnet_root_ok first, and must call
# gate_cache_check next: this half produces the whole of the key's material
# (the app assembly and the generated surface), so it cannot move behind the
# check — there would be nothing left to key on.
godot_dotnet_transpile() {
    local out="$1"

    echo "-- Generating the sample's nuget.config (local feed + nuget.org)"
    godot_dotnet_nuget_config "$GODOT_DOTNET_SAMPLE_DIR"

    echo "-- Building the sample game assembly (real Godot.NET.Sdk)"
    # ExportRelease is Godot.NET.Sdk's release configuration; the Sdk routes
    # output to .godot/mono/temp/bin/<config>/. Built here even under
    # DN2CPP_SKIP_BUILD: the orchestrator's prebuild phase uses -c Release, not
    # this Sdk-specific configuration, and of the gates only the two
    # godot-dotnet ones touch this project — the lib gate in the parallel phase,
    # the handshake gate in the later serial Godot phase (never concurrently).
    dotnet build "$GODOT_DOTNET_SAMPLE_DIR/DotnetSample.csproj" -c ExportRelease --nologo -v q
    local app="$GODOT_DOTNET_SAMPLE_DIR/.godot/mono/temp/bin/ExportRelease/DotnetSample.dll"
    [ -f "$app" ] || { echo "error: not built: $app" >&2; return 1; }

    echo "-- Transpiling (--dotnet-module, real GodotSharp + net10 CoreLib)"
    build_proj src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj
    # Pin net10.0 (the JSON/measure gates' rule): the highest installed runtime
    # can be an 11.0 preview whose CoreLib shape skews the transpile spuriously.
    local corelib; corelib=$(resolve_net10_corelib)
    rm -rf "$out"
    invoke_cli "$app" --dotnet-module -r "$corelib" -r "$GODOT_DOTNET_GODOTSHARP" --auto-ref -o "$out"
}

# godot_dotnet_link_lib OUT — the other half: link the mono-module shared library
# godot_dotnet_link_lib OUT — the other half: link the mono-module shared library
# OUT/$(lib_name DotnetSample). Call it AFTER gate_cache_check, never before.
#
# It contributes nothing to the key, measured rather than assumed: everything it
# writes into OUT is either dot-prefixed (the .cmake/.cmake-android build trees)
# or named lib<Assembly>.<ext>, and _gate_surface_lines lists OUT with a bare
# `ls -1` keeping only ^(generated|pinvoke-libs.txt|base-abi.json) — so it sees
# neither. And no consumer needs it on a hit: all four of its callers
# (godot-dotnet-{handshake,lib,sample,trim}) exit at gate_cache_hit_msg, before
# their first mention of $DYLIB. That is a DIFFERENT four from the set at the top
# of this file, which signs the editor/template paths — say which you mean, since
# "all four" read alone points at neither.
godot_dotnet_link_lib() {
    local out="$1"
    echo "-- Building the mono-module shared library"
    compile_dotnet_module "$out" "$out/$(lib_name DotnetSample)"
}

# godot_dotnet_pin_abi_check PINNED_COMMIT ABI_EXPECTED — the shared pin +
# interop-ABI tripwire of the engine E2E gates. The artifacts, the clone, and
# the gate's expectations must all describe one commit: a drifted clone
# silently changes the engine side of the handshake ABI. Fingerprints the three
# ABI surfaces the emitted entry hard-codes assumptions about: the engine's
# interop function table (order/count of unmanaged_callbacks —
# NativeFuncs.Initialize indexes into it), the ManagedCallbacks struct
# (order/count of the 37 slots; the FrameCallback wrap addresses slot 7), and
# the C# interop declarations themselves — the backend reconstructs each calli
# function-pointer type from those signatures, so a changed one is a re-audit
# and not a re-freeze.
# Requires $GODOT_DOTNET_ROOT/pin.txt + editor_bin; exports CLONE.
godot_dotnet_pin_abi_check() {
    local pinned="$1" abi_expected="$2"
    local root_pin
    root_pin="$(cat "$GODOT_DOTNET_ROOT/pin.txt")"
    if [ "$root_pin" != "$pinned" ]; then
        echo "FAIL: $GODOT_DOTNET_ROOT/pin.txt ($root_pin) != expected pin $pinned" >&2
        echo "      Rebuild the artifacts (gates/setup-godot-dotnet.sh) or update the pin deliberately." >&2
        return 1
    fi
    # Resolve the clone to fingerprint. In order: an explicit override; what
    # setup-godot-dotnet.sh recorded; then the two legacy inferences, kept for a
    # root assembled before clone.txt existed — editor_bin pointing into
    # <clone>/bin/ (true only when the editor was built from the clone, never for
    # a prebuilt official install), else the sibling-directory default.
    CLONE="${DN2CPP_GODOT_CLONE:-}"
    if [ -z "$CLONE" ] && [ -f "$GODOT_DOTNET_ROOT/clone.txt" ]; then
        CLONE="$(cat "$GODOT_DOTNET_ROOT/clone.txt")"
    fi
    if [ -z "$CLONE" ]; then
        local editor_target
        editor_target="$(readlink "$GODOT_DOTNET_EDITOR" || true)"
        case "$editor_target" in
            /*) CLONE="$(dirname "$(dirname "$editor_target")")" ;;
            *)  CLONE="$(dirname "$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)")/godot" ;;
        esac
    fi
    if [ ! -e "$CLONE/modules/mono/mono_gd/gd_mono.cpp" ]; then
        echo "FAIL: godot clone not found at $CLONE (set DN2CPP_GODOT_CLONE) — the pin/ABI tripwire cannot run" >&2
        return 1
    fi
    local clone_head
    clone_head="$(git -C "$CLONE" rev-parse HEAD)"
    if [ "$clone_head" != "$pinned" ]; then
        echo "FAIL: godot clone at $CLONE has DRIFTED from the pinned commit" >&2
        echo "      expected: $pinned" >&2
        echo "      actual:   $clone_head" >&2
        return 1
    fi
    local abi_actual
    abi_actual="$(
        awk '/^static const void \*unmanaged_callbacks\[\]\{/{f=1} f{print} f&&/^\};/{exit}' \
            "$CLONE/modules/mono/glue/runtime_interop.cpp" \
            | shasum -a 256 | awk '{print $1"  unmanaged_callbacks"}'
        shasum -a 256 "$CLONE/modules/mono/glue/GodotSharp/GodotSharp/Core/Bridge/ManagedCallbacks.cs" \
            | awk '{print $1"  ManagedCallbacks.cs"}'
        shasum -a 256 "$CLONE/modules/mono/glue/GodotSharp/GodotSharp/Core/NativeInterop/NativeFuncs.cs" \
            | awk '{print $1"  NativeFuncs.cs"}'
    )"
    # A MISSING baseline is a failure, not an invitation to record one. The
    # self-freezing arm this replaces turned the whole ABI assertion into
    # A MISSING baseline is a failure, not an invitation to record one. A
    # self-freezing arm turns the whole ABI assertion into "certify whatever this
    # clone says" for three gates (handshake, sample, gdtask) whenever the
    # committed file is absent — and deleting the file is the obvious thing to
    # reach for when the tripwire goes red, which converts a caught ABI drift
    # into silence. The baseline IS committed, so this can only fire on a
    # deletion; the fork twin (_godot_fork.sh) asks it the same way.
    if [ ! -f "$abi_expected" ]; then
        echo "FAIL: $abi_expected is missing — the interop-ABI baseline is committed, so this is a deletion, not a first run." >&2
        echo "      Restore it from git rather than re-freezing: a regenerated baseline certifies whatever this clone happens to say." >&2
        echo "      The current clone's fingerprints, for reference:" >&2
        printf '%s\n' "$abi_actual" >&2
        return 1
    fi
    if [ "$abi_actual" != "$(cat "$abi_expected")" ]; then
        echo "FAIL: interop ABI fingerprints differ from $abi_expected" >&2
        echo "expected:" >&2; cat "$abi_expected" >&2
        echo "actual:" >&2; printf '%s\n' "$abi_actual" >&2
        echo "      The mono-module handshake ABI changed — re-audit the emitted entry" >&2
        echo "      (slot order/count, interop signature widths) before re-freezing." >&2
        return 1
    fi
    echo "pin OK: $pinned (clone: $CLONE); ABI fingerprints OK"
}

# godot_dotnet_interop_return_abi_check CLONE AGG_EXPECTED — that no interop
# slot's RETURN width disagrees between the C# declaration and the engine's C
# definition, except the one class the DotnetModule backend corrects: a
# return-position core enum, which Godot's bindings generator emits as `: long`
# while the glue returns `int32_t`. The backend rebuilds each calli
# function-pointer type from the managed signature, so that pair traps wasm32's
# call_indirect ("function signature mismatch"); a native target absorbs it in
# the return register, which is why nothing else names it.
#
# Parameters are not compared: the NativeFuncs.cs fingerprint above already
# turns any declaration change into a re-audit, for a fraction of what modelling
# both sides' parameter spellings would cost. Aggregates are not modelled either
# — many slots return a struct by value and the glue collapses all ten
# packed-array flavours onto one opaque type. A pair with an aggregate on either
# side is skipped, and the per-side count of those is frozen in AGG_EXPECTED so a
# new type spelling cannot quietly leave the comparison.
godot_dotnet_interop_return_abi_check() {
    local clone="$1" agg_expected="$2"
    local cpp="$clone/modules/mono/glue/runtime_interop.cpp"
    local cs="$clone/modules/mono/glue/GodotSharp/GodotSharp/Core/NativeInterop/NativeFuncs.cs"
    if [ ! -f "$agg_expected" ]; then
        echo "FAIL: $agg_expected is missing — the baseline is committed, so this is a deletion, not a first run." >&2
        echo "      Restore it from git rather than re-freezing: a regenerated baseline certifies whatever this clone says." >&2
        return 1
    fi
    LC_ALL=C awk -v agg_expected="$agg_expected" '
    function trim(s) { gsub(/^[ \t]+|[ \t]+$/, "", s); return s }
    # A wasm32 value type. An unrecognised spelling is "aggregate", which drops
    # the pair from the comparison rather than inventing a width for it — the
    # frozen aggregate count is what refuses that drift.
    function reduce(t, side,   s) {
        s = trim(t); sub(/^const[ \t]+/, "", s); s = trim(s)
        if (s ~ /[*&]$/ || s ~ /^delegate\*/) return "i32"   # a pointer is 32-bit here
        if (side == "cs")  return (s in csw)  ? csw[s]  : "aggregate"
        return (s in cppw) ? cppw[s] : "aggregate"
    }
    function fill(spec, tbl,   a, kv, i) {
        split(spec, a, " ")
        for (i in a) { split(a[i], kv, ":"); tbl[kv[1]] = kv[2] }
    }
    BEGIN {
        # C++, as the glue spells it. Bare `long` is absent on purpose: C `long`
        # is pointer-width, so widening it here would invent an ABI.
        fill("void:void bool:i32 char:i32 int:i32 int8_t:i32 uint8_t:i32 int16_t:i32 uint16_t:i32 int32_t:i32 uint32_t:i32 Error:i32 int64_t:i64 uint64_t:i64 float:f32 double:f64", cppw)
        # C#. IntPtr is POINTER-width, i.e. i32 on wasm32 and not i64. godot_bool
        # is a byte-backed enum, so it too is a scalar; every other Godot core
        # enum is `: long`, which is the whole disagreement this checks for.
        fill("void:void bool:i32 char:i32 sbyte:i32 byte:i32 short:i32 ushort:i32 int:i32 uint:i32 Int32:i32 UInt32:i32 IntPtr:i32 UIntPtr:i32 nint:i32 nuint:i32 godot_bool:i32 long:i64 ulong:i64 Int64:i64 UInt64:i64 float:f32 double:f64", csw)
        enum64["Error"] = 1
        for (e in enum64) csw[e] = "i64"
    }
    NR == FNR {
        if ($0 ~ /^static const void \*unmanaged_callbacks\[\]\{/) { blk = 1; next }
        if (blk) {
            if ($0 ~ /^\};/) { blk = 0; next }
            if (match($0, /godotsharp_[A-Za-z0-9_]+/)) slot[++ns] = substr($0, RSTART, RLENGTH)
            next
        }
        # Every definition puts `<rettype> godotsharp_<name>(` on one line at
        # column 0; only the parameter lists wrap.
        if ($0 ~ /^[A-Za-z_]/ && match($0, /godotsharp_[A-Za-z0-9_]+\(/)) {
            n = substr($0, RSTART, RLENGTH - 1); nc++
            cppret[n] = trim(substr($0, 1, RSTART - 1))
        }
        next
    }
    # The C# return type is on the `static partial` line even for the two
    # declarations that wrap before the name.
    {
        if (pend != "") {
            if (match($0, /godotsharp_[A-Za-z0-9_]+/)) {
                n = substr($0, RSTART, RLENGTH); csname[++nm] = n; csret[n] = pend
            }
            pend = ""; next
        }
        if (match($0, /static partial /)) {
            r = substr($0, RSTART + RLENGTH)
            if (match(r, /godotsharp_[A-Za-z0-9_]+\(/)) {
                n = substr(r, RSTART, RLENGTH - 1); csname[++nm] = n
                csret[n] = trim(substr(r, 1, RSTART - 1))
            } else pend = trim(r)
        }
    }
    END {
        if (ns == 0) { print "FAIL: no unmanaged_callbacks[] block parsed — the slot count has no source"; exit 1 }
        if (ns != nc || ns != nm) {
            printf "FAIL: parse counts differ — %d unmanaged_callbacks[] slots, %d glue definitions, %d C# declarations\n", ns, nc, nm
            bad++
        }
        # The C# declarations are compared in ORDER (NativeFuncs.Initialize
        # indexes into the table); the glue definitions only as a set, because
        # they are grouped by the header they wrap and never were in table order.
        for (i = 1; i <= ns; i++) {
            if (slot[i] != csname[i]) {
                printf "FAIL: slot %d is %s in unmanaged_callbacks[] and %s in NativeFuncs.cs\n", \
                    i, slot[i], (i in csname) ? csname[i] : "<nothing parsed>"
                bad++
            }
            if (!(slot[i] in cppret)) { printf "FAIL: no glue definition parsed for %s\n", slot[i]; bad++ }
            # An empty token means the line matched but the return type is not on
            # it — a wrapped definition, which would otherwise reduce to
            # "aggregate" and leave the slot uncompared.
            if (cppret[slot[i]] == "") { printf "FAIL: no return type on the glue definition line of %s\n", slot[i]; bad++ }
            if (csret[slot[i]] == "")  { printf "FAIL: no return type on the C# declaration of %s\n", slot[i]; bad++ }
            known[slot[i]] = 1
        }
        for (i = ns + 1; i <= nm; i++) { printf "FAIL: NativeFuncs.cs declares %s, which no slot names\n", csname[i]; bad++ }
        for (n in cppret) if (!(n in known)) { printf "FAIL: the glue defines %s, which no slot names\n", n; bad++ }

        for (i = 1; i <= ns; i++) {
            n = slot[i]
            cr = reduce(cppret[n], "cpp"); sr = reduce(csret[n], "cs")
            if (cr == "aggregate") { cagg++; caggspell[cppret[n]]++ }
            if (sr == "aggregate") { sagg++; saggspell[csret[n]]++ }
            if (cr == "aggregate" || sr == "aggregate") continue
            ncmp++
            if (cr == sr) continue
            if (cr == "i32" && sr == "i64" && (csret[n] in enum64)) {
                if (corrected != "") corrected = corrected ", "
                corrected = corrected n
                continue
            }
            printf "FAIL: %s returns %s (%s) in C# and %s (%s) in the glue\n", n, csret[n], sr, cppret[n], cr
            bad++
        }

        got = sprintf("cpp-aggregate %d\ncs-aggregate %d\n", cagg, sagg)
        want = ""
        while ((getline ln < agg_expected) > 0) want = want ln "\n"
        if (want != got) {
            printf "FAIL: aggregate-return counts differ from %s\nexpected:\n%sactual:\n%s", agg_expected, want, got
            printf "      A slot changed to a type spelling the reduction table does not name — widen the table or audit the slot, then re-freeze.\n"
            printf "      Unmodelled return spellings, glue side:\n"
            for (s in caggspell) printf "        %-32s %d\n", s, caggspell[s]
            printf "      Unmodelled return spellings, C# side:\n"
            for (s in saggspell) printf "        %-32s %d\n", s, saggspell[s]
            bad++
        }
        if (bad) exit 1
        printf "interop returns OK: %d slots, %d width-compared, %d aggregate (glue) / %d aggregate (C#)\n", ns, ncmp, cagg, sagg
        printf "corrected return-position enums (glue int32_t, C# 64-bit enum): %s\n", (corrected != "") ? corrected : "none"
    }
    ' "$cpp" "$cs" || return 1
}
