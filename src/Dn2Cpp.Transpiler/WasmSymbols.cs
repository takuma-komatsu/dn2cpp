// Reads a wasm binary's import/export surface without running it, and answers the
// side-module import-closure question: which of a SIDE module's imports can nothing
// satisfy? Emscripten will not say — dlopen succeeds, dlsym resolves, and the lazy
// stub throws `TypeError: resolved is not a function` only when the function is first
// CALLED, naming a function index and not the symbol; an unexported GOT address is
// left at 0 the same way. This converts both into a named failure at build time, from
// the shipped toolchain, with no node dependency. Behavior-equivalent port of
// `gates/_wasm_symbols.js unsatisfied` (the gate-side oracle; a differential gate
// diffs the two outputs literally, so ordering and line format must match it).

using System.Text;

namespace Dn2Cpp;

public static class WasmSymbols
{
    private sealed class WasmImport
    {
        public string Mod = "";
        public string Field = "";
        public bool IsFunc;
    }

    private sealed class WasmExport
    {
        public string Name = "";
        public bool IsFunc;
    }

    private sealed class WasmModuleInfo
    {
        public readonly List<WasmImport> Imports = new();
        public readonly List<WasmExport> Exports = new();
    }

    /// <summary>Prints each unsatisfied SIDE import on its own line to stdout, in
    /// import-section order, and returns how many were printed. Function imports
    /// resolve against MAIN's func exports plus the JS-library functions its glue
    /// supplies — without the glue file, MAIN's own env func imports stand in for
    /// the latter (the subset MAIN's own code calls); with it, the glue's
    /// wasmImports keys are the whole set. GOT.mem/GOT.func entries resolve
    /// against the GLOBAL symbol table — every module's exports pooled, the SIDE
    /// module's own included, of any kind. Throws <see cref="NotSupportedException"/>
    /// on a glue file with no wasmImports object.</summary>
    public static int PrintUnsatisfied(string sidePath, string mainPath, string? gluePath)
    {
        WasmModuleInfo side = Parse(sidePath);
        WasmModuleInfo main = Parse(mainPath);
        HashSet<string> glueFuncs = gluePath is null ? new HashSet<string>() : GlueWasmImports(gluePath);

        var providedFuncs = new HashSet<string>(glueFuncs);
        foreach (WasmExport e in main.Exports)
            if (e.IsFunc)
                providedFuncs.Add(e.Name);
        foreach (WasmImport im in main.Imports)
            if (im.IsFunc && im.Mod == "env")
                providedFuncs.Add(im.Field);

        // A glue-supplied JS function is given a table slot on demand, so it too can
        // satisfy a GOT.func entry.
        var globalSymbols = new HashSet<string>(glueFuncs);
        foreach (WasmExport e in main.Exports)
            globalSymbols.Add(e.Name);
        foreach (WasmExport e in side.Exports)
            globalSymbols.Add(e.Name);

        int unsatisfied = 0;
        foreach (WasmImport im in side.Imports)
        {
            if (im.IsFunc && im.Mod == "env")
            {
                if (providedFuncs.Contains(im.Field) || IsBenignTlsInit(im.Field))
                    continue;
                Console.WriteLine(im.Field);
                unsatisfied++;
            }
            else if (im.Mod == "GOT.mem" || im.Mod == "GOT.func")
            {
                if (globalSymbols.Contains(im.Field) || IsBenignTlsInit(im.Field))
                    continue;
                Console.WriteLine($"{im.Mod}.{im.Field}");
                unsatisfied++;
            }
        }
        return unsatisfied;
    }

    // _ZTH<digits>sf_… — the weak, never-emitted TLS-init routine for a
    // constant-initialized `thread_local` ([ThreadStatic]). Emscripten resolves it to
    // null exactly as a static link does, and the TLS wrapper's guard skips the call —
    // verified benign across the dlink boundary, and these name the transpiled
    // program's own statics, so no main module could ever define one.
    private static bool IsBenignTlsInit(string field)
    {
        if (!field.StartsWith("_ZTH", StringComparison.Ordinal))
            return false;
        int i = 4;
        while (i < field.Length && field[i] >= '0' && field[i] <= '9')
            i++;
        return i > 4 && i + 3 <= field.Length
            && field[i] == 's' && field[i + 1] == 'f' && field[i + 2] == '_';
    }

