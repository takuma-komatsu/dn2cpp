// Post-link splitter for a side module's __wasm_apply_data_relocs.
//
// A position-independent side module initializes every pointer in static data
// from one linker-synthesized function, so its body grows with the program's
// vtables, interface tables and metadata — and V8 refuses any function body
// over 7,654,321 bytes at WebAssembly.instantiate, before a byte of it runs
// (wasm-ld has no split of its own: llvm/llvm-project#58780). The body is a
// straight-line store sequence. A cut is safe where the operand stack is empty;
// locals that are live there are returned to a dispatcher and passed into the
// next chunk. This script rewrites the function into sub-limit chunk functions
// called in order from the original index, keeping every export, import, table
// and name intact.
//
// Usage: node dn2cpp_split_apply_data_relocs.mjs <module.wasm>
// No-op (exit 0, no output) when the function is absent or already under the
// limit. Any instruction outside the reloc-sequence subset is a hard error:
// shipping the module unsplit would fail only at instantiate in the browser.

import { readFileSync, writeFileSync, renameSync } from 'node:fs';

const V8_MAX_FUNCTION_BODY = 7_654_321;
const CHUNK_TARGET = 4 * 1024 * 1024;
const TARGET_NAME = '__wasm_apply_data_relocs';

const path = process.argv[2];
if (!path) { console.error('usage: dn2cpp_split_apply_data_relocs.mjs <module.wasm>'); process.exit(2); }
const buf = readFileSync(path);
if (buf.length < 8 || buf.readUInt32LE(0) !== 0x6d736100) {
    console.error(`${path}: not a wasm module`);
    process.exit(2);
}

class Reader {
    constructor(buf, off = 0) { this.buf = buf; this.off = off; }
    u8() { return this.buf[this.off++]; }
    u32() { // LEB128
        let r = 0, s = 0, b;
        do { b = this.buf[this.off++]; r += (b & 0x7f) * 2 ** s; s += 7; } while (b & 0x80);
        return r;
    }
    skipS64() { while (this.buf[this.off++] & 0x80); }
    bytes(n) { const v = this.buf.subarray(this.off, this.off + n); this.off += n; return v; }
}

function lebU32(v) {
    const out = [];
    do { let b = v & 0x7f; v = Math.floor(v / 128); if (v) b |= 0x80; out.push(b); } while (v);
    return Buffer.from(out);
}

function valType(r) {
    const start = r.off, kind = r.u8();
    if (kind === 0x63 || kind === 0x64) r.skipS64(); // ref null/ref heaptype
    return buf.subarray(start, r.off);
}

function localOp(op, index) {
    return Buffer.concat([Buffer.from([op]), lebU32(index)]);
}

// --- section scan ---
const sections = []; // {id, hdrStart, payloadStart, payloadEnd}
{
    const r = new Reader(buf, 8);
    while (r.off < buf.length) {
        const hdrStart = r.off, id = r.u8(), size = r.u32();
        sections.push({ id, hdrStart, payloadStart: r.off, payloadEnd: r.off + size });
        r.off += size;
    }
}
const section = id => sections.find(s => s.id === id);

// --- find the target function's index via the export section ---
let targetFunc = -1;
{
    const sec = section(7);
    if (!sec) process.exit(0);
    const r = new Reader(buf, sec.payloadStart);
    for (let n = r.u32(); n > 0; n--) {
        const name = r.bytes(r.u32()).toString('utf8');
        const kind = r.u8(), idx = r.u32();
        if (kind === 0 && name === TARGET_NAME) targetFunc = idx;
    }
}
if (targetFunc < 0) process.exit(0);

// --- imported-function count (code/function sections index defined funcs only) ---
let numImportedFuncs = 0;
{
    const sec = section(2);
    if (sec) {
        const r = new Reader(buf, sec.payloadStart);
        for (let n = r.u32(); n > 0; n--) {
            r.bytes(r.u32()); r.bytes(r.u32());
            const kind = r.u8();
            if (kind === 0) { r.u32(); numImportedFuncs++; }
            else if (kind === 1) { r.u8(); const f = r.u8(); r.u32(); if (f & 1) r.u32(); }
            else if (kind === 2) { const f = r.u8(); r.u32(); if (f & 1) r.u32(); }
            else if (kind === 3) { r.u8(); r.u8(); }
            else if (kind === 4) { r.u8(); r.u32(); } // exception tag
            else { console.error(`${path}: unknown import kind ${kind}`); process.exit(2); }
        }
    }
}
const definedIdx = targetFunc - numImportedFuncs;
if (definedIdx < 0) { console.error(`${path}: ${TARGET_NAME} is an import`); process.exit(2); }

