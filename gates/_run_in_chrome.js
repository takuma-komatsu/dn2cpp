#!/usr/bin/env node
// Load a page in a real Chrome and stream its console to stdout.
//
// The caller asserts on the output; this only reports. Console messages are
// printed verbatim so a marker the page prints is a marker the caller can grep.
// Node >= 22 is assumed (global fetch and WebSocket, no npm dependencies).
//
//   run-in-chrome.js <url> [timeout-ms] [done-marker]
//   run-in-chrome.js --probe
//
// Exits 0 when the page settled (marker seen, or the timeout elapsed), 77 when
// no browser could be found, 1 on a protocol failure.
//
// `--probe` resolves a browser and prints its path, launching nothing; it is how
// a caller answers "would this run find a browser?" WITHOUT keeping a second copy
// of the candidate list. A gate needs that answer before it runs, because whether
// a browser exists is a cache-key term -- one arriving later must move the key and
// force the run section live -- and a shell-side mirror of the list below is
// exactly what went stale: it had no Windows arm long after this file grew one, so
// the key recorded `chrome=no` on a box where the run would have launched one, and
// the gate replayed a cached partial forever.
//
// Why the browser is NOT headless, and why it rasterizes through SwiftShader
// ------------------------------------------------------------------------
// The caller is the Web export gate, and the thing it has to exercise is the
// display server a SHIPPED game gets. Godot's driver fallback deliberately never
// selects DisplayServerHeadless, so a game in a browser always gets a real
// DisplayServerWeb -- which needs a WebGL2 context to come up at all. A headless
// Chrome (and `--disable-gpu`, which is what "headless" used to imply here) hands
// the page NO WebGL2 context whatsoever, so the gate could only run the engine by
// passing Godot's own `--headless` -- i.e. by testing a display server no shipped
// game ever instantiates, and skipping past the one the shipped path has the UB in.
//
// So: a real browser window, with ANGLE pointed at SwiftShader for a software GL
// stack that needs no GPU. `--enable-unsafe-swiftshader` is what lets a page take
// the SwiftShader fallback for WebGL (Chrome refuses it silently otherwise).
'use strict';

const { spawn } = require('node:child_process');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');

const probeOnly = process.argv[2] === '--probe';
const url = probeOnly ? '' : process.argv[2];
const timeoutMs = Number.parseInt(process.argv[3] || '60000', 10);
const doneMarker = process.argv[4] || '';
if (!probeOnly && !url) {
    console.error('usage: run-in-chrome.js <url> [timeout-ms] [done-marker]');
    console.error('       run-in-chrome.js --probe');
    process.exit(2);
}

// A FULL browser only. Playwright's `chromium_headless_shell` is deliberately not
// in this list even though it is the one most likely to be installed: it is a
// headless-ONLY binary, so it cannot honour the non-headless run above, and it
// hands the page no WebGL2 context. Preferring it -- which this list used to --
// is what silently confined the gate to the display server no shipped game gets.
//
// Windows roots come from the environment rather than from hardcoded C:\ paths:
// a relocated or localized Program Files is ordinary, and `path.join` on an
// undefined root throws -- so each is dropped when its variable is unset, which
// is also what keeps this list inert off Windows.
const windowsCandidates = () => {
    if (process.platform !== 'win32') return [];
    const local = process.env.LOCALAPPDATA;
    const installed = [process.env.ProgramFiles, process.env['ProgramFiles(x86)'], local]
        .filter(Boolean)
        .map((root) => path.join(root, 'Google', 'Chrome', 'Application', 'chrome.exe'));
    // Forward slashes on purpose: this is a glob PATTERN, where a backslash is
    // an escape rather than a separator (path.join would emit backslashes).
    const cached = local && fs.globSync
        ? fs.globSync(`${local.replace(/\\/g, '/')}/ms-playwright/chromium-*/chrome-win/chrome.exe`)
        : [];
    return [...installed, ...cached];
};

const candidates = [
    process.env.DN2CPP_CHROME,
    '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome',
    '/Applications/Chromium.app/Contents/MacOS/Chromium',
    ...(fs.globSync
        ? fs.globSync(path.join(os.homedir(), 'Library/Caches/ms-playwright/chromium-*/chrome-mac/Chromium.app/Contents/MacOS/Chromium'))
        : []),
    '/usr/bin/google-chrome',
    '/usr/bin/chromium',
    ...windowsCandidates(),
].filter(Boolean);

const browser = candidates.find((b) => {
    try {
        fs.accessSync(b, fs.constants.X_OK);
        return true;
    } catch {
        return false;
    }
});
if (!browser) {
    console.error('no full chrome/chromium found; set DN2CPP_CHROME (a headless_shell will not do — see the header)');
    process.exit(77);
}
if (probeOnly) {
    console.log(browser);
    process.exit(0);
}

