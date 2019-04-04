using System.Security.Cryptography;
using NUnit.Framework;

namespace Threshold.Tests
{
    internal sealed class NUnitRandomNumberGenerator : RandomNumberGenerator
    {
        public static NUnitRandomNumberGenerator Instance { get; } = new NUnitRandomNumberGenerator();

        private NUnitRandomNumberGenerator()
        {
        }

        public override void GetBytes(byte[] data)
        {
            TestContext.CurrentContext.Random.NextBytes(data);
        }
    }
}
