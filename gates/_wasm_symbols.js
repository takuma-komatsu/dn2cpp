// _wasm_symbols.js — read a wasm binary's import/export surface without running it.
//
// The gates need to ask three questions about an Emscripten SIDE MODULE, and none
// of the usual tools answers them here: the host `nm` reads Mach-O/PE, a plain
// emscripten install ships no `wasm-objdump`, and `WebAssembly.Module` — which
// would be the ideal oracle, since it is literally what the browser does — REFUSES
// to compile a module holding an oversized function, which is exactly one of the
// conditions a gate must be able to report on. So parse the sections directly.
// Node is already a prerequisite of every wasm gate, so this adds no dependency.
//
//   exports FILE            — one "kind name" per line
//   imports FILE            — one "kind module.name" per line
//   unsatisfied SIDE MAIN [MAINJS]
//                           — the SIDE module's imports that nothing can satisfy:
//                             env function and env tag imports the MAIN module
//                             cannot provide, plus GOT.mem/GOT.func entries no
//                             module exports, one per line (see below). MAINJS is the main module's JS
//                             glue; when given, the JS-library functions it defines
//                             count as provided (see below).
//   maxfunc FILE            — "SIZE NAME" of the largest function body in the code
//                             section (NAME needs --profiling-funcs at link, else "?")
//
// `unsatisfied` is the interesting one. Emscripten resolves a side module's imports
// against the main module's combined symbol table, so "what the main module can
// provide" is its own wasm exports PLUS the JS-library functions its glue supplies.
// The main module's own `env` function imports are the visible-in-wasm subset of the
// latter — but not all of it: a JS-library function nothing in the MAIN module's own
// code calls is still emitted into the glue's `wasmImports` object under
// MAIN_MODULE=1 (INCLUDE_FULL_LIBRARY), where the loader's resolveGlobalSymbol finds
// it for side modules, without ever appearing in the main wasm's import section.
// That is exactly where a vendor's --js-library functions live, so a check that only
// reads the main WASM would call them missing. Passing MAINJS (the main module's JS
// glue file) closes that gap: the keys of its `wasmImports = {...}` object literal
// join the provided set, for functions and GOT entries alike. Anything
// a side module imports that is in neither is a symbol NOBODY defines — and
// emscripten will NOT tell you: dlopen hands back a valid handle, dlsym resolves,
// and the lazy stub throws `TypeError: resolved is not a function` at the instant
// the function is first CALLED, naming a wasm function index and not the symbol.
// This is the check that converts that into a named failure at build time.
//
// The same check covers GOT.mem / GOT.func imports — a side module's reference to the
// ADDRESS of a data symbol or the table index of a function. Emscripten's loader fills
// each GOT entry from the GLOBAL symbol table (every module's exports pooled), so an
// entry is satisfied by the main module's exports OR by the side module's own exports.
// One that nothing exports is an address dlopen leaves at 0: like the function case it
// does not fail the load, and the first read or call through that null names nothing. A
// data-address reference such as `__data_end` compiled into the side module is exactly
// this shape, and this is what turns it into a named failure at build time.

const fs = require('fs');

