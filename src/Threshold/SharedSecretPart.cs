using System;

namespace Threshold
{
    public readonly struct SharedSecretPart
    {
        public SharedSecretPart(int x, ReadOnlyMemory<byte> y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public ReadOnlyMemory<byte> Y { get; }
    }
}
