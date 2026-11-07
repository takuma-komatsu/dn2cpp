#include "dn2cpp_core.h"
#include "dn2cpp_vectors.h"

static_assert(HWY_NATIVE_FMA == DN2CPP_VECTOR_EXPECT_FMA,
    "The fixture must execute the requested FMA implementation.");

template <class T, int N>
static bool check_fma() {
    Dn2CppVec<N> a{}, b{}, c{}, expected{};
    volatile T delta_source = std::is_same<T, float>::value ? T(0.0001220703125) : T(0.000000000931322574615478515625);
    const T delta = delta_source;
    const T largest = std::numeric_limits<T>::max();
    const T epsilon = std::numeric_limits<T>::denorm_min();
    constexpr int lanes = N / sizeof(T);
    for (int start = 0; start < 4; ++start) {
        for (int lane = 0; lane < lanes; ++lane) {
            T av, bv, cv, result;
            switch ((start + lane) % 4) {
                case 0: av = T(1) + delta; bv = T(1) - delta; cv = T(-1); result = -delta * delta; break;
                case 1: av = largest; bv = T(2); cv = -largest; result = largest; break;
                case 2: av = epsilon; bv = T(0.5); cv = epsilon; result = epsilon * T(2); break;
                default: av = -T(0); bv = T(1); cv = -T(0); result = -T(0); break;
            }
            dn2cpp_vec_set<T>(a, lane, av);
            dn2cpp_vec_set<T>(b, lane, bv);
            dn2cpp_vec_set<T>(c, lane, cv);
            dn2cpp_vec_set<T>(expected, lane, result);
        }
        const auto actual = dn2cpp_vec_fma<T, N>(a, b, c);
        if (std::memcmp(actual.b, expected.b, N) != 0) {
            std::fprintf(stderr, "FMA mismatch: element=%zu bytes, vector=%d bytes, rotation=%d\n", sizeof(T), N, start);
            return false;
        }
    }
    return true;
}

int main() {
    if (!check_fma<float, 8>() || !check_fma<double, 8>()
        || !check_fma<float, 16>() || !check_fma<double, 16>()
        || !check_fma<float, 32>() || !check_fma<double, 32>()
        || !check_fma<float, 64>() || !check_fma<double, 64>())
        return 1;
    std::puts(DN2CPP_VECTOR_EXPECT_FMA ? "FMA native branch: exact" : "FMA software branch: exact");
    return 0;
}