function parse(file) {
    const buf = fs.readFileSync(file);
    let p = 8; // magic + version
    const secs = [];
    const u32 = () => { let r = 0, s = 0, b; do { b = buf[p++]; r |= (b & 0x7f) << s; s += 7; } while (b & 0x80); return r >>> 0; };
    const str = () => { const n = u32(); const v = buf.slice(p, p + n).toString(); p += n; return v; };
    while (p < buf.length) {
        const id = buf[p++], size = u32(), start = p;
        let name = String(id);
        if (id === 0) { const q = p; name = 'custom:' + str(); p = q; }
        secs.push({ id, name, size, start });
        p = start + size;
    }

    const KIND = ['func', 'table', 'mem', 'global', 'tag'];
    const imports = [], exports = [];

    const im = secs.find(s => s.id === 2);
    if (im) {
        p = im.start;
        const n = u32();
        for (let i = 0; i < n; i++) {
            const mod = str(), field = str(), k = buf[p++];
            // Skip each kind's descriptor so the cursor lands on the next entry.
            if (k === 0) u32();                                                   // typeidx
            else if (k === 1) { p++; const f = buf[p++]; u32(); if (f & 1) u32(); } // table
            else if (k === 2) { const f = buf[p++]; u32(); if (f & 1) u32(); }      // mem
            else if (k === 3) { p++; p++; }                                         // global
            else if (k === 4) { p++; u32(); }                                       // tag
            imports.push({ mod, field, kind: KIND[k] ?? String(k) });
        }
    }

    const ex = secs.find(s => s.id === 7);
    if (ex) {
        p = ex.start;
        const n = u32();
        for (let i = 0; i < n; i++) { const name = str(), k = buf[p++]; u32(); exports.push({ name, kind: KIND[k] ?? String(k) }); }
    }

    // Function bodies, and the name section that names them (if it survived the link).
    const funcs = [];
    const code = secs.find(s => s.id === 10);
    if (code) { p = code.start; const n = u32(); for (let i = 0; i < n; i++) { const sz = u32(); funcs.push(sz); p += sz; } }
    const names = {};
    const ns = secs.find(s => s.name === 'custom:name');
    if (ns) {
        p = ns.start; str();
        const end = ns.start + ns.size;
        while (p < end) {
            const sid = buf[p++], ssz = u32(), sstart = p;
            if (sid === 1) { const c = u32(); for (let k = 0; k < c; k++) { const idx = u32(); names[idx] = str(); } }
            p = sstart + ssz;
        }
    }
    // The name section indexes the FULL function space (imports first); `funcs`
    // indexes only the defined ones. Imported functions are the offset between them.
    const importedFuncs = imports.filter(i => i.kind === 'func').length;
    return { imports, exports, funcs, names, importedFuncs };
}

// glueWasmImports — the keys of an Emscripten glue file's `wasmImports = {...}`
// object literal: the JS-supplied symbols the loader's resolveGlobalSymbol serves
// to side modules. A depth-aware scan, not a regex over the whole object: values
// may be inline functions holding their own object literals, so only depth-1 keys
// count. The keys are unminified even in an -O3 glue — a dynamic-linking main
// module must keep them, side modules resolve by name. No `wasmImports` object at
// all means the file is not a main-module glue, and that must fail loudly rather
// than silently weaken the provided set.
//
// Both locating the object and scanning its keys run over a lexed view of the
// file (lexJs): comments, string/template-literal contents and regex bodies are
// not code, so a `}` inside a value's template literal cannot close the object
// early, and a commented-out `wasmImports = {...}` cannot be taken for the real
// one — each was a measured failure of the raw character walk.

const isWordChar = c => /[A-Za-z0-9_]/.test(c);

// After ')', ']', a word char or a closed literal a slash is division;
// elsewhere (start, operators, '{', '}', ';') it opens a regex. Safe for
// glue-shaped input — `({}/x)` or `i++/n` would misread — not a full JS lexer.
const regexCanFollow = last => last === '' || '(,=:[!&|?{};+-*%~^<>}'.includes(last);

// From the opening slash past the closing one and its flags; a newline ends an
// unterminated body (a real regex cannot contain one).
function skipRegex(js, i) {
    i++;
    let inClass = false;
    while (i < js.length) {
        const c = js[i];
        if (c === '\\') { i += 2; continue; }
        if (c === '[') { inClass = true; }
        else if (c === ']') { inClass = false; }
        else if (c === '/' && !inClass) { i++; break; }
        else if (c === '\n') { break; }
        i++;
    }
    while (i < js.length && isWordChar(js[i])) i++;
    return i;
}

// From the opening backtick past the closing one; `${}` interpolations may nest
// braces, strings, comments, regexes and further templates.
function skipTemplate(js, i) {
    i++;
    while (i < js.length) {
        const c = js[i];
        if (c === '\\') { i += 2; continue; }
        if (c === '`') { return i + 1; }
        if (c === '$' && i + 1 < js.length && js[i + 1] === '{') { i = skipInterp(js, i + 2); continue; }
        i++;
    }
    return i;
}

