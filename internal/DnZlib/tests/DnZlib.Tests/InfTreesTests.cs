using DnZlib.Inflate;
using DnZlib.Internal;

namespace DnZlib.Tests;

public class InfTreesTests
{
    [Fact]
    public unsafe void FixedTables_MatchReference()
    {
        InfTrees.GetFixedTables(out Code* lenCode, out uint lenBits, out Code* distCode, out uint distBits);
        Assert.Equal(9u, lenBits);
        Assert.Equal(5u, distBits);

        // distfix in inffixed.h is a faithful dump (no makefixed printer quirk), so compare fully.
        (int Op, int Bits, int Val)[] distfix =
        [
            (16, 5, 1), (23, 5, 257), (19, 5, 17), (27, 5, 4097), (17, 5, 5), (25, 5, 1025),
            (21, 5, 65), (29, 5, 16385), (16, 5, 3), (24, 5, 513), (20, 5, 33), (28, 5, 8193),
            (18, 5, 9), (26, 5, 2049), (22, 5, 129), (64, 5, 0), (16, 5, 2), (23, 5, 385),
            (19, 5, 25), (27, 5, 6145), (17, 5, 7), (25, 5, 1537), (21, 5, 97), (29, 5, 24577),
            (16, 5, 4), (24, 5, 769), (20, 5, 49), (28, 5, 12289), (18, 5, 13), (26, 5, 3073),
            (22, 5, 193), (64, 5, 0),
        ];
        for (int i = 0; i < distfix.Length; i++)
        {
            Assert.Equal(distfix[i].Op, distCode[i].Op);
            Assert.Equal(distfix[i].Bits, distCode[i].Bits);
            Assert.Equal(distfix[i].Val, distCode[i].Val);
        }

        // Spot-check the leading lit/len root entries (indices free of the printer quirk).
        (int Op, int Bits, int Val)[] lenHead =
        [
            (96, 7, 0), (0, 8, 80), (0, 8, 16), (20, 8, 115), (18, 7, 31), (0, 8, 112),
            (0, 8, 48), (0, 9, 192), (16, 7, 10),
        ];
        for (int i = 0; i < lenHead.Length; i++)
        {
            Assert.Equal(lenHead[i].Op, lenCode[i].Op);
            Assert.Equal(lenHead[i].Bits, lenCode[i].Bits);
            Assert.Equal(lenHead[i].Val, lenCode[i].Val);
        }
    }
}