const userDir = fs.mkdtempSync(path.join(os.tmpdir(), 'dn2cpp-chrome-'));
const proc = spawn(browser, [
    // No --headless and no --disable-gpu, on purpose: either one denies the page a
    // WebGL2 context, and without one DisplayServerWeb -- the display server the
    // shipped game gets -- cannot come up. See the header.
    '--enable-unsafe-swiftshader',
    '--use-gl=angle',
    '--use-angle=swiftshader',
    // No user ever clicks in this browser, so under the default autoplay policy
    // every AudioContext a page creates is born "suspended" and stays that way.
    // The CRI Web gate needs the page's own Web Audio context actually running
    // (its audio path is real, not stubbed); for the callers that stub audio out
    // the flag is inert.
    '--autoplay-policy=no-user-gesture-required',
    '--no-first-run',
    '--no-default-browser-check',
    '--disable-dev-shm-usage',
    '--remote-debugging-port=0',
    `--user-data-dir=${userDir}`,
    'about:blank',
], { stdio: ['ignore', 'ignore', 'pipe'] });

let stderrTail = '';
proc.stderr.on('data', (d) => { stderrTail = (stderrTail + d).slice(-4000); });

const cleanup = () => {
    try { proc.kill('SIGKILL'); } catch { /* already gone */ }
    try { fs.rmSync(userDir, { recursive: true, force: true }); } catch { /* best effort */ }
};
process.on('exit', cleanup);

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

async function devtoolsPort() {
    const portFile = path.join(userDir, 'DevToolsActivePort');
    for (let i = 0; i < 200; i++) {
        try {
            const [port] = fs.readFileSync(portFile, 'utf8').split('\n');
            if (port) {
                return port.trim();
            }
        } catch { /* not written yet */ }
        await sleep(50);
    }
    throw new Error(`browser never opened a devtools port\n${stderrTail}`);
}

function describe(arg) {
    if (arg === undefined || arg === null) {
        return String(arg);
    }
    if ('value' in arg) {
        return typeof arg.value === 'string' ? arg.value : JSON.stringify(arg.value);
    }
    return arg.description ?? arg.unserializableValue ?? arg.type ?? '?';
}

(async () => {
    const port = await devtoolsPort();
    const version = await (await fetch(`http://127.0.0.1:${port}/json/version`)).json();
    const ws = new WebSocket(version.webSocketDebuggerUrl);

    let nextId = 1;
    const pending = new Map();
    const send = (method, params = {}, sessionId) => new Promise((resolve, reject) => {
        const id = nextId++;
        pending.set(id, { resolve, reject });
        ws.send(JSON.stringify(sessionId ? { id, method, params, sessionId } : { id, method, params }));
    });

    let settled = false;
    const done = (reason) => {
        if (settled) {
            return;
        }
        settled = true;
        console.log(`__RUNNER_DONE ${reason}`);
        cleanup();
        process.exit(0);
    };

    ws.addEventListener('message', (ev) => {
        const msg = JSON.parse(ev.data);
        if (msg.id !== undefined) {
            const p = pending.get(msg.id);
            pending.delete(msg.id);
            if (p) {
                msg.error ? p.reject(new Error(JSON.stringify(msg.error))) : p.resolve(msg.result);
            }
            return;
        }
        let line = null;
        if (msg.method === 'Runtime.consoleAPICalled') {
            line = msg.params.args.map(describe).join(' ');
        } else if (msg.method === 'Runtime.exceptionThrown') {
            const d = msg.params.exceptionDetails;
            line = `__PAGE_EXCEPTION ${d.exception?.description ?? d.text}`;
        } else if (msg.method === 'Log.entryAdded') {
            line = `__BROWSER_LOG [${msg.params.entry.level}] ${msg.params.entry.text}`;
        }
        if (line !== null) {
            console.log(line);
            if (doneMarker && line.includes(doneMarker)) {
                done(`marker: ${doneMarker}`);
            }
        }
    });

    await new Promise((resolve, reject) => {
        ws.addEventListener('open', resolve, { once: true });
        ws.addEventListener('error', () => reject(new Error('devtools websocket failed')), { once: true });
    });

    const { targetId } = await send('Target.createTarget', { url: 'about:blank' });
    const { sessionId } = await send('Target.attachToTarget', { targetId, flatten: true });
    await send('Runtime.enable', {}, sessionId);
    await send('Log.enable', {}, sessionId);
    await send('Page.enable', {}, sessionId);
    await send('Page.navigate', { url }, sessionId);

    setTimeout(() => done(`timeout after ${timeoutMs}ms`), timeoutMs);
})().catch((e) => {
    console.error(`runner failed: ${e.message}`);
    cleanup();
    process.exit(1);
});