// From just past `${` past the matching `}`.
function skipInterp(js, i) {
    let depth = 0, last = '';
    while (i < js.length) {
        const c = js[i];
        if (c === '/' && i + 1 < js.length && js[i + 1] === '/') {
            i += 2;
            while (i < js.length && js[i] !== '\n') i++;
            continue;
        }
        if (c === '/' && i + 1 < js.length && js[i + 1] === '*') {
            i += 2;
            while (i + 1 < js.length && !(js[i] === '*' && js[i + 1] === '/')) i++;
            i = Math.min(i + 2, js.length);
            continue;
        }
        if (c === '"' || c === "'") {
            i++;
            let esc = false;
            while (i < js.length) {
                if (esc) { esc = false; } else if (js[i] === '\\') { esc = true; }
                else if (js[i] === c) { i++; break; }
                i++;
            }
            last = c;
            continue;
        }
        if (c === '`') { i = skipTemplate(js, i); last = '`'; continue; }
        if (c === '/' && regexCanFollow(last)) { i = skipRegex(js, i); last = '/'; continue; }
        if (c === '{') { depth++; }
        else if (c === '}') { if (depth === 0) { return i + 1; } depth--; }
        if (!/\s/.test(c)) { last = c; }
        i++;
    }
    return i;
}

// One entry per lexed character. kind: 'c' code, 'o'/'e' string open/close quote,
// 's' string content (needed for quoted keys). Comments, template literals and
// regex bodies yield nothing.
function lexJs(js) {
    const toks = [];
    let i = 0, last = '';
    while (i < js.length) {
        const ch = js[i];
        if (ch === '/' && i + 1 < js.length && js[i + 1] === '/') {
            i += 2;
            while (i < js.length && js[i] !== '\n') i++;
            continue;
        }
        if (ch === '/' && i + 1 < js.length && js[i + 1] === '*') {
            i += 2;
            while (i + 1 < js.length && !(js[i] === '*' && js[i + 1] === '/')) i++;
            i = Math.min(i + 2, js.length);
            continue;
        }
        if (ch === '"' || ch === "'") {
            toks.push({ ch, kind: 'o', idx: i });
            i++;
            let esc = false;
            while (i < js.length) {
                const c = js[i];
                if (esc) { esc = false; } else if (c === '\\') { esc = true; }
                else if (c === ch) { break; }
                else { toks.push({ ch: c, kind: 's', idx: i }); }
                i++;
            }
            if (i < js.length) { toks.push({ ch, kind: 'e', idx: i }); i++; }
            last = ch;
            continue;
        }
        if (ch === '`') { i = skipTemplate(js, i); last = '`'; continue; }
        if (ch === '/' && regexCanFollow(last)) { i = skipRegex(js, i); last = '/'; continue; }
        toks.push({ ch, kind: 'c', idx: i });
        if (!/\s/.test(ch)) { last = ch; }
        i++;
    }
    return toks;
}

// `wasmImports` spelled by raw-contiguous code characters, preceded by a
// non-word char, then \s* = \s* { — the token index of the `{`, or -1.
function findWasmImportsBrace(toks) {
    const target = 'wasmImports';
    for (let k = 0; k + target.length <= toks.length; k++) {
        let hit = true;
        for (let j = 0; j < target.length; j++) {
            const t = toks[k + j];
            if (t.kind !== 'c' || t.ch !== target[j] || t.idx !== toks[k].idx + j) { hit = false; break; }
        }
        if (!hit) continue;
        if (k > 0 && toks[k - 1].kind === 'c' && toks[k - 1].idx === toks[k].idx - 1 && isWordChar(toks[k - 1].ch)) continue;
        let m = k + target.length;
        while (m < toks.length && toks[m].kind === 'c' && /\s/.test(toks[m].ch)) m++;
        if (m >= toks.length || toks[m].kind !== 'c' || toks[m].ch !== '=') continue;
        m++;
        while (m < toks.length && toks[m].kind === 'c' && /\s/.test(toks[m].ch)) m++;
        if (m < toks.length && toks[m].kind === 'c' && toks[m].ch === '{') return m;
    }
    return -1;
}

function glueWasmImports(file) {
    const js = fs.readFileSync(file, 'utf8');
    const toks = lexJs(js);
    const brace = findWasmImportsBrace(toks);
    if (brace < 0) {
        console.error(`error: ${file} has no "wasmImports = {...}" — not an emscripten main-module JS glue?`);
        process.exit(2);
    }
    const names = new Set();
    let depth = 0, inValue = false, token = '';
    for (let k = brace; k < toks.length; k++) {
        const { ch, kind } = toks[k];
        if (kind === 'o' || kind === 'e') { continue; }
        if (kind === 's') {
            if (!inValue && depth === 1) { token += ch; } // quoted key
            continue;
        }
        if (ch === '{') { depth++; continue; }
        if (ch === '}') { if (--depth === 0) { break; } continue; }
        if (depth !== 1) { continue; }
        if (ch === ',') { inValue = false; token = ''; }
        else if (ch === ':' && !inValue) { if (token) { names.add(token); } inValue = true; token = ''; }
        else if (!inValue && /[A-Za-z0-9_$]/.test(ch)) { token += ch; }
        else if (!inValue && !/\s/.test(ch)) { token = ''; }
    }
    return names;
}

