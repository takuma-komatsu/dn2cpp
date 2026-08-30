// dn2cpp_env_node.js — Emscripten --pre-js of the console executable.
//
// The wasm environment block is synthetic: a browser page has no process
// environment, which is why the runtime carries DN2CPP_CPU_FEATURES_DEFAULT
// for browsers. Under node the process does have one, and the runtime's
// knobs (DN2CPP_CPU_FEATURES, DN2CPP_CPU_FEATURES_DIAG, ...) must reach
// getenv for the masked configuration of a wasm binary to be testable at
// all. Only the DN2CPP_ prefix is forwarded: the block stays deterministic
// apart from the knobs a gate sets on purpose.
//
// ENV is Emscripten's user-environment object; getEnvStrings merges it into
// the block lazily, on the first environ_* call from the wasm side, which is
// after preRun. A pre-js runs before the library variables are declared, so
// the copy is deferred to preRun rather than done at load.
Module['preRun'] = Module['preRun'] || [];
Module['preRun'].push(function () {
    if (typeof process === 'undefined' || !process.env || typeof ENV === 'undefined') {
        return;
    }
    var env = process.env;
    for (var key in env) {
        if (Object.prototype.hasOwnProperty.call(env, key) && key.indexOf('DN2CPP_') === 0) {
            ENV[key] = env[key];
        }
    }
});
