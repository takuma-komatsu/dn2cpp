# Vendored Google Highway (header-only subset)

- Upstream: https://github.com/google/highway
- Pinned version: **1.4.0** (git tag `1.4.0`)
- License: Apache-2.0 / BSD-3 (dual). See `LICENSE`.

This is a **header-only** vendoring for `HWY_STATIC_DISPATCH` (single static ISA).
Only headers are included — no `.cc`, no tests/examples/benchmarks. Subset:

- `hwy/*.h`            — top-level (highway.h transitive closure)
- `hwy/ops/*.h`        — per-ISA op implementations
- `hwy/contrib/math/`  — transcendentals (Cos/Exp/… for later phases)

Used only when the runtime is built with `-DDN2CPP_USE_HIGHWAY=ON`
(`runtime/CMakeLists.txt`, enabled by default). Scalar builds opt out with
`-DDN2CPP_USE_HIGHWAY=OFF`.

To re-vendor: `git clone --depth 1 --branch <tag> https://github.com/google/highway`
then copy the header subsets above + `LICENSE` and reapply the backport below.

## Local backport

The floating-point `operator/` in `hwy/ops/emu128-inl.h` is taken from upstream
commit [`e472c0f2aaa55cf5e1116d126732d7883c760c37`](https://github.com/google/highway/commit/e472c0f2aaa55cf5e1116d126732d7883c760c37).
It preserves NaN and signed infinity when the divisor is zero, without executing
a division that floating-point sanitizers reject. The scalar target in the pinned
release already uses ordinary floating-point division and needs no backport.

To reproduce the backport after copying the pinned headers, fetch that commit in
the upstream checkout:

```sh
git fetch --depth 1 origin e472c0f2aaa55cf5e1116d126732d7883c760c37
git show e472c0f2aaa55cf5e1116d126732d7883c760c37:hwy/ops/emu128-inl.h
```

Replace only the floating-point `Vec128<T, N> operator/` with the function from
that revision, including its zero-divisor handling. Keep the upstream license
header. An update containing that function supersedes this backport; verify it
with `gates/build-and-run-vector-probe.sh` and the floating-point Web axes in
`gates/build-and-run-wasm-console.sh` before removing this note.
