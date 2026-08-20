// Post-link splitter for a side module's __wasm_apply_data_relocs.
//
// A position-independent side module initializes every pointer in static data
// from one linker-synthesized function, so its body grows with the program's
// vtables, interface tables and metadata — and V8 refuses any function body
// over 7,654,321 bytes at WebAssembly.instantiate, before a byte of it runs
// (wasm-ld has no split of its own: llvm/llvm-project#58780). The body is a
// flat store sequence with an empty operand stack between stores, so any
// store boundary is a safe cut: this script rewrites the function into
// sub-limit chunk functions called in order from the original index, keeping
// every export, import, table and name intact.
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

// --- decode the body: locals declaration, then a flat instruction sequence ---
const r = new Reader(buf, entry.bodyStart);
const localsStart = r.off;
for (let n = r.u32(); n > 0; n--) { r.u32(); r.u8(); }
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
const cuts = []; // instruction-stream offsets where the operand stack is empty
let depth = 0;
while (true) {
    const at = r.off, op = r.u8();
    if (op === 0x0b) {
        if (r.off !== entry.bodyEnd || depth !== 0) {
            console.error(`${path}: ${TARGET_NAME}: malformed body end`);
            process.exit(2);
        }
        cuts.push(at);
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
    if (spec[2]) spec[2](r);
    if (depth === 0) cuts.push(r.off);
}

// --- slice into chunks of ~CHUNK_TARGET, cut only at empty-stack boundaries ---
const chunks = [];
let sliceStart = instrStart;
for (const cut of cuts) {
    if (cut - sliceStart >= CHUNK_TARGET || cut === cuts[cuts.length - 1]) {
        if (cut > sliceStart) chunks.push(buf.subarray(sliceStart, cut));
        sliceStart = cut;
    }
}
if (chunks.length < 2) process.exit(0);

const totalFuncs = numImportedFuncs + funcTypes.length;
const typeIdx = funcTypes[definedIdx];

const chunkBodies = chunks.map(slice => {
    const body = Buffer.concat([localsDecl, slice, Buffer.from([0x0b])]);
    return Buffer.concat([lebU32(body.length), body]);
});
const dispatcher = (() => {
    const parts = [Buffer.from([0x00])]; // no locals
    for (let i = 0; i < chunks.length; i++)
        parts.push(Buffer.from([0x10]), lebU32(totalFuncs + i));
    parts.push(Buffer.from([0x0b]));
    const body = Buffer.concat(parts);
    return Buffer.concat([lebU32(body.length), body]);
})();

// --- reassemble: rewrite the function and code sections, copy the rest ---
function rebuildFunctionSection() {
    const parts = [lebU32(funcTypes.length + chunks.length)];
    for (const t of funcTypes) parts.push(lebU32(t));
    for (let i = 0; i < chunks.length; i++) parts.push(lebU32(typeIdx));
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
    const payload = sec === funcSec ? rebuildFunctionSection()
        : sec === codeSec ? rebuildCodeSection()
        : buf.subarray(sec.payloadStart, sec.payloadEnd);
    out.push(Buffer.from([sec.id]), lebU32(payload.length), payload);
}
const tmp = path + '.split-tmp';
writeFileSync(tmp, Buffer.concat(out));
renameSync(tmp, path);
const maxChunk = Math.max(...chunkBodies.map(b => b.length));
console.log(`dn2cpp: split ${TARGET_NAME}: ${bodySize} bytes -> ${chunks.length} chunks (largest ${maxChunk})`);
