// Reads a wasm binary's import/export surface without running it, and answers the
// side-module import-closure question: which of a SIDE module's imports can nothing
// satisfy? Emscripten will not say — dlopen succeeds, dlsym resolves, and the lazy
// stub throws `TypeError: resolved is not a function` only when the function is first
// CALLED, naming a function index and not the symbol; an unexported GOT address is
// left at 0 the same way. This converts both into a named failure at build time, from
// the shipped toolchain, with no node dependency. Behavior-equivalent port of
// `gates/_wasm_symbols.js unsatisfied` (the gate-side oracle; a differential gate
// diffs the two outputs literally, so ordering and line format must match it) — plus
// --peer-module, which the oracle deliberately lacks.

using System.Text;

namespace Dn2Cpp;

public static class WasmSymbols
{
    private sealed class WasmImport
    {
        public string Mod = "";
        public string Field = "";
        public byte Kind;
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
    /// wasmImports keys are the whole set. Tag imports (`env.__cpp_exception`,
    /// `env.__c_longjmp`) resolve against MAIN's exports of any kind plus the glue
    /// keys: the loader's env proxy serves them from the same flat wasmImports pool,
    /// and unlike a function a tag gets no lazy stub — an unresolved one is a
    /// LinkError at instantiation. GOT.mem/GOT.func entries resolve against the
    /// GLOBAL symbol table — every module's exports pooled, the SIDE module's own
    /// included, of any kind. A PEER is another side module staged on the same page:
    /// mergeLibSymbols folds a globally loaded library's exports into wasmImports and
    /// updateGOT folds them into the GOT, so a peer's exports join all three provided
    /// sets; its own imports are the caller's separate question. Throws
    /// <see cref="NotSupportedException"/> on a malformed wasm binary or a glue file
    /// with no wasmImports object.</summary>
    public static int PrintUnsatisfied(string sidePath, string mainPath, string? gluePath,
        List<string>? peerPaths = null)
    {
        WasmModuleInfo side = Parse(sidePath);
        WasmModuleInfo main = Parse(mainPath);
        HashSet<string> glueFuncs = gluePath is null ? new HashSet<string>() : GlueWasmImports(gluePath);

        var peerExports = new HashSet<string>();
        if (peerPaths is not null)
            foreach (string peerPath in peerPaths)
                foreach (WasmExport e in Parse(peerPath).Exports)
                    peerExports.Add(e.Name);

        var providedFuncs = new HashSet<string>(glueFuncs);
        foreach (WasmExport e in main.Exports)
            if (e.IsFunc)
                providedFuncs.Add(e.Name);
        foreach (WasmImport im in main.Imports)
            if (im.Kind == 0 && im.Mod == "env")
                providedFuncs.Add(im.Field);
        providedFuncs.UnionWith(peerExports);

        // Tags ride the same wasmImports pool as functions, which holds MAIN's
        // exports of every kind (mergeLibSymbols copies them all).
        var providedTags = new HashSet<string>(glueFuncs);
        foreach (WasmExport e in main.Exports)
            providedTags.Add(e.Name);
        providedTags.UnionWith(peerExports);

        // A glue-supplied JS function is given a table slot on demand, so it too can
        // satisfy a GOT.func entry.
        var globalSymbols = new HashSet<string>(glueFuncs);
        foreach (WasmExport e in main.Exports)
            globalSymbols.Add(e.Name);
        foreach (WasmExport e in side.Exports)
            globalSymbols.Add(e.Name);
        globalSymbols.UnionWith(peerExports);

        int unsatisfied = 0;
        foreach (WasmImport im in side.Imports)
        {
            if (im.Kind == 0 && im.Mod == "env")
            {
                if (providedFuncs.Contains(im.Field) || IsBenignTlsInit(im.Field))
                    continue;
                Console.WriteLine(im.Field);
                unsatisfied++;
            }
            else if (im.Kind == 4 && im.Mod == "env")
            {
                if (providedTags.Contains(im.Field))
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
            // env global/table/memory imports (__memory_base, __table_base,
            // __stack_pointer, memory, __indirect_function_table) are supplied by
            // the JS loader itself, not by any module's export — checking them
            // against exports would fail every healthy page. Out of scope.
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

    // Structural reads are bounds-checked against the file and the enclosing
    // section: a truncated or lying binary must throw (exit 2, "the check could
    // not run"), never read as "no imports found" — that is the fail-open this
    // guards. The .js oracle keeps its looser walk; the differential gate only
    // ever feeds both tools well-formed linker output.
    private static WasmModuleInfo Parse(string path)
    {
        byte[] buf = File.ReadAllBytes(path);
        if (buf.Length < 8 || buf[0] != 0x00 || buf[1] != 0x61 || buf[2] != 0x73 || buf[3] != 0x6d)
            throw new NotSupportedException($"{path} is not a wasm binary (no \\0asm magic)");
        if (buf[4] != 0x01 || buf[5] != 0x00 || buf[6] != 0x00 || buf[7] != 0x00)
            throw new NotSupportedException($"{path} is not a version-1 wasm binary");
        var m = new WasmModuleInfo();
        int p = 8; // magic + version
        int importStart = -1, importEnd = -1, exportStart = -1, exportEnd = -1;
        while (p < buf.Length)
        {
            byte id = buf[p++];
            int size = (int)ReadU32(buf, ref p, buf.Length, path);
            if (size > buf.Length - p)
                throw new NotSupportedException($"{path}: section {id} at offset {p} declares {size} bytes but only {buf.Length - p} remain");
            if (id == 2 && importStart < 0)
            {
                importStart = p;
                importEnd = p + size;
            }
            else if (id == 7 && exportStart < 0)
            {
                exportStart = p;
                exportEnd = p + size;
            }
            p += size;
        }

        if (importStart >= 0)
        {
            p = importStart;
            uint n = ReadU32(buf, ref p, importEnd, path);
            for (uint i = 0; i < n; i++)
            {
                string mod = ReadName(buf, ref p, importEnd, path);
                string field = ReadName(buf, ref p, importEnd, path);
                byte kind = ReadByte(buf, ref p, importEnd, path);
                // Skip each kind's descriptor so the cursor lands on the next entry;
                // table and mem limits carry an optional max (flags bit 0).
                if (kind == 0)
                    ReadU32(buf, ref p, importEnd, path); // typeidx
                else if (kind == 1)
                {
                    ReadByte(buf, ref p, importEnd, path); // elemtype
                    byte flags = ReadByte(buf, ref p, importEnd, path);
                    ReadU32(buf, ref p, importEnd, path);
                    if ((flags & 1) != 0)
                        ReadU32(buf, ref p, importEnd, path);
                }
                else if (kind == 2)
                {
                    byte flags = ReadByte(buf, ref p, importEnd, path);
                    ReadU32(buf, ref p, importEnd, path);
                    if ((flags & 1) != 0)
                        ReadU32(buf, ref p, importEnd, path);
                }
                else if (kind == 3)
                {
                    ReadByte(buf, ref p, importEnd, path); // valtype
                    ReadByte(buf, ref p, importEnd, path); // mutability
                }
                else if (kind == 4)
                {
                    ReadByte(buf, ref p, importEnd, path); // attribute
                    ReadU32(buf, ref p, importEnd, path);
                }
                else
                    throw new NotSupportedException($"{path}: import {mod}.{field} has unknown kind {kind}");
                m.Imports.Add(new WasmImport { Mod = mod, Field = field, Kind = kind });
            }
        }

        if (exportStart >= 0)
        {
            p = exportStart;
            uint n = ReadU32(buf, ref p, exportEnd, path);
            for (uint i = 0; i < n; i++)
            {
                string name = ReadName(buf, ref p, exportEnd, path);
                byte kind = ReadByte(buf, ref p, exportEnd, path);
                ReadU32(buf, ref p, exportEnd, path); // index
                m.Exports.Add(new WasmExport { Name = name, IsFunc = kind == 0 });
            }
        }
        return m;
    }

    private static byte ReadByte(byte[] buf, ref int p, int end, string path)
    {
        if (p >= end)
            throw new NotSupportedException($"{path}: truncated at offset {p}");
        return buf[p++];
    }

    private static uint ReadU32(byte[] buf, ref int p, int end, string path)
    {
        uint r = 0;
        int s = 0;
        byte b;
        do
        {
            if (s > 28)
                throw new NotSupportedException($"{path}: malformed LEB128 at offset {p}");
            b = ReadByte(buf, ref p, end, path);
            r |= (uint)(b & 0x7f) << s;
            s += 7;
        } while ((b & 0x80) != 0);
        return r;
    }

    private static string ReadName(byte[] buf, ref int p, int end, string path)
    {
        int n = (int)ReadU32(buf, ref p, end, path);
        if (n > end - p)
            throw new NotSupportedException($"{path}: name of {n} bytes at offset {p} overruns its section");
        string v = Encoding.UTF8.GetString(buf, p, n);
        p += n;
        return v;
    }

    // ---- JS glue scanning ------------------------------------------------------
    //
    // The depth-1 keys of an Emscripten glue file's `wasmImports = {...}` object
    // literal: the JS-supplied symbols the loader's resolveGlobalSymbol serves to
    // side modules — under MAIN_MODULE=1 that includes --js-library functions the
    // main wasm's own import section never names. A depth-aware scan, not a regex
    // over the whole object: values may be inline functions holding their own object
    // literals, so only depth-1 keys count. The keys are unminified even in an -O3
    // glue — side modules resolve by name. No `wasmImports` object at all means the
    // file is not a main-module glue, and that must fail loudly rather than silently
    // weaken the provided set.
    //
    // Both locating the object and scanning its keys run over a lexed view of the
    // file (LexJs): comments, string/template-literal contents and regex bodies are
    // not code, so a `}` inside a value's template literal cannot close the object
    // early, and a commented-out `wasmImports = {...}` cannot be taken for the real
    // one — each was a measured failure of the raw character walk.

    // One lexed character. Kind: 'c' code, 'o'/'e' string open/close quote,
    // 's' string content (needed for quoted keys). Comments, template literals
    // and regex bodies yield nothing.
    private struct JsTok
    {
        public char Ch;
        public char Kind;
        public int Idx;
    }

    private static HashSet<string> GlueWasmImports(string path)
    {
        string js = File.ReadAllText(path);
        List<JsTok> toks = LexJs(js);
        int brace = FindWasmImportsBrace(toks);
        if (brace < 0)
            throw new NotSupportedException($"{path} has no \"wasmImports = {{...}}\" — not an emscripten main-module JS glue?");
        var names = new HashSet<string>();
        int depth = 0;
        bool inValue = false;
        var token = new StringBuilder();
        for (int k = brace; k < toks.Count; k++)
        {
            char kind = toks[k].Kind;
            char ch = toks[k].Ch;
            if (kind == 'o' || kind == 'e')
                continue;
            if (kind == 's')
            {
                if (!inValue && depth == 1)
                    token.Append(ch); // quoted key
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

    // `wasmImports` spelled by raw-contiguous code characters, preceded by a
    // non-word char, then \s* = \s* { — the index of the `{` token, or -1.
    private static int FindWasmImportsBrace(List<JsTok> toks)
    {
        const string target = "wasmImports";
        for (int k = 0; k + target.Length <= toks.Count; k++)
        {
            bool hit = true;
            for (int j = 0; j < target.Length; j++)
            {
                JsTok t = toks[k + j];
                if (t.Kind != 'c' || t.Ch != target[j] || t.Idx != toks[k].Idx + j)
                {
                    hit = false;
                    break;
                }
            }
            if (!hit)
                continue;
            if (k > 0 && toks[k - 1].Kind == 'c' && toks[k - 1].Idx == toks[k].Idx - 1 && IsWordChar(toks[k - 1].Ch))
                continue;
            int m = k + target.Length;
            while (m < toks.Count && toks[m].Kind == 'c' && char.IsWhiteSpace(toks[m].Ch))
                m++;
            if (m >= toks.Count || toks[m].Kind != 'c' || toks[m].Ch != '=')
                continue;
            m++;
            while (m < toks.Count && toks[m].Kind == 'c' && char.IsWhiteSpace(toks[m].Ch))
                m++;
            if (m < toks.Count && toks[m].Kind == 'c' && toks[m].Ch == '{')
                return m;
        }
        return -1;
    }

    // After ')', ']', a word char or a closed literal a slash is division;
    // elsewhere (start, operators, '{', '}', ';') it opens a regex. Safe for
    // glue-shaped input — `({}/x)` or `i++/n` would misread — not a full JS lexer.
    private static bool RegexCanFollow(char last) =>
        last == '\0' || "(,=:[!&|?{};+-*%~^<>}".IndexOf(last) >= 0;

    private static List<JsTok> LexJs(string js)
    {
        var toks = new List<JsTok>();
        int i = 0;
        char last = '\0'; // last significant code char (quotes of closed literals included)
        while (i < js.Length)
        {
            char ch = js[i];
            if (ch == '/' && i + 1 < js.Length && js[i + 1] == '/')
            {
                i += 2;
                while (i < js.Length && js[i] != '\n')
                    i++;
                continue;
            }
            if (ch == '/' && i + 1 < js.Length && js[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < js.Length && !(js[i] == '*' && js[i + 1] == '/'))
                    i++;
                i = Math.Min(i + 2, js.Length);
                continue;
            }
            if (ch == '"' || ch == '\'')
            {
                toks.Add(new JsTok { Ch = ch, Kind = 'o', Idx = i });
                i++;
                bool esc = false;
                while (i < js.Length)
                {
                    char c = js[i];
                    if (esc)
                        esc = false;
                    else if (c == '\\')
                        esc = true;
                    else if (c == ch)
                        break;
                    else
                        toks.Add(new JsTok { Ch = c, Kind = 's', Idx = i });
                    i++;
                }
                if (i < js.Length)
                {
                    toks.Add(new JsTok { Ch = ch, Kind = 'e', Idx = i });
                    i++;
                }
                last = ch;
                continue;
            }
            if (ch == '`')
            {
                i = SkipTemplate(js, i);
                last = '`';
                continue;
            }
            if (ch == '/' && RegexCanFollow(last))
            {
                i = SkipRegex(js, i);
                last = '/';
                continue;
            }
            toks.Add(new JsTok { Ch = ch, Kind = 'c', Idx = i });
            if (!char.IsWhiteSpace(ch))
                last = ch;
            i++;
        }
        return toks;
    }

    // From the opening backtick past the closing one; `${}` interpolations may nest
    // braces, strings, comments, regexes and further templates.
    private static int SkipTemplate(string js, int i)
    {
        i++;
        while (i < js.Length)
        {
            char c = js[i];
            if (c == '\\')
            {
                i += 2;
                continue;
            }
            if (c == '`')
            {
                return i + 1;
            }
            if (c == '$' && i + 1 < js.Length && js[i + 1] == '{')
            {
                i = SkipInterp(js, i + 2);
                continue;
            }
            i++;
        }
        return i;
    }

    // From just past `${` past the matching `}`.
    private static int SkipInterp(string js, int i)
    {
        int depth = 0;
        char last = '\0';
        while (i < js.Length)
        {
            char c = js[i];
            if (c == '/' && i + 1 < js.Length && js[i + 1] == '/')
            {
                i += 2;
                while (i < js.Length && js[i] != '\n')
                    i++;
                continue;
            }
            if (c == '/' && i + 1 < js.Length && js[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < js.Length && !(js[i] == '*' && js[i + 1] == '/'))
                    i++;
                i = Math.Min(i + 2, js.Length);
                continue;
            }
            if (c == '"' || c == '\'')
            {
                i++;
                bool esc = false;
                while (i < js.Length)
                {
                    if (esc)
                        esc = false;
                    else if (js[i] == '\\')
                        esc = true;
                    else if (js[i] == c)
                    {
                        i++;
                        break;
                    }
                    i++;
                }
                last = c;
                continue;
            }
            if (c == '`')
            {
                i = SkipTemplate(js, i);
                last = '`';
                continue;
            }
            if (c == '/' && RegexCanFollow(last))
            {
                i = SkipRegex(js, i);
                last = '/';
                continue;
            }
            if (c == '{')
                depth++;
            else if (c == '}')
            {
                if (depth == 0)
                    return i + 1;
                depth--;
            }
            if (!char.IsWhiteSpace(c))
                last = c;
            i++;
        }
        return i;
    }

    // From the opening slash past the closing one and its flags; a newline ends an
    // unterminated body (a real regex cannot contain one).
    private static int SkipRegex(string js, int i)
    {
        i++;
        bool inClass = false;
        while (i < js.Length)
        {
            char c = js[i];
            if (c == '\\')
            {
                i += 2;
                continue;
            }
            if (c == '[')
                inClass = true;
            else if (c == ']')
                inClass = false;
            else if (c == '/' && !inClass)
            {
                i++;
                break;
            }
            else if (c == '\n')
                break;
            i++;
        }
        while (i < js.Length && IsWordChar(js[i]))
            i++;
        return i;
    }

    private static bool IsWordChar(char c) =>
        c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_';

    private static bool IsIdentChar(char c) => IsWordChar(c) || c == '$';
}