// --- function section: per-defined-function type indices ---
const funcSec = section(3), codeSec = section(10);
if (!funcSec || !codeSec) { console.error(`${path}: missing function/code section`); process.exit(2); }
const funcTypes = [];
{
    const r = new Reader(buf, funcSec.payloadStart);
    for (let n = r.u32(); n > 0; n--) funcTypes.push(r.u32());
}
const typeIdx = funcTypes[definedIdx];

// --- code section: per-entry {start,end} covering size prefix + body ---
const entries = [];
{
    const r = new Reader(buf, codeSec.payloadStart);
    const n = r.u32();
    for (let i = 0; i < n; i++) {
        const start = r.off, size = r.u32(), bodyStart = r.off;
        entries.push({ start, bodyStart, bodyEnd: bodyStart + size });
        r.off = bodyStart + size;
    }
    if (entries.length !== funcTypes.length) {
        console.error(`${path}: code/function section count mismatch`);
        process.exit(2);
    }
}
const entry = entries[definedIdx];
const bodySize = entry.bodyEnd - entry.bodyStart;
if (bodySize <= V8_MAX_FUNCTION_BODY) process.exit(0);

// The replacement dispatcher preserves the exported () -> () signature. The
// private chunk signatures carry original locals as parameters and only the
// values live at their ending boundary as results.
const typeSec = section(1);
if (!typeSec) { console.error(`${path}: missing type section`); process.exit(2); }
let typeCount, typeEntriesStart, targetParamCount = -1, targetResultCount = -1;
{
    const r = new Reader(buf, typeSec.payloadStart);
    typeCount = r.u32();
    typeEntriesStart = r.off;
    for (let i = 0; i < typeCount; i++) {
        const form = r.u8();
        if (form !== 0x60) {
            console.error(`${path}: ${TARGET_NAME}: unsupported type form 0x${form.toString(16)}`);
            process.exit(2);
        }
        const paramCount = r.u32();
        for (let j = 0; j < paramCount; j++) valType(r);
        const resultCount = r.u32();
        for (let j = 0; j < resultCount; j++) valType(r);
        if (i === typeIdx) {
            targetParamCount = paramCount;
            targetResultCount = resultCount;
        }
    }
    if (r.off !== typeSec.payloadEnd || targetParamCount < 0) {
        console.error(`${path}: malformed type section`);
        process.exit(2);
    }
}
if (targetParamCount !== 0 || targetResultCount !== 0) {
    console.error(`${path}: ${TARGET_NAME}: expected a () -> () function`);
    process.exit(2);
}

// --- decode the body: locals declaration, then a flat instruction sequence ---
const r = new Reader(buf, entry.bodyStart);
const localsStart = r.off;
const localTypes = [];
for (let n = r.u32(); n > 0; n--) {
    const count = r.u32(), type = valType(r);
    for (let i = 0; i < count; i++) localTypes.push(type);
}
const localsDecl = buf.subarray(localsStart, r.off);
const instrStart = r.off;

