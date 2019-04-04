using NUnit.Framework;
using Shouldly;

namespace Threshold.Tests
{
    public static class MathUtilsTests
    {
        [Test]
        public static void Check_multiplicative_inverse()
        {
            const uint modulus = 0b01111111_11111111_11111111_11111111;

            for (var i = 0; i < 1_000_000; i++)
            {
                var value = (uint)TestContext.CurrentContext.Random.Next(1, (int)modulus);

                var result = MathUtils.MultiplicativeInverse(value, modulus);

                ((ulong)result * value % modulus).ShouldBe(1UL);
            }
        }
    }
}