const [, , cmd, a, b, c] = process.argv;
if (cmd === 'exports') {
    for (const e of parse(a).exports) console.log(`${e.kind} ${e.name}`);
} else if (cmd === 'imports') {
    for (const i of parse(a).imports) console.log(`${i.kind} ${i.mod}.${i.field}`);
} else if (cmd === 'unsatisfied') {
    const side = parse(a), main = parse(b);
    // Function imports resolve against the main module's own wasm exports plus the
    // JS-library functions its glue supplies. Without the glue file, the main
    // module's env func imports stand in for the latter — the subset its own code
    // calls; with it, the glue's wasmImports keys are the whole set (see header).
    const glueFuncs = c ? glueWasmImports(c) : new Set();
    const providedFuncs = new Set([
        ...main.exports.filter(e => e.kind === 'func').map(e => e.name),
        ...main.imports.filter(i => i.mod === 'env' && i.kind === 'func').map(i => i.field),
        ...glueFuncs,
    ]);
    // Tags (__cpp_exception / __c_longjmp) ride the same wasmImports pool as
    // functions, which holds MAIN's exports of every kind (mergeLibSymbols copies
    // them all) — and unlike a function a tag gets no lazy stub, so an unresolved
    // one is a LinkError at instantiation. env global/table/memory imports
    // (__memory_base, __stack_pointer, memory, ...) are supplied by the JS loader
    // itself, not by any export — checking them would fail every healthy page.
    const providedTags = new Set([...main.exports.map(e => e.name), ...glueFuncs]);
    // GOT entries resolve against the GLOBAL symbol table — every module's exports,
    // the side module's own included — so an entry is satisfied by either module's
    // exports, of any kind (a GOT.mem symbol is data, a GOT.func symbol is code; a
    // glue-supplied JS function is given a table slot on demand, so it too can
    // satisfy a GOT.func entry).
    const globalSymbols = new Set([
        ...main.exports.map(e => e.name),
        ...side.exports.map(e => e.name),
        ...glueFuncs,
    ]);
    // A weak, never-emitted TLS-init routine for a CONSTANT-initialized `thread_local`
    // ([ThreadStatic] in the BCL). Clang emits no _ZTH when no dynamic initialization is
    // needed, so a TU that only sees `extern thread_local` weakly references one; a
    // static link resolves that to null and the TLS wrapper's guard skips the call. A
    // side module turns the same weak-undefined into an env function import (a TU that
    // calls it) or a GOT.func entry (a TU that takes its address), and emscripten
    // resolves both to null again — verified: reading such a field through the dlink
    // boundary returns the right value and does not trap. Benign, and not something any
    // main module could ever define, since these name the transpiled program's own statics.
    const benignTlsInit = f => /^_ZTH\d+sf_/.test(f);
    for (const i of side.imports) {
        if (i.kind === 'func' && i.mod === 'env') {
            if (providedFuncs.has(i.field)) continue;
            if (benignTlsInit(i.field)) continue;
            console.log(i.field);
        } else if (i.kind === 'tag' && i.mod === 'env') {
            if (providedTags.has(i.field)) continue;
            console.log(i.field);
        } else if (i.mod === 'GOT.mem' || i.mod === 'GOT.func') {
            if (globalSymbols.has(i.field)) continue;
            if (benignTlsInit(i.field)) continue;
            console.log(`${i.mod}.${i.field}`);
        }
    }
} else if (cmd === 'maxfunc') {
    const m = parse(a);
    let best = -1, at = -1;
    m.funcs.forEach((sz, i) => { if (sz > best) { best = sz; at = i; } });
    console.log(`${best} ${m.names[at + m.importedFuncs] ?? '?'}`);
} else {
    console.error('usage: _wasm_symbols.js exports|imports|maxfunc FILE | unsatisfied SIDE MAIN [MAINJS]');
    process.exit(2);
}