// opcode -> [pops, pushes, immediates]; the subset a reloc sequence can contain
const memarg = r => {
    const align = r.u32(); // bit 6 = multi-memory index follows; reject rather than misparse
    if (align & 0x40) { console.error(`${path}: ${TARGET_NAME}: multi-memory memarg`); process.exit(2); }
    r.u32();
};
const OPS = {
    0x20: [0, 1, r => r.u32()], 0x21: [1, 0, r => r.u32()], 0x22: [1, 1, r => r.u32()],
    0x23: [0, 1, r => r.u32()], 0x24: [1, 0, r => r.u32()],
    0x28: [1, 1, memarg], 0x29: [1, 1, memarg],
    0x36: [2, 0, memarg], 0x37: [2, 0, memarg],
    0x41: [0, 1, r => r.u32()], 0x42: [0, 1, r => r.skipS64()],
    0x6a: [2, 1, null], 0x6b: [2, 1, null], 0x7c: [2, 1, null], 0x7d: [2, 1, null],
};
const instrs = []; // straight-line instructions, for stack depth + local liveness
let depth = 0;
let finalCut = -1;
while (true) {
    const at = r.off, op = r.u8();
    if (op === 0x0b) {
        if (r.off !== entry.bodyEnd || depth !== 0) {
            console.error(`${path}: ${TARGET_NAME}: malformed body end`);
            process.exit(2);
        }
        finalCut = at;
        break;
    }
    const spec = OPS[op];
    if (!spec) {
        console.error(`${path}: ${TARGET_NAME}: unsupported opcode 0x${op.toString(16)} at +0x${at.toString(16)} — extend dn2cpp_split_apply_data_relocs.mjs`);
        process.exit(2);
    }
    depth -= spec[0];
    if (depth < 0) { console.error(`${path}: ${TARGET_NAME}: stack underflow at +0x${at.toString(16)}`); process.exit(2); }
    depth += spec[1];
    let localRead = -1, localWrite = -1;
    if (op === 0x20)
        localRead = r.u32();
    else if (op === 0x21 || op === 0x22)
        localWrite = r.u32();
    else if (spec[2])
        spec[2](r);
    if ((localRead >= localTypes.length && localRead >= 0)
        || (localWrite >= localTypes.length && localWrite >= 0)) {
        console.error(`${path}: ${TARGET_NAME}: local index out of range at +0x${at.toString(16)}`);
        process.exit(2);
    }
    instrs.push({ end: r.off, depth, localRead, localWrite });
}

// Backwards liveness identifies the state each empty-stack boundary must hand to
// the next chunk. local.get makes its local live; local.set/local.tee kills the
// prior value. Control flow remains outside OPS and therefore fails above rather
// than making this straight-line analysis optimistic.
const liveAtCut = new Map([[finalCut, []]]);
const live = new Set();
for (let i = instrs.length - 1; i >= 0; i--) {
    const ins = instrs[i];
    if (ins.depth === 0)
        liveAtCut.set(ins.end, [...live].sort((a, b) => a - b));
    if (ins.localWrite >= 0)
        live.delete(ins.localWrite);
    if (ins.localRead >= 0)
        live.add(ins.localRead);
}
const cuts = [...liveAtCut.keys()].sort((a, b) => a - b);

function chunkBodySize(start, cut) {
    let size = 1 + (cut - start) + 1; // zero local groups + instructions + end
    for (const index of liveAtCut.get(cut)) size += 1 + lebU32(index).length;
    return size;
}

// --- slice into chunks of ~CHUNK_TARGET, without crossing V8's hard limit ---
const chunks = [];
let sliceStart = instrStart;
let lastWithinLimit = -1;
for (let i = 0; i < cuts.length; i++) {
    const cut = cuts[i];
    const bodySizeAtCut = chunkBodySize(sliceStart, cut);
    if (bodySizeAtCut > V8_MAX_FUNCTION_BODY) {
        if (lastWithinLimit <= sliceStart) {
            console.error(`${path}: ${TARGET_NAME}: no empty-stack cut within V8's ${V8_MAX_FUNCTION_BODY}-byte function-body limit`);
            process.exit(2);
        }
        chunks.push({
            slice: buf.subarray(sliceStart, lastWithinLimit),
            liveOut: liveAtCut.get(lastWithinLimit),
        });
        sliceStart = lastWithinLimit;
        lastWithinLimit = -1;
        i--; // reconsider this cut relative to the new chunk start
        continue;
    }
    lastWithinLimit = cut;
    if (bodySizeAtCut >= CHUNK_TARGET || cut === finalCut) {
        if (cut > sliceStart)
            chunks.push({ slice: buf.subarray(sliceStart, cut), liveOut: liveAtCut.get(cut) });
        sliceStart = cut;
        lastWithinLimit = -1;
    }
}
if (sliceStart !== finalCut) {
    console.error(`${path}: ${TARGET_NAME}: failed to cover the relocation body with safe chunks`);
    process.exit(2);
}

