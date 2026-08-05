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
(`runtime/CMakeLists.txt`). The default scalar build does not reference it.

To re-vendor: `git clone --depth 1 --branch <tag> https://github.com/google/highway`
then copy the three subsets above + `LICENSE`.
