// Proves the __wasm_apply_data_relocs post-link splitter end to end, on a
// synthesized module — the real side modules in this suite are under V8's
// per-function cap, so without this the split path itself would ship dark.
//
// Usage: node _wasm_split_selftest.js <splitter.mjs> <workdir>
//
// Synthesizes a module whose exported __wasm_apply_data_relocs is one flat
// (__memory_base + off) <- value store sequence past the cap, then asserts:
// V8 refuses it; the splitter rewrites it; V8 compiles the result; running it
// against a nonzero __memory_base leaves exactly the right bytes in memory;
// a second splitter run is a byte-identical no-op.
'use strict';
const fs = require('fs');
const path = require('path');
const { spawnSync } = require('child_process');

const [splitter, workdir] = process.argv.slice(2);
if (!splitter || !workdir) { console.error('usage: _wasm_split_selftest.js <splitter.mjs> <workdir>'); process.exit(2); }

function leb(v) { const o = []; do { let b = v & 0x7f; v = Math.floor(v / 128); if (v) b |= 0x80; o.push(b); } while (v); return o; }
function sleb(v) { const o = []; for (;;) { const b = v & 0x7f; v >>= 7; if ((v === 0 && !(b & 0x40)) || (v === -1 && (b & 0x40))) { o.push(b); break; } o.push(b | 0x80); } return o; }
function section(id, payload) { return [id, ...leb(payload.length), ...payload]; }
function vec(items) { return [...leb(items.length), ...items.flat()]; }
function str(s) { const b = [...Buffer.from(s, 'utf8')]; return [...leb(b.length), ...b]; }

const N = 640_000; // ~10 MB of body — past kV8MaxWasmFunctionSize
const value = i => (Math.imul(i, 2654435761) >>> 8) & 0x7fffffff;

const body = [0x00]; // no locals
for (let i = 0; i < N; i++)
    body.push(0x23, 0x00,                 // global.get $__memory_base
        ...[0x41, ...sleb(4 * i)], 0x6a,  // i32.const 4i; i32.add
        ...[0x41, ...sleb(value(i))],     // i32.const value
        0x36, 0x02, 0x00);                // i32.store align=4 offset=0
body.push(0x0b);

const FN = '__wasm_apply_data_relocs';
const module_ = Buffer.from([
    0x00, 0x61, 0x73, 0x6d, 0x01, 0x00, 0x00, 0x00,
    ...section(1, vec([[0x60, 0x00, 0x00]])),                       // type: () -> ()
    ...section(2, vec([[...str('env'), ...str('__memory_base'), 0x03, 0x7f, 0x00]])),
    ...section(3, vec([[0x00]])),                                   // func 0: type 0
    ...section(5, vec([[0x00, ...leb(Math.ceil(4 * N / 65536) + 1)]])),
    ...section(7, vec([[...str(FN), 0x00, 0x00], [...str('memory'), 0x02, 0x00]])),
    ...section(10, vec([[...leb(body.length), ...body]])),
]);
const file = path.join(workdir, 'split-selftest.wasm');
fs.writeFileSync(file, module_);

(async () => {
    let refused = false;
    try { await WebAssembly.compile(fs.readFileSync(file)); }
    catch (e) { refused = /maximum function size/.test(e.message); if (!refused) throw e; }
    if (!refused) { console.error('FAIL: V8 compiled the oversized function — the premise is gone; re-measure kV8MaxWasmFunctionSize'); process.exit(1); }
    console.log(`pre-split: V8 refuses the ${body.length}-byte function, as the splitter presumes`);

    const run = spawnSync(process.execPath, [splitter, file], { encoding: 'utf8' });
    if (run.status !== 0) { console.error(`FAIL: splitter exited ${run.status}\n${run.stderr}`); process.exit(1); }
    if (!/split __wasm_apply_data_relocs/.test(run.stdout)) { console.error(`FAIL: splitter did not report a split:\n${run.stdout}`); process.exit(1); }
    process.stdout.write(run.stdout);

    const split = fs.readFileSync(file);
    const mod = await WebAssembly.compile(split);
    const base = 64; // nonzero, to prove __memory_base actually reaches the stores
    const inst = await WebAssembly.instantiate(mod, { env: { __memory_base: base } });
    inst.exports[FN]();
    const mem = new DataView(inst.exports.memory.buffer);
    for (let i = 0; i < N; i += 977) {
        const got = mem.getInt32(base + 4 * i, true);
        if (got !== value(i)) { console.error(`FAIL: store ${i}: expected ${value(i)}, memory holds ${got}`); process.exit(1); }
    }
    console.log(`post-split: compiled, ran, and all sampled stores landed (base=${base})`);

    const again = spawnSync(process.execPath, [splitter, file], { encoding: 'utf8' });
    if (again.status !== 0 || !fs.readFileSync(file).equals(split)) {
        console.error('FAIL: a second splitter run was not a byte-identical no-op'); process.exit(1);
    }
    console.log('re-run: no-op, byte-identical');
})().catch(e => { console.error(`FAIL: ${e.stack}`); process.exit(1); });