const totalFuncs = numImportedFuncs + funcTypes.length;
const addedTypes = [];
const chunkTypes = new Map();
function chunkType(liveOut) {
    if (localTypes.length === 0) return typeIdx;
    const key = liveOut.join(',');
    if (chunkTypes.has(key)) return chunkTypes.get(key);
    const params = Buffer.concat(localTypes);
    const results = Buffer.concat(liveOut.map(index => localTypes[index]));
    const body = Buffer.concat([
        Buffer.from([0x60]), lebU32(localTypes.length), params,
        lebU32(liveOut.length), results,
    ]);
    const index = typeCount + addedTypes.length;
    addedTypes.push(body);
    chunkTypes.set(key, index);
    return index;
}
for (const chunk of chunks) chunk.typeIdx = chunkType(chunk.liveOut);

const chunkBodies = chunks.map(chunk => {
    const returns = chunk.liveOut.map(index => localOp(0x20, index));
    const body = Buffer.concat([
        Buffer.from([0x00]), chunk.slice, ...returns, Buffer.from([0x0b]),
    ]);
    if (body.length > V8_MAX_FUNCTION_BODY) {
        console.error(`${path}: ${TARGET_NAME}: generated chunk exceeds V8's ${V8_MAX_FUNCTION_BODY}-byte function-body limit`);
        process.exit(2);
    }
    return Buffer.concat([lebU32(body.length), body]);
});
const dispatcher = (() => {
    const parts = [localsDecl];
    const passLocals = Buffer.concat(localTypes.map((_, index) => localOp(0x20, index)));
    for (let i = 0; i < chunks.length; i++) {
        parts.push(passLocals);
        parts.push(Buffer.from([0x10]), lebU32(totalFuncs + i));
        for (let j = chunks[i].liveOut.length - 1; j >= 0; j--)
            parts.push(localOp(0x21, chunks[i].liveOut[j]));
    }
    parts.push(Buffer.from([0x0b]));
    const body = Buffer.concat(parts);
    if (body.length > V8_MAX_FUNCTION_BODY) {
        console.error(`${path}: ${TARGET_NAME}: generated dispatcher exceeds V8's ${V8_MAX_FUNCTION_BODY}-byte function-body limit`);
        process.exit(2);
    }
    return Buffer.concat([lebU32(body.length), body]);
})();

// --- reassemble: rewrite the function and code sections, copy the rest ---
function rebuildTypeSection() {
    return Buffer.concat([
        lebU32(typeCount + addedTypes.length),
        buf.subarray(typeEntriesStart, typeSec.payloadEnd),
        ...addedTypes,
    ]);
}
function rebuildFunctionSection() {
    const parts = [lebU32(funcTypes.length + chunks.length)];
    for (const t of funcTypes) parts.push(lebU32(t));
    for (const chunk of chunks) parts.push(lebU32(chunk.typeIdx));
    return Buffer.concat(parts);
}
function rebuildCodeSection() {
    const parts = [lebU32(entries.length + chunks.length)];
    for (let i = 0; i < entries.length; i++)
        parts.push(i === definedIdx ? dispatcher : buf.subarray(entries[i].start, entries[i].bodyEnd));
    parts.push(...chunkBodies);
    return Buffer.concat(parts);
}
const out = [buf.subarray(0, 8)];
for (const sec of sections) {
    const payload = sec === typeSec ? rebuildTypeSection()
        : sec === funcSec ? rebuildFunctionSection()
        : sec === codeSec ? rebuildCodeSection()
        : buf.subarray(sec.payloadStart, sec.payloadEnd);
    out.push(Buffer.from([sec.id]), lebU32(payload.length), payload);
}
const tmp = path + '.split-tmp';
writeFileSync(tmp, Buffer.concat(out));
renameSync(tmp, path);
const maxChunk = Math.max(...chunkBodies.map(b => b.length));
console.log(`dn2cpp: split ${TARGET_NAME}: ${bodySize} bytes -> ${chunks.length} chunks (largest ${maxChunk})`);
