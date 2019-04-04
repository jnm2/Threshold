using System.Runtime.InteropServices;
using NUnit.Framework;
using Shouldly;

namespace Threshold.Tests
{
    public static class SecretSharingAlgorithmTests
    {
        [Test, Repeat(10_000)]
        public static void Can_roundtrip_random_messages()
        {
            var rng = TestContext.CurrentContext.Random;

            var secret = new byte[rng.Next(0, 100)];
            rng.NextBytes(secret);

            var totalParts = rng.Next(1, 101);
            var requiredParts = rng.Next(1, totalParts + 1);

            var secretSharingAlgorithm = new SecretSharingAlgorithm(NUnitRandomNumberGenerator.Instance);

            var shares = secretSharingAlgorithm.GenerateShares(secret, totalParts, requiredParts).ToBuilder();

            while (shares.Count > requiredParts)
                shares.RemoveAt(rng.Next(shares.Count));

            var reconstructed = secretSharingAlgorithm.Reconstruct(shares.ToImmutable());

            MemoryMarshal.ToEnumerable(reconstructed).ShouldBe(secret);
        }
    }
}
