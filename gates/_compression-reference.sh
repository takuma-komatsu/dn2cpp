#!/usr/bin/env bash
# CompressionCore contains a malformed Brotli stream that requires a patched oracle.
compression_require_brotli_reference() {
    local corelib runtime_dir version
    corelib=$(resolve_net10_corelib) || return 1
    runtime_dir=${corelib%/*}
    version=${runtime_dir##*/}
    if [[ ! "$version" =~ ^10\.0\.([0-9]+)$ ]] \
            || (( 10#${BASH_REMATCH[1]} < 14 )); then
        echo "FAIL: Brotli security regression requires .NET 10.0.14 or a newer stable 10.0 patch; found $version." >&2
        echo "      Install a patched runtime and select its dotnet before running this gate." >&2
        return 1
    fi
    return 0
}

compression_assert_brotli_security() {
    local out="$1" side
    cat > "$out/brotli-security.expected" <<'EOF'
Brotli invalid block switch oneshot rejected: True
Brotli invalid block switch chunked rejected: True
Brotli invalid block switch buffered rejected: True
brotli-invalid-block-switch-stream: threw=True type=InvalidOperationException
EOF
    for side in native oracle; do
        gate_output suffix "$out/$side.stdout" "$out/brotli-security.expected" --policy crlf || return 1
    done
}
