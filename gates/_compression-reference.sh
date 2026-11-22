#!/usr/bin/env bash
# CompressionCore contains a malformed Brotli stream that only a patched oracle
# rejects; every stable runtime of the supported major carries the patch.
compression_require_brotli_reference() {
    local corelib runtime_dir version
    corelib=$(resolve_pinned_corelib) || return 1
    runtime_dir=${corelib%/*}
    version=${runtime_dir##*/}
    if [[ ! "$version" =~ ^${DN2CPP_BCL_MAJOR}\.0\.[0-9]+$ ]]; then
        echo "FAIL: Brotli security regression requires a stable .NET $DN2CPP_BCL_MAJOR.0 runtime; found $version." >&2
        echo "      Install one and select its dotnet before running this gate." >&2
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