    private static WasmModuleInfo Parse(string path)
    {
        byte[] buf = File.ReadAllBytes(path);
        // The .js oracle skips this check and walks garbage silently; a wrong file
        // here must not read as "no imports" — that is the fail-open this guards.
        if (buf.Length < 8 || buf[0] != 0x00 || buf[1] != 0x61 || buf[2] != 0x73 || buf[3] != 0x6d)
            throw new NotSupportedException($"{path} is not a wasm binary (no \\0asm magic)");
        var m = new WasmModuleInfo();
        int p = 8; // magic + version
        int importStart = -1, exportStart = -1;
        while (p < buf.Length)
        {
            byte id = buf[p++];
            int size = (int)ReadU32(buf, ref p);
            if (id == 2 && importStart < 0)
                importStart = p;
            else if (id == 7 && exportStart < 0)
                exportStart = p;
            p += size;
        }

        if (importStart >= 0)
        {
            p = importStart;
            uint n = ReadU32(buf, ref p);
            for (uint i = 0; i < n; i++)
            {
                string mod = ReadName(buf, ref p);
                string field = ReadName(buf, ref p);
                byte kind = buf[p++];
                // Skip each kind's descriptor so the cursor lands on the next entry;
                // table and mem limits carry an optional max (flags bit 0).
                if (kind == 0)
                    ReadU32(buf, ref p); // typeidx
                else if (kind == 1)
                {
                    p++; // elemtype
                    byte flags = buf[p++];
                    ReadU32(buf, ref p);
                    if ((flags & 1) != 0)
                        ReadU32(buf, ref p);
                }
                else if (kind == 2)
                {
                    byte flags = buf[p++];
                    ReadU32(buf, ref p);
                    if ((flags & 1) != 0)
                        ReadU32(buf, ref p);
                }
                else if (kind == 3)
                    p += 2; // valtype + mutability
                else if (kind == 4)
                {
                    p++; // attribute
                    ReadU32(buf, ref p);
                }
                m.Imports.Add(new WasmImport { Mod = mod, Field = field, IsFunc = kind == 0 });
            }
        }

        if (exportStart >= 0)
        {
            p = exportStart;
            uint n = ReadU32(buf, ref p);
            for (uint i = 0; i < n; i++)
            {
                string name = ReadName(buf, ref p);
                byte kind = buf[p++];
                ReadU32(buf, ref p); // index
                m.Exports.Add(new WasmExport { Name = name, IsFunc = kind == 0 });
            }
        }
        return m;
    }

    private static uint ReadU32(byte[] buf, ref int p)
    {
        uint r = 0;
        int s = 0;
        byte b;
        do
        {
            b = buf[p++];
            r |= (uint)(b & 0x7f) << s;
            s += 7;
        } while ((b & 0x80) != 0);
        return r;
    }

    private static string ReadName(byte[] buf, ref int p)
    {
        int n = (int)ReadU32(buf, ref p);
        string v = Encoding.UTF8.GetString(buf, p, n);
        p += n;
        return v;
    }

    // The depth-1 keys of an Emscripten glue file's `wasmImports = {...}` object
    // literal: the JS-supplied symbols the loader's resolveGlobalSymbol serves to
    // side modules — under MAIN_MODULE=1 that includes --js-library functions the
    // main wasm's own import section never names. A depth-aware scan, not a regex
    // over the whole object: values may be inline functions holding their own object
    // literals, so only depth-1 keys count. The keys are unminified even in an -O3
    // glue — side modules resolve by name. No `wasmImports` object at all means the
    // file is not a main-module glue, and that must fail loudly rather than silently
    // weaken the provided set.
    private static HashSet<string> GlueWasmImports(string path)
    {
        string js = File.ReadAllText(path);
        int brace = FindWasmImportsBrace(js);
        if (brace < 0)
            throw new NotSupportedException($"{path} has no \"wasmImports = {{...}}\" — not an emscripten main-module JS glue?");
        var names = new HashSet<string>();
        int depth = 0;
        char inStr = '\0';
        bool esc = false, inValue = false;
        var token = new StringBuilder();
        for (int i = brace; i < js.Length; i++)
        {
            char ch = js[i];
            if (inStr != '\0')
            {
                if (esc)
                    esc = false;
                else if (ch == '\\')
                    esc = true;
                else if (ch == inStr)
                    inStr = '\0';
                else if (!inValue && depth == 1)
                    token.Append(ch); // quoted key
                continue;
            }
            if (ch == '"' || ch == '\'')
            {
                inStr = ch;
                continue;
            }
            if (ch == '{')
            {
                depth++;
                continue;
            }
            if (ch == '}')
            {
                if (--depth == 0)
                    break;
                continue;
            }
            if (depth != 1)
                continue;
            if (ch == ',')
            {
                inValue = false;
                token.Clear();
            }
            else if (ch == ':' && !inValue)
            {
                if (token.Length > 0)
                    names.Add(token.ToString());
                inValue = true;
                token.Clear();
            }
            else if (!inValue && IsIdentChar(ch))
                token.Append(ch);
            else if (!inValue && !char.IsWhiteSpace(ch))
                token.Clear();
        }
        return names;
    }

    // `wasmImports` preceded by a non-word char, then \s* = \s* { — the opening
    // brace's index, or -1.
    private static int FindWasmImportsBrace(string js)
    {
        int from = 0;
        while (true)
        {
            int at = js.IndexOf("wasmImports", from, StringComparison.Ordinal);
            if (at < 0)
                return -1;
            from = at + 1;
            if (at > 0 && IsWordChar(js[at - 1]))
                continue;
            int j = at + 11;
            while (j < js.Length && char.IsWhiteSpace(js[j]))
                j++;
            if (j >= js.Length || js[j] != '=')
                continue;
            j++;
            while (j < js.Length && char.IsWhiteSpace(js[j]))
                j++;
            if (j < js.Length && js[j] == '{')
                return j;
        }
    }

    private static bool IsWordChar(char c) =>
        c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_';

    private static bool IsIdentChar(char c) => IsWordChar(c) || c == '$';
}
